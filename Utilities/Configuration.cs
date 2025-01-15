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
    public static void FamiliarBans()
    {
        List<PrefabGUID> unitBans = ParseConfigIntegerString(ConfigService.BannedUnits)
            .Select(unit => new PrefabGUID(unit))
            .ToList();

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

        return configString.Split(',').Select(int.Parse).ToList();
    }
    public static void QuestRewardItems()
    {
        List<int> rewardAmounts = [.. ParseConfigIntegerString(ConfigService.QuestRewardAmounts)];
        List<PrefabGUID> questRewards = ParseConfigIntegerString(ConfigService.QuestRewards)
            .Select(itemPrefab => new PrefabGUID(itemPrefab))
            .ToList();

        if (questRewards.Count != rewardAmounts.Count)
        {
            Core.Log.LogWarning("QuestRewards and QuestRewardAmounts are not the same length, please correct this for proper behaviour when receiving quest rewards!");
        }

        for (int i = 0; i < questRewards.Count; i++)
        {
            QuestSystem.QuestRewards.TryAdd(questRewards[i], rewardAmounts[i]);
        }
    }
    public static void StarterKitItems()
    {
        List<int> kitAmounts = [.. ParseConfigIntegerString(ConfigService.KitQuantities)];
        List<PrefabGUID> kitPrefabs = ParseConfigIntegerString(ConfigService.KitPrefabs)
            .Select(itemPrefab => new PrefabGUID(itemPrefab))
            .ToList();

        if (kitPrefabs.Count != kitAmounts.Count)
        {
            Core.Log.LogWarning("KitPrefabs and KitQuantities are not the same length, please correct this for proper behaviour when using the kit command!");
        }

        for (int i = 0; i < kitPrefabs.Count; i++)
        {
            MiscCommands.StarterKitItemPrefabGUIDs.TryAdd(kitPrefabs[i], kitAmounts[i]);
        }
    }
    public static void ClassSpellCooldownMap()
    {
        foreach (var keyValuePair in Classes.ClassSpellsMap)
        {
            List<PrefabGUID> spellPrefabs = ParseConfigIntegerString(keyValuePair.Value)
                .Select(x => new PrefabGUID(x))
                .ToList();

            foreach (PrefabGUID spell in spellPrefabs)
            {
                AbilityRunScriptsSystemPatch.ClassSpells.TryAdd(spell, spellPrefabs.IndexOf(spell));
            }
        }
    }
    public static void ClassPassiveBuffsMap()
    {
        foreach (var keyValuePair in Classes.ClassBuffMap)
        {
            List<PrefabGUID> buffPrefabs = ParseConfigIntegerString(keyValuePair.Value)
                .Select(buffPrefab => new PrefabGUID(buffPrefab))
                .ToList();

            UpdateBuffsBufferDestroyPatch.ClassBuffs.TryAdd(keyValuePair.Key, buffPrefabs);
        }
    }
}
