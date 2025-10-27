using System;
using System.Collections.Generic;
using ProjectM;
using Unity.Entities;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class FamiliarImprisonmentWorkTests
{
    [Fact]
    public void DescribeQuery_BuildsImprisonedBuffRequirements()
    {
        var description = FactoryTestUtilities.DescribeQuery<FamiliarImprisonmentWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(Buff), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(ImprisonedBuff), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.True(description.RequireForUpdate);
        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.IncludeDisabled, description.Options);
    }

    [Fact]
    public void OnCreate_RegistersBuffAndTargetLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<FamiliarImprisonmentWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Contains(new LookupRequest(typeof(Buff), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(CharmSource), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(BlockFeedBuff), false), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(Disabled), false), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(Minion), false), registrar.ComponentLookups);
    }

    [Fact]
    public void Tick_RemovesComponentsAndDestroysFamiliar()
    {
        var removedComponents = new List<(EntityHandle Entity, Type ComponentType)>();
        var destroyedTargets = new List<EntityHandle>();

        var work = new FamiliarImprisonmentWork(
            componentRemovalHook: (entity, component) => removedComponents.Add((entity, component)),
            destroyHook: entity => destroyedTargets.Add(entity));

        var buff = new EntityHandle(1);
        var target = new EntityHandle(2);
        var charmBuff = new EntityHandle(3);
        var charmedTarget = new EntityHandle(4);
        var missingBuff = new EntityHandle(5);

        work.AddImprisonedEntry(new FamiliarImprisonmentWork.ImprisonedEntry(
            Buff: buff,
            Target: target,
            TargetHasBlockFeedBuff: true,
            TargetHasDisabled: true,
            TargetHasMinion: true));

        work.AddImprisonedEntry(new FamiliarImprisonmentWork.ImprisonedEntry(
            Buff: charmBuff,
            Target: charmedTarget,
            TargetHasCharmSource: true,
            TargetHasBlockFeedBuff: true,
            TargetHasDisabled: true,
            TargetHasMinion: true));

        work.AddImprisonedEntry(new FamiliarImprisonmentWork.ImprisonedEntry(
            Buff: missingBuff,
            Target: new EntityHandle(6),
            BuffHasBuffComponent: false,
            TargetHasBlockFeedBuff: true));

        Assert.True(work.TryGetImprisonedEntry(buff, out var storedEntry));
        Assert.Equal(target, storedEntry.Target);

        var requestedQueries = new List<QueryDescription>();
        var iterationCount = 0;
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);

                if (query == work.ImprisonedQuery)
                {
                    foreach (var entity in work.ImprisonedOrder)
                    {
                        iterationCount++;
                        action(entity);
                    }
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.ImprisonedQuery, requestedQueries);
        Assert.Equal(work.ImprisonedOrder.Count, iterationCount);

        Assert.Contains((target, typeof(Disabled)), removedComponents);
        Assert.Contains((target, typeof(Minion)), removedComponents);
        Assert.Contains((target, typeof(BlockFeedBuff)), removedComponents);
        Assert.Equal(3, removedComponents.Count);

        Assert.Single(destroyedTargets, target);

        Assert.Equal(removedComponents, work.RemovedComponents);
        Assert.Equal(destroyedTargets, work.DestroyedTargets);

        Assert.DoesNotContain(charmedTarget, destroyedTargets);
    }
}
