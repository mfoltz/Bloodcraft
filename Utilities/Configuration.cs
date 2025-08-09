using Bloodcraft.Commands;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Quests;
using ProjectM;
using Stunlock.Core;

namespace Bloodcraft.Utilities;
internal static class Configuration
{
    static PrefabCollectionSystem PrefabCollectionSystem => Core.SystemService.PrefabCollectionSystem;
    public static void GetExcludedFamiliars()
    {
        List<PrefabGUID> unitBans = ParsePrefabGuidsFromString(ConfigService.BannedUnits);

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

    public static List<PrefabGUID> ParsePrefabGuidsFromString(string configString)
    {
        if (string.IsNullOrWhiteSpace(configString))
            return [];

        List<PrefabGUID> result = [];

        foreach (string part in configString.Split(','))
        {
            string token = part.Trim();
            if (string.IsNullOrEmpty(token)) continue;

            if (int.TryParse(token, out int hash))
            {
                result.Add(new PrefabGUID(hash));
                continue;
            }

            PrefabGUID prefabGuid;
            bool resolved = false;

            if (PrefabCollectionSystem.SpawnableNameToPrefabGuidDictionary.TryGetValue(token, out prefabGuid))
            {
                result.Add(prefabGuid);
                continue;
            }

            foreach (var kvp in PrefabCollectionSystem.SpawnableNameToPrefabGuidDictionary)
            {
                if (kvp.Key.Equals(token, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(kvp.Value);
                    resolved = true;
                    break;
                }
            }

            if (!resolved)
            {
                foreach (var kvp in LocalizationService.PrefabGuidNames)
                {
                    if (kvp.Value.Equals(token, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(kvp.Key);
                        resolved = true;
                        break;
                    }
                }
            }

            if (!resolved)
            {
                Core.Log.LogWarning($"Unable to resolve prefab GUID from '{token}'.");
            }
        }

        return result;
    }
    public static List<int> ParseIntegersFromString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }

        return [..configString.Split(',').Select(int.Parse)];
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
        List<int> rewardAmounts = [..ParseIntegersFromString(ConfigService.QuestRewardAmounts)];
        List<PrefabGUID> questRewards = [..ParseIntegersFromString(ConfigService.QuestRewards).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (questRewards.Count != rewardAmounts.Count)
        {
            Core.Log.LogWarning("QuestRewards and QuestRewardAmounts are not the same length, please correct this for predictable behavior when receiving quest rewards!");
        }

        for (int i = 0; i < questRewards.Count; i++)
        {
            QuestSystem.QuestRewards.TryAdd(questRewards[i], rewardAmounts[i]);
        }
    }
    public static void GetStarterKitItems()
    {
        List<int> kitAmounts = [..ParseIntegersFromString(ConfigService.KitQuantities)];
        List<PrefabGUID> kitPrefabs = [..ParseIntegersFromString(ConfigService.KitPrefabs).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (kitPrefabs.Count != kitAmounts.Count)
        {
            Core.Log.LogWarning("KitPrefabs and KitQuantities are not the same length, please correct this for predictable behavior when using the kit command!");
        }

        for (int i = 0; i < kitPrefabs.Count; i++)
        {
            MiscCommands.StarterKitItemPrefabGUIDs.TryAdd(kitPrefabs[i], kitAmounts[i]);
        }
    }
    public static void GetClassSpellCooldowns()
    {
        foreach (var keyValuePair in Classes.ClassSpellsMap)
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
