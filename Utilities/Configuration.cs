using Bloodcraft.Commands;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using ProjectM;
using Stunlock.Core;

namespace Bloodcraft.Utilities;
internal static class Configuration
{
    internal static Func<IReadOnlyDictionary<ClassManager.PlayerClass, string>> ClassSpellsMapAccessor { get; set; } = () => Classes.ClassSpellsMap;

    public static void GetExcludedFamiliars()
    {
        List<PrefabGUID> unitBans = [..ParseIntegersFromString(ConfigService.BannedUnits).Select(unit => new PrefabGUID(unit))];

        foreach (PrefabGUID unit in unitBans)
        {
            if (unit.HasValue()) FamiliarUnlockSystem.ConfiguredPrefabGuidBans.Add(unit);
        }

        List<string> categoryBans = ConfigService.BannedTypes.Split(',').Select(s => s.Trim()).ToList();

        foreach (string category in categoryBans)
        {
            if (Enum.TryParse(category, out UnitCategory unitCategory))
            {
                FamiliarUnlockSystem.ConfiguredCategoryBans.Add(unitCategory);
            }
        }
    }
    public static List<int> ParseIntegersFromString(string configString)
    {
        if (string.IsNullOrWhiteSpace(configString))
        {
            return [];
        }

        List<int> results = [];

        foreach (string segment in configString.Split(','))
        {
            string trimmedSegment = segment.Trim();

            if (string.IsNullOrEmpty(trimmedSegment))
            {
                continue;
            }

            if (int.TryParse(trimmedSegment, out int value))
            {
                results.Add(value);
            }
        }

        return results;
    }
    public static List<T> ParseEnumsFromString<T>(string configString) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(configString))
            return [];

        List<T> result = [];

        foreach (var part in configString.Split(','))
        {
            if (Enum.TryParse<T>(part.Trim(), ignoreCase: true, out var value))
            {
                result.Add(value);
            }
        }

        return result;
    }

    public static void GetQuestRewardItems()
    {
        GetQuestRewardItems(
            ConfigService.QuestRewards,
            ConfigService.QuestRewardAmounts,
            QuestSystem.QuestRewards,
            LogConfigurationWarning);
    }

    internal static void GetQuestRewardItems(
        string questRewardsConfig,
        string questRewardAmountsConfig,
        IDictionary<PrefabGUID, int> destination,
        Action<string> logWarning = null)
    {
        ArgumentNullException.ThrowIfNull(destination);

        logWarning ??= LogConfigurationWarning;

        List<int> rewardAmounts = ParseIntegersFromString(questRewardAmountsConfig);
        List<PrefabGUID> questRewards = [..ParseIntegersFromString(questRewardsConfig).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (questRewards.Count != rewardAmounts.Count)
        {
            logWarning?.Invoke("QuestRewards and QuestRewardAmounts are not the same length, please correct this for predictable behavior when receiving quest rewards!");
        }

        int pairCount = Math.Min(questRewards.Count, rewardAmounts.Count);

        for (int i = 0; i < pairCount; i++)
        {
            PrefabGUID reward = questRewards[i];

            if (!destination.ContainsKey(reward))
            {
                destination[reward] = rewardAmounts[i];
            }
        }
    }

    public static void GetStarterKitItems()
    {
        GetStarterKitItems(
            ConfigService.KitPrefabs,
            ConfigService.KitQuantities,
            MiscCommands.StarterKitItemPrefabGUIDs,
            LogConfigurationWarning);
    }

    internal static void GetStarterKitItems(
        string kitPrefabsConfig,
        string kitQuantitiesConfig,
        IDictionary<PrefabGUID, int> destination,
        Action<string> logWarning = null)
    {
        ArgumentNullException.ThrowIfNull(destination);

        logWarning ??= LogConfigurationWarning;

        List<int> kitAmounts = ParseIntegersFromString(kitQuantitiesConfig);
        List<PrefabGUID> kitPrefabs = [..ParseIntegersFromString(kitPrefabsConfig).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (kitPrefabs.Count != kitAmounts.Count)
        {
            logWarning?.Invoke("KitPrefabs and KitQuantities are not the same length, please correct this for predictable behavior when using the kit command!");
        }

        int pairCount = Math.Min(kitPrefabs.Count, kitAmounts.Count);

        for (int i = 0; i < pairCount; i++)
        {
            PrefabGUID prefab = kitPrefabs[i];

            if (!destination.ContainsKey(prefab))
            {
                destination[prefab] = kitAmounts[i];
            }
        }
    }

    private static void LogConfigurationWarning(string message)
    {
        global::Bloodcraft.Plugin.Instance?.Log?.LogWarning(message);
    }

    public static void GetClassSpellCooldowns()
    {
        foreach (var keyValuePair in ClassSpellsMapAccessor())
        {
            List<PrefabGUID> spellPrefabs = [..ParseIntegersFromString(keyValuePair.Value).Select(x => new PrefabGUID(x))];

            foreach (PrefabGUID spell in spellPrefabs)
            {
                AbilityRunScriptsSystemPatch.AddClassSpell(spell, spellPrefabs.IndexOf(spell));
            }
        }
    }

    /*
    public static void InitializeClassPassiveBuffs()
    {
        foreach (var keyValuePair in Classes.ClassBuffMap)
        {
            HashSet<PrefabGUID> buffPrefabs = [..ParseIntegersFromString(keyValuePair.Value).Select(buffPrefab => new PrefabGUID(buffPrefab))];

            List<PrefabGUID> orderedBuffs = [..ParseIntegersFromString(keyValuePair.Value).Select(buffPrefab => new PrefabGUID(buffPrefab))];

            UpdateBuffsBufferDestroyPatch.ClassBuffsSet.TryAdd(keyValuePair.Key, buffPrefabs);
            UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered.Add(keyValuePair.Key, orderedBuffs);
        }
    }
    */
}
