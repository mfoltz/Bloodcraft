using ProjectM;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class DeathEventAggregationWorkTests
{
    [Fact]
    public void DescribeQuery_RequiresDeathEventComponent()
    {
        var description = FactoryTestUtilities.DescribeQuery<DeathEventAggregationWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(DeathEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void OnCreate_RegistersDeathEventLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<DeathEventAggregationWork>();

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Equal(1, registrar.EntityTypeHandleRequests);
        Assert.Equal(1, registrar.EntityStorageInfoLookupRequests);

        Assert.Collection(
            registrar.ComponentLookups,
            request =>
            {
                Assert.Equal(typeof(Movement), request.ElementType);
                Assert.True(request.IsReadOnly);
            },
            request =>
            {
                Assert.Equal(typeof(BlockFeedBuff), request.ElementType);
                Assert.True(request.IsReadOnly);
            },
            request =>
            {
                Assert.Equal(typeof(Trader), request.ElementType);
                Assert.True(request.IsReadOnly);
            },
            request =>
            {
                Assert.Equal(typeof(UnitLevel), request.ElementType);
                Assert.True(request.IsReadOnly);
            },
            request =>
            {
                Assert.Equal(typeof(Minion), request.ElementType);
                Assert.True(request.IsReadOnly);
            },
            request =>
            {
                Assert.Equal(typeof(VBloodConsumeSource), request.ElementType);
                Assert.True(request.IsReadOnly);
            });
    }

    [Fact]
    public void ShouldProcessLegacyProgression_RequiresLegaciesAndFeedKill()
    {
        var feedKill = new DeathEvent
        {
            StatChangeReason = StatChangeReason.HandleGameplayEventsBase_11,
        };

        Assert.True(DeathEventAggregationWork.ShouldProcessLegacyProgression(true, in feedKill));
        Assert.False(DeathEventAggregationWork.ShouldProcessLegacyProgression(false, in feedKill));

        var nonFeedKill = new DeathEvent();

        Assert.False(DeathEventAggregationWork.ShouldProcessLegacyProgression(true, in nonFeedKill));
    }

    [Theory]
    [InlineData(true, true, true, true, true)]
    [InlineData(true, true, true, false, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(false, true, true, true, false)]
    public void ShouldResetActiveFamiliar_EvaluatesFamiliarConditions(
        bool familiarsEnabled,
        bool diedHasFollowedPlayer,
        bool playerHasActiveFamiliar,
        bool deceasedMatchesActiveFamiliar,
        bool expected)
    {
        var result = DeathEventAggregationWork.ShouldResetActiveFamiliar(
            familiarsEnabled,
            diedHasFollowedPlayer,
            playerHasActiveFamiliar,
            deceasedMatchesActiveFamiliar);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void ShouldProcessMinionUnlock_EvaluatesMinionSettings(bool allowMinions, bool isMinion, bool expected)
    {
        var result = DeathEventAggregationWork.ShouldProcessMinionUnlock(allowMinions, isMinion);
        Assert.Equal(expected, result);
    }
}
