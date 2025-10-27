using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class QuestTargetWorkTests
{
    [Fact]
    public void DescribeQuery_ReturnsTargetCompositionWithIncludeDisabled()
    {
        var description = FactoryTestUtilities.DescribeQuery<QuestTargetWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(PrefabGUID), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Health), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(UnitLevel), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(UnitStats), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Movement), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(AggroConsumer), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.Any);

        Assert.Collection(
            description.None,
            requirement =>
            {
                Assert.Equal(typeof(Minion), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(DestroyOnSpawn), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Trader), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(BlockFeedBuff), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Equal(EntityQueryOptions.IncludeDisabled, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void ImprisonedQuery_DescribesBuffComposition()
    {
        var work = FactoryTestUtilities.CreateWork<QuestTargetWork>();
        var description = work.ImprisonedQuery;

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

        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.IncludeDisabled, description.Options);
        // Mirroring production, the imprisoned query remains optional to avoid
        // stalling updates when no prisoners are present.
        Assert.False(description.RequireForUpdate);
    }

    [Fact]
    public void OnCreate_RegistersHandleRefreshes()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<QuestTargetWork>();

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Equal(1, registrar.EntityTypeHandleRequests);
        Assert.Equal(1, registrar.EntityStorageInfoLookupRequests);

        Assert.Collection(
            registrar.ComponentTypeHandles,
            request =>
            {
                Assert.Equal(typeof(PrefabGUID), request.ElementType);
                Assert.True(request.IsReadOnly);
            },
            request =>
            {
                Assert.Equal(typeof(Buff), request.ElementType);
                Assert.True(request.IsReadOnly);
            });

        Assert.Empty(registrar.ComponentLookups);
        Assert.Empty(registrar.BufferTypeHandles);
        Assert.Empty(registrar.BufferLookups);
    }

    [Fact]
    public void Capacities_ExposeInitialNativeContainerSizes()
    {
        Assert.Equal(1024, QuestTargetWork.TargetUnitsCapacity);
        Assert.Equal(256, QuestTargetWork.BlacklistedUnitsCapacity);
        Assert.Equal(512, QuestTargetWork.ImprisonedUnitsCapacity);
    }

    [Fact]
    public void OnUpdate_ProcessesTargetsAndImprisonedUnits()
    {
        var processedTargets = new List<EntityHandle>();
        var blacklistedTargets = new List<EntityHandle>();
        var imprisonedTargets = new List<EntityHandle>();

        var targetHandles = new[] { new EntityHandle(1), new EntityHandle(2), new EntityHandle(3) };
        var imprisonedHandles = new[] { new EntityHandle(10) };

        var work = new QuestTargetWork(
            targetUpdater: processedTargets.Add,
            blacklistUpdater: blacklistedTargets.Add,
            imprisonedUpdater: imprisonedTargets.Add,
            targetFilter: handle => handle.Id != 3,
            blacklistEvaluator: handle => handle.Id == 2);

        var registrar = new RecordingRegistrar();
        var enumeratedQueries = new List<QueryDescription>();
        var targetQuery = FactoryTestUtilities.DescribeQuery<QuestTargetWork>();

        var context = FactoryTestUtilities.CreateContext(
            registrar,
            query: targetQuery,
            forEachEntity: (query, action) =>
            {
                enumeratedQueries.Add(query);

                if (query.Equals(targetQuery))
                {
                    foreach (var handle in targetHandles)
                    {
                        action(handle);
                    }
                }
                else if (query.Equals(work.ImprisonedQuery))
                {
                    foreach (var handle in imprisonedHandles)
                    {
                        action(handle);
                    }
                }
            });

        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Equal(new[] { targetHandles[0], targetHandles[1] }, processedTargets);
        Assert.Equal(new[] { targetHandles[1] }, blacklistedTargets);
        Assert.Equal(imprisonedHandles, imprisonedTargets);
        Assert.Contains(targetQuery, enumeratedQueries);
        Assert.Contains(work.ImprisonedQuery, enumeratedQueries);
    }

    [Fact]
    public void OnUpdate_SkipsImprisonedQueryWhenNoCallbackProvided()
    {
        var processedTargets = new List<EntityHandle>();
        var targetHandles = new[] { new EntityHandle(5) };
        var work = new QuestTargetWork(
            targetUpdater: processedTargets.Add,
            blacklistUpdater: null,
            imprisonedUpdater: null,
            targetFilter: null,
            blacklistEvaluator: null);

        var registrar = new RecordingRegistrar();
        var enumeratedQueries = new List<QueryDescription>();
        var targetQuery = FactoryTestUtilities.DescribeQuery<QuestTargetWork>();

        var context = FactoryTestUtilities.CreateContext(
            registrar,
            query: targetQuery,
            forEachEntity: (query, action) =>
            {
                enumeratedQueries.Add(query);

                if (query.Equals(targetQuery))
                {
                    foreach (var handle in targetHandles)
                    {
                        action(handle);
                    }
                }
            });

        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Equal(targetHandles, processedTargets);
        Assert.Contains(targetQuery, enumeratedQueries);
        Assert.DoesNotContain(work.ImprisonedQuery, enumeratedQueries);
    }
}
