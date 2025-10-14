using System;
using System.Collections.Generic;
using System.Reflection;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Utilities;
using HarmonyLib;
using Xunit;

namespace Bloodcraft.Tests.Systems.Expertise;

public class WeaponSystemTests
{
    const ulong TestSteamId = 76561198000000050UL;

    static WeaponSystemTests()
    {
        Harmony harmony = new("Bloodcraft.Tests.Systems.Expertise.WeaponSystemTests");
        ConstructorInfo? typeInitializer = typeof(WeaponSystem).TypeInitializer;
        MethodInfo? prefixMethod = typeof(WeaponSystemTests).GetMethod(nameof(WeaponSystemCctorPrefix), BindingFlags.Static | BindingFlags.NonPublic);
        if (typeInitializer != null && prefixMethod != null)
        {
            HarmonyMethod prefix = new(prefixMethod);
            MethodInfo? patch = typeof(Harmony).GetMethod(
                "Patch",
                new[] { typeof(MethodBase), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod) })
                ?? typeof(Harmony).GetMethod(
                    "Patch",
                    new[]
                    {
                        typeof(MethodBase), typeof(HarmonyMethod), typeof(HarmonyMethod),
                        typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod)
                    });

            if (patch == null)
            {
                throw new InvalidOperationException("Harmony Patch overload not found.");
            }

            object?[] arguments = patch.GetParameters().Length switch
            {
                5 => new object?[] { typeInitializer, prefix, null, null, null },
                6 => new object?[] { typeInitializer, prefix, null, null, null, null },
                _ => throw new InvalidOperationException("Unexpected Harmony Patch signature.")
            };

            patch.Invoke(harmony, arguments);
        }
    }

    static bool WeaponSystemCctorPrefix()
    {
        Traverse weaponSystem = Traverse.Create(typeof(WeaponSystem));

        weaponSystem.Field("_maxExpertiseLevel").SetValue(ConfigService.MaxExpertiseLevel);
        weaponSystem.Field("_expertiseStatChoices").SetValue(ConfigService.ExpertiseStatChoices);
        weaponSystem.Field("_unitExpertiseMultiplier").SetValue(ConfigService.UnitExpertiseMultiplier);
        weaponSystem.Field("_vBloodExpertiseMultiplier").SetValue(ConfigService.VBloodExpertiseMultiplier);
        weaponSystem.Field("_prestigeRatesReducer").SetValue(ConfigService.PrestigeRatesReducer);
        weaponSystem.Field("_prestigeRateMultiplier").SetValue(ConfigService.PrestigeRateMultiplier);
        weaponSystem.Field("_unitSpawnerExpertiseFactor").SetValue(ConfigService.UnitSpawnerExpertiseFactor);
        weaponSystem.Field("TryGetExtensionMap").SetValue(new Dictionary<WeaponType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>>());
        weaponSystem.Field("SetExtensionMap").SetValue(new Dictionary<WeaponType, Action<ulong, KeyValuePair<int, float>>>());
        weaponSystem.Field("_delay").SetValue(null);

        return false;
    }

    sealed class StubWeaponExpertise : IWeaponExpertise
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

        int expected = 100 - (int)Math.Ceiling((nextLevelXp - currentXp) / neededXp * 100);

        Assert.Equal(25, expected);
        Assert.Equal(expected, progress);
    }
}
