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
                Assert.Equal(typeof(PrefabGUID), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Health), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(UnitLevel), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(UnitStats), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Movement), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(AggroConsumer), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.Any);

        Assert.Collection(
            description.None,
            requirement =>
            {
                Assert.Equal(typeof(Minion), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(DestroyOnSpawn), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Trader), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(BlockFeedBuff), requirement.ComponentType);
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
                Assert.Equal(typeof(Buff), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(ImprisonedBuff), requirement.ComponentType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.IncludeDisabled, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void OnCreate_RegistersHandleRefreshes()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<QuestTargetWork>();

        work.OnCreate(context);

        Assert.Equal(1, registrar.RegistrationCount);

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
}
