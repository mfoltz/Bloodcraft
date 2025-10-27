using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class SpawnBuffWorkTests
{
    [Fact]
    public void DescribeQuery_CombinesScriptAndMinionQueries()
    {
        var description = FactoryTestUtilities.DescribeQuery<SpawnBuffWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(PrefabGUID), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Buff), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(EntityOwner), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.True(description.RequireForUpdate);
        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);

        var work = new SpawnBuffWork();
        var minionQuery = work.MinionSpawnQuery;

        Assert.Collection(
            minionQuery.All,
            requirement =>
            {
                Assert.Equal(typeof(IsMinion), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadWrite, requirement.AccessMode);
            });

        Assert.True(minionQuery.RequireForUpdate);
        Assert.Empty(minionQuery.Any);
        Assert.Empty(minionQuery.None);
        Assert.Equal(EntityQueryOptions.None, minionQuery.Options);
    }

    [Fact]
    public void OnCreate_RegistersRequiredLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<SpawnBuffWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Contains(new LookupRequest(typeof(PlayerCharacter), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(BlockFeedBuff), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(BloodBuff), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(IsMinion), false), registrar.ComponentLookups);
    }

    [Fact]
    public void OnUpdate_InvokesInjectedCallbacksAndMarksMinions()
    {
        var cooldowns = new List<(EntityHandle Source, int Group, float Cooldown)>();
        var statRefreshTargets = new List<EntityHandle>();

        const int abilityGroup = 4242;
        var work = new SpawnBuffWork(
            bloodBoltCooldownSeconds: 2.5f,
            abilityCooldownSetter: (source, group, cooldown) => cooldowns.Add((source, group, cooldown)),
            statRefreshDelegate: target => statRefreshTargets.Add(target),
            bloodBoltAbilityGroupHash: abilityGroup);

        var cooldownBuffEntity = new EntityHandle(1);
        var cooldownTarget = new EntityHandle(2);
        var cooldownOwner = new EntityHandle(3);
        var refreshBuffEntity = new EntityHandle(4);
        var refreshTarget = new EntityHandle(5);
        var minionEntity = new EntityHandle(6);

        work.AddScriptSpawnEntry(new SpawnBuffWork.ScriptSpawnEntry(
            cooldownBuffEntity,
            cooldownTarget,
            cooldownOwner,
            136816739,
            TargetIsPlayer: true,
            TargetIsFamiliar: false,
            OwnerIsFamiliar: false,
            IsBloodBuff: false,
            IsDebuff: false));

        work.AddScriptSpawnEntry(new SpawnBuffWork.ScriptSpawnEntry(
            refreshBuffEntity,
            refreshTarget,
            new EntityHandle(7),
            999_999,
            TargetIsPlayer: true,
            TargetIsFamiliar: false,
            OwnerIsFamiliar: false,
            IsBloodBuff: true,
            IsDebuff: false));

        work.AddMinionEntity(minionEntity, isMinion: false);

        var requestedQueries = new List<QueryDescription>();
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);

                if (query == work.ScriptSpawnQuery)
                {
                    foreach (var entity in work.ScriptSpawnEntities)
                    {
                        action(entity);
                    }
                }
                else if (query == work.MinionSpawnQuery)
                {
                    foreach (var entity in work.MinionEntities)
                    {
                        action(entity);
                    }
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.ScriptSpawnQuery, requestedQueries);
        Assert.Contains(work.MinionSpawnQuery, requestedQueries);

        Assert.Single(cooldowns);
        Assert.Equal(cooldownOwner, cooldowns[0].Source);
        Assert.Equal(abilityGroup, cooldowns[0].Group);
        Assert.Equal(2.5f, cooldowns[0].Cooldown);

        Assert.Single(statRefreshTargets);
        Assert.Equal(refreshTarget, statRefreshTargets[0]);

        Assert.True(work.TryGetMinionState(minionEntity, out var isMinion) && isMinion);
    }

    [Fact]
    public void ClassificationTables_ExposePatchHashes()
    {
        Assert.Contains(-31099041, SpawnBuffWork.ShapeshiftBuffHashes);
        Assert.Contains(1615225381, SpawnBuffWork.BloodBoltTriggerHashes);
        Assert.Contains(-1598161201, SpawnBuffWork.WerewolfBuffHashes);
    }

    [Fact]
    public void ClassifyBuff_RecognisesBloodBuffs()
    {
        var category = SpawnBuffWork.ClassifyBuff(
            prefabHash: 555_555,
            isDebuff: false,
            targetIsPlayer: true,
            targetIsFamiliar: false,
            isBloodBuff: true);

        Assert.Equal(SpawnBuffWork.BuffCategory.RefreshBloodStats, category);
        Assert.False(SpawnBuffWork.IsBloodBoltTrigger(136816739));
        Assert.True(SpawnBuffWork.IsShapeshiftBuff(-31099041));
        Assert.True(SpawnBuffWork.IsWerewolfBuff(-1598161201));
    }
}
