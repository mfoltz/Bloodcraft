using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
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
    static readonly bool _legacy = ConfigService.BloodSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _weaponBloodSystems = _expertise && _legacy;
    static readonly bool _infiniteDailies = ConfigService.InfiniteDailies;

    static readonly int _maxPlayerLevel = ConfigService.MaxLevel;
    static readonly int _maxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    static readonly int _maxLegacyLevel = ConfigService.MaxBloodLevel;
    static readonly int _maxFamiliarLevel = ConfigService.MaxFamiliarLevel;

    static readonly float _resourceYieldModifier = SystemService.ServerGameSettingsSystem._Settings.MaterialYieldModifier_Global;

    static readonly Random _random = new();
    static readonly Regex _regex = new(@"T\d{2}");

    public static readonly HashSet<PrefabGUID> CraftPrefabs = [];
    public static readonly HashSet<PrefabGUID> ResourcePrefabs = [];

    static readonly PrefabGUID _invulnerableBuff = new(-480024072);

    static readonly PrefabGUID _graveyardSkeleton = new(1395549638);
    static readonly PrefabGUID _forestWolf = new(-1418430647);

    static readonly PrefabGUID _reinforcedBoneSword = new(-796306296);
    static readonly PrefabGUID _reinforcedBoneMace = new(-1998017941);

    static readonly PrefabGUID _itemIngredientWood = new(-1593377811);
    static readonly PrefabGUID _itemIngredientStone = new(-1531666018);

    const int DEFAULT_MAX_LEVEL = 90;
    const float XP_PERCENTAGE = 0.03f;
    const int VBLOOD_FACTOR = 3;

    static readonly HashSet<string> _filteredResources =
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
        Dictionary<PrefabGUID, HashSet<Entity>> TargetPrefabs = new(QuestService._targetCache);
        HashSet<PrefabGUID> prefabs = [];

        foreach (PrefabGUID prefab in TargetPrefabs.Keys)
        {
            if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefab, out Entity targetEntity) && targetEntity.TryGetComponent(out UnitLevel unitLevel))
            {
                bool isVBlood = targetEntity.Has<VBloodUnit>();

                if (!isVBlood)
                {
                    if (playerLevel > DEFAULT_MAX_LEVEL && unitLevel.Level._Value > 80) // account for higher player level values than default
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
                    else if (playerLevel > DEFAULT_MAX_LEVEL && unitLevel.Level._Value > 80) // account for higher player level values than default
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
            PrefabGUID prefabGUID = prefabEntity.ReadRO<PrefabGUID>();
            ItemData itemData = prefabEntity.ReadRO<ItemData>();

            string prefabName = prefabGUID.GetPrefabName();
            string tier;

            Match match = _regex.Match(prefabName);
            if (match.Success) tier = match.Value;
            else continue;

            if (itemData.ItemType == ItemType.Equippable)
            {
                if (IsWithinLevelRange(tier, playerLevel, _equipmentTierLevelRangeMap))
                {
                    prefabs.Add(prefabGUID);
                }
            }
            else if (itemData.ItemType == ItemType.Consumable)
            {
                if (IsConsumableWithinLevelRange(prefabName, playerLevel, _consumableTierLevelRangeMap))
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
                            string prefabName = dropTableData.ItemGuid.GetPrefabName();

                            if (prefabName.Contains("Item_Ingredient") && !_filteredResources.Any(part => prefabName.Contains(part)))
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
                    target = targets.ElementAt(_random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = _graveyardSkeleton;
                else if (questType.Equals(QuestType.Weekly)) target = _forestWolf;

                requiredAmount = _random.Next(6, 8) * _questMultipliers[questType];
                string targetLower = target.GetPrefabName().ToLower();

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
                    target = targets.ElementAt(_random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = _reinforcedBoneSword;
                else if (questType.Equals(QuestType.Weekly)) target = _reinforcedBoneMace;

                requiredAmount = _random.Next(2, 4) * _questMultipliers[questType];

                break;
            case TargetType.Gather:

                if (targets.Count != 0)
                {
                    target = targets.ElementAt(_random.Next(targets.Count));
                    targets.Remove(target);
                }
                else if (questType.Equals(QuestType.Daily)) target = _itemIngredientWood;
                else if (questType.Equals(QuestType.Weekly)) target = _itemIngredientStone;

                List<int> amounts = [500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000];
                requiredAmount = (int)(amounts.ElementAt(_random.Next(amounts.Count)) * _questMultipliers[questType] * _resourceYieldModifier);

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
            TargetType goal = GetRandomQuestType();
            HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);

            questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, DateTime.UtcNow);
            steamId.SetPlayerQuests(questData);
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
        }
    }
    static TargetType GetUniqueQuestType(Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, QuestType questType)
    {
        List<TargetType> targetTypes = new(_targetTypes);
        if (questData.TryGetValue(questType, out var dailyData))
        {
            targetTypes.Remove(dailyData.Objective.Goal);
        }

        return targetTypes[_random.Next(targetTypes.Count)];
    }
    static TargetType GetRandomQuestType()
    {
        List<TargetType> targetTypes = new(_targetTypes);
        TargetType targetType = targetTypes[_random.Next(targetTypes.Count)];

        return targetType;
    }
    static List<TargetType> GetRandomQuestTypes()
    {
        List<TargetType> targetTypes = new(_targetTypes);

        TargetType firstGoal = targetTypes[_random.Next(targetTypes.Count)];
        targetTypes.Remove(firstGoal);

        TargetType secondGoal = targetTypes[_random.Next(targetTypes.Count)];

        return [firstGoal, secondGoal];
    }
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        Entity died = deathEvent.Target;
        PrefabGUID target = died.ReadRO<PrefabGUID>();
        HashSet<Entity> participants = deathEvent.DeathParticipants;

        foreach (Entity player in participants)
        {
            User user = player.GetUser();
            ulong steamId = player.GetSteamId(); // participants are character entities

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

                    Core.StartCoroutine(DelayedProgressUpdate(questData, quest, user, steamId, colorType));
                }
                else
                {
                    _questCoroutines[steamId][quest.Key] = (questData[quest.Key].Progress, true);
                }

                if (quest.Value.Objective.RequiredAmount <= questData[quest.Key].Progress && !quest.Value.Objective.Complete)
                {
                    quest.Value.Objective.Complete = true;

                    LocalizationService.HandleServerReply(EntityManager, user, $"{colorType} complete!");
                    if (QuestRewards.Any())
                    {
                        HandleItemReward(user, quest.Key, quest.Value.Objective, colorType);
                        HandleExperienceReward(user, quest.Key);
                    }
                    else
                    {
                        HandleExperienceReward(user, quest.Key);
                    }

                    if (quest.Key == QuestType.Daily && _infiniteDailies)
                    {
                        int level = (_leveling && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)user.LocalCharacter._Entity.ReadRO<Equipment>().GetFullLevel();
                        TargetType goal = GetRandomQuestType();

                        HashSet<PrefabGUID> targets = GetGoalPrefabsForLevel(goal, level);
                        questData[QuestType.Daily] = (GenerateQuestObjective(goal, targets, QuestType.Daily), 0, DateTime.UtcNow);

                        var dailyQuest = questData[QuestType.Daily];
                        LocalizationService.HandleServerReply(EntityManager, user, $"New <color=#00FFFF>Daily Quest</color> available: <color=green>{dailyQuest.Objective.Goal}</color> <color=white>{dailyQuest.Objective.Target.GetLocalizedName()}</color>x<color=#FFC0CB>{dailyQuest.Objective.RequiredAmount}</color> [<color=white>{dailyQuest.Progress}</color>/<color=yellow>{dailyQuest.Objective.RequiredAmount}</color>]");
                    }
                }
            }
        }

        if (updated) steamId.SetPlayerQuests(questData);
    }
    static void HandleItemReward(User user, QuestType questType, QuestObjective objective, string colorType)
    {
        PrefabGUID reward = QuestRewards.Keys.ElementAt(_random.Next(QuestRewards.Count));
        int quantity = QuestRewards[reward];

        if (questType == QuestType.Weekly) quantity *= _questMultipliers[questType];

        if (objective.Target.GetPrefabName().ToLower().Contains("vblood")) quantity *= VBLOOD_FACTOR;

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
        float gainedXP = 0f;

        ulong steamId = user.PlatformId;
        Entity character = user.LocalCharacter.GetEntityOnServer();

        int currentLevel = steamId.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;

        // If not at max player level, just give player XP
        if (currentLevel < _maxPlayerLevel)
        {
            gainedXP = ConvertLevelToXp(currentLevel) * percentOfTotalXP * multiplier;
            progressString = GainPlayerExperience(character, steamId, gainedXP);

            return progressString;
        }

        // If at max player level, we start distributing XP to other systems
        // depending on which systems are enabled and which ones are at max.
        KeyValuePair<int, float> expertiseData = new(0, 0);
        int expertiseLevel = 0;

        KeyValuePair<int, float> legacyData = new(0, 0);
        int legacyLevel = 0;

        if (_weaponBloodSystems)
        {
            // Get current weapon and blood handlers
            Expertise.WeaponType weaponType = WeaponManager.GetCurrentWeaponType(character);
            IWeaponHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            BloodType bloodType = BloodManager.GetCurrentBloodType(character);
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
                progressString = TryGainFamiliarExperience(character, steamId, percentOfTotalXP, multiplier);
                return progressString;
            }

            // If expertise is maxed but legacy is not, give legacy XP
            if (maxExpertise && !maxLegacy && bloodHandler != null)
            {
                gainedXP = ConvertLevelToXp(legacyLevel) * percentOfTotalXP * multiplier;
                progressString = GainLegacyExperience(user, steamId, bloodType, bloodHandler, gainedXP);
                return progressString;
            }

            // If legacy is maxed but expertise is not, give expertise XP
            if (!maxExpertise && maxLegacy && expertiseHandler != null)
            {
                gainedXP = ConvertLevelToXp(expertiseLevel) * percentOfTotalXP * multiplier;
                progressString = GainWeaponExperience(user, steamId, weaponType, expertiseHandler, gainedXP);
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
                    expertiseString = GainWeaponExperience(user, steamId, weaponType, expertiseHandler, gainedXP);
                }

                if (bloodHandler != null)
                {
                    gainedXP = ConvertLevelToXp(legacyLevel) * percentOfTotalXP * multiplier;
                    legacyString = GainLegacyExperience(user, steamId, bloodType, bloodHandler, gainedXP);
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
            // If only Expertise is enabled
            Expertise.WeaponType weaponType = WeaponManager.GetCurrentWeaponType(character);
            IWeaponHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler != null)
            {
                expertiseData = expertiseHandler.GetExpertiseData(steamId);
                expertiseLevel = expertiseData.Key;

                gainedXP = ConvertLevelToXp(expertiseLevel) * percentOfTotalXP * multiplier;
                progressString = GainWeaponExperience(user, steamId, weaponType, expertiseHandler, gainedXP);
                return progressString;
            }
        }
        else if (_legacy)
        {
            // If only Legacy is enabled
            BloodType bloodType = BloodManager.GetCurrentBloodType(character);
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyData = bloodHandler.GetLegacyData(steamId);
                legacyLevel = legacyData.Key;

                gainedXP = ConvertLevelToXp(legacyLevel) * percentOfTotalXP * multiplier;
                progressString = GainLegacyExperience(user, steamId, bloodType, bloodHandler, gainedXP);
                return progressString;
            }
        }

        return progressString;
    }
    static string GainPlayerExperience(Entity character, ulong steamId, float gainedXP)
    {
        LevelingSystem.SaveLevelingExperience(steamId, gainedXP, out bool leveledUp, out int newLevel);
        LevelingSystem.NotifyPlayer(character, steamId, gainedXP, leveledUp, newLevel);
        return "<color=#FFC0CB>experience</color>";
    }
    static string GainWeaponExperience(User user, ulong steamId, Expertise.WeaponType weaponType, IWeaponHandler handler, float gainedXP)
    {
        WeaponSystem.SaveWeaponExperience(steamId, handler, gainedXP, out bool leveledUp, out int newLevel);
        WeaponSystem.NotifyPlayer(user, weaponType, gainedXP, leveledUp, newLevel, handler);
        return "<color=#FFC0CB>expertise</color>";
    }
    static string GainLegacyExperience(User user, ulong steamId, BloodType bloodType, IBloodHandler handler, float gainedXP)
    {
        BloodSystem.SaveBloodExperience(steamId, handler, gainedXP, out bool leveledUp, out int newLevel);
        BloodSystem.NotifyPlayer(user, bloodType, gainedXP, leveledUp, newLevel, handler);
        return "<color=#FFC0CB>essence</color>";
    }
    static string GainFamiliarExperience(Entity character, Entity familiar, int familiarId, ulong steamId, KeyValuePair<int, float> xpData, float gainedXP, int currentLevel)
    {
        FamiliarLevelingSystem.UpdateFamiliarExperience(character, familiar, familiarId, steamId, xpData, gainedXP, currentLevel);
        return "<color=#FFC0CB>familiar experience</color>";
    }
    static string TryGainFamiliarExperience(Entity character, ulong steamId, float percentOfTotalXP, int multiplier)
    {
        Entity familiar = Utilities.Familiars.FindPlayerFamiliar(character);

        if (familiar.TryGetComponent(out PrefabGUID prefabGUID) && !familiar.IsDisabled() && !familiar.HasBuff(_invulnerableBuff))
        {
            int familiarId = prefabGUID.GuidHash;

            var familiarXP = FamiliarLevelingSystem.GetFamiliarExperience(steamId, familiarId);
            int familiarLevel = familiarXP.Key;

            if (familiarLevel >= _maxFamiliarLevel) return string.Empty;

            float gainedXP = ConvertLevelToXp(familiarLevel) * percentOfTotalXP * multiplier;
            return GainFamiliarExperience(character, familiar, familiarId, steamId, familiarXP, gainedXP, familiarLevel);
        }

        return string.Empty;
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

        yield return _questMessageDelay;

        if (Misc.GetPlayerBool(steamId, "QuestLogging") && !quest.Value.Objective.Complete)
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
