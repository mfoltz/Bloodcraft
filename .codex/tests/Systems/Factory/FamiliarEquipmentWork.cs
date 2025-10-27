using System;
using System.Collections.Generic;
using ProjectM;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the familiar equipment and teleport patches.
/// </summary>
public sealed class FamiliarEquipmentWork : ISystemWork
{
    /// <summary>
    /// Delegate used to validate whether an inventory slot should block familiar equipment.
    /// </summary>
    /// <param name="inventory">Inventory entity handle being evaluated.</param>
    /// <param name="slotIndex">Slot index that triggered the evaluation.</param>
    /// <returns><c>true</c> when the equipment is invalid and should be rejected.</returns>
    public delegate bool InventoryValidationDelegate(EntityHandle inventory, int slotIndex);

    /// <summary>
    /// Delegate used to refresh familiar statistics after equipment changes.
    /// </summary>
    /// <param name="familiar">Familiar entity handle whose stats should be refreshed.</param>
    public delegate void StatRefreshDelegate(EntityHandle familiar);

    /// <summary>
    /// Delegate used to resolve network identifiers to entity handles.
    /// </summary>
    /// <param name="networkId">Network identifier to resolve.</param>
    /// <param name="entity">Resolved entity handle when successful.</param>
    /// <returns><c>true</c> when the lookup succeeded.</returns>
    public delegate bool NetworkEntityLookupDelegate(int networkId, out EntityHandle entity);

    /// <summary>
    /// Delegate used to resolve familiars from servants or player characters.
    /// </summary>
    /// <param name="source">Source entity handle (servant or player).</param>
    /// <returns>Resolved familiar entity handle when available.</returns>
    public delegate EntityHandle FamiliarResolverDelegate(EntityHandle source);

    /// <summary>
    /// Represents cached data for a servant inventory equipment event.
    /// </summary>
    /// <param name="EventEntity">Entity associated with the equipment event.</param>
    /// <param name="ServantNetworkId">Network identifier for the servant.</param>
    /// <param name="InventoryNetworkId">Network identifier for the inventory.</param>
    /// <param name="SlotIndex">Inventory slot being modified.</param>
    public readonly record struct EquipFromInventoryEventData(
        EntityHandle EventEntity,
        int ServantNetworkId,
        int InventoryNetworkId,
        int SlotIndex);

    /// <summary>
    /// Represents cached data for an equipment event sourced from a player inventory.
    /// </summary>
    /// <param name="EventEntity">Entity associated with the equipment event.</param>
    /// <param name="ServantNetworkId">Network identifier for the servant.</param>
    /// <param name="Inventory">Player inventory handle providing the equipment.</param>
    /// <param name="SlotIndex">Inventory slot being modified.</param>
    public readonly record struct EquipServantEventData(
        EntityHandle EventEntity,
        int ServantNetworkId,
        EntityHandle Inventory,
        int SlotIndex);

    /// <summary>
    /// Represents cached data for a servant unequip request.
    /// </summary>
    /// <param name="EventEntity">Entity associated with the unequip event.</param>
    /// <param name="ServantNetworkId">Network identifier for the servant.</param>
    public readonly record struct UnequipServantEventData(
        EntityHandle EventEntity,
        int ServantNetworkId);

    /// <summary>
    /// Represents cached data for an equipment transfer event.
    /// </summary>
    /// <param name="EventEntity">Entity associated with the transfer event.</param>
    /// <param name="ServantNetworkId">Network identifier for the servant involved in the transfer.</param>
    /// <param name="ServantToCharacter">Indicates whether the transfer moved equipment from the servant to the character.</param>
    /// <param name="PlayerCharacter">Player character associated with the transfer.</param>
    /// <param name="EquipmentInventory">Inventory that provided the equipment for servant transfers.</param>
    /// <param name="SlotIndex">Inventory slot being modified.</param>
    public readonly record struct EquipmentTransferEventData(
        EntityHandle EventEntity,
        int ServantNetworkId,
        bool ServantToCharacter,
        EntityHandle PlayerCharacter,
        EntityHandle EquipmentInventory,
        int SlotIndex);

    /// <summary>
    /// Represents cached data for a teleport debug event.
    /// </summary>
    /// <param name="EventEntity">Entity associated with the teleport event.</param>
    /// <param name="OwnerCharacter">Player character requesting the teleport.</param>
    public readonly record struct TeleportDebugEventData(
        EntityHandle EventEntity,
        EntityHandle OwnerCharacter);

    static QueryDescription CreateEquipFromInventoryQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<EquipServantItemFromInventoryEvent>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateEquipServantQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<EquipServantItemEvent>();
        builder.AddAllReadOnly<FromCharacter>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateUnequipServantQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<UnequipServantItemEvent>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateEquipmentTransferQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<EquipmentToEquipmentTransferEvent>();
        builder.AddAllReadOnly<FromCharacter>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateTeleportDebugQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<PlayerTeleportDebugEvent>();
        builder.AddAllReadOnly<FromCharacter>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription equipFromInventoryQuery = CreateEquipFromInventoryQuery();
    static readonly QueryDescription equipServantQuery = CreateEquipServantQuery();
    static readonly QueryDescription unequipServantQuery = CreateUnequipServantQuery();
    static readonly QueryDescription equipmentTransferQuery = CreateEquipmentTransferQuery();
    static readonly QueryDescription teleportDebugQuery = CreateTeleportDebugQuery();

    InventoryValidationDelegate? inventoryValidator;
    StatRefreshDelegate? statRefresher;
    NetworkEntityLookupDelegate? networkLookup;
    FamiliarResolverDelegate? servantFamiliarResolver;
    FamiliarResolverDelegate? activeFamiliarResolver;
    Action<EntityHandle, EntityHandle>? teleportReturn;

    Dictionary<EntityHandle, EquipFromInventoryEventData>? equipFromInventoryEvents;
    Dictionary<EntityHandle, EquipServantEventData>? equipServantEvents;
    Dictionary<EntityHandle, UnequipServantEventData>? unequipServantEvents;
    Dictionary<EntityHandle, EquipmentTransferEventData>? equipmentTransferEvents;
    Dictionary<EntityHandle, TeleportDebugEventData>? teleportDebugEvents;
    Dictionary<int, EntityHandle>? networkEntities;
    List<(EntityHandle Familiar, EntityHandle Owner)>? teleportExpectations;

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarEquipmentWork"/> class.
    /// </summary>
    public FamiliarEquipmentWork()
        : this(null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarEquipmentWork"/> class.
    /// </summary>
    /// <param name="inventoryValidator">Optional validation delegate.</param>
    /// <param name="statRefresher">Optional stat refresh delegate.</param>
    /// <param name="networkLookup">Optional network lookup delegate.</param>
    /// <param name="servantFamiliarResolver">Optional servant familiar resolver.</param>
    /// <param name="activeFamiliarResolver">Optional active familiar resolver.</param>
    /// <param name="teleportReturn">Optional teleport return delegate.</param>
    public FamiliarEquipmentWork(
        InventoryValidationDelegate? inventoryValidator,
        StatRefreshDelegate? statRefresher,
        NetworkEntityLookupDelegate? networkLookup,
        FamiliarResolverDelegate? servantFamiliarResolver,
        FamiliarResolverDelegate? activeFamiliarResolver,
        Action<EntityHandle, EntityHandle>? teleportReturn)
    {
        this.inventoryValidator = inventoryValidator;
        this.statRefresher = statRefresher;
        this.networkLookup = networkLookup;
        this.servantFamiliarResolver = servantFamiliarResolver;
        this.activeFamiliarResolver = activeFamiliarResolver;
        this.teleportReturn = teleportReturn;

        equipFromInventoryEvents = null;
        equipServantEvents = null;
        unequipServantEvents = null;
        equipmentTransferEvents = null;
        teleportDebugEvents = null;
        networkEntities = null;
        teleportExpectations = new List<(EntityHandle Familiar, EntityHandle Owner)>();
    }

    /// <summary>
    /// Gets the query used to capture servant equipment requests sourced from other inventories.
    /// </summary>
    public QueryDescription EquipFromInventoryQuery => equipFromInventoryQuery;

    /// <summary>
    /// Gets the query used to capture servant equipment requests sourced from players.
    /// </summary>
    public QueryDescription EquipServantQuery => equipServantQuery;

    /// <summary>
    /// Gets the query used to capture servant unequip requests.
    /// </summary>
    public QueryDescription UnequipServantQuery => unequipServantQuery;

    /// <summary>
    /// Gets the query used to capture equipment transfer requests.
    /// </summary>
    public QueryDescription EquipmentTransferQuery => equipmentTransferQuery;

    /// <summary>
    /// Gets the query used to capture teleport debug events.
    /// </summary>
    public QueryDescription TeleportDebugQuery => teleportDebugQuery;

    /// <summary>
    /// Gets the recorded teleport expectations.
    /// </summary>
    public IReadOnlyList<(EntityHandle Familiar, EntityHandle Owner)> TeleportExpectations
        => teleportExpectations ?? (IReadOnlyList<(EntityHandle Familiar, EntityHandle Owner)>)Array.Empty<(EntityHandle Familiar, EntityHandle Owner)>();

    /// <summary>
    /// Adds a cached equipment-from-inventory event.
    /// </summary>
    public void AddEquipFromInventoryEvent(EquipFromInventoryEventData eventData)
    {
        (equipFromInventoryEvents ??= new()).Add(eventData.EventEntity, eventData);
    }

    /// <summary>
    /// Adds a cached equipment-from-player event.
    /// </summary>
    public void AddEquipServantEvent(EquipServantEventData eventData)
    {
        (equipServantEvents ??= new()).Add(eventData.EventEntity, eventData);
    }

    /// <summary>
    /// Adds a cached servant unequip event.
    /// </summary>
    public void AddUnequipServantEvent(UnequipServantEventData eventData)
    {
        (unequipServantEvents ??= new()).Add(eventData.EventEntity, eventData);
    }

    /// <summary>
    /// Adds a cached equipment transfer event.
    /// </summary>
    public void AddEquipmentTransferEvent(EquipmentTransferEventData eventData)
    {
        (equipmentTransferEvents ??= new()).Add(eventData.EventEntity, eventData);
    }

    /// <summary>
    /// Adds a cached teleport debug event.
    /// </summary>
    public void AddTeleportDebugEvent(TeleportDebugEventData eventData)
    {
        (teleportDebugEvents ??= new()).Add(eventData.EventEntity, eventData);
    }

    /// <summary>
    /// Adds a mocked network entity lookup entry.
    /// </summary>
    /// <param name="networkId">Network identifier.</param>
    /// <param name="entity">Entity handle associated with the identifier.</param>
    public void AddNetworkEntity(int networkId, EntityHandle entity)
    {
        (networkEntities ??= new())[networkId] = entity;
    }

    /// <summary>
    /// Gets the cached equipment-from-inventory event handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetEquipFromInventoryEventHandles() => GetHandles(equipFromInventoryEvents);

    /// <summary>
    /// Gets the cached equipment-from-player event handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetEquipServantEventHandles() => GetHandles(equipServantEvents);

    /// <summary>
    /// Gets the cached servant unequip event handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetUnequipServantEventHandles() => GetHandles(unequipServantEvents);

    /// <summary>
    /// Gets the cached equipment transfer event handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetEquipmentTransferEventHandles() => GetHandles(equipmentTransferEvents);

    /// <summary>
    /// Gets the cached teleport debug event handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetTeleportDebugEventHandles() => GetHandles(teleportDebugEvents);

    static IReadOnlyList<EntityHandle> GetHandles<T>(Dictionary<EntityHandle, T>? source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<EntityHandle>();
        }

        var handles = new EntityHandle[source.Count];
        var index = 0;
        foreach (var key in source.Keys)
        {
            handles[index++] = key;
        }

        return handles;
    }

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAny(ComponentRequirements.ReadOnly<EquipServantItemFromInventoryEvent>());
        builder.AddAny(ComponentRequirements.ReadOnly<EquipServantItemEvent>());
        builder.AddAny(ComponentRequirements.ReadOnly<UnequipServantItemEvent>());
        builder.AddAny(ComponentRequirements.ReadOnly<EquipmentToEquipmentTransferEvent>());
        builder.AddAny(ComponentRequirements.ReadOnly<PlayerTeleportDebugEvent>());
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetComponentLookup<BlockFeedBuff>();
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        if (equipFromInventoryEvents != null && equipFromInventoryEvents.Count > 0)
        {
            context.ForEachEntity(equipFromInventoryQuery, ProcessEquipFromInventoryEvent);
        }

        if (equipServantEvents != null && equipServantEvents.Count > 0)
        {
            context.ForEachEntity(equipServantQuery, ProcessEquipServantEvent);
        }

        if (unequipServantEvents != null && unequipServantEvents.Count > 0)
        {
            context.ForEachEntity(unequipServantQuery, ProcessUnequipServantEvent);
        }

        if (equipmentTransferEvents != null && equipmentTransferEvents.Count > 0)
        {
            context.ForEachEntity(equipmentTransferQuery, ProcessEquipmentTransferEvent);
        }

        if (teleportDebugEvents != null && teleportDebugEvents.Count > 0)
        {
            context.ForEachEntity(teleportDebugQuery, ProcessTeleportDebugEvent);
        }
    }

    void ProcessEquipFromInventoryEvent(EntityHandle handle)
    {
        if (equipFromInventoryEvents == null)
            return;

        if (!equipFromInventoryEvents.TryGetValue(handle, out var eventData))
            return;

        if (!TryResolveNetworkEntity(eventData.ServantNetworkId, out var servant))
            return;

        if (!TryResolveNetworkEntity(eventData.InventoryNetworkId, out var inventory))
            return;

        var familiar = ResolveServantFamiliar(servant);
        if (!IsValid(familiar))
            return;

        if (InventoryIsInvalid(inventory, eventData.SlotIndex))
            return;

        RefreshStats(familiar);
    }

    void ProcessEquipServantEvent(EntityHandle handle)
    {
        if (equipServantEvents == null)
            return;

        if (!equipServantEvents.TryGetValue(handle, out var eventData))
            return;

        if (!TryResolveNetworkEntity(eventData.ServantNetworkId, out var servant))
            return;

        var familiar = ResolveServantFamiliar(servant);
        if (!IsValid(familiar))
            return;

        if (IsValid(eventData.Inventory) && InventoryIsInvalid(eventData.Inventory, eventData.SlotIndex))
            return;

        RefreshStats(familiar);
    }

    void ProcessUnequipServantEvent(EntityHandle handle)
    {
        if (unequipServantEvents == null)
            return;

        if (!unequipServantEvents.TryGetValue(handle, out var eventData))
            return;

        if (!TryResolveNetworkEntity(eventData.ServantNetworkId, out var servant))
            return;

        var familiar = ResolveServantFamiliar(servant);
        if (!IsValid(familiar))
            return;

        RefreshStats(familiar);
    }

    void ProcessEquipmentTransferEvent(EntityHandle handle)
    {
        if (equipmentTransferEvents == null)
            return;

        if (!equipmentTransferEvents.TryGetValue(handle, out var eventData))
            return;

        if (eventData.ServantToCharacter)
        {
            var familiar = ResolveActiveFamiliar(eventData.PlayerCharacter);
            if (!IsValid(familiar))
                return;

            RefreshStats(familiar);
            return;
        }

        if (!TryResolveNetworkEntity(eventData.ServantNetworkId, out var servant))
            return;

        var servantFamiliar = ResolveServantFamiliar(servant);
        if (!IsValid(servantFamiliar))
            return;

        if (IsValid(eventData.EquipmentInventory) && InventoryIsInvalid(eventData.EquipmentInventory, eventData.SlotIndex))
            return;

        RefreshStats(servantFamiliar);
    }

    void ProcessTeleportDebugEvent(EntityHandle handle)
    {
        if (teleportDebugEvents == null)
            return;

        if (!teleportDebugEvents.TryGetValue(handle, out var eventData))
            return;

        var familiar = ResolveActiveFamiliar(eventData.OwnerCharacter);
        if (!IsValid(familiar))
            return;

        CaptureTeleportExpectation(familiar, eventData.OwnerCharacter);
    }

    bool TryResolveNetworkEntity(int networkId, out EntityHandle entity)
    {
        if (networkLookup != null && networkLookup(networkId, out entity))
        {
            return true;
        }

        if (networkEntities != null && networkEntities.TryGetValue(networkId, out entity))
        {
            return true;
        }

        entity = default;
        return false;
    }

    EntityHandle ResolveServantFamiliar(EntityHandle servant)
    {
        return servantFamiliarResolver?.Invoke(servant) ?? default;
    }

    EntityHandle ResolveActiveFamiliar(EntityHandle owner)
    {
        return activeFamiliarResolver?.Invoke(owner) ?? default;
    }

    bool InventoryIsInvalid(EntityHandle inventory, int slotIndex)
    {
        var validator = inventoryValidator ?? DefaultInventoryValidator;
        return validator(inventory, slotIndex);
    }

    void RefreshStats(EntityHandle familiar)
    {
        var refresher = statRefresher ?? DefaultStatRefresher;
        refresher(familiar);
    }

    void CaptureTeleportExpectation(EntityHandle familiar, EntityHandle owner)
    {
        if (!IsValid(familiar) || !IsValid(owner))
            return;

        (teleportExpectations ??= new()).Add((familiar, owner));
        teleportReturn?.Invoke(familiar, owner);
    }

    static bool IsValid(EntityHandle entity) => entity != default;

    static bool DefaultInventoryValidator(EntityHandle _, int __) => false;

    static void DefaultStatRefresher(EntityHandle _)
    {
    }
}
