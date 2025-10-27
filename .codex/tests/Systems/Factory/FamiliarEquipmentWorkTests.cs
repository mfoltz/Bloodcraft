using System.Collections.Generic;
using ProjectM;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class FamiliarEquipmentWorkTests
{
    [Fact]
    public void DescribeQuery_ListsServantEquipmentAndTeleportEvents()
    {
        var description = FactoryTestUtilities.DescribeQuery<FamiliarEquipmentWork>();

        Assert.Empty(description.All);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
        Assert.True(description.RequireForUpdate);

        Assert.Collection(
            description.Any,
            requirement =>
            {
                Assert.Equal(typeof(EquipServantItemFromInventoryEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(EquipServantItemEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(UnequipServantItemEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(EquipmentToEquipmentTransferEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(PlayerTeleportDebugEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });
    }

    [Fact]
    public void OnCreate_RegistersBlockFeedLookup()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<FamiliarEquipmentWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Single(registrar.ComponentLookups);
        Assert.Equal(typeof(BlockFeedBuff), registrar.ComponentLookups[0].ElementType);
        Assert.False(registrar.ComponentLookups[0].IsReadOnly);
    }

    [Fact]
    public void Tick_RequestsQueriesAndCapturesDelegateInteractions()
    {
        var validatorCalls = new List<(EntityHandle Inventory, int SlotIndex)>();
        var refreshCalls = new List<EntityHandle>();
        var networkRequests = new List<int>();
        var teleportCallback = new List<(EntityHandle Familiar, EntityHandle Owner)>();

        var servantHandle = new EntityHandle(10);
        var inventoryHandle = new EntityHandle(20);
        var playerInventoryHandle = new EntityHandle(21);
        var playerCharacterHandle = new EntityHandle(30);
        var familiarHandle = new EntityHandle(40);

        var networkMap = new Dictionary<int, EntityHandle>
        {
            [1] = servantHandle,
            [2] = inventoryHandle,
        };

        var work = new FamiliarEquipmentWork(
            inventoryValidator: (inventory, slotIndex) =>
            {
                validatorCalls.Add((inventory, slotIndex));
                return false;
            },
            statRefresher: familiar => refreshCalls.Add(familiar),
            networkLookup: (int networkId, out EntityHandle entity) =>
            {
                networkRequests.Add(networkId);
                return networkMap.TryGetValue(networkId, out entity);
            },
            servantFamiliarResolver: servant => servant == servantHandle ? familiarHandle : default,
            activeFamiliarResolver: owner => owner == playerCharacterHandle ? familiarHandle : default,
            teleportReturn: (familiar, owner) => teleportCallback.Add((familiar, owner)));

        work.AddEquipFromInventoryEvent(new FamiliarEquipmentWork.EquipFromInventoryEventData(
            new EntityHandle(1),
            ServantNetworkId: 1,
            InventoryNetworkId: 2,
            SlotIndex: 5));

        work.AddEquipServantEvent(new FamiliarEquipmentWork.EquipServantEventData(
            new EntityHandle(2),
            ServantNetworkId: 1,
            Inventory: playerInventoryHandle,
            SlotIndex: 6));

        work.AddUnequipServantEvent(new FamiliarEquipmentWork.UnequipServantEventData(
            new EntityHandle(3),
            ServantNetworkId: 1));

        work.AddEquipmentTransferEvent(new FamiliarEquipmentWork.EquipmentTransferEventData(
            new EntityHandle(4),
            ServantNetworkId: 1,
            ServantToCharacter: false,
            PlayerCharacter: playerCharacterHandle,
            EquipmentInventory: playerInventoryHandle,
            SlotIndex: 7));

        work.AddEquipmentTransferEvent(new FamiliarEquipmentWork.EquipmentTransferEventData(
            new EntityHandle(5),
            ServantNetworkId: 1,
            ServantToCharacter: true,
            PlayerCharacter: playerCharacterHandle,
            EquipmentInventory: default,
            SlotIndex: 0));

        work.AddTeleportDebugEvent(new FamiliarEquipmentWork.TeleportDebugEventData(
            new EntityHandle(6),
            OwnerCharacter: playerCharacterHandle));

        var registrar = new RecordingRegistrar();
        var requestedQueries = new List<QueryDescription>();

        var context = FactoryTestUtilities.CreateContext(
            registrar,
            query: FactoryTestUtilities.DescribeQuery<FamiliarEquipmentWork>(),
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);

                if (query.Equals(work.EquipFromInventoryQuery))
                {
                    foreach (var handle in work.GetEquipFromInventoryEventHandles())
                    {
                        action(handle);
                    }
                }
                else if (query.Equals(work.EquipServantQuery))
                {
                    foreach (var handle in work.GetEquipServantEventHandles())
                    {
                        action(handle);
                    }
                }
                else if (query.Equals(work.UnequipServantQuery))
                {
                    foreach (var handle in work.GetUnequipServantEventHandles())
                    {
                        action(handle);
                    }
                }
                else if (query.Equals(work.EquipmentTransferQuery))
                {
                    foreach (var handle in work.GetEquipmentTransferEventHandles())
                    {
                        action(handle);
                    }
                }
                else if (query.Equals(work.TeleportDebugQuery))
                {
                    foreach (var handle in work.GetTeleportDebugEventHandles())
                    {
                        action(handle);
                    }
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.EquipFromInventoryQuery, requestedQueries);
        Assert.Contains(work.EquipServantQuery, requestedQueries);
        Assert.Contains(work.UnequipServantQuery, requestedQueries);
        Assert.Contains(work.EquipmentTransferQuery, requestedQueries);
        Assert.Contains(work.TeleportDebugQuery, requestedQueries);

        Assert.Equal(new[]
        {
            (inventoryHandle, 5),
            (playerInventoryHandle, 6),
            (playerInventoryHandle, 7),
        },
        validatorCalls);

        Assert.Equal(5, refreshCalls.Count);
        Assert.All(refreshCalls, familiar => Assert.Equal(familiarHandle, familiar));

        Assert.Equal(new[] { 1, 2, 1, 1, 1 }, networkRequests);

        Assert.Contains((familiarHandle, playerCharacterHandle), work.TeleportExpectations);
        Assert.Equal(new[] { (familiarHandle, playerCharacterHandle) }, teleportCallback);
    }
}
