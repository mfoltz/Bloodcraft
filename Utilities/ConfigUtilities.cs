﻿using Bloodcraft.Commands;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using Stunlock.Core;

namespace Bloodcraft.Utilities;

internal static class ConfigUtilities
{
    public static void FamiliarBans()
    {
        List<int> unitBans = ParseConfigString(ConfigService.BannedUnits);
        List<string> typeBans = ConfigService.BannedTypes.Split(',').Select(s => s.Trim()).ToList();
        if (unitBans.Count > 0) FamiliarUnlockSystem.ExemptPrefabs = unitBans;
        if (typeBans.Count > 0) FamiliarUnlockSystem.ExemptTypes = typeBans;
    }
    public static List<int> ParseConfigString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }
        return configString.Split(',').Select(int.Parse).ToList();
    }
    public static void QuestRewards()
    {
        List<PrefabGUID> questRewards = ParseConfigString(ConfigService.QuestRewards).Select(x => new PrefabGUID(x)).ToList();
        List<int> rewardAmounts = [.. ParseConfigString(ConfigService.QuestRewardAmounts)];
        QuestSystem.QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToDictionary(x => x.reward, x => x.amount);
    }
    public static void StarterKit()
    {
        List<PrefabGUID> kitPrefabs = ParseConfigString(ConfigService.KitPrefabs).Select(x => new PrefabGUID(x)).ToList();
        List<int> kitAmounts = [.. ParseConfigString(ConfigService.KitQuantities)];
        MiscCommands.KitPrefabs = kitPrefabs.Zip(kitAmounts, (item, amount) => new { item, amount }).ToDictionary(x => x.item, x => x.amount);
    }
    public static void ClassSpellCooldownMap()
    {
        foreach (LevelingSystem.PlayerClass playerClass in Enum.GetValues(typeof(LevelingSystem.PlayerClass)))
        {
            if (!string.IsNullOrEmpty(LevelingSystem.ClassSpellsMap[playerClass])) ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]).Select((x, index) => new { Hash = x, Index = index }).ToList().ForEach(x => AbilityRunScriptsSystemPatch.ClassSpells.TryAdd(x.Hash, x.Index));
        }
    }
    public static void ClassBuffMap()
    {
        foreach (var kvp in LevelingSystem.ClassBuffMap)
        {
            List<PrefabGUID> buffPrefabs = ParseConfigString(kvp.Value).Select(x => new PrefabGUID(x)).ToList();
            UpdateBuffsBufferDestroyPatch.ClassBuffs.TryAdd(kvp.Key, buffPrefabs);
        }
    }
}
