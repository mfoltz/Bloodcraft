using System;
using System.Collections.Generic;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Xunit;

namespace Bloodcraft.Tests.Systems.Leveling;

public class LevelingSystemTests : IClassFixture<LevelingSystemTests.Fixture>
{
    const ulong TestSteamId = 76561198000000042UL;

    public class Fixture : IDisposable
    {
        readonly IDisposable persistenceScope;

        public Fixture()
        {
            persistenceScope = DataService.SuppressPersistence();
            LevelingSystem.EnablePrefabEffects = false;
        }

        public void Dispose()
        {
            LevelingSystem.EnablePrefabEffects = true;
            persistenceScope.Dispose();
        }
    }

    static void ResetPlayerData()
    {
        DataService.PlayerDictionaries._playerExperience.Clear();
        DataService.PlayerDictionaries._playerRestedXP.Clear();
    }

    static int ConvertLevelToXp(int level)
    {
        const double ExpConstant = 0.1;
        const double ExpPower = 2d;

        return (int)Math.Pow(level / ExpConstant, ExpPower);
    }

    [Fact]
    public void SaveLevelingExperience_RaisesLevelWhenCrossingBoundary()
    {
        ResetPlayerData();

        const int startingLevel = 1;
        float xpBefore = ConvertLevelToXp(startingLevel + 1) - 10;
        DataService.PlayerDictionaries._playerExperience[TestSteamId] = new KeyValuePair<int, float>(startingLevel, xpBefore);

        const float gainedXp = 20f;

        LevelingSystem.SaveLevelingExperience(TestSteamId, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> storedData = DataService.PlayerDictionaries._playerExperience[TestSteamId];

        Assert.True(leveledUp);
        Assert.Equal(startingLevel + 1, newLevel);
        Assert.Equal(startingLevel + 1, storedData.Key);
        Assert.Equal(xpBefore + gainedXp, storedData.Value);
    }

    [Fact]
    public void SaveLevelingExperience_CapsExperienceAtConfiguredMaximum()
    {
        ResetPlayerData();

        int maxLevel = ConfigService.MaxLevel;
        int startingLevel = maxLevel - 1;
        float startingXp = ConvertLevelToXp(startingLevel);
        DataService.PlayerDictionaries._playerExperience[TestSteamId] = new KeyValuePair<int, float>(startingLevel, startingXp);

        float targetXp = ConvertLevelToXp(maxLevel + 5);
        float gainedXp = targetXp - startingXp;

        LevelingSystem.SaveLevelingExperience(TestSteamId, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> storedData = DataService.PlayerDictionaries._playerExperience[TestSteamId];
        float maxLevelXp = ConvertLevelToXp(maxLevel);

        Assert.True(leveledUp);
        Assert.Equal(maxLevel, newLevel);
        Assert.Equal(maxLevel, storedData.Key);
        Assert.Equal(maxLevelXp, storedData.Value);
    }

    [Fact]
    public void SaveLevelingExperience_DoesNotReportLevelUpWhenThresholdNotReached()
    {
        ResetPlayerData();

        const int startingLevel = 5;
        float startingXp = ConvertLevelToXp(startingLevel);
        DataService.PlayerDictionaries._playerExperience[TestSteamId] = new KeyValuePair<int, float>(startingLevel, startingXp);

        float nextLevelXp = ConvertLevelToXp(startingLevel + 1);
        float gainedXp = (nextLevelXp - startingXp) / 2f;

        LevelingSystem.SaveLevelingExperience(TestSteamId, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> storedData = DataService.PlayerDictionaries._playerExperience[TestSteamId];

        Assert.False(leveledUp);
        Assert.Equal(startingLevel, newLevel);
        Assert.Equal(startingLevel, storedData.Key);
        Assert.Equal(startingXp + gainedXp, storedData.Value);
    }

    [Fact]
    public void UpdateMaxRestedXP_ClampsRestedValueAndPreservesTimestamp()
    {
        ResetPlayerData();

        const int currentLevel = 10;
        float currentLevelXp = ConvertLevelToXp(currentLevel);
        var experienceData = new KeyValuePair<int, float>(currentLevel, currentLevelXp);
        DataService.PlayerDictionaries._playerExperience[TestSteamId] = experienceData;

        DateTime timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int restedLevel = Math.Min(ConfigService.RestedXPMax + currentLevel, ConfigService.MaxLevel);
        float restedCap = ConvertLevelToXp(restedLevel) - ConvertLevelToXp(currentLevel);
        float storedRestedXp = restedCap * 1.5f;

        DataService.PlayerDictionaries._playerRestedXP[TestSteamId] = new KeyValuePair<DateTime, float>(timestamp, storedRestedXp);

        LevelingSystem.UpdateMaxRestedXP(TestSteamId, experienceData);

        KeyValuePair<DateTime, float> storedData = DataService.PlayerDictionaries._playerRestedXP[TestSteamId];

        Assert.Equal(timestamp, storedData.Key);
        Assert.Equal(restedCap, storedData.Value);
    }
}
