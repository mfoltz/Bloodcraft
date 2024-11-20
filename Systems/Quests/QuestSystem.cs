using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using Match = System.Text.RegularExpressions.Match;
using Random = System.Random;
using Regex = System.Text.RegularExpressions.Regex;

namespace Bloodcraft.Systems.Quests;
internal static class QuestSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly float ResourceYieldModifier = SystemService.ServerGameSettingsSystem._Settings.MaterialYieldModifier_Global;

    static readonly Random Random = new();
    static readonly Regex Regex = new(@"T\d{2}");

    public static readonly HashSet<PrefabGUID> CraftPrefabs = [];
    public static readonly HashSet<PrefabGUID> ResourcePrefabs = [];

    static readonly PrefabGUID GraveyardSkeleton = new(1395549638);
    static readonly PrefabGUID ForestWolf = new(-1418430647);

    static readonly PrefabGUID ReinforcedBoneSword = new(-796306296);
    static readonly PrefabGUID ReinforcedBoneMace = new(-1998017941);

    static readonly PrefabGUID ItemIngredientWood = new(-1593377811);
    static readonly PrefabGUID ItemIngredientStone = new(-1531666018);

    const int DefaultMaxPlayerLevel = 90;

    static readonly HashSet<string> FilteredResources =
    [
        "Item_Ingredient_Crystal",
        "Coal",
        "Thistle"
    ];
    public enum QuestType
    {
        Daily,
        Weekly
    }
    public enum TargetType
    {
        Kill,
        Craft,
        Gather
    }

    static readonly List<TargetType> TargetTypes =
    [
        TargetType.Kill,
        TargetType.Craft,
        TargetType.Gather
    ];

    static readonly Dictionary<QuestType, int> QuestMultipliers = new()
    {
        { QuestType.Daily, 1 },
        { QuestType.Weekly, 5 }
    };

    public static readonly Dictionary<PrefabGUID, int> QuestRewards = [];
    public class QuestObjective
    {
        public TargetType Goal { get; set; }
        public PrefabGUID Target { get; set; }
        public int RequiredAmount { get; set; }
        public bool Complete { get; set; }
    }

    static readonly Dictionary<string, (int MinLevel, int MaxLevel)> EquipmentTierLevelRangeMap = new()
    {
        { "T01", (0, 15) },
        { "T02", (20, 30) },
        { "T03", (30, 45) },
        { "T04", (40, 60) },
        { "T05", (50, ConfigService.MaxLevel) },
        { "T06", (60, ConfigService.MaxLevel) },
        { "T07", (70, ConfigService.MaxLevel) }
        //{ "T08", (70, ConfigService.MaxLevel) },
        //{ "T09", (80, ConfigService.MaxLevel) }
    };

    static readonly Dictionary<string, (int MinLevel, int MaxLevel)> ConsumableTierLevelRangeMap = new()
    {
        { "Salve_Vermin", (0, 30) },
        { "PhysicalPowerPotion_T01", (15, ConfigService.MaxLevel) },
        { "SpellPowerPotion_T01", (15, ConfigService.MaxLevel) },
        { "WranglersPotion_T01", (15, ConfigService.MaxLevel) },
        { "SunResistancePotion_T01", (15, ConfigService.MaxLevel) },
        { "HealingPotion_T01", (15, ConfigService.MaxLevel) },
        { "FireResistancePotion_T01", (15, ConfigService.MaxLevel) },
        { "DuskCaller", (50, ConfigService.MaxLevel) },
        { "SpellLeechPotion_T01", (50, ConfigService.MaxLevel) },
        { "PhysicalPowerPotion_T02", (65, ConfigService.MaxLevel) },
        { "SpellPowerPotion_T02", (65, ConfigService.MaxLevel) },
        { "HealingPotion_T02", (40, ConfigService.MaxLevel) },
        { "HolyResistancePotion_T01", (40, ConfigService.MaxLevel) },
        { "HolyResistancePotion_T02", (40, ConfigService.MaxLevel) }
    };

    static readonly Dictionary<ulong, Dictionary<QuestType, (int Progress, bool Active)>> QuestCoroutines = [];
    static readonly WaitForSeconds QuestMessageDelay = new(0.1f);
    static HashSet<PrefabGUID> GetKillPrefabsForLevel(int playerLevel)
    {
        Dictionary<PrefabGUID, HashSet<Entity>> TargetPrefabs = new(QuestService.TargetCache);
        HashSet<PrefabGUID> prefabs = [];

        foreach (PrefabGUID prefab in TargetPrefabs.Keys)
        {
            if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefab, out Entity targetEntity) && targetEntity.TryGetComponent(out UnitLevel unitLevel))
            {
                bool isVBlood = targetEntity.Has<VBloodUnit>();

                if (!isVBlood)
                {
                    if (playerLevel > DefaultMaxPlayerLevel && unitLevel.Level._Value > 80) // account for higher player level values than default
                    {
                        prefabs.Add(prefab);
                    }
                    else if (Math.Abs(unitLevel.Level._Value - playerLevel) <= 10) // within 10 level difference check otherwise
                    {
                        prefabs.Add(prefab);
                    }
                }
                else if (isVBlood)
                {
                    if (unitLevel.Level._Value > playerLevel) // skip vbloods higher than player
                    {
                        continue;
                    }
                    else if (playerLevel > DefaultMaxPlayerLevel && unitLevel.Level._Value > 80) // account for higher player level values than default
                    {
                        prefabs.Add(prefab);
                    }
                    else if (Math.Abs(unitLevel.Level._Value - playerLevel) <= 10) // within 10 level difference check otherwise
                    {
                        prefabs.Add(prefab);
                    }
                }
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
            PrefabGUID prefabGUID = prefabEntity.Read<PrefabGUID>();
            ItemData itemData = prefabEntity.Read<ItemData>();

            string prefabName = prefabGUID.LookupName();
            string tier;

            Match match = Regex.Match(prefabName);
            if (match.Success) tier = match.Value;
            else continue;

            if (itemData.ItemType == ItemType.Equippable)
            {
                if (IsWithinLevelRange(tier, playerLevel, EquipmentTierLevelRangeMap))
                {
                    prefabs.Add(prefabGUID);
                }
            }
            else if (itemData.ItemType == ItemType.Consumable)
            {
                if (IsConsumableWithinLevelRange(prefabName, playerLevel, ConsumableTierLevelRangeMap))
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

        foreach (PrefabGUID prefab in ResourcePrefabs)
        {
            Entity prefabEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];

            if (prefabEntity.TryGetComponent(out EntityCategory entityCategory) && entityCategory.ResourceLevel._Value <= playerLevel)
            {
                var buffer = prefabEntity.ReadBuffer<DropTableBuffer>();

                foreach (DropTableBuffer drop in buffer)
                {
                    if (drop.DropTrigger == DropTriggerType.YieldResourceOnDamageTaken)
                    {
                        Entity dropTable = PrefabCollectionSystem._PrefabGuidToEntityMap[drop.DropTableGuid];
                        if (!dropTable.Has<DropTableDataBuffer>()) continue;

                        var dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                        foreach (DropTableDataBuffer dropTableData in dropTableDataBuffer)
                        {
                            string prefabName = dropTableData.ItemGuid.LookupName();

                            if (prefabName.Contains("Item_Ingredient") && !FilteredResources.Any(part => prefabName.Contains(part)))
                            {
                                prefabs.Add(dropTableData.ItemGuid);

                                break;
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
    static bool IsConsumableWithinLevelRange(string prefabName, int playerLevel, Dictionary<string, (int MinLevel, int MaxLevel)> tierMap)
    {
        foreach (var kvp in tierMap)
        {
            if (prefabName.Contains(kvp.Key))
            {
                return playerLevel >= kvp.Value.MinLevel && playerLevel <= kvp.Value.MaxLevel;
            }
        }

        return false;
    }
    static QuestObjective GenerateQuestObjective(TargetType goal, HashSet<PrefabGUID> targets, QuestType questType)
    {
        PrefabGUID target = PrefabGUID.Empty;
        int requiredAmount;

        switch (goal)
        {
            case TargetType.Kill:

                if (targets.Count != 0)
                {
                    target = targets.ElementAt(Random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = GraveyardSkeleton;
                else if (questType.Equals(QuestType.Weekly)) target = ForestWolf;

                requiredAmount = Random.Next(6, 8) * QuestMultipliers[questType];
                string targetLower = target.LookupName().ToLower();

                if ((targetLower.Contains("vblood") || targetLower.Contains("vhunter")))
                {
                    if (!questType.Equals(QuestType.Weekly))
                    {
                        requiredAmount = 2;
                    }
                    else if (questType.Equals(QuestType.Weekly))
                    {
                        requiredAmount = 10;
                    }
                }

                break;
            case TargetType.Craft:

                if (targets.Count != 0)
                {
                    target = targets.ElementAt(Random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = ReinforcedBoneSword;
                else if (questType.Equals(QuestType.Weekly)) target = ReinforcedBoneMace;

                requiredAmount = Random.Next(2, 4) * QuestMultipliers[questType];

                break;
            case TargetType.Gather:

                if (targets.Count != 0)
                {
                    target = targets.ElementAt(Random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = ItemIngredientWood;
                else if (questType.Equals(QuestType.Weekly)) target = ItemIngredientStone;

                List<int> amounts = [500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000];
                requiredAmount = (int)(amounts.ElementAt(Random.Next(amounts.Count)) * QuestMultipliers[questType] * ResourceYieldModifier);

                break;
            default:
                throw new ArgumentOutOfRangeException(goal.ToString(), "Unknown quest target type encountered when generating quest objective...");
        }
        return new QuestObjective { Goal = goal, Target = target, RequiredAmount = requiredAmount };
    }
    static HashSet<PrefabGUID> GetGoalPrefabsForLevel(TargetType goal, int level)
    {
        HashSet<PrefabGUID> prefabs = goal switch
        {
            TargetType.Kill => GetKillPrefabsForLevel(level),
            TargetType.Craft => GetCraftPrefabsForLevel(level),
            TargetType.Gather => GetGatherPrefabsForLevel(level),
            _ => throw new ArgumentOutOfRangeException(),
        };

        return prefabs;
    }
    public static void InitializePlayerQuests(ulong steamId, int level)
    {
        List<TargetType> targetTypes = GetRandomQuestTypes();

        TargetType dailyGoal = targetTypes.First();
        TargetType weeklyGoal = targetTypes.Last();

        HashSet<PrefabGUID> dailyTargets = GetGoalPrefabsForLevel(dailyGoal, level);
        HashSet<PrefabGUID> weeklyTargets = GetGoalPrefabsForLevel(weeklyGoal, level);

        Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData = new()
            {
                { QuestType.Daily, (GenerateQuestObjective(dailyGoal, dailyTargets, QuestType.Daily), 0, DateTime.UtcNow) },
                { QuestType.Weekly, (GenerateQuestObjective(weeklyGoal, weeklyTargets, QuestType.Weekly), 0, DateTime.UtcNow) }
            };

        steamId.SetPlayerQuests(questData);
    }
    public static void RefreshQuests(User user, ulong steamId, int level)
    {
        if (steamId.TryGetPlayerQuests(out var questData))
        {
            DateTime lastDaily = questData[QuestType.Daily].LastReset;
            DateTime lastWeekly = questData[QuestType.Weekly].LastReset;

            DateTime nextDaily = lastDaily.AddDays(1);
            DateTime nextWeekly = lastWeekly.AddDays(7);

            DateTime now = DateTime.UtcNow;

            bool refreshDaily = now >= nextDaily;
            bool refreshWeekly = now >= nextWeekly;

            if (refreshDaily || refreshWeekly)
            {
                HashSet<PrefabGUID> targets;
                TargetType goal;

                if (refreshDaily && refreshWeekly)
                {
                    List<TargetType> targetTypes = GetRandomQuestTypes();

                    goal = targetTypes.First();
                    targets = GetGoalPrefabsForLevel(goal, level);

                    questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#00FFFF>Daily Quest</color> has been refreshed!");

                    goal = targetTypes.Last();
                    targets = GetGoalPrefabsForLevel(goal, level);

                    questData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, QuestType.Weekly), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#BF40BF>Weekly Quest</color> has been refreshed!");
                }
                else if (refreshDaily)
                {
                    //goal = GetUniqueQuestType(questData, QuestType.Weekly);
                    goal = GetRandomQuestType();
                    targets = GetGoalPrefabsForLevel(goal, level);

                    questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#00FFFF>Daily Quest</color> has been refreshed!");
                }
                else if (refreshWeekly)
                {
                    goal = GetUniqueQuestType(questData, QuestType.Daily);
                    targets = GetGoalPrefabsForLevel(goal, level);

                    questData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, QuestType.Weekly), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#BF40BF>Weekly Quest</color> has been refreshed!");
                }

                steamId.SetPlayerQuests(questData);
            }
        }
        else
        {
            InitializePlayerQuests(steamId, level);
        }
    }
    public static void ForceRefresh(ulong steamId, int level)
    {
        List<TargetType> goals = GetRandomQuestTypes();
        TargetType dailyGoal = goals.First();
        TargetType weeklyGoal = goals.Last();

        if (steamId.TryGetPlayerQuests(out var questData))
        {
            HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(dailyGoal, level);
            questData[QuestType.Daily] = (GenerateQuestObjective(dailyGoal, targets, QuestType.Daily), 0, DateTime.UtcNow);

            targets = GetGoalPrefabsForLevel(weeklyGoal, level);
            questData[QuestType.Weekly] = (GenerateQuestObjective(weeklyGoal, targets, QuestType.Weekly), 0, DateTime.UtcNow);

            steamId.SetPlayerQuests(questData);
        }
        else
        {
            InitializePlayerQuests(steamId, level);
        }
    }
    public static void ForceDaily(ulong steamId, int level)
    {
        if (steamId.TryGetPlayerQuests(out var questData))
        {
            //TargetType goal = GetUniqueQuestType(questData, QuestType.Weekly); // get unique goal different from weekly
            TargetType goal = GetRandomQuestType();
            HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

            questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, DateTime.UtcNow);
            steamId.SetPlayerQuests(questData);

            //LocalizationService.HandleServerReply(EntityManager, user, "<color=#00FFFF>Daily Quest</color> has been rerolled~");
        }
    }
    public static void ForceWeekly(ulong steamId, int level)
    {
        if (steamId.TryGetPlayerQuests(out var questData))
        {
            TargetType goal = GetUniqueQuestType(questData, QuestType.Daily); // get unique goal different from daily
            HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

            questData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, QuestType.Weekly), 0, DateTime.UtcNow);
            steamId.SetPlayerQuests(questData);

            //LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#BF40BF>Weekly Quest</color> has been rerolled~");
        }
    }
    static TargetType GetUniqueQuestType(Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, QuestType questType)
    {
        List<TargetType> targetTypes = new(TargetTypes);      
        if (questData.TryGetValue(questType, out var dailyData))
        {
            targetTypes.Remove(dailyData.Objective.Goal);
        }

        return targetTypes[Random.Next(targetTypes.Count)];
    }
    static TargetType GetRandomQuestType()
    {
        List<TargetType> targetTypes = new(TargetTypes);
        TargetType targetType = targetTypes[Random.Next(targetTypes.Count)];

        return targetType;
    }
    static List<TargetType> GetRandomQuestTypes()
    {
        List<TargetType> targetTypes = new(TargetTypes);

        TargetType firstGoal = targetTypes[Random.Next(targetTypes.Count)];
        targetTypes.Remove(firstGoal);

        TargetType secondGoal = targetTypes[Random.Next(targetTypes.Count)];

        return [firstGoal, secondGoal];
    }
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        //HashSet<ulong> processed = []; // may not need to check this with new event subscription stuff, will check later

        Entity source = deathEvent.Source;
        Entity died = deathEvent.Target;

        PrefabGUID target = died.Read<PrefabGUID>();
        HashSet<Entity> participants = deathEvent.DeathParticipants;

        //HashSet<Entity> participants = PlayerUtilities.GetDeathParticipants(source, userEntity);
        foreach (Entity player in participants)
        {
            User user = player.GetUser();
            ulong steamId = player.GetSteamId(); // participants are character entities

            /*
            if (!processed.Contains(steamId) && steamId.TryGetPlayerQuests(out var questData))
            {
                ProcessQuestProgress(questData, target, 1, user);
                processed.Add(steamId);
            }
            */

            if (steamId.TryGetPlayerQuests(out var questData))
            {
                ProcessQuestProgress(questData, target, 1, user);
            }
        }
    }
    public static void ProcessQuestProgress(Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, PrefabGUID target, int amount, User user)
    {
        bool updated = false;
        ulong steamId = user.PlatformId;

        for (int i = 0; i < questData.Count; i++)
        {
            var quest = questData.ElementAt(i);
            if (quest.Value.Objective.Target == target)
            {
                updated = true;
                string colorType = quest.Key == QuestType.Daily ? $"<color=#00FFFF>{QuestType.Daily} Quest</color>" : $"<color=#BF40BF>{QuestType.Weekly} Quest</color>";

                questData[quest.Key] = new(quest.Value.Objective, quest.Value.Progress + amount, quest.Value.LastReset);

                if (!QuestCoroutines.ContainsKey(steamId))
                {
                    QuestCoroutines[steamId] = [];
                }

                if (!QuestCoroutines[steamId].ContainsKey(quest.Key))
                {
                    var questEntry = (questData[quest.Key].Progress, true);
                    QuestCoroutines[steamId].Add(quest.Key, questEntry);

                    Core.StartCoroutine(DelayedProgressUpdate(questData, quest, user, steamId, colorType));
                }
                else
                {
                    QuestCoroutines[steamId][quest.Key] = (questData[quest.Key].Progress, true);
                }

                /*
                if (PlayerUtilities.GetPlayerBool(steamId, "QuestLogging") && !quest.Value.Objective.Complete)
                {
                    string message = $"Progress added to {colorType}: <color=green>{quest.Value.Objective.Goal}</color> <color=white>{quest.Value.Objective.Target.GetPrefabName()}</color> [<color=white>{questData[quest.Key].Progress}</color>/<color=yellow>{quest.Value.Objective.RequiredAmount}</color>]";
                    LocalizationService.HandleServerReply(EntityManager, user, message);
                }
                */

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
                        int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                        TargetType goal = GetRandomQuestType();

                        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);
                        questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, DateTime.UtcNow);

                        var dailyQuest = questData[QuestType.Daily];
                        LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>Daily Quest</color> available: <color=green>{dailyQuest.Objective.Goal}</color> <color=white>{dailyQuest.Objective.Target.GetPrefabName()}</color>x<color=#FFC0CB>{dailyQuest.Objective.RequiredAmount}</color> [<color=white>{dailyQuest.Progress}</color>/<color=yellow>{dailyQuest.Objective.RequiredAmount}</color>]");
                    }
                }
            }
        }

        if (updated) steamId.SetPlayerQuests(questData);
    }
    static IEnumerator DelayedProgressUpdate(
    Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData,
    KeyValuePair<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> quest,
    User user,
    ulong steamId,
    string colorType)
    {
        if (questData[quest.Key].Progress >= questData[quest.Key].Objective.RequiredAmount)
        {
            yield break;
        }

        yield return QuestMessageDelay;

        if (PlayerUtilities.GetPlayerBool(steamId, "QuestLogging") && !quest.Value.Objective.Complete)
        {
            string message = $"Progress added to {colorType}: <color=green>{quest.Value.Objective.Goal}</color> " +
                             $"<color=white>{quest.Value.Objective.Target.GetPrefabName()}</color> " +
                             $"[<color=white>{questData[quest.Key].Progress}</color>/<color=yellow>{quest.Value.Objective.RequiredAmount}</color>]";

            LocalizationService.HandleServerReply(EntityManager, user, message);
        }

        QuestCoroutines[steamId].Remove(quest.Key);
        if (QuestCoroutines[steamId].Count == 0)
        {
            QuestCoroutines.Remove(steamId);
        }
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
