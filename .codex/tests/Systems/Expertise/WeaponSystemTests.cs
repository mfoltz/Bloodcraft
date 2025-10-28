using System;
using System.Collections.Generic;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Utilities;
using Bloodcraft.Tests.Support;
using Xunit;

namespace Bloodcraft.Tests.Systems.Expertise;

[Collection(UnityRuntimeTestCollection.CollectionName)]
public class WeaponSystemTests
{
    const ulong TestSteamId = 76561198000000050UL;

    private sealed class StubWeaponExpertise : IWeaponExpertise
    {
        readonly Dictionary<ulong, KeyValuePair<int, float>> storage = new();
        readonly WeaponType weaponType;

        public StubWeaponExpertise(WeaponType weaponType = WeaponType.Sword)
        {
            this.weaponType = weaponType;
        }

        public void Seed(ulong steamId, int level, float xp)
        {
            storage[steamId] = new KeyValuePair<int, float>(level, xp);
        }

        public KeyValuePair<int, float> GetExpertiseData(ulong steamId)
        {
            return storage.TryGetValue(steamId, out var value) ? value : new KeyValuePair<int, float>(0, 0f);
        }

        public void SetExpertiseData(ulong steamId, KeyValuePair<int, float> xpData)
        {
            storage[steamId] = xpData;
        }

        public WeaponType GetWeaponType()
        {
            return weaponType;
        }
    }

    [Fact]
    public void SaveExpertiseExperience_RaisesLevelWhenCrossingThreshold()
    {
        var expertise = new StubWeaponExpertise();
        const int startingLevel = 5;
        float xpBefore = Progression.ConvertLevelToXp(startingLevel + 1) - 10f;
        expertise.Seed(TestSteamId, startingLevel, xpBefore);

        const float gainedXp = 15f;

        WeaponSystem.SaveExpertiseExperience(TestSteamId, expertise, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> stored = expertise.GetExpertiseData(TestSteamId);

        Assert.True(leveledUp);
        Assert.Equal(startingLevel + 1, newLevel);
        Assert.Equal(startingLevel + 1, stored.Key);
        Assert.Equal(xpBefore + gainedXp, stored.Value);
    }

    [Fact]
    public void SaveExpertiseExperience_ClampsExperienceAtMaximumLevel()
    {
        var expertise = new StubWeaponExpertise();
        int maxLevel = ConfigService.MaxExpertiseLevel;
        int startingLevel = maxLevel - 1;
        float startingXp = Progression.ConvertLevelToXp(startingLevel);
        expertise.Seed(TestSteamId, startingLevel, startingXp);

        float targetXp = Progression.ConvertLevelToXp(maxLevel + 5);
        float gainedXp = targetXp - startingXp;

        WeaponSystem.SaveExpertiseExperience(TestSteamId, expertise, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> stored = expertise.GetExpertiseData(TestSteamId);
        float maxLevelXp = Progression.ConvertLevelToXp(maxLevel);

        Assert.True(leveledUp);
        Assert.Equal(maxLevel, newLevel);
        Assert.Equal(maxLevel, stored.Key);
        Assert.Equal(maxLevelXp, stored.Value);
    }

    [Fact]
    public void GetLevelProgress_UsesCurrentAndNextLevelXpThresholds()
    {
        var expertise = new StubWeaponExpertise();
        const int currentLevel = 10;
        int currentLevelXp = Progression.ConvertLevelToXp(currentLevel);
        int nextLevelXp = Progression.ConvertLevelToXp(currentLevel + 1);
        double neededXp = nextLevelXp - currentLevelXp;
        double earnedTowardsNext = neededXp * 0.25;
        float currentXp = currentLevelXp + (float)earnedTowardsNext;

        expertise.Seed(TestSteamId, currentLevel, currentXp);

        int progress = WeaponSystem.GetLevelProgress(TestSteamId, expertise);

        double progressFraction = (currentXp - currentLevelXp) / neededXp;
        int expected = (int)Math.Floor(progressFraction * 100);

        Assert.Equal(expected, progress);
    }
}
