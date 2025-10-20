using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using Xunit;

namespace Bloodcraft.Tests.Utilities;

public sealed class PlayerProgressionCacheManagerTests : IDisposable
{
    static readonly FieldInfo CacheField;

    static PlayerProgressionCacheManagerTests()
    {
        ConstructorInfo? typeInitializer = typeof(Progression).TypeInitializer;
        MethodInfo? prefix = typeof(PlayerProgressionCacheManagerTests)
            .GetMethod(nameof(ProgressionCctorPrefix), BindingFlags.Static | BindingFlags.NonPublic);

        Harmony harmony = new("Bloodcraft.Tests.Utilities.PlayerProgressionCacheManagerTests");

        if (typeInitializer != null && prefix != null)
        {
            PatchTypeInitializer(harmony, typeInitializer, prefix);
        }

        CacheField = typeof(Progression.PlayerProgressionCacheManager)
            .GetField("_playerProgressionCache", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("The player progression cache field could not be located.");

        object? cacheInstance = CacheField.GetValue(null);
        if (cacheInstance == null)
        {
            Type dataType = typeof(Progression.PlayerProgressionCacheManager.PlayerProgressionData);
            Type dictionaryType = typeof(ConcurrentDictionary<,>).MakeGenericType(typeof(ulong), dataType);
            cacheInstance = Activator.CreateInstance(dictionaryType)
                ?? throw new InvalidOperationException("Failed to create player progression cache instance.");
            CacheField.SetValue(null, cacheInstance);
        }
    }

    static void PatchTypeInitializer(Harmony harmony, ConstructorInfo typeInitializer, MethodInfo prefix)
    {
        HarmonyMethod prefixMethod = new(prefix);
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
            5 => new object?[] { typeInitializer, prefixMethod, null, null, null },
            6 => new object?[] { typeInitializer, prefixMethod, null, null, null, null },
            _ => throw new InvalidOperationException("Unexpected Harmony Patch signature.")
        };

        patch.Invoke(harmony, arguments);
    }

    readonly List<ulong> seededSteamIds = new();

    public PlayerProgressionCacheManagerTests()
    {
        ClearProgressionCache();
    }

    [Fact]
    public void UpdatePlayerProgression_InsertsAndOverwritesEntries()
    {
        ulong steamId = RegisterSteamId();

        Progression.PlayerProgressionCacheManager.UpdatePlayerProgression(steamId, 10, false);
        var initial = Progression.PlayerProgressionCacheManager.GetProgressionCacheData(steamId);

        Assert.Equal(10, initial.Level);
        Assert.False(initial.HasPrestiged);

        Progression.PlayerProgressionCacheManager.UpdatePlayerProgression(steamId, 15, true);
        var updated = Progression.PlayerProgressionCacheManager.GetProgressionCacheData(steamId);

        Assert.Equal(15, updated.Level);
        Assert.True(updated.HasPrestiged);
    }

    [Fact]
    public void UpdatePlayerProgressionLevelAndPrestige_MutateCachedValues()
    {
        ulong steamId = RegisterSteamId();

        Progression.PlayerProgressionCacheManager.UpdatePlayerProgression(steamId, 8, false);

        Progression.PlayerProgressionCacheManager.UpdatePlayerProgressionLevel(steamId, 18);
        var afterLevelUpdate = Progression.PlayerProgressionCacheManager.GetProgressionCacheData(steamId);

        Assert.Equal(18, afterLevelUpdate.Level);
        Assert.False(afterLevelUpdate.HasPrestiged);

        Progression.PlayerProgressionCacheManager.UpdatePlayerProgressionPrestige(steamId, true);
        var afterPrestigeUpdate = Progression.PlayerProgressionCacheManager.GetProgressionCacheData(steamId);

        Assert.Equal(18, afterPrestigeUpdate.Level);
        Assert.True(afterPrestigeUpdate.HasPrestiged);
    }

    [Fact]
    public void GetProgressionCacheData_PopulatesCacheFromServices()
    {
        ulong steamId = RegisterSteamId();
        SeedExperience(steamId, 24);
        SeedPrestige(steamId, prestigeCount: 2);

        var progression = Progression.PlayerProgressionCacheManager.GetProgressionCacheData(steamId);

        Assert.Equal(24, progression.Level);
        Assert.True(progression.HasPrestiged);

        object? cacheInstance = CacheField.GetValue(null);
        bool containsKey = cacheInstance?.GetType().GetMethod("ContainsKey")?.Invoke(cacheInstance, new object[] { steamId }) as bool? ?? false;

        Assert.True(containsKey);

        var cachedProgression = Progression.PlayerProgressionCacheManager.GetProgressionCacheData(steamId);
        Assert.Same(progression, cachedProgression);
    }

    public void Dispose()
    {
        foreach (ulong steamId in seededSteamIds)
        {
            DataService.PlayerDictionaries._playerExperience.TryRemove(steamId, out _);
            DataService.PlayerDictionaries._playerPrestiges.TryRemove(steamId, out _);
        }

        ClearProgressionCache();
    }

    static void ClearProgressionCache()
    {
        object? cacheInstance = CacheField.GetValue(null);
        cacheInstance?.GetType().GetMethod("Clear")?.Invoke(cacheInstance, null);
    }

    ulong RegisterSteamId()
    {
        ulong steamId = (ulong)(1000 + seededSteamIds.Count);
        seededSteamIds.Add(steamId);
        return steamId;
    }

    static void SeedExperience(ulong steamId, int level)
    {
        DataService.PlayerDictionaries._playerExperience[steamId] = new KeyValuePair<int, float>(level, 0f);
    }

    static void SeedPrestige(ulong steamId, int prestigeCount)
    {
        DataService.PlayerDictionaries._playerPrestiges[steamId] = new Dictionary<PrestigeType, int>
        {
            [PrestigeType.Experience] = prestigeCount
        };
    }

    static bool ProgressionCctorPrefix()
    {
        return false;
    }
}
