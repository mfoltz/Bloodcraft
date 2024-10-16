using Bloodcraft.Commands;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using ProjectM;
using Stunlock.Core;

namespace Bloodcraft.Utilities;
internal static class ConfigUtilities
{
    public static void FamiliarBans()
    {
        List<PrefabGUID> unitBans = ParseConfigIntegerString(ConfigService.BannedUnits)
            .Select(unit => new PrefabGUID(unit))
            .ToList();

        foreach (PrefabGUID unit in unitBans)
        {
            if (unit.HasValue()) FamiliarUnlockSystem.ExemptPrefabGUIDs.Add(unit);
        }

        List<string> categoryBans = ConfigService.BannedTypes.Split(',').Select(s => s.Trim()).ToList();

        foreach (string category in categoryBans)
        {
            if (Enum.TryParse(category, out UnitCategory unitCategory))
            {
                FamiliarUnlockSystem.ExemptCategories.Add(unitCategory);
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
        List<int> rewardAmounts = [..ParseConfigIntegerString(ConfigService.QuestRewardAmounts)];
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

        //QuestSystem.QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToList().ForEach(x => QuestSystem.QuestRewards.TryAdd(x.reward, x.amount)); implementation pending removal before changing QuestRewards to readonly
    }
    public static void StarterKitItems()
    {
        List<int> kitAmounts = [..ParseConfigIntegerString(ConfigService.KitQuantities)];
        List<PrefabGUID> kitPrefabs = ParseConfigIntegerString(ConfigService.KitPrefabs)
            .Select(itemPrefab => new PrefabGUID(itemPrefab))
            .ToList();

        if (kitPrefabs.Count != kitAmounts.Count)
        {
            Core.Log.LogWarning("KitPrefabs and KitQuantities are not the same length, please correct this for proper behaviour when using the kit command!");
        }

        for (int i = 0; i < kitPrefabs.Count; i++)
        {
            MiscCommands.KitPrefabs.TryAdd(kitPrefabs[i], kitAmounts[i]);
        }

        //MiscCommands.KitPrefabs = kitPrefabs.Zip(kitAmounts, (item, amount) => new { item, amount }).ToDictionary(x => x.item, x => x.amount); implementation pending removal before changing KitPrefabs to readonly
    }
    public static void ClassSpellCooldownMap()
    {
        foreach (var keyValuePair in LevelingSystem.ClassSpellsMap)
        {
            List<PrefabGUID> spellPrefabs = ParseConfigIntegerString(keyValuePair.Value)
                .Select(x => new PrefabGUID(x))
                .ToList();

            foreach (PrefabGUID spell in spellPrefabs)
            {
                AbilityRunScriptsSystemPatch.ClassSpells.TryAdd(spell, spellPrefabs.IndexOf(spell));
            }
        }

        /* implementation pending removal before changing ClassSpells to readonly Dictionary<PrefabGUID, int> from Dictionary<int, int>
        foreach (LevelingSystem.PlayerClass playerClass in Enum.GetValues(typeof(LevelingSystem.PlayerClass)))
        {
            if (LevelingSystem.ClassSpellsMap.TryGetValue(playerClass, out string classSpells))
            {
                ParseConfigString(classSpells)
                    .Select((x, index) => new { Hash = x, Index = index })
                    .ToList()
                    .ForEach(x => AbilityRunScriptsSystemPatch.ClassSpells.TryAdd(x.Hash, x.Index));
            }    
        }
        */
    }
    public static void ClassPassiveBuffsMap()
    {
        foreach (var keyValuePair in LevelingSystem.ClassBuffMap)
        {
            List<PrefabGUID> buffPrefabs = ParseConfigIntegerString(keyValuePair.Value)
                .Select(buffPrefab => new PrefabGUID(buffPrefab))
                .ToList();

            UpdateBuffsBufferDestroyPatch.ClassBuffs.TryAdd(keyValuePair.Key, buffPrefabs);
        }
    }
}
