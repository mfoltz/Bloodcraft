using System;
using System.Collections.Generic;
using System.Reflection;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Utilities;
using HarmonyLib;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Systems.Legacies;

public class BloodSystemTests
{
    const ulong TestSteamId = 76561198000000042UL;

    static readonly Harmony harmony;
    static bool bloodSystemPatched;

    static BloodSystemTests()
    {
        harmony = new Harmony("Bloodcraft.Tests.Systems.Legacies");
        PatchAssetGuid();
        PatchPrefabGuid();
        PatchBloodSystemCctor();
    }

    sealed class InMemoryBloodLegacy : IBloodLegacy
    {
        readonly Dictionary<ulong, KeyValuePair<int, float>> storage = new();
        readonly BloodType bloodType;

        public InMemoryBloodLegacy(BloodType bloodType = BloodType.Worker)
        {
            this.bloodType = bloodType;
        }

        public void Seed(ulong steamId, KeyValuePair<int, float> xpData)
        {
            storage[steamId] = xpData;
        }

        public KeyValuePair<int, float> GetLegacyData(ulong steamId)
        {
            return storage.TryGetValue(steamId, out var data)
                ? data
                : new KeyValuePair<int, float>(0, 0);
        }

        public void SetLegacyData(ulong steamId, KeyValuePair<int, float> xpData)
        {
            storage[steamId] = xpData;
        }

        public BloodType GetBloodType()
        {
            return bloodType;
        }
    }

    static float ConvertLevelToXp(int level)
    {
        return Progression.ConvertLevelToXp(level);
    }

    static void PatchAssetGuid()
    {
        MethodInfo target = AccessTools.Method(typeof(AssetGuid), nameof(AssetGuid.FromString), new[] { typeof(string) })!;
        MethodInfo prefix = typeof(BloodSystemTests).GetMethod(nameof(AssetGuidFromStringPrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
        var processor = harmony.CreateProcessor(target);
        processor.AddPrefix(new HarmonyMethod(prefix));
        processor.Patch();
    }

    static bool AssetGuidFromStringPrefix(string hexString, ref AssetGuid __result)
    {
        __result = default;
        return false;
    }

    static void PatchPrefabGuid()
    {
        MethodBase? cctor = typeof(PrefabGUID).TypeInitializer;
        if (cctor != null)
        {
            MethodInfo skipPrefix = typeof(BloodSystemTests).GetMethod(nameof(SkipOriginal), BindingFlags.NonPublic | BindingFlags.Static)!;
            var ctorProcessor = harmony.CreateProcessor(cctor);
            ctorProcessor.AddPrefix(new HarmonyMethod(skipPrefix));
            ctorProcessor.Patch();
        }

        MethodInfo getHashCode = AccessTools.Method(typeof(PrefabGUID), nameof(PrefabGUID.GetHashCode))!;
        MethodInfo hashPrefix = typeof(BloodSystemTests).GetMethod(nameof(PrefabGuidGetHashCodePrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
        var hashProcessor = harmony.CreateProcessor(getHashCode);
        hashProcessor.AddPrefix(new HarmonyMethod(hashPrefix));
        hashProcessor.Patch();
    }

    static bool PrefabGuidGetHashCodePrefix(ref int __result)
    {
        __result = 0;
        return false;
    }

    static bool SkipOriginal()
    {
        return false;
    }

    static void PatchBloodSystemCctor()
    {
        if (bloodSystemPatched)
        {
            return;
        }

        MethodBase? cctor = typeof(BloodSystem).TypeInitializer;
        if (cctor != null)
        {
            MethodInfo prefix = typeof(BloodSystemTests).GetMethod(nameof(BloodSystemCctorPrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
            var processor = harmony.CreateProcessor(cctor);
            processor.AddPrefix(new HarmonyMethod(prefix));
            processor.Patch();
            bloodSystemPatched = true;
        }
    }

    static bool BloodSystemCctorPrefix()
    {
        SetStaticFieldValue("_maxBloodLevel", ConfigService.MaxBloodLevel);
        SetStaticFieldValue("_legacyStatChoices", ConfigService.LegacyStatChoices);
        SetStaticFieldValue("_vBloodLegacyMultiplier", ConfigService.VBloodLegacyMultiplier);
        SetStaticFieldValue("_unitLegacyMultiplier", ConfigService.UnitLegacyMultiplier);
        SetStaticFieldValue("_prestigeRatesReducer", ConfigService.PrestigeRatesReducer);
        SetStaticFieldValue("_prestigeRateMultiplier", ConfigService.PrestigeRateMultiplier);
        SetStaticFieldToNewDictionary("_tryGetExtensions");
        SetStaticFieldToNewDictionary("_setExtensions");
        SetStaticFieldToNewDictionary("_bloodPrestigeTypes");
        SetStaticFieldToNewDictionary("_bloodBuffToBloodType");
        SetStaticFieldToNewDictionary("_bloodTypeToBloodBuff");
        SetStaticFieldToNewDictionary("_bloodTypeToConsumeSource");
        return false;
    }

    static void SetStaticFieldValue(string fieldName, object value)
    {
        FieldInfo field = typeof(BloodSystem).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)!;
        field.SetValue(null, value);
    }

    static void SetStaticFieldToNewDictionary(string fieldName)
    {
        FieldInfo field = typeof(BloodSystem).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)!;
        object instance = Activator.CreateInstance(field.FieldType)!;
        field.SetValue(null, instance);
    }

    [Fact]
    public void SaveBloodExperience_RaisesLevelWhenCrossingThreshold()
    {
        var handler = new InMemoryBloodLegacy();

        const int startingLevel = 1;
        float xpBefore = ConvertLevelToXp(startingLevel + 1) - 5f;
        handler.Seed(TestSteamId, new KeyValuePair<int, float>(startingLevel, xpBefore));

        const float gainedXp = 10f;

        BloodSystem.SaveBloodExperience(TestSteamId, handler, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> storedData = handler.GetLegacyData(TestSteamId);

        Assert.True(leveledUp);
        Assert.Equal(startingLevel + 1, newLevel);
        Assert.Equal(startingLevel + 1, storedData.Key);
        Assert.Equal(xpBefore + gainedXp, storedData.Value);
    }

    [Fact]
    public void SaveBloodExperience_ClampsAtConfiguredMaximum()
    {
        var handler = new InMemoryBloodLegacy();

        int maxLevel = ConfigService.MaxBloodLevel;
        int startingLevel = maxLevel - 1;
        float startingXp = ConvertLevelToXp(startingLevel);
        handler.Seed(TestSteamId, new KeyValuePair<int, float>(startingLevel, startingXp));

        float targetXp = ConvertLevelToXp(maxLevel + 5);
        float gainedXp = targetXp - startingXp;

        BloodSystem.SaveBloodExperience(TestSteamId, handler, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> storedData = handler.GetLegacyData(TestSteamId);
        float maxLevelXp = ConvertLevelToXp(maxLevel);

        Assert.True(leveledUp);
        Assert.Equal(maxLevel, newLevel);
        Assert.Equal(maxLevel, storedData.Key);
        Assert.Equal(maxLevelXp, storedData.Value);
    }

    [Fact]
    public void GetLevelProgress_ReturnsExpectedPercentage()
    {
        var handler = new InMemoryBloodLegacy();

        const int currentLevel = 3;
        float currentLevelXp = ConvertLevelToXp(currentLevel);
        float nextLevelXp = ConvertLevelToXp(currentLevel + 1);
        float xpDelta = nextLevelXp - currentLevelXp;
        float storedXp = currentLevelXp + (xpDelta / 2f);

        handler.Seed(TestSteamId, new KeyValuePair<int, float>(currentLevel, storedXp));

        int progress = BloodSystem.GetLevelProgress(TestSteamId, handler);

        double manualEarned = nextLevelXp - storedXp;
        double manualNeeded = nextLevelXp - currentLevelXp;
        int expectedProgress = 100 - (int)Math.Ceiling(manualEarned / manualNeeded * 100d);

        Assert.Equal(expectedProgress, progress);
    }
}
