using BepInEx.Unity.IL2CPP.Utils.Collections;
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
    public static HashSet<Entity> UnitEntities = [];
    public static HashSet<PrefabGUID> UnitPrefabs = [];
    public static HashSet<Entity> CraftEntities = [];
    public static HashSet<PrefabGUID> CraftPrefabs = [];
    public static HashSet<Entity> GatherEntities = [];
    public static HashSet<PrefabGUID> GatherPrefabs = [];
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

    public static Dictionary<QuestType, DateTime> QuestResets = new()
    {
        { QuestType.Daily, GetNextDailyReset() },
        { QuestType.Weekly, GetNextWeeklyReset() }
    };
    public static DateTime GetNextDailyReset()
    {
        DateTime now = DateTime.UtcNow;
        DateTime nextMidnight = now.Date.AddDays(1); // Get the next midnight, which is now.Date + 1 day at 00:00
        return nextMidnight;
    }
    public static DateTime GetNextWeeklyReset()
    {
        DateTime now = DateTime.UtcNow;
        // Calculate the number of days until the next Sunday and set the time to midnight
        int daysUntilNextSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        DateTime nextSundayMidnight = now.Date.AddDays(daysUntilNextSunday == 0 ? 7 : daysUntilNextSunday); // If today is Sunday, set to next Sunday
        return nextSundayMidnight;
    }
    public static Random RandomDailySeed()
    {
        DateTime now = DateTime.UtcNow;
        int seed = now.Year * 10000 + now.Month * 100 + now.Day;
        return new Random(seed);
    }
    public static Random RandomWeeklySeed()
    {
        DateTime now = DateTime.UtcNow;
        int seed = now.Year * 100 + now.Month;
        return new Random(seed);
    }
    static HashSet<PrefabGUID> GetKillPrefabsForLevel(int playerLevel)
    {
        HashSet<PrefabGUID> prefabs = [];
        foreach (PrefabGUID prefab in UnitPrefabs)
        {
            Entity prefabEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefab];
            //if (prefabEntity == Entity.Null || !EntityManager.Exists(prefabEntity)) continue;
            //Core.Log.LogInfo($"Checking {prefabEntity.Read<PrefabGUID>().GuidHash}...");
            PrefabGUID prefabGUID = prefabEntity.Read<PrefabGUID>();
            if (prefabs.Contains(prefabGUID) || prefabGUID.LookupName().Contains("Trader")) continue;
            if (!prefabEntity.Has<UnitLevel>() || !prefabEntity.Has<EntityCategory>()) continue;
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
                    if (nameCheck.Contains("T01_Bone") || nameCheck.Contains("Item_Cloak")) continue;
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
    static QuestObjective GenerateQuestObjective(Random random, int level, QuestType questType)
    {
        QuestGoal goal = (QuestGoal)random.Next(Enum.GetValues(typeof(QuestGoal)).Length);

        if (!Plugin.ProfessionSystem.Value && goal == QuestGoal.Craft) goal = QuestGoal.Kill;

        HashSet<PrefabGUID> targets;
        PrefabGUID target;
        int requiredAmount;
        //Core.Log.LogInfo($"Generating {questType} quest with goal {goal}...");
        switch (goal)
        {
            case QuestGoal.Kill:
                targets = GetKillPrefabsForLevel(level);
                target = targets.ElementAt(random.Next(targets.Count));
                if (questType.Equals(QuestType.Weekly)) requiredAmount = target.LookupName().ToLower().Contains("vblood") ? random.Next(5, 10) * 2 : random.Next(5, 10) * QuestMultipliers[questType];
                else requiredAmount = random.Next(5, 10) * QuestMultipliers[questType];
                break;
            case QuestGoal.Craft:
                targets = GetCraftPrefabsForLevel(level);
                if (targets.Count == 0) // fallback to kill quest if none found for lower level players
                {
                    goal = QuestGoal.Kill;
                    targets = GetKillPrefabsForLevel(level);
                    target = targets.ElementAt(random.Next(targets.Count));
                    if (questType.Equals(QuestType.Weekly)) requiredAmount = target.LookupName().ToLower().Contains("vblood") ? random.Next(5, 10) * 2 : random.Next(5, 10) * QuestMultipliers[questType];
                    else requiredAmount = random.Next(5, 10) * QuestMultipliers[questType];
                    break;
                }
                target = targets.ElementAt(random.Next(targets.Count));
                requiredAmount = random.Next(10, 15) * QuestMultipliers[questType];
                break;
            case QuestGoal.Gather:
                targets = GetGatherPrefabsForLevel(level);
                target = targets.ElementAt(random.Next(targets.Count));
                List<int> amounts = [500, 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000];
                requiredAmount = amounts.ElementAt(random.Next(amounts.Count)) * QuestMultipliers[questType];
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return new QuestObjective { Goal = goal, Target = target, RequiredAmount = requiredAmount };
    }
    public static void InitializePlayerQuests(ulong steamId, int level)
    {
        Random dailyRandom = RandomDailySeed();
        Random weeklyRandom = RandomWeeklySeed();
        Core.DataStructures.PlayerQuests.Add(steamId, new Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)>
            {
                { QuestType.Daily, (GenerateQuestObjective(dailyRandom, level, QuestType.Daily), 0, DateTime.Now) },
                { QuestType.Weekly, (GenerateQuestObjective(weeklyRandom, level, QuestType.Weekly), 0, DateTime.Now) }
            });
        Core.DataStructures.SavePlayerQuests();
    }
    public static void RefreshQuests(ulong steamId, int level)
    {
        var playerQuestData = Core.DataStructures.PlayerQuests[steamId];

        bool refreshDaily = playerQuestData[QuestType.Daily].LastReset > GetNextDailyReset();
        bool refreshWeekly = playerQuestData[QuestType.Weekly].LastReset > GetNextWeeklyReset();

        if (refreshDaily || refreshWeekly)
        {
            if (refreshDaily)
            {
                Random dailyRandom = RandomDailySeed();
                playerQuestData[QuestType.Daily] = (GenerateQuestObjective(dailyRandom, level, QuestType.Daily), 0, DateTime.Now);
            }

            if (refreshWeekly)
            {
                Random weeklyRandom = RandomWeeklySeed();
                playerQuestData[QuestType.Weekly] = (GenerateQuestObjective(weeklyRandom, level, QuestType.Weekly), 0, DateTime.Now);
            }

            Core.DataStructures.PlayerQuests[steamId] = playerQuestData;
            Core.DataStructures.SavePlayerQuests();
        }
    }
    public static void ForceRefresh(ulong steamId, int level)
    {
        var playerQuestData = Core.DataStructures.PlayerQuests[steamId];

        Random dailyRandom = new(Random.Next(100));
        playerQuestData[QuestType.Daily] = (GenerateQuestObjective(dailyRandom, level, QuestType.Daily), 0, DateTime.Now);

        Random weeklyRandom = new(Random.Next(100));
        playerQuestData[QuestType.Weekly] = (GenerateQuestObjective(weeklyRandom, level, QuestType.Weekly), 0, DateTime.Now);

        Core.DataStructures.PlayerQuests[steamId] = playerQuestData;
        Core.DataStructures.SavePlayerQuests();
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
                    
                    LocalizationService.HandleServerReply(EntityManager, user, $"{colorType} complete.");
                    if (QuestRewards.Count > 0)
                    {
                        PrefabGUID reward = QuestRewards.Keys.ElementAt(randomReward.Next(QuestRewards.Count));
                        int quantity = QuestRewards[reward];
                        if (quest.Key == QuestType.Weekly) quantity *= QuestMultipliers[quest.Key];
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
                            string xpMessage = $"Additionally, you've been awarded <color=yellow>{(0.10f * QuestMultipliers[quest.Key] * 100).ToString("F0") + "%"}</color> of your total <color=#FFC0CB>experience</color>.";
                            LocalizationService.HandleServerReply(EntityManager, user, xpMessage);
                        }
                    }
                    else
                    {
                        LocalizationService.HandleServerReply(EntityManager, user, $"Couldn't find any valid reward prefabs...");
                    }
                }
            }
        }
        Core.DataStructures.SavePlayerQuests();
    }
}
