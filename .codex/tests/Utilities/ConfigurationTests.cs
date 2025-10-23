using System;
using System.Collections.Generic;
using Bloodcraft.Interfaces;
using Bloodcraft.Utilities;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Utilities;

public sealed class ConfigurationTests : TestHost
{
    [Fact]
    public void ParseIntegersFromString_IgnoresInvalidTokens()
    {
        List<int> values = Configuration.ParseIntegersFromString("101, 202, not-a-number, 303, 202");

        Assert.Equal(new[] { 101, 202, 303, 202 }, values);
    }

    [Fact]
    public void ParseEnumsFromString_IgnoresInvalidEntries()
    {
        List<WeaponType> weapons = Configuration.ParseEnumsFromString<WeaponType>("Sword, Whip, invalid, Daggers");

        Assert.Equal(new[] { WeaponType.Sword, WeaponType.Whip, WeaponType.Daggers }, weapons);
    }

    [Fact]
    public void GetQuestRewardItems_PopulatesDictionaryAndSkipsDuplicates()
    {
        const string questRewardsConfig = "1001, 1002, 1002, 1003";
        const string questRewardAmountsConfig = "5, 10, 99, 20";

        Dictionary<PrefabGUID, int> questRewards = new();
        List<string> warnings = new();

        Configuration.GetQuestRewardItems(
            questRewardsConfig,
            questRewardAmountsConfig,
            questRewards,
            warnings.Add);

        Assert.Equal(3, questRewards.Count);
        Assert.Equal(5, questRewards[new PrefabGUID(1001)]);
        Assert.Equal(10, questRewards[new PrefabGUID(1002)]);
        Assert.Equal(20, questRewards[new PrefabGUID(1003)]);
        Assert.Empty(warnings);
    }

    [Fact]
    public void GetQuestRewardItems_LogsWarningWhenCountsMismatch()
    {
        const string questRewardsConfig = "2001";
        const string questRewardAmountsConfig = "7, 11";

        Dictionary<PrefabGUID, int> questRewards = new();
        List<string> warnings = new();

        Configuration.GetQuestRewardItems(
            questRewardsConfig,
            questRewardAmountsConfig,
            questRewards,
            warnings.Add);

        string warning = Assert.Single(warnings);
        Assert.Contains("QuestRewards and QuestRewardAmounts are not the same length", warning);
        Assert.Equal(7, questRewards[new PrefabGUID(2001)]);
    }

    [Fact]
    public void GetStarterKitItems_PopulatesDictionaryAndSkipsDuplicates()
    {
        const string kitPrefabsConfig = "3001, 3002, 3002";
        const string kitQuantitiesConfig = "3, 6, 9";

        Dictionary<PrefabGUID, int> starterKit = new();
        List<string> warnings = new();

        Configuration.GetStarterKitItems(
            kitPrefabsConfig,
            kitQuantitiesConfig,
            starterKit,
            warnings.Add);

        Assert.Equal(2, starterKit.Count);
        Assert.Equal(3, starterKit[new PrefabGUID(3001)]);
        Assert.Equal(6, starterKit[new PrefabGUID(3002)]);
        Assert.Empty(warnings);
    }

    [Fact]
    public void GetStarterKitItems_LogsWarningWhenCountsMismatch()
    {
        const string kitPrefabsConfig = "4001";
        const string kitQuantitiesConfig = "4, 8";

        Dictionary<PrefabGUID, int> starterKit = new();
        List<string> warnings = new();

        Configuration.GetStarterKitItems(
            kitPrefabsConfig,
            kitQuantitiesConfig,
            starterKit,
            warnings.Add);

        string warning = Assert.Single(warnings);
        Assert.Contains("KitPrefabs and KitQuantities are not the same length", warning);
        Assert.Equal(4, starterKit[new PrefabGUID(4001)]);
    }

}
