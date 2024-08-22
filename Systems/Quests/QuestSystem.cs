using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Match = System.Text.RegularExpressions.Match;
using Regex = System.Text.RegularExpressions.Regex;
using static Bloodcraft.Core.DataStructures;
using Random = System.Random;

namespace Bloodcraft.Systems.Quests;
internal static class QuestSystem
{
    static readonly Regex Regex = new(@"T\d{2}");
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly Random Random = new();

    //public static HashSet<PrefabGUID> UnitPrefabs = [];
    public static HashSet<PrefabGUID> CraftPrefabs = [];
    public static HashSet<PrefabGUID> GatherPrefabs = [];

    static readonly PrefabGUID graveyardSkeleton = new(1395549638);
    static readonly PrefabGUID forestWolf = new(-1418430647);
    static readonly PrefabGUID reinforcedBoneSword = new(-796306296);
    static readonly PrefabGUID reinforcedBoneMace = new(-1998017941);
    static readonly PrefabGUID trackingBuff = new(746504391);
    static readonly PrefabGUID villageElder = new(-1505705712);
    public enum QuestGoal
    {
        Kill,
        Craft,
        Gather
    }
    public enum QuestType
    {
        Daily,
        Weekly
    }
    static readonly Dictionary<QuestType, int> QuestMultipliers = new()
    {
        { QuestType.Daily, 1 },
        { QuestType.Weekly, 5 }
    };

    public static Dictionary<PrefabGUID, int> QuestRewards = [];
    public class QuestObjective
    {
        public QuestGoal Goal { get; set; }
        public PrefabGUID Target { get; set; }
        public int RequiredAmount { get; set; }
        public bool Complete { get; set;}
    }

    static readonly Dictionary<string, (int MinLevel, int MaxLevel)> EquipmentTierLevelRangeMap = new()
    {
        { "T01", (0, 10) },
        { "T02", (11, 20) },
        { "T03", (21, 30) },
        { "T04", (31, 40) },
        { "T05", (41, 50) },
        { "T06", (51, 60) },
        { "T07", (61, 70) },
        { "T08", (71, 80) },
        { "T09", (81, 90) }
    };

    static readonly Dictionary<string, (int MinLevel, int MaxLevel)> ConsumableTierLevelRangeMap = new()
    {
        { "T01", (0, 45) },
        { "T02", (46, 90) }
    };
    static HashSet<PrefabGUID> GetKillPrefabsForLevel(int playerLevel)
    {
        Dictionary<PrefabGUID, HashSet<Entity>> TargetPrefabs = new(QuestService.TargetCache);
        HashSet<PrefabGUID> prefabs = [];
        foreach (PrefabGUID prefab in TargetPrefabs.Keys)
        {
            if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefab, out Entity prefabEntity)) continue;

            PrefabGUID prefabGUID = prefabEntity.Read<PrefabGUID>();

            string check = prefabGUID.LookupName();

            if (check.Contains("Trader") || check.Contains("Vermin") || check.Contains("Servant") || check.Contains("Horse") || check.Contains("Carriage") || check.Contains("Minion") || check.Contains("Unholy") || check.Contains("Surprise")) continue; // need to check if behaviour prefab matches with name for some units for valid spawns?
            if (!prefabEntity.Has<UnitLevel>() || prefabEntity.Has<Minion>()) continue;     

            UnitLevel level = prefabEntity.Read<UnitLevel>();
            if (Math.Abs(level.Level._Value - playerLevel) <= 10)
            {
                if (prefabEntity.Has<VBloodUnit>() && level.Level._Value > playerLevel) continue;
                if (FamiliarPatches.shardBearers.Contains(prefabGUID) || prefabGUID.Equals(villageElder)) continue;
                prefabs.Add(prefabGUID);
            }
        }
        return prefabs;
    }
    static HashSet<PrefabGUID> GetCraftPrefabsForLevel(int playerLevel)
    {
        HashSet<PrefabGUID> prefabs = [];
        foreach (PrefabGUID prefab in CraftPrefabs)
        {
            Entity prefabEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
            //Core.Log.LogInfo($"Checking {prefabEntity.Read<PrefabGUID>().GuidHash}...");
            PrefabGUID prefabGUID = prefabEntity.Read<PrefabGUID>();
            if (prefabs.Contains(prefabGUID)) continue;
            if (!prefabEntity.Has<ItemData>()) continue;
            ItemData itemData = prefabEntity.Read<ItemData>();
            if (itemData.ItemType != ItemType.Equippable && itemData.ItemType != ItemType.Consumable) continue;
            string tierCheck = prefabEntity.Read<PrefabGUID>().LookupName();
            Match match = Regex.Match(tierCheck);

            if (match.Success)
            {
                tierCheck = match.Value;
            }
            else
            {
                continue;
            }
            
            // Check if the item is within the player's level range based on T01, etc
            if (itemData.ItemType == ItemType.Equippable)
            {
                if (IsWithinLevelRange(tierCheck, playerLevel, EquipmentTierLevelRangeMap))
                {
                    string nameCheck = prefabGUID.LookupName();
                    if (nameCheck.Contains("T01_Bone") || nameCheck.Contains("Item_Cloak") || nameCheck.Contains("BloodKey_T01")) continue;
                    prefabs.Add(prefabGUID);
                }
            }
            /*
            else if (itemData.ItemType == ItemType.Consumable)
            {
                if (IsWithinLevelRange(tierCheck, playerLevel, ConsumableTierLevelRangeMap))
                {
                    prefabs.Add(prefabGUID);
                }
            }
            */
        }
        return prefabs;
    }
    static HashSet<PrefabGUID> GetGatherPrefabsForLevel(int playerLevel)
    {
        HashSet<PrefabGUID> prefabs = [];
        foreach (PrefabGUID prefab in GatherPrefabs)
        {
            Entity prefabEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
            //if (prefabEntity == Entity.Null || !EntityManager.Exists(prefabEntity)) continue;
            //Core.Log.LogInfo($"Checking {prefabEntity.Read<PrefabGUID>().GuidHash}...");
            if (!prefabEntity.Has<DropTableBuffer>() || !prefabEntity.Has<EntityCategory>()) continue;
            int resourceLevel = prefabEntity.Read<EntityCategory>().ResourceLevel._Value;
            if (resourceLevel <= playerLevel)
            {
                var buffer = prefabEntity.ReadBuffer<DropTableBuffer>();
                foreach (var drop in buffer)
                {
                    if (drop.DropTrigger == DropTriggerType.YieldResourceOnDamageTaken)
                    {
                        Entity dropTable = PrefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                        if (!dropTable.Has<DropTableDataBuffer>()) continue;
                        var dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                        foreach (var dropTableData in dropTableDataBuffer)
                        {
                            if (dropTableData.ItemGuid.LookupName().Contains("Item_Ingredient"))
                            {
                                prefabs.Add(dropTableData.ItemGuid);
                            }
                        }
                        break;
                    }
                }
            }
        }
        return prefabs;
    }
    static bool IsWithinLevelRange(string tier, int playerLevel, Dictionary<string, (int MinLevel, int MaxLevel)> tierMap)
    {
        if (tierMap.TryGetValue(tier, out var range))
        {
            return playerLevel >= range.MinLevel && playerLevel <= range.MaxLevel;
        }
        return false;
    }
    static QuestObjective GenerateQuestObjective(QuestGoal goal, HashSet<PrefabGUID> targets, int level, QuestType questType)
    {
        PrefabGUID target = new(0);
        int requiredAmount;

        switch (goal)
        {
            case QuestGoal.Kill:
                if (targets.Count != 0)
                {
                    target = targets.ElementAt(Random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = graveyardSkeleton;
                else if (questType.Equals(QuestType.Weekly)) target = forestWolf;
                requiredAmount = Random.Next(6, 8) * QuestMultipliers[questType];
                if ((target.LookupName().ToLower().Contains("vblood") || target.LookupName().ToLower().Contains("vhunter")) && !questType.Equals(QuestType.Weekly))
                {
                    requiredAmount = 2;
                }
                else if ((target.LookupName().ToLower().Contains("vblood") || target.LookupName().ToLower().Contains("vhunter")) && questType.Equals(QuestType.Weekly))
                {
                    requiredAmount = 10;
                }
                break;
            case QuestGoal.Craft:
                if (targets.Count != 0)
                {
                    target = targets.ElementAt(Random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = reinforcedBoneSword;
                else if (questType.Equals(QuestType.Weekly)) target = reinforcedBoneMace;
                requiredAmount = Random.Next(10, 15) * QuestMultipliers[questType];
                break;
            case QuestGoal.Gather:
                targets = GetGatherPrefabsForLevel(level);
                target = targets.ElementAt(Random.Next(targets.Count));
                List<int> amounts = [500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000];
                requiredAmount = amounts.ElementAt(Random.Next(amounts.Count)) * QuestMultipliers[questType];
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return new QuestObjective { Goal = goal, Target = target, RequiredAmount = requiredAmount };
    }
    static HashSet<PrefabGUID> GetGoalPrefabsForLevel(QuestGoal goal, int level)
    {
        HashSet<PrefabGUID> prefabs = goal switch
        {
            QuestGoal.Kill => GetKillPrefabsForLevel(level),
            QuestGoal.Craft => GetCraftPrefabsForLevel(level),
            QuestGoal.Gather => GetGatherPrefabsForLevel(level),
            _ => throw new ArgumentOutOfRangeException(),
        };

        return prefabs;
    }
    public static void InitializePlayerQuests(ulong steamId, int level)
    {
        QuestGoal goal = QuestGoal.Kill;
        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

        PlayerQuests.TryAdd(steamId, new Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)>
            {
                { QuestType.Daily, (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, DateTime.UtcNow) },
                { QuestType.Weekly, (GenerateQuestObjective(goal, targets, level, QuestType.Weekly), 0, DateTime.UtcNow) }
            });

        SavePlayerQuests();
    }
    public static void RefreshQuests(User user, ulong steamId, int level)
    {
        if (PlayerQuests.TryGetValue(steamId, out var playerQuestData))
        {
            DateTime lastDaily = playerQuestData[QuestType.Daily].LastReset;
            DateTime lastWeekly = playerQuestData[QuestType.Weekly].LastReset;

            DateTime nextDaily = lastDaily.AddDays(1);
            DateTime nextWeekly = lastWeekly.AddDays(7);

            DateTime now = DateTime.UtcNow;

            bool refreshDaily = now >= nextDaily;
            bool refreshWeekly = now >= nextWeekly;

            if (refreshDaily || refreshWeekly)
            {
                QuestGoal goal = QuestGoal.Kill;
                HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

                if (refreshDaily)
                {
                    playerQuestData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#00FFFF>Daily Quest</color> has been refreshed~");
                }

                if (refreshWeekly)
                {
                    playerQuestData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, level, QuestType.Weekly), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#BF40BF>Weekly Quest</color> has been refreshed~");
                }
                SavePlayerQuests();
            }
        }
        else
        {
            InitializePlayerQuests(steamId, level);
        }
    }
    public static void ForceRefresh(ulong steamId, int level)
    {
        QuestGoal goal = QuestGoal.Kill;
        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

        if (PlayerQuests.TryGetValue(steamId, out var playerQuestData))
        {
            playerQuestData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, DateTime.UtcNow);
            playerQuestData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, level, QuestType.Weekly), 0, DateTime.UtcNow);

            SavePlayerQuests();
        }
        else
        {
            InitializePlayerQuests(steamId, level);
        }
    }
    public static void ForceDaily(User user, ulong steamId, int level)
    {
        QuestGoal goal = QuestGoal.Kill;
        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

        if (PlayerQuests.TryGetValue(steamId, out var playerQuestData))
        {
            playerQuestData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, DateTime.UtcNow);
            SavePlayerQuests();
            LocalizationService.HandleServerReply(EntityManager, user, "<color=#00FFFF>Daily Quest</color> has been rerolled~");
        }
    }
    public static void ForceWeekly(User user, ulong steamId, int level)
    {
        QuestGoal goal = QuestGoal.Kill;
        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

        if (PlayerQuests.TryGetValue(steamId, out var playerQuestData))
        {
            playerQuestData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, level, QuestType.Weekly), 0, DateTime.UtcNow);
            SavePlayerQuests();
            LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#BF40BF>Weekly Quest</color> has been rerolled~");
        }
    }
    public static void UpdateQuests(Entity source, Entity userEntity, PrefabGUID target)
    {
        HashSet<Entity> participants = LevelingSystem.GetParticipants(source, userEntity); // want list of participants to process quest credit for, this is doing double right now?
        List<ulong> processed = [];
        foreach (Entity participant in participants)
        {
            User user = participant.Read<PlayerCharacter>().UserEntity.Read<User>();
            ulong steamId = user.PlatformId; // participants are character entities
            if (PlayerQuests.TryGetValue(steamId, out var questData) && !processed.Contains(steamId))
            {
                ProcessQuestProgress(questData, target, 1, user);
                processed.Add(steamId);
            }
        }
    }
    public static void ProcessQuestProgress(Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, PrefabGUID target, int amount, User user)
    {
        bool updated = false;
        for (int i = 0; i < questData.Count; i++)
        {
            var quest = questData.ElementAt(i);
            if (quest.Value.Objective.Target == target)
            {
                updated = true;
                string colorType = quest.Key == QuestType.Daily ? $"<color=#00FFFF>{QuestType.Daily} Quest</color>" : $"<color=#BF40BF>{QuestType.Weekly} Quest</color>";
                questData[quest.Key] = new(quest.Value.Objective, quest.Value.Progress + amount, quest.Value.LastReset);

                if (PlayerBools.TryGetValue(user.PlatformId, out var bools) && bools["QuestLogging"] && !quest.Value.Objective.Complete)
                {
                    string message = $"Progress added to {colorType}: <color=green>{quest.Value.Objective.Goal}</color> <color=white>{quest.Value.Objective.Target.GetPrefabName()}</color> [<color=white>{questData[quest.Key].Progress}</color>/<color=yellow>{quest.Value.Objective.RequiredAmount}</color>]";
                    LocalizationService.HandleServerReply(EntityManager, user, message);
                }

                if (quest.Value.Objective.RequiredAmount <= questData[quest.Key].Progress && !quest.Value.Objective.Complete)
                {
                    quest.Value.Objective.Complete = true;
                    LocalizationService.HandleServerReply(EntityManager, user, $"{colorType} complete!");
                    if (QuestRewards.Count > 0)
                    {
                        PrefabGUID reward = QuestRewards.Keys.ElementAt(Random.Next(QuestRewards.Count));
                        int quantity = QuestRewards[reward];

                        if (quest.Key == QuestType.Weekly) quantity *= QuestMultipliers[quest.Key];

                        if (quest.Value.Objective.Target.LookupName().ToLower().Contains("vblood")) quantity *= 3;

                        if (ServerGameManager.TryAddInventoryItem(user.LocalCharacter._Entity, reward, quantity))
                        {
                            string message = $"You've received <color=#ffd9eb>{reward.GetPrefabName()}</color>x<color=white>{quantity}</color> for completing your {colorType}!";
                            LocalizationService.HandleServerReply(EntityManager, user, message);
                        }
                        else
                        {
                            InventoryUtilitiesServer.CreateDropItem(EntityManager, user.LocalCharacter._Entity, reward, quantity, new Entity());
                            string message = $"You've received <color=#ffd9eb>{reward.GetPrefabName()}</color>x<color=white>{quantity}</color> for completing your {colorType}! It dropped on the ground because your inventory was full.";
                            LocalizationService.HandleServerReply(EntityManager, user, message);
                        }

                        if (ConfigService.LevelingSystem)
                        {
                            LevelingSystem.ProcessQuestExperienceGain(user, QuestMultipliers[quest.Key]);
                            string xpMessage = $"Additionally, you've been awarded <color=yellow>{(0.025f * QuestMultipliers[quest.Key] * 100).ToString("F0") + "%"}</color> of your total <color=#FFC0CB>experience</color>.";
                            LocalizationService.HandleServerReply(EntityManager, user, xpMessage);
                        }
                    }
                    else
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, $"Couldn't find any valid reward prefabs...");
                    }

                    if (quest.Key == QuestType.Daily && ConfigService.InfiniteDailies)
                    {
                        int level = ConfigService.LevelingSystem ? PlayerExperience[user.PlatformId].Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                        QuestGoal goal = QuestGoal.Kill;
                        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);
                        questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, DateTime.UtcNow);
                        var dailyQuest = questData[QuestType.Daily];
                        SavePlayerQuests();
                        LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>Daily Quest</color> available: <color=green>{dailyQuest.Objective.Goal}</color> <color=white>{dailyQuest.Objective.Target.GetPrefabName()}</color>x<color=#FFC0CB>{dailyQuest.Objective.RequiredAmount}</color> [<color=white>{dailyQuest.Progress}</color>/<color=yellow>{dailyQuest.Objective.RequiredAmount}</color>]");
                    }
                }
            }
        }
        if (updated) SavePlayerQuests();
    }
    public static string GetCardinalDirection(float3 direction)
    {
        float angle = math.degrees(math.atan2(direction.z, direction.x));
        if (angle < 0) angle += 360;

        if (angle >= 337.5 || angle < 22.5)
            return "East";
        else if (angle >= 22.5 && angle < 67.5)
            return "Northeast";
        else if (angle >= 67.5 && angle < 112.5)
            return "North";
        else if (angle >= 112.5 && angle < 157.5)
            return "Northwest";
        else if (angle >= 157.5 && angle < 202.5)
            return "West";
        else if (angle >= 202.5 && angle < 247.5)
            return "Southwest";
        else if (angle >= 247.5 && angle < 292.5)
            return "South";
        else
            return "Southeast";
    }
}
