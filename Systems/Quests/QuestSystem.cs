using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
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

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _legacy = ConfigService.LegacySystem;
    static readonly bool _progression = _leveling || _expertise || _legacy;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _expertiseAndLegacies = _expertise && _legacy;
    static readonly bool _infiniteDailies = ConfigService.InfiniteDailies;
    static readonly bool _recipes = ConfigService.ExtraRecipes;

    static readonly int _maxPlayerLevel = ConfigService.MaxLevel;
    static readonly int _maxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    static readonly int _maxLegacyLevel = ConfigService.MaxBloodLevel;
    static readonly int _maxFamiliarLevel = ConfigService.MaxFamiliarLevel;

    static readonly float _dailyPerfectChance = ConfigService.DailyPerfectChance;

    static readonly float _resourceYieldModifier = SystemService.ServerGameSettingsSystem._Settings.MaterialYieldModifier_Global;

    static readonly Random _random = new();
    static readonly Regex _regex = new(@"T\d{2}", System.Text.RegularExpressions.RegexOptions.Compiled);

    public static readonly HashSet<PrefabGUID> CraftPrefabs = [];
    public static readonly HashSet<PrefabGUID> ResourcePrefabs = [];

    static readonly PrefabGUID _invulnerableBuff = new(-480024072);

    static readonly PrefabGUID _graveyardSkeleton = new(1395549638);
    static readonly PrefabGUID _forestWolf = new(-1418430647);

    static readonly PrefabGUID _reinforcedBoneSword = new(-796306296);
    static readonly PrefabGUID _reinforcedBoneMace = new(-1998017941);

    static readonly PrefabGUID _standardWood = new(-1593377811);
    static readonly PrefabGUID _hallowedWood = new(-2014020548);
    static readonly PrefabGUID _gloomWood = new(-1740500585);
    static readonly PrefabGUID _cursedWood = new(608397239);
    static readonly PrefabGUID _itemIngredientStone = new(-1531666018);

    const int DEFAULT_MAX_LEVEL = 90;
    const float XP_PERCENTAGE = 0.03f;
    const int VBLOOD_FACTOR = 3;
    const float DELAY = 0.75f;

    static readonly List<PrefabGUID> _perfectGems =
    [
        Prefabs.Item_Ingredient_Gem_Amethyst_T04,
        Prefabs.Item_Ingredient_Gem_Ruby_T04,
        Prefabs.Item_Ingredient_Gem_Sapphire_T04,
        Prefabs.Item_Ingredient_Gem_Emerald_T04,
        Prefabs.Item_Ingredient_Gem_Topaz_T04,
        Prefabs.Item_Ingredient_Gem_Miststone_T04
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

    static readonly List<TargetType> _targetTypes =
    [
        TargetType.Kill,
        TargetType.Craft,
        TargetType.Gather
    ];

    static readonly Dictionary<QuestType, int> _questMultipliers = new()
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

    static readonly Dictionary<string, (int MinLevel, int MaxLevel)> _equipmentTierLevelRangeMap = new()
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

    static readonly Dictionary<string, (int MinLevel, int MaxLevel)> _consumableTierLevelRangeMap = new()
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

    static readonly Dictionary<ulong, Dictionary<QuestType, (int Progress, bool Active)>> _questCoroutines = [];
    static readonly WaitForSeconds _questMessageDelay = new(0.1f);
    static HashSet<PrefabGUID> GetKillPrefabsForLevel(int playerLevel)
    {
        var prefabMap = PrefabCollectionSystem._PrefabGuidToEntityMap;
        HashSet<PrefabGUID> prefabs = [];

        foreach (PrefabGUID prefabGuid in QuestService.TargetCache.Keys)
        {
            if (prefabMap.TryGetValue(prefabGuid, out Entity targetEntity) && targetEntity.TryGetComponent(out UnitLevel unitLevel))
            {
                bool isVBlood = targetEntity.Has<VBloodUnit>();

                if (!isVBlood)
                {
                    if (playerLevel > DEFAULT_MAX_LEVEL && unitLevel.Level._Value > 80)
                    {
                        prefabs.Add(prefabGuid);
                    }
                    else if (Math.Abs(unitLevel.Level._Value - playerLevel) <= 10)
                    {
                        prefabs.Add(prefabGuid);
                    }
                }
                else if (isVBlood)
                {
                    if (unitLevel.Level._Value > playerLevel)
                    {
                        continue;
                    }
                    else if (playerLevel > DEFAULT_MAX_LEVEL && unitLevel.Level._Value > 80)
                    {
                        prefabs.Add(prefabGuid);
                    }
                    else if (Math.Abs(unitLevel.Level._Value - playerLevel) <= 10)
                    {
                        prefabs.Add(prefabGuid);
                    }
                }
            }
        }

        return prefabs;
    }
    static IEnumerable<PrefabGUID> GetKillPrefabsForLevelEnumerable(int playerLevel)
    {
        var prefabMap = PrefabCollectionSystem._PrefabGuidToEntityMap;

        foreach (PrefabGUID prefabGuid in QuestService.TargetCache.Keys)
        {
            if (prefabMap.TryGetValue(prefabGuid, out Entity targetEntity) && targetEntity.TryGetComponent(out UnitLevel unitLevel))
            {
                bool isVBlood = targetEntity.Has<VBloodUnit>();
                int level = unitLevel.Level._Value;

                if (isVBlood && level > playerLevel) continue;

                bool isHighLevel = (playerLevel > DEFAULT_MAX_LEVEL && level > 80);
                bool isWithinRange = (Math.Abs(level - playerLevel) <= 10);

                if (isHighLevel || isWithinRange) yield return prefabGuid;
            }
        }
    }
    static HashSet<PrefabGUID> GetCraftPrefabsForLevel(int playerLevel)
    {
        var prefabMap = PrefabCollectionSystem._PrefabGuidToEntityMap;

        HashSet<PrefabGUID> prefabs = [];

        foreach (PrefabGUID prefabGuid in CraftPrefabs)
        {
            if (!prefabMap.TryGetValue(prefabGuid, out Entity prefab) || !prefab.TryGetComponent(out ItemData itemData)) continue;

            string prefabName = prefabGuid.GetPrefabName();
            string tier;

            Match match = _regex.Match(prefabName);

            if (match.Success) tier = match.Value;
            else continue;

            if (itemData.ItemType == ItemType.Equippable)
            {
                if (IsWithinLevelRange(tier, playerLevel, _equipmentTierLevelRangeMap))
                {
                    prefabs.Add(prefabGuid);
                }
            }
            else if (itemData.ItemType == ItemType.Consumable)
            {
                if (IsConsumableWithinLevelRange(prefabName, playerLevel, _consumableTierLevelRangeMap))
                {
                    prefabs.Add(prefabGuid);
                }
            }
        }

        return prefabs;
    }
    static IEnumerable<PrefabGUID> GetCraftPrefabsForLevelEnumerable(int playerLevel)
    {
        var prefabMap = PrefabCollectionSystem._PrefabGuidToEntityMap;

        foreach (PrefabGUID prefabGuid in CraftPrefabs)
        {
            if (prefabMap.TryGetValue(prefabGuid, out Entity prefab) && prefab.TryGetComponent(out ItemData itemData))
            {
                string prefabName = prefabGuid.GetPrefabName();

                Match match = _regex.Match(prefabName);
                if (!match.Success) continue;

                string tier = match.Value;

                if (itemData.ItemType == ItemType.Equippable)
                {
                    if (IsWithinLevelRange(tier, playerLevel, _equipmentTierLevelRangeMap)) yield return prefabGuid;
                }
                else if (itemData.ItemType == ItemType.Consumable)
                {
                    if (IsConsumableWithinLevelRange(prefabName, playerLevel, _consumableTierLevelRangeMap)) yield return prefabGuid;
                }
            }
        }
    }
    static HashSet<PrefabGUID> GetGatherPrefabsForLevel(int playerLevel)
    {
        var prefabMap = PrefabCollectionSystem._PrefabGuidToEntityMap;

        HashSet<PrefabGUID> prefabs = [];

        foreach (PrefabGUID prefab in ResourcePrefabs)
        {
            if (prefabMap.TryGetValue(prefab, out Entity prefabEntity) && prefabEntity.TryGetComponent(out EntityCategory entityCategory) && entityCategory.ResourceLevel._Value <= playerLevel)
            {
                var buffer = prefabEntity.ReadBuffer<DropTableBuffer>();

                foreach (DropTableBuffer drop in buffer)
                {
                    if (drop.DropTrigger == DropTriggerType.YieldResourceOnDamageTaken)
                    {
                        Entity dropTable = prefabMap[drop.DropTableGuid];
                        if (!dropTable.Has<DropTableDataBuffer>()) continue;

                        var dropTableDataBuffer = dropTable.ReadBuffer<DropTableDataBuffer>();
                        foreach (DropTableDataBuffer dropTableData in dropTableDataBuffer)
                        {
                            string prefabName = dropTableData.ItemGuid.GetPrefabName();

                            if (prefabName.Contains("Item_Ingredient"))
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
    static IEnumerable<PrefabGUID> GetGatherPrefabsForLevelEnumerable(int playerLevel)
    {
        var prefabMap = PrefabCollectionSystem._PrefabGuidToEntityMap;

        foreach (PrefabGUID prefabGuid in ResourcePrefabs)
        {
            if (prefabMap.TryGetValue(prefabGuid, out Entity prefabEntity) && prefabEntity.TryGetComponent(out EntityCategory entityCategory) && entityCategory.ResourceLevel._Value <= playerLevel)
            {
                var buffer = prefabEntity.ReadBuffer<DropTableBuffer>();

                foreach (DropTableBuffer drop in buffer)
                {
                    if (drop.DropTrigger == DropTriggerType.YieldResourceOnDamageTaken)
                    {
                        Entity dropTable = prefabMap[drop.DropTableGuid];
                        if (!dropTable.TryGetBuffer<DropTableDataBuffer>(out var dropTableDataBuffer)) continue;

                        foreach (DropTableDataBuffer dropTableData in dropTableDataBuffer)
                        {
                            string prefabName = dropTableData.ItemGuid.GetPrefabName();

                            if (prefabName.Contains("Item_Ingredient"))
                            {
                                yield return dropTableData.ItemGuid;
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }
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

                if (targets.Any())
                {
                    target = targets.ElementAt(_random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = _graveyardSkeleton;
                else if (questType.Equals(QuestType.Weekly)) target = _forestWolf;

                // requiredAmount = _random.Next(6, 8) * _questMultipliers[questType];
                requiredAmount = 6 * _questMultipliers[questType]; // mmm want to redo this a bit eventually but need to ignore for now, 6 it is
                string targetLower = target.GetPrefabName().ToLower();

                if ((targetLower.Contains("vblood") || targetLower.Contains("vhunter")))
                {
                    if (questType.Equals(QuestType.Daily))
                    {
                        requiredAmount = 1;
                    }
                    else if (questType.Equals(QuestType.Weekly))
                    {
                        requiredAmount = 5;
                    }
                }
                break;
            case TargetType.Craft:

                if (targets.Any())
                {
                    target = targets.ElementAt(_random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = _reinforcedBoneSword;
                else if (questType.Equals(QuestType.Weekly)) target = _reinforcedBoneMace;

                requiredAmount = _random.Next(2, 4) * _questMultipliers[questType];
                break;
            case TargetType.Gather:

                if (targets.Any())
                {
                    target = targets.ElementAt(_random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = _standardWood;
                else if (questType.Equals(QuestType.Weekly)) target = _itemIngredientStone;

                List<int> amounts = [500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000];
                requiredAmount = (int)(amounts.ElementAt(_random.Next(amounts.Count)) * _questMultipliers[questType] * _resourceYieldModifier);

                if (target.Equals(_cursedWood) || target.Equals(_gloomWood) || target.Equals(_hallowedWood)) requiredAmount /= 2;
                break;
            default:
                throw new ArgumentOutOfRangeException(goal.ToString(), "Unknown quest goal type encountered when generating quest objective!");
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
            _ => throw new ArgumentOutOfRangeException(goal.ToString(), "Unknown quest goal type encountered when generating quest objective!")
        };

        return prefabs;
    }
    static IEnumerable<PrefabGUID> GetGoalPrefabsForLevelEnumerable(TargetType goal, int level)
    {
        return goal switch
        {
            TargetType.Kill => GetKillPrefabsForLevelEnumerable(level),
            TargetType.Craft => GetCraftPrefabsForLevelEnumerable(level),
            TargetType.Gather => GetGatherPrefabsForLevelEnumerable(level),
            _ => throw new ArgumentOutOfRangeException(
                goal.ToString(), 
                "Unknown quest goal type encountered when generating quest objective!")
        };
    }
    public static void InitializePlayerQuests(ulong steamId, int level)
    {
        List<TargetType> targetTypes = GetRandomQuestTypes();

        TargetType dailyGoal = targetTypes.First();
        TargetType weeklyGoal = targetTypes.Last();

        HashSet<PrefabGUID> dailyTargets = [..GetGoalPrefabsForLevelEnumerable(dailyGoal, level)];
        HashSet<PrefabGUID> weeklyTargets = [..GetGoalPrefabsForLevelEnumerable(weeklyGoal, level)];

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
                    targets = [..GetGoalPrefabsForLevelEnumerable(goal, level)];

                    questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#00FFFF>Daily Quest</color> has been refreshed!");

                    goal = targetTypes.Last();
                    targets = [..GetGoalPrefabsForLevelEnumerable(goal, level)];

                    questData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, QuestType.Weekly), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#BF40BF>Weekly Quest</color> has been refreshed!");
                }
                else if (refreshDaily)
                {
                    goal = GetRandomQuestType();
                    targets = [..GetGoalPrefabsForLevelEnumerable(goal, level)];

                    questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, now);
                    LocalizationService.HandleServerReply(EntityManager, user, "Your <color=#00FFFF>Daily Quest</color> has been refreshed!");
                }
                else if (refreshWeekly)
                {
                    goal = GetUniqueQuestType(questData, QuestType.Daily);
                    targets = [..GetGoalPrefabsForLevelEnumerable(goal, level)];

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
            HashSet<PrefabGUID> targets = [..GetGoalPrefabsForLevelEnumerable(dailyGoal, level)];
            questData[QuestType.Daily] = (GenerateQuestObjective(dailyGoal, targets, QuestType.Daily), 0, DateTime.UtcNow);

            targets = [..GetGoalPrefabsForLevelEnumerable(weeklyGoal, level)];
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
            TargetType goal = GetRandomQuestType();
            HashSet<PrefabGUID> targets = [..GetGoalPrefabsForLevelEnumerable(goal, level)];

            questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, DateTime.UtcNow);
            steamId.SetPlayerQuests(questData);
        }
    }
    public static void ForceWeekly(ulong steamId, int level)
    {
        if (steamId.TryGetPlayerQuests(out var questData))
        {
            TargetType goal = GetUniqueQuestType(questData, QuestType.Daily);
            HashSet<PrefabGUID> targets = [..GetGoalPrefabsForLevelEnumerable(goal, level)];

            questData[QuestType.Weekly] = (GenerateQuestObjective(goal, targets, QuestType.Weekly), 0, DateTime.UtcNow);
            steamId.SetPlayerQuests(questData);
        }
    }
    static TargetType GetUniqueQuestType(Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, QuestType questType)
    {
        List<TargetType> targetTypes = [.._targetTypes];

        if (questData.TryGetValue(questType, out var dailyData))
        {
            targetTypes.Remove(dailyData.Objective.Goal);
        }

        return targetTypes[_random.Next(targetTypes.Count)];
    }
    static TargetType GetRandomQuestType()
    {
        List<TargetType> targetTypes = [.._targetTypes];
        TargetType targetType = targetTypes[_random.Next(targetTypes.Count)];
        return targetType;
    }
    static List<TargetType> GetRandomQuestTypes()
    {
        List<TargetType> targetTypes = [.._targetTypes];
        TargetType firstGoal = targetTypes[_random.Next(targetTypes.Count)];

        targetTypes.Remove(firstGoal);
        TargetType secondGoal = targetTypes[_random.Next(targetTypes.Count)];

        return [firstGoal, secondGoal];
    }
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        PrefabGUID target = deathEvent.Target.GetPrefabGuid();
        HashSet<Entity> participants = deathEvent.DeathParticipants;

        foreach (Entity player in participants)
        {
            User user = player.GetUser();
            ulong steamId = player.GetSteamId();

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

                if (!_questCoroutines.ContainsKey(steamId))
                {
                    _questCoroutines[steamId] = [];
                }

                if (!_questCoroutines[steamId].ContainsKey(quest.Key))
                {
                    var questEntry = (questData[quest.Key].Progress, true);
                    _questCoroutines[steamId].Add(quest.Key, questEntry);

                    QuestProgressDelayRoutine(questData, quest, user, steamId, colorType).Start();
                }
                else
                {
                    _questCoroutines[steamId][quest.Key] = (questData[quest.Key].Progress, true);
                }

                if (quest.Value.Objective.RequiredAmount <= questData[quest.Key].Progress && !quest.Value.Objective.Complete)
                {
                    quest.Value.Objective.Complete = true;

                    if (QuestRewards.Any()) HandleItemReward(user, quest.Key, quest.Value.Objective, colorType);
                    if (_progression) HandleExperienceReward(user, quest.Key);
                    if (_recipes) HandlePerfectGemReward(user, quest.Key);

                    if (quest.Key == QuestType.Daily && _infiniteDailies)
                    {
                        // int level = (_leveling && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                        int level = (_leveling && steamId.TryGetPlayerExperience(out var data)) ? data.Key : GetSimulatedLevel(PlayerService.GetPlayerInfo(user.CharacterName.Value).UserEntity);
                        TargetType goal = GetRandomQuestType();

                        HashSet<PrefabGUID> targets = [..GetGoalPrefabsForLevelEnumerable(goal, level)];
                        questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, DateTime.UtcNow);

                        var dailyQuest = questData[QuestType.Daily];
                        LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>Daily Quest</color> available: <color=green>{dailyQuest.Objective.Goal}</color> <color=white>{dailyQuest.Objective.Target.GetLocalizedName()}</color>x<color=#FFC0CB>{dailyQuest.Objective.RequiredAmount}</color> [<color=white>{dailyQuest.Progress}</color>/<color=yellow>{dailyQuest.Objective.RequiredAmount}</color>]");
                    }
                }
            }
        }

        if (updated) steamId.SetPlayerQuests(questData);
    }
    static void HandlePerfectGemReward(User user, QuestType questType)
    {
        PrefabGUID perfectGem = _perfectGems.ElementAt(_random.Next(_perfectGems.Count));

        if (questType.Equals(QuestType.Weekly))
        {
            Utilities.Misc.GiveOrDropItem(user, user.LocalCharacter.GetEntityOnServer(), perfectGem, 1);
        }
        else if (questType.Equals(QuestType.Daily) && Utilities.Misc.RollForChance(_dailyPerfectChance))
        {
            Utilities.Misc.GiveOrDropItem(user, user.LocalCharacter.GetEntityOnServer(), perfectGem, 1);
        }
    }
    static void HandleItemReward(User user, QuestType questType, QuestObjective objective, string colorType)
    {
        PrefabGUID reward = QuestRewards.Keys.ElementAt(_random.Next(QuestRewards.Count));
        int quantity = QuestRewards[reward];

        if (questType == QuestType.Weekly) quantity *= _questMultipliers[questType];

        if (objective.Target.GetPrefabName().Contains("vblood", StringComparison.OrdinalIgnoreCase)) quantity *= VBLOOD_FACTOR;

        if (ServerGameManager.TryAddInventoryItem(user.LocalCharacter._Entity, reward, quantity))
        {
            string message = $"You've received <color=#ffd9eb>{reward.GetLocalizedName()}</color>x<color=white>{quantity}</color> for completing your {colorType}!";
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
        else
        {
            InventoryUtilitiesServer.CreateDropItem(EntityManager, user.LocalCharacter._Entity, reward, quantity, new Entity());
            string message = $"You've received <color=#ffd9eb>{reward.GetLocalizedName()}</color>x<color=white>{quantity}</color> for completing your {colorType}! It dropped on the ground because your inventory was full.";
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
    }
    static void HandleExperienceReward(User user, QuestType questType)
    {
        string progressType = ProcessQuestExperienceGain(user, _questMultipliers[questType], XP_PERCENTAGE);

        if (string.IsNullOrEmpty(progressType)) return;
        else
        {
            float xpPercentage = XP_PERCENTAGE * _questMultipliers[questType] * 100;
            if (progressType.Contains("expertise") && progressType.Contains("essence")) xpPercentage *= 0.5f;

            string xpMessage = $"You've been awarded <color=yellow>{xpPercentage:F1}%</color> of your total {progressType}!";
            LocalizationService.HandleServerReply(EntityManager, user, xpMessage);
        }
    }
    static string ProcessQuestExperienceGain(User user, int multiplier, float percentOfTotalXP)
    {
        string progressString = string.Empty;
        float gainedXP;

        ulong steamId = user.PlatformId;
        Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();
        Entity userEntity = playerCharacter.GetUserEntity();

        int currentLevel = steamId.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;

        // If not at max player level, just give player XP
        if (_leveling && currentLevel < _maxPlayerLevel)
        {
            gainedXP = ConvertLevelToXp(currentLevel) * percentOfTotalXP * multiplier;
            progressString = GainPlayerExperience(playerCharacter, steamId, gainedXP);

            return progressString;
        }

        // If at max player level, we start distributing XP to other systems
        // depending on which systems are enabled and which ones are at max.
        KeyValuePair<int, float> expertiseData;
        int expertiseLevel = 0;

        KeyValuePair<int, float> legacyData;
        int legacyLevel = 0;

        if (_expertiseAndLegacies)
        {
            // Get current weapon and blood handlers
            Expertise.WeaponType weaponType = WeaponManager.GetCurrentWeaponType(playerCharacter);
            IWeaponHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            BloodType bloodType = BloodManager.GetCurrentBloodType(playerCharacter.GetBlood());
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (expertiseHandler != null)
            {
                expertiseData = expertiseHandler.GetExpertiseData(steamId);
                expertiseLevel = expertiseData.Key;
            }

            if (bloodHandler != null)
            {
                legacyData = bloodHandler.GetLegacyData(steamId);
                legacyLevel = legacyData.Key;
            }

            bool maxExpertise = expertiseLevel >= _maxExpertiseLevel;
            bool maxLegacy = legacyLevel >= _maxLegacyLevel;

            // If both expertise and legacy are maxed and familiars are enabled
            if (maxExpertise && maxLegacy && _familiars)
            {
                progressString = TryGainFamiliarExperience(playerCharacter, steamId, percentOfTotalXP, multiplier);
                return progressString;
            }

            // If expertise is maxed but legacy is not, give legacy XP
            if (maxExpertise && !maxLegacy && bloodHandler != null)
            {
                gainedXP = ConvertLevelToXp(legacyLevel) * percentOfTotalXP * multiplier;
                progressString = GainLegacyExperience(playerCharacter, userEntity, user, steamId, bloodType, bloodHandler, gainedXP);
                return progressString;
            }

            // If legacy is maxed but expertise is not, give expertise XP
            if (!maxExpertise && maxLegacy && expertiseHandler != null)
            {
                gainedXP = ConvertLevelToXp(expertiseLevel) * percentOfTotalXP * multiplier;
                progressString = GainExpertiseExperience(playerCharacter, userEntity, user, steamId, weaponType, expertiseHandler, gainedXP);
                return progressString;
            }

            // If neither are maxed, give half XP to both
            if (!maxExpertise && !maxLegacy)
            {
                percentOfTotalXP *= 0.5f;
                string expertiseString = string.Empty;
                string legacyString = string.Empty;

                if (expertiseHandler == null || bloodHandler == null) percentOfTotalXP *= 2f;

                if (expertiseHandler != null)
                {
                    gainedXP = ConvertLevelToXp(expertiseLevel) * percentOfTotalXP * multiplier;
                    expertiseString = GainExpertiseExperience(playerCharacter, userEntity, user, steamId, weaponType, expertiseHandler, gainedXP);
                }

                if (bloodHandler != null)
                {
                    gainedXP = ConvertLevelToXp(legacyLevel) * percentOfTotalXP * multiplier;
                    legacyString = GainLegacyExperience(playerCharacter, userEntity, user, steamId, bloodType, bloodHandler, gainedXP);
                }

                // Combine strings if both exist
                if (!string.IsNullOrEmpty(expertiseString) && !string.IsNullOrEmpty(legacyString))
                {
                    progressString = expertiseString + " & " + legacyString;
                }
                else
                {
                    progressString = expertiseString + legacyString;
                }

                return progressString;
            }
        }
        else if (_expertise)
        {
            Expertise.WeaponType weaponType = WeaponManager.GetCurrentWeaponType(playerCharacter);
            IWeaponHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler != null)
            {
                expertiseData = expertiseHandler.GetExpertiseData(steamId);
                expertiseLevel = expertiseData.Key;

                gainedXP = ConvertLevelToXp(expertiseLevel) * percentOfTotalXP * multiplier;
                progressString = GainExpertiseExperience(playerCharacter, userEntity, user, steamId, weaponType, expertiseHandler, gainedXP);
                return progressString;
            }
        }
        else if (_legacy)
        {
            BloodType bloodType = BloodManager.GetCurrentBloodType(playerCharacter.GetBlood());
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyData = bloodHandler.GetLegacyData(steamId);
                legacyLevel = legacyData.Key;

                gainedXP = ConvertLevelToXp(legacyLevel) * percentOfTotalXP * multiplier;
                progressString = GainLegacyExperience(playerCharacter, userEntity, user, steamId, bloodType, bloodHandler, gainedXP);
                return progressString;
            }
        }

        return progressString;
    }
    static string GainPlayerExperience(Entity playerCharacter, ulong steamId, float gainedXP)
    {
        LevelingSystem.SaveLevelingExperience(steamId, gainedXP, out bool leveledUp, out int newLevel);
        LevelingSystem.NotifyPlayer(playerCharacter, steamId, gainedXP, leveledUp, newLevel, DELAY);
        return "<color=#FFC0CB>experience</color>";
    }
    static string GainExpertiseExperience(Entity playerCharacter, Entity userEntity, User user, ulong steamId, Expertise.WeaponType weaponType, IWeaponHandler handler, float gainedXP)
    {
        WeaponSystem.SaveExpertiseExperience(steamId, handler, gainedXP, out bool leveledUp, out int newLevel);
        WeaponSystem.NotifyPlayer(playerCharacter, userEntity, user, steamId, weaponType, gainedXP, leveledUp, newLevel, handler, DELAY);
        return "<color=#FFC0CB>expertise</color>";
    }
    static string GainLegacyExperience(Entity playerCharacter, Entity userEntity, User user, ulong steamId, BloodType bloodType, IBloodHandler handler, float gainedXP)
    {
        BloodSystem.SaveBloodExperience(steamId, handler, gainedXP, out bool leveledUp, out int newLevel);
        BloodSystem.NotifyPlayer(playerCharacter, userEntity, user, steamId, bloodType, gainedXP, leveledUp, newLevel, handler, DELAY);
        return "<color=#FFC0CB>essence</color>";
    }
    static string GainFamiliarExperience(Entity character, Entity familiar, int familiarId, ulong steamId, KeyValuePair<int, float> xpData, float gainedXP, int currentLevel)
    {
        FamiliarLevelingSystem.UpdateFamiliarExperience(character, familiar, familiarId, steamId, xpData, gainedXP, currentLevel);
        return "<color=#FFC0CB>familiar experience</color>";
    }
    static string TryGainFamiliarExperience(Entity character, ulong steamId, float percentOfTotalXP, int multiplier)
    {
        Entity familiar = Utilities.Familiars.GetActiveFamiliar(character);
        int familiarId = familiar.GetGuidHash();

        if (!familiar.IsDisabled() && !familiar.HasBuff(_invulnerableBuff))
        {
            var familiarXP = FamiliarLevelingSystem.GetFamiliarExperience(steamId, familiarId);
            int familiarLevel = familiarXP.Key;

            if (familiarLevel >= _maxFamiliarLevel) return string.Empty;

            float gainedXP = ConvertLevelToXp(familiarLevel) * percentOfTotalXP * multiplier;
            return GainFamiliarExperience(character, familiar, familiarId, steamId, familiarXP, gainedXP, familiarLevel);
        }

        return string.Empty;
    }
    static IEnumerator QuestProgressDelayRoutine(
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

        yield return _questMessageDelay;

        if (GetPlayerBool(steamId, QUEST_LOG_KEY) && !quest.Value.Objective.Complete)
        {
            string message = $"Progress added to {colorType}: <color=green>{quest.Value.Objective.Goal}</color> " +
                             $"<color=white>{quest.Value.Objective.Target.GetLocalizedName()}</color> " +
                             $"[<color=white>{questData[quest.Key].Progress}</color>/<color=yellow>{quest.Value.Objective.RequiredAmount}</color>]";

            LocalizationService.HandleServerReply(EntityManager, user, message);
        }

        _questCoroutines[steamId].Remove(quest.Key);

        if (_questCoroutines[steamId].Count == 0)
        {
            _questCoroutines.Remove(steamId);
        }
    }
}
