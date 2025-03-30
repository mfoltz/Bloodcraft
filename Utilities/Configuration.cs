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
    public static void InitializeBannedFamiliarUnits()
    {
        List<PrefabGUID> unitBans = [..ParseConfigIntegerString(ConfigService.BannedUnits).Select(unit => new PrefabGUID(unit))];

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
    public static List<int> ParseConfigIntegerString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }

        return [..configString.Split(',').Select(int.Parse)];
    }
    public static void InitializeQuestRewardItems()
    {
        List<int> rewardAmounts = [..ParseConfigIntegerString(ConfigService.QuestRewardAmounts)];
        List<PrefabGUID> questRewards = [..ParseConfigIntegerString(ConfigService.QuestRewards).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (questRewards.Count != rewardAmounts.Count)
        {
            Core.Log.LogWarning("QuestRewards and QuestRewardAmounts are not the same length, please correct this for predictable behavior when receiving quest rewards!");
        }

        for (int i = 0; i < questRewards.Count; i++)
        {
            QuestSystem.QuestRewards.TryAdd(questRewards[i], rewardAmounts[i]);
        }
    }
    public static void InitializeStarterKitItems()
    {
        List<int> kitAmounts = [..ParseConfigIntegerString(ConfigService.KitQuantities)];
        List<PrefabGUID> kitPrefabs = [..ParseConfigIntegerString(ConfigService.KitPrefabs).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (kitPrefabs.Count != kitAmounts.Count)
        {
            Core.Log.LogWarning("KitPrefabs and KitQuantities are not the same length, please correct this for predictable behavior when using the kit command!");
        }

        for (int i = 0; i < kitPrefabs.Count; i++)
        {
            MiscCommands.StarterKitItemPrefabGUIDs.TryAdd(kitPrefabs[i], kitAmounts[i]);
        }
    }
    public static void InitializeClassSpellCooldowns()
    {
        foreach (var keyValuePair in Classes.ClassSpellsMap)
        {
            List<PrefabGUID> spellPrefabs = [..ParseConfigIntegerString(keyValuePair.Value).Select(x => new PrefabGUID(x))];

            foreach (PrefabGUID spell in spellPrefabs)
            {
                AbilityRunScriptsSystemPatch.ClassSpells.TryAdd(spell, spellPrefabs.IndexOf(spell));
            }
        }
    }
    public static void InitializeClassPassiveBuffs()
    {
        foreach (var keyValuePair in Classes.ClassBuffMap)
        {
            HashSet<PrefabGUID> buffPrefabs = [..ParseConfigIntegerString(keyValuePair.Value).Select(buffPrefab => new PrefabGUID(buffPrefab))];

            List<PrefabGUID> orderedBuffs = [..ParseConfigIntegerString(keyValuePair.Value).Select(buffPrefab => new PrefabGUID(buffPrefab))];

            UpdateBuffsBufferDestroyPatch.ClassBuffsSet.TryAdd(keyValuePair.Key, buffPrefabs);
            UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered.Add(keyValuePair.Key, orderedBuffs);
        }
    }
}
