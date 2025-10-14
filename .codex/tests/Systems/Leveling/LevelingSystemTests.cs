using System;
using System.Collections.Generic;
using System.Reflection;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using Xunit;
using System.Runtime.CompilerServices;

namespace Bloodcraft.Tests.Systems.Leveling;

public class LevelingSystemTests : IClassFixture<LevelingSystemTests.Fixture>
{
    const ulong TestSteamId = 76561198000000042UL;

    [ModuleInitializer]
    internal static void RegisterHarmonyResolver()
    {
        AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmonyAssembly;
    }

    static Assembly? ResolveHarmonyAssembly(object? sender, ResolveEventArgs args)
    {
        if (!args.Name.StartsWith("0Harmony", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return typeof(Harmony).Assembly;
    }

    public class Fixture : IDisposable
    {
        readonly Harmony harmony;

        public Fixture()
        {
            harmony = new Harmony("Bloodcraft.Tests.Systems.Leveling.LevelingSystemTests");
            PatchPersistence(harmony, nameof(DataService.PlayerPersistence.SavePlayerExperience));
            PatchPersistence(harmony, nameof(DataService.PlayerPersistence.SavePlayerRestedXP));
        }

        public void Dispose()
        {
            MethodInfo? unpatch = typeof(Harmony).GetMethod("UnpatchSelf", BindingFlags.Instance | BindingFlags.Public);
            unpatch?.Invoke(harmony, null);
        }

        static void PatchPersistence(Harmony harmonyInstance, string methodName)
        {
            MethodInfo? target = typeof(DataService.PlayerPersistence).GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(target);

            MethodInfo prefix = typeof(Fixture).GetMethod(nameof(SkipPersistence), BindingFlags.NonPublic | BindingFlags.Static)!;
            ApplyPatch(harmonyInstance, target!, prefix);
        }

        static bool SkipPersistence() => false;

        static void ApplyPatch(Harmony harmonyInstance, MethodInfo original, MethodInfo prefix)
        {
            HarmonyMethod harmonyPrefix = new(prefix);
            var signatures = new[]
            {
                new { Parameters = new[] { typeof(MethodBase), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod) }, Arguments = new object?[] { original, harmonyPrefix, null, null, null, null } },
                new { Parameters = new[] { typeof(MethodBase), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod) }, Arguments = new object?[] { original, harmonyPrefix, null, null, null } },
                new { Parameters = new[] { typeof(MethodBase), typeof(HarmonyMethod), typeof(HarmonyMethod), typeof(HarmonyMethod) }, Arguments = new object?[] { original, harmonyPrefix, null, null } }
            };

            foreach (var signature in signatures)
            {
                MethodInfo? patchMethod = typeof(Harmony).GetMethod("Patch", BindingFlags.Instance | BindingFlags.Public, Type.DefaultBinder, signature.Parameters, null);
                if (patchMethod != null)
                {
                    patchMethod.Invoke(harmonyInstance, signature.Arguments);
                    return;
                }
            }

            throw new InvalidOperationException("Harmony.Patch signature not supported.");
        }
    }

    static void ResetPlayerData()
    {
        DataService.PlayerDictionaries._playerExperience.Clear();
        DataService.PlayerDictionaries._playerRestedXP.Clear();
    }

    [Fact]
    public void SaveLevelingExperience_RaisesLevelWhenCrossingBoundary()
    {
        ResetPlayerData();

        const int startingLevel = 1;
        float xpBefore = Progression.ConvertLevelToXp(startingLevel + 1) - 10;
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
        float startingXp = Progression.ConvertLevelToXp(startingLevel);
        DataService.PlayerDictionaries._playerExperience[TestSteamId] = new KeyValuePair<int, float>(startingLevel, startingXp);

        float targetXp = Progression.ConvertLevelToXp(maxLevel + 5);
        float gainedXp = targetXp - startingXp;

        LevelingSystem.SaveLevelingExperience(TestSteamId, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> storedData = DataService.PlayerDictionaries._playerExperience[TestSteamId];
        float maxLevelXp = Progression.ConvertLevelToXp(maxLevel);

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
        float startingXp = Progression.ConvertLevelToXp(startingLevel);
        DataService.PlayerDictionaries._playerExperience[TestSteamId] = new KeyValuePair<int, float>(startingLevel, startingXp);

        float nextLevelXp = Progression.ConvertLevelToXp(startingLevel + 1);
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
        float currentLevelXp = Progression.ConvertLevelToXp(currentLevel);
        var experienceData = new KeyValuePair<int, float>(currentLevel, currentLevelXp);
        DataService.PlayerDictionaries._playerExperience[TestSteamId] = experienceData;

        DateTime timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int restedLevel = Math.Min(ConfigService.RestedXPMax + currentLevel, ConfigService.MaxLevel);
        float restedCap = Progression.ConvertLevelToXp(restedLevel) - Progression.ConvertLevelToXp(currentLevel);
        float storedRestedXp = restedCap * 1.5f;

        DataService.PlayerDictionaries._playerRestedXP[TestSteamId] = new KeyValuePair<DateTime, float>(timestamp, storedRestedXp);

        LevelingSystem.UpdateMaxRestedXP(TestSteamId, experienceData);

        KeyValuePair<DateTime, float> storedData = DataService.PlayerDictionaries._playerRestedXP[TestSteamId];

        Assert.Equal(timestamp, storedData.Key);
        Assert.Equal(restedCap, storedData.Value);
    }
}
