using System.Collections.Generic;
using FromCharacter = ProjectM.Network.FromCharacter;
using PlayerTeleportDebugEvent = ProjectM.PlayerTeleportDebugEvent;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Tests covering <see cref="FamiliarTeleportReturnWork"/>.
/// </summary>
public sealed class FamiliarTeleportReturnWorkTests
{
    [Fact]
    public void DescribeQuery_BuildsTeleportDebugRequirements()
    {
        var description = FactoryTestUtilities.DescribeQuery<FamiliarTeleportReturnWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(PlayerTeleportDebugEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(FromCharacter), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.True(description.RequireForUpdate);
        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
    }

    [Fact]
    public void OnCreate_RegistersTeleportLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<FamiliarTeleportReturnWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Contains(new LookupRequest(typeof(PlayerTeleportDebugEvent), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(FromCharacter), true), registrar.ComponentLookups);
    }

    [Fact]
    public void Tick_InvokesReturnDelegateForResolvedFamiliars()
    {
        var returns = new List<(EntityHandle Familiar, EntityHandle Owner)>();

        var owner = new EntityHandle(1);
        var familiar = new EntityHandle(2);
        var skippedOwner = new EntityHandle(3);
        var skippedEvent = new EntityHandle(4);
        var processedEvent = new EntityHandle(5);

        var work = new FamiliarTeleportReturnWork(
            familiarResolver: character => character == owner ? familiar : null,
            familiarReturner: (resolvedFamiliar, character) => returns.Add((resolvedFamiliar, character)));

        work.AddTeleportEvent(new FamiliarTeleportReturnWork.TeleportEventData(
            EventEntity: processedEvent,
            OwnerCharacter: owner));

        work.AddTeleportEvent(new FamiliarTeleportReturnWork.TeleportEventData(
            EventEntity: skippedEvent,
            OwnerCharacter: skippedOwner,
            HasTeleportDebug: true,
            HasFromCharacter: false));

        Assert.True(work.TryGetTeleportEvent(processedEvent, out _));
        Assert.True(work.TryGetTeleportEvent(skippedEvent, out _));

        var requestedQueries = new List<QueryDescription>();
        var iterationCount = 0;
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);

                if (query == work.TeleportQuery)
                {
                    foreach (var entity in work.TeleportOrder)
                    {
                        iterationCount++;
                        action(entity);
                    }
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.TeleportQuery, requestedQueries);
        Assert.Equal(work.TeleportOrder.Count, iterationCount);

        Assert.Single(returns);
        Assert.Equal(familiar, returns[0].Familiar);
        Assert.Equal(owner, returns[0].Owner);
    }
}
