using System.Collections.Generic;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Tests;
using Bloodcraft.Tests.Support;
using Stunlock.Core;
using Unity.Entities;
using Xunit;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager;
using static Bloodcraft.Utilities.Familiars.ActiveFamiliarManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Tests.Systems.Familiars;

[Collection(FamiliarSystemTestCollection.CollectionName)]
public class FamiliarLevelingTests : TestHost
{
    const ulong SteamId = 76561198000052020UL;
    static readonly PrefabGUID FamiliarPrefab = new(987654321);

    public FamiliarLevelingTests(FamiliarSystemFixture _)
    {
        FamiliarSystemFixture.ClearLogs();
    }

    [Fact]
    public void ProcessFamiliarExperience_StoresExperienceForUnitDefeats()
    {
        using var experienceScope = new FamiliarExperienceDataScope(SteamId);
        using var entityScope = new EntityTestScope();
        using var familiarScope = new ActiveFamiliarResetScope(SteamId);
        using var config = WithConfigOverrides(
            ("FamiliarPrestige", false),
            ("UnitFamiliarMultiplier", 1f),
            ("VBloodFamiliarMultiplier", 3f),
            ("UnitSpawnerMultiplier", 1f));
        using var playerData = CapturePlayerData();

        const int targetLevel = 25;

        var player = entityScope.CreateEntity(index: 1, steamId: SteamId);
        var familiar = entityScope.CreateEntity(index: 2, eligibleForCombat: true);
        var target = entityScope.CreateEntity(index: 3, unitLevel: targetLevel);

        entityScope.SetComponent(familiar, FamiliarPrefab);

        UpdateActiveFamiliarData(SteamId, familiar, Entity.Null, FamiliarPrefab.GuidHash);

        FamiliarLevelingSystem.ProcessFamiliarExperience(player, target, SteamId, groupMultiplier: 1f);

        FamiliarExperienceData data = LoadFamiliarExperienceData(SteamId);
        Assert.True(data.FamiliarExperience.TryGetValue(FamiliarPrefab.GuidHash, out KeyValuePair<int, float> stored));

        float baseXp = ConvertLevelToXp(FamiliarBindingSystem.BASE_LEVEL);
        float gainedXp = targetLevel;
        float expectedXp = baseXp + gainedXp;

        Assert.Equal(FamiliarBindingSystem.BASE_LEVEL, stored.Key);
        Assert.Equal(expectedXp, stored.Value, 5);
    }

    [Fact]
    public void ProcessFamiliarExperience_UsesVBloodMultiplier()
    {
        using var experienceScope = new FamiliarExperienceDataScope(SteamId);
        using var entityScope = new EntityTestScope();
        using var familiarScope = new ActiveFamiliarResetScope(SteamId);
        using var config = WithConfigOverrides(
            ("FamiliarPrestige", false),
            ("UnitFamiliarMultiplier", 1f),
            ("VBloodFamiliarMultiplier", 4f),
            ("UnitSpawnerMultiplier", 1f));
        using var playerData = CapturePlayerData();

        const int targetLevel = 30;

        var player = entityScope.CreateEntity(index: 10, steamId: SteamId);
        var familiar = entityScope.CreateEntity(index: 11, eligibleForCombat: true);
        var target = entityScope.CreateEntity(index: 12, unitLevel: targetLevel, isVBlood: true);

        entityScope.SetComponent(familiar, FamiliarPrefab);

        UpdateActiveFamiliarData(SteamId, familiar, Entity.Null, FamiliarPrefab.GuidHash);

        FamiliarLevelingSystem.ProcessFamiliarExperience(player, target, SteamId, groupMultiplier: 1f);

        FamiliarExperienceData data = LoadFamiliarExperienceData(SteamId);
        Assert.True(data.FamiliarExperience.TryGetValue(FamiliarPrefab.GuidHash, out KeyValuePair<int, float> stored));

        float baseXp = ConvertLevelToXp(FamiliarBindingSystem.BASE_LEVEL);
        float gainedXp = targetLevel * 4f;
        float expectedXp = baseXp + gainedXp;

        Assert.Equal(FamiliarBindingSystem.BASE_LEVEL, stored.Key);
        Assert.Equal(expectedXp, stored.Value, 5);
    }
}

public static class FamiliarSystemTestCollection
{
    public const string CollectionName = "Familiars System Tests";
}

[CollectionDefinition(FamiliarSystemTestCollection.CollectionName, DisableParallelization = true)]
public sealed class FamiliarSystemTestCollectionDefinition : ICollectionFixture<FamiliarSystemFixture>
{
}
