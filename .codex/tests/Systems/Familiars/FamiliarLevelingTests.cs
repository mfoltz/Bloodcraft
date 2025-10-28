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

        Assert.NotNull(FamiliarBindingSystem.PlayerBattleGroups);

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

    [Fact]
    public void ProcessFamiliarExperience_RespondsToConfigOverrides()
    {
        using var experienceScope = new FamiliarExperienceDataScope(SteamId);
        using var entityScope = new EntityTestScope();
        using var familiarScope = new ActiveFamiliarResetScope(SteamId);
        using var config = WithConfigOverrides(
            ("FamiliarPrestige", false),
            ("UnitFamiliarMultiplier", 2.5f),
            ("VBloodFamiliarMultiplier", 7f),
            ("UnitSpawnerMultiplier", 1f));
        using var playerData = CapturePlayerData();

        Assert.NotNull(FamiliarBindingSystem.PlayerBattleGroups);

        const int unitLevel = 18;
        const int vBloodLevel = 22;

        var player = entityScope.CreateEntity(index: 20, steamId: SteamId);
        var familiar = entityScope.CreateEntity(index: 21, eligibleForCombat: true);
        var unitTarget = entityScope.CreateEntity(index: 22, unitLevel: unitLevel);
        var vBloodTarget = entityScope.CreateEntity(index: 23, unitLevel: vBloodLevel, isVBlood: true);

        entityScope.SetComponent(familiar, FamiliarPrefab);

        UpdateActiveFamiliarData(SteamId, familiar, Entity.Null, FamiliarPrefab.GuidHash);

        FamiliarLevelingSystem.ProcessFamiliarExperience(player, unitTarget, SteamId, groupMultiplier: 1f);

        FamiliarExperienceData data = LoadFamiliarExperienceData(SteamId);
        KeyValuePair<int, float> stored = data.FamiliarExperience[FamiliarPrefab.GuidHash];

        float baseXp = ConvertLevelToXp(FamiliarBindingSystem.BASE_LEVEL);
        float expectedAfterUnit = baseXp + unitLevel * 2.5f;

        Assert.Equal(FamiliarBindingSystem.BASE_LEVEL, stored.Key);
        Assert.Equal(expectedAfterUnit, stored.Value, 5);

        FamiliarLevelingSystem.ProcessFamiliarExperience(player, vBloodTarget, SteamId, groupMultiplier: 1f);

        data = LoadFamiliarExperienceData(SteamId);
        stored = data.FamiliarExperience[FamiliarPrefab.GuidHash];

        float expectedAfterVBlood = expectedAfterUnit + vBloodLevel * 7f;

        Assert.Equal(expectedAfterVBlood, stored.Value, 5);
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
