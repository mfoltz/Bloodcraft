using System.Collections.Generic;
using ProjectM;
using ProjectM.Behaviours;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Tests covering <see cref="FamiliarBehaviourStateWork"/>.
/// </summary>
public sealed class FamiliarBehaviourStateWorkTests
{
    [Fact]
    public void DescribeQuery_BuildsBehaviourStateRequirements()
    {
        var description = FactoryTestUtilities.DescribeQuery<FamiliarBehaviourStateWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(BehaviourTreeStateChangedEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadWrite, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(BehaviourTreeState), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadWrite, requirement.AccessMode);
            });

        Assert.True(description.RequireForUpdate);
        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
    }

    [Fact]
    public void OnCreate_RegistersBehaviourLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<FamiliarBehaviourStateWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Contains(new LookupRequest(typeof(BehaviourTreeStateChangedEvent), false), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(BehaviourTreeState), false), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(BlockFeedBuff), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(Follower), true), registrar.ComponentLookups);
    }

    [Fact]
    public void Tick_HandlesReturnTransition()
    {
        var minionCalls = new List<EntityHandle>();
        var returnCalls = new List<(EntityHandle Owner, EntityHandle Familiar)>();

        var owner = new EntityHandle(1);
        var familiar = new EntityHandle(2);
        var eventEntity = new EntityHandle(3);

        var work = new FamiliarBehaviourStateWork(
            familiarMinionHandler: entity => minionCalls.Add(entity),
            familiarReturner: (resolvedOwner, resolvedFamiliar) => returnCalls.Add((resolvedOwner, resolvedFamiliar)));

        work.SetBehaviourState(familiar, GenericEnemyState.Idle);

        work.AddBehaviourEvent(new FamiliarBehaviourStateWork.BehaviourStateEventData(
            EventEntity: eventEntity,
            TargetEntity: familiar,
            NewState: GenericEnemyState.Return,
            FollowedPlayer: owner));

        Assert.True(work.TryGetBehaviourEvent(eventEntity, out _));

        var requestedQueries = new List<QueryDescription>();
        var iterationCount = 0;
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);

                if (query == work.BehaviourStateChangedQuery)
                {
                    foreach (var entity in work.BehaviourEventOrder)
                    {
                        iterationCount++;
                        action(entity);
                    }
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.BehaviourStateChangedQuery, requestedQueries);
        Assert.Equal(work.BehaviourEventOrder.Count, iterationCount);

        Assert.Single(minionCalls);
        Assert.Equal(familiar, minionCalls[0]);
        Assert.Empty(returnCalls);

        Assert.True(work.TryGetBehaviourEvent(eventEntity, out var processedEvent));
        Assert.Equal(GenericEnemyState.Follow, processedEvent.NewState);

        Assert.True(work.TryGetBehaviourState(familiar, out var familiarState));
        Assert.Equal(GenericEnemyState.Follow, familiarState);
    }

    [Fact]
    public void Tick_HandlesIdleTransition()
    {
        var minionCalls = new List<EntityHandle>();
        var returnCalls = new List<(EntityHandle Owner, EntityHandle Familiar)>();

        var owner = new EntityHandle(4);
        var familiar = new EntityHandle(5);
        var eventEntity = new EntityHandle(6);

        var work = new FamiliarBehaviourStateWork(
            familiarMinionHandler: entity => minionCalls.Add(entity),
            familiarReturner: (resolvedOwner, resolvedFamiliar) => returnCalls.Add((resolvedOwner, resolvedFamiliar)));

        work.SetBehaviourState(familiar, GenericEnemyState.Follow);

        work.AddBehaviourEvent(new FamiliarBehaviourStateWork.BehaviourStateEventData(
            EventEntity: eventEntity,
            TargetEntity: familiar,
            NewState: GenericEnemyState.Idle,
            FollowedPlayer: owner));

        var requestedQueries = new List<QueryDescription>();
        var iterationCount = 0;
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);

                if (query == work.BehaviourStateChangedQuery)
                {
                    foreach (var entity in work.BehaviourEventOrder)
                    {
                        iterationCount++;
                        action(entity);
                    }
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.BehaviourStateChangedQuery, requestedQueries);
        Assert.Equal(work.BehaviourEventOrder.Count, iterationCount);

        Assert.Empty(minionCalls);
        Assert.Single(returnCalls);
        Assert.Equal(owner, returnCalls[0].Owner);
        Assert.Equal(familiar, returnCalls[0].Familiar);

        Assert.True(work.TryGetBehaviourEvent(eventEntity, out var processedEvent));
        Assert.Equal(GenericEnemyState.Idle, processedEvent.NewState);

        Assert.True(work.TryGetBehaviourState(familiar, out var familiarState));
        Assert.Equal(GenericEnemyState.Follow, familiarState);
    }
}
