using System.Collections.Generic;
using ProjectM;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class FamiliarMinionSpawnWorkTests
{
    [Fact]
    public void DescribeQuery_BuildsSpawnRequirements()
    {
        var description = FactoryTestUtilities.DescribeQuery<FamiliarMinionSpawnWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(EntityOwner), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Minion), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(Unity.Entities.SpawnTag), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.True(description.RequireForUpdate);
        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
    }

    [Fact]
    public void OnCreate_RegistersOwnerAndMinionLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<FamiliarMinionSpawnWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Contains(new LookupRequest(typeof(EntityOwner), false), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(Minion), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(BlockFeedBuff), true), registrar.ComponentLookups);
    }

    [Fact]
    public void Tick_TracksMinionsAndSchedulesLifetime()
    {
        var scheduledLifetimes = new List<(EntityHandle Minion, float Lifetime)>();

        var followedPlayer = new EntityHandle(1);
        var activeFamiliar = new EntityHandle(2);
        var followedMinion = new EntityHandle(3);
        var familiarOwner = new EntityHandle(4);
        var familiarMinion = new EntityHandle(5);
        var disabledOwner = new EntityHandle(6);
        var disabledMinion = new EntityHandle(7);

        var work = new FamiliarMinionSpawnWork(
            familiarResolver: player => player == followedPlayer ? activeFamiliar : null,
            lifetimeScheduler: (minion, lifetime) => scheduledLifetimes.Add((minion, lifetime)));

        work.AddSpawnEntry(new FamiliarMinionSpawnWork.SpawnEntry(
            Minion: followedMinion,
            Owner: new EntityHandle(8),
            FollowedPlayer: followedPlayer));

        work.AddSpawnEntry(new FamiliarMinionSpawnWork.SpawnEntry(
            Minion: familiarMinion,
            Owner: familiarOwner,
            OwnerHasBlockFeedBuff: true));

        work.AddSpawnEntry(new FamiliarMinionSpawnWork.SpawnEntry(
            Minion: disabledMinion,
            Owner: disabledOwner,
            OwnerDisabled: true));

        Assert.True(work.TryGetSpawnEntry(followedMinion, out _));
        Assert.True(work.TryGetSpawnEntry(familiarMinion, out _));
        Assert.True(work.TryGetSpawnEntry(disabledMinion, out _));

        Assert.Contains(followedMinion, work.SpawnOrder);
        Assert.Contains(familiarMinion, work.SpawnOrder);
        Assert.Contains(disabledMinion, work.SpawnOrder);

        var requestedQueries = new List<QueryDescription>();
        var iterationCount = 0;
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);

                if (query == work.SpawnQuery)
                {
                    foreach (var entity in work.SpawnOrder)
                    {
                        iterationCount++;
                        action(entity);
                    }
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.SpawnQuery, requestedQueries);
        Assert.Equal(work.SpawnOrder.Count, iterationCount);

        Assert.True(work.FamiliarMinions.TryGetValue(activeFamiliar, out var familiarEntries));
        Assert.Contains(followedMinion, familiarEntries);

        Assert.True(work.ReassignedOwners.TryGetValue(followedMinion, out var reassignedOwner));
        Assert.Equal(followedPlayer, reassignedOwner);

        Assert.True(work.FamiliarMinions.TryGetValue(familiarOwner, out var ownerEntries));
        Assert.Contains(familiarMinion, ownerEntries);

        Assert.Contains((followedMinion, FamiliarMinionSpawnWork.FamiliarMinionLifetimeSeconds), scheduledLifetimes);
        Assert.Contains((familiarMinion, FamiliarMinionSpawnWork.FamiliarMinionLifetimeSeconds), scheduledLifetimes);
        Assert.Equal(2, scheduledLifetimes.Count);

        Assert.Contains(disabledMinion, work.DestroyedMinionEntities);
    }
}
