using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;
using Match = System.Text.RegularExpressions.Match;
using Regex = System.Text.RegularExpressions.Regex;

namespace Bloodcraft.SystemUtilities.Quests;
internal static class QuestUtilities
{
    static readonly Regex Regex = new(@"T\d{2}");
    static EntityManager EntityManager => Core.EntityManager;
    static Random Random => new();
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static readonly Random randomReward = new();
    static readonly bool InfiniteDailies = Plugin.InfiniteDailies.Value;
    public static HashSet<PrefabGUID> UnitPrefabs = [];
    public static HashSet<PrefabGUID> CraftPrefabs = [];
    public static HashSet<PrefabGUID> GatherPrefabs = [];
    static readonly PrefabGUID graveyardSkeleton = new(1395549638);
    static readonly PrefabGUID forestWolf = new(-1418430647);
    static readonly PrefabGUID reinforcedBoneSword = new(-796306296);
    static readonly PrefabGUID reinforcedBoneMace = new(-1998017941);
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
        HashSet<PrefabGUID> prefabs = [];
        foreach (PrefabGUID prefab in UnitPrefabs)
        {
            Entity prefabEntity;
            if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(prefab))
            {
                prefabEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
            }
            else continue;
            //if (prefabEntity == Entity.Null || !EntityManager.Exists(prefabEntity)) continue;
            //Core.Log.LogInfo($"Checking {prefabEntity.Read<PrefabGUID>().GuidHash}...");
            PrefabGUID prefabGUID = prefabEntity.Read<PrefabGUID>();
            if (prefabs.Contains(prefabGUID) || prefabGUID.LookupName().Contains("Trader") || prefabGUID.LookupName().Contains("Vermin") || prefabGUID.LookupName().Contains("Servant") || prefabGUID.LookupName().Contains("Horse") || prefabGUID.LookupName().Contains("Carriage")) continue;
            if (!prefabEntity.Has<UnitLevel>() || !prefabEntity.Has<EntityCategory>() || prefabEntity.Has<Minion>()) continue;
            UnitLevel level = prefabEntity.Read<UnitLevel>();
            if (Math.Abs(level.Level._Value - playerLevel) <= 10)
            {
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
            Entity prefabEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
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
            else if (itemData.ItemType == ItemType.Consumable)
            {
                if (IsWithinLevelRange(tierCheck, playerLevel, ConsumableTierLevelRangeMap))
                {
                    prefabs.Add(prefabGUID);
                }
            }
        }
        return prefabs;
    }
    static HashSet<PrefabGUID> GetGatherPrefabsForLevel(int playerLevel)
    {
        HashSet<PrefabGUID> prefabs = [];
        foreach (PrefabGUID prefab in GatherPrefabs)
        {
            Entity prefabEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
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
                        Entity dropTable = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
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

        Core.DataStructures.PlayerQuests.TryAdd(steamId, new Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)>
            {
                { QuestType.Daily, (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, DateTime.UtcNow) },
                { QuestType.Weekly, (GenerateQuestObjective(goal, targets, level, QuestType.Weekly), 0, DateTime.UtcNow) }
            });

        Core.DataStructures.SavePlayerQuests();
    }
    public static void RefreshQuests(User user, ulong steamId, int level)
    {
        if (Core.DataStructures.PlayerQuests.TryGetValue(steamId, out var playerQuestData))
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

                Core.DataStructures.SavePlayerQuests();
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

        if (Core.DataStructures.PlayerQuests.TryGetValue(steamId, out var playerQuestData))
        {
            playerQuestData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, DateTime.UtcNow);
            playerQuestData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, level, QuestType.Weekly), 0, DateTime.UtcNow);

            Core.DataStructures.SavePlayerQuests();
        }
        else
        {
            InitializePlayerQuests(steamId, level);
        }
    }
    public static void UpdateQuestProgress(Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, PrefabGUID target, int amount, User user)
    {
        for (int i = 0; i < questData.Count; i++)
        {
            var quest = questData.ElementAt(i);
            if (quest.Value.Objective.Target == target)
            {
                string colorType = quest.Key == QuestType.Daily ? $"<color=#00FFFF>{QuestType.Daily} Quest</color>" : $"<color=#BF40BF>{QuestType.Weekly} Quest</color>";
                questData[quest.Key] = new(quest.Value.Objective, quest.Value.Progress + amount, quest.Value.LastReset);
                if (Core.DataStructures.PlayerBools.TryGetValue(user.PlatformId, out var bools) && bools["QuestLogging"] && !quest.Value.Objective.Complete)
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
                        PrefabGUID reward = QuestRewards.Keys.ElementAt(randomReward.Next(QuestRewards.Count));
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
                            InventoryUtilitiesServer.CreateDropItem(Core.EntityManager, user.LocalCharacter._Entity, reward, quantity, new Entity());
                            string message = $"You've received <color=#ffd9eb>{reward.GetPrefabName()}</color>x<color=white>{quantity}</color> for completing your {colorType}! It dropped on the ground because your inventory was full.";
                            LocalizationService.HandleServerReply(EntityManager, user, message);
                        }
                        if (Plugin.LevelingSystem.Value)
                        {
                            PlayerLevelingUtilities.ProcessQuestExperienceGain(user.PlatformId, QuestMultipliers[quest.Key]);
                            string xpMessage = $"Additionally, you've been awarded <color=yellow>{(0.025f * QuestMultipliers[quest.Key] * 100).ToString("F0") + "%"}</color> of your total <color=#FFC0CB>experience</color>.";
                            LocalizationService.HandleServerReply(EntityManager, user, xpMessage);
                        }
                    }
                    else
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, $"Couldn't find any valid reward prefabs...");
                    }
                    if (quest.Key == QuestType.Daily && InfiniteDailies)
                    {
                        int level = Plugin.LevelingSystem.Value ? Core.DataStructures.PlayerExperience[user.PlatformId].Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                        QuestGoal goal = QuestGoal.Kill;
                        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);
                        questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, level, QuestType.Daily), 0, DateTime.UtcNow);
                        Core.DataStructures.SavePlayerQuests();
                        var dailyQuest = questData[QuestType.Daily];
                        LocalizationService.HandleServerReply(Core.EntityManager, user, $"New <color=#00FFFF>Daily Quest</color> available: <color=green>{dailyQuest.Objective.Goal}</color> <color=white>{dailyQuest.Objective.Target.GetPrefabName()}</color>x<color=#FFC0CB>{dailyQuest.Objective.RequiredAmount}</color> [<color=white>{dailyQuest.Progress}</color>/<color=yellow>{dailyQuest.Objective.RequiredAmount}</color>]");
                    }
                }
            }
        }

        Core.DataStructures.SavePlayerQuests();
    }
}
