using System;
using System.Collections.Generic;
using FromCharacter = ProjectM.Network.FromCharacter;
using PlayerTeleportDebugEvent = ProjectM.PlayerTeleportDebugEvent;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition that mirrors the familiar return logic executed during teleport debug events.
/// </summary>
public sealed class FamiliarTeleportReturnWork : ISystemWork
{
    /// <summary>
    /// Delegate used to resolve the active familiar for a teleporting character.
    /// </summary>
    /// <param name="character">Player character requesting the teleport.</param>
    /// <returns>The familiar entity handle when available.</returns>
    public delegate EntityHandle? ActiveFamiliarResolver(EntityHandle character);

    /// <summary>
    /// Delegate used to return the familiar to its owner once it has been resolved.
    /// </summary>
    /// <param name="familiar">Familiar entity handle that should be returned.</param>
    /// <param name="character">Player character associated with the familiar.</param>
    public delegate void FamiliarReturnDelegate(EntityHandle familiar, EntityHandle character);

    /// <summary>
    /// Represents cached data associated with a teleport debug event.
    /// </summary>
    /// <param name="EventEntity">Entity representing the teleport debug request.</param>
    /// <param name="OwnerCharacter">Player character initiating the teleport.</param>
    /// <param name="HasTeleportDebug">Indicates whether the entity has a <see cref="PlayerTeleportDebugEvent"/> component.</param>
    /// <param name="HasFromCharacter">Indicates whether the entity has a <see cref="FromCharacter"/> component.</param>
    public readonly record struct TeleportEventData(
        EntityHandle EventEntity,
        EntityHandle OwnerCharacter,
        bool HasTeleportDebug = true,
        bool HasFromCharacter = true);

    static QueryDescription CreateTeleportQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<PlayerTeleportDebugEvent>();
        builder.AddAllReadOnly<FromCharacter>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription teleportQuery = CreateTeleportQuery();

    readonly ActiveFamiliarResolver? familiarResolver;
    readonly FamiliarReturnDelegate? familiarReturner;

    Dictionary<EntityHandle, TeleportEventData>? teleportEvents;
    List<EntityHandle>? teleportOrder;

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarTeleportReturnWork"/> class.
    /// </summary>
    public FamiliarTeleportReturnWork()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarTeleportReturnWork"/> class.
    /// </summary>
    /// <param name="familiarResolver">Optional resolver used to fetch active familiars.</param>
    /// <param name="familiarReturner">Optional delegate invoked when a familiar should be returned.</param>
    public FamiliarTeleportReturnWork(
        ActiveFamiliarResolver? familiarResolver,
        FamiliarReturnDelegate? familiarReturner)
    {
        this.familiarResolver = familiarResolver;
        this.familiarReturner = familiarReturner;
        teleportEvents = null;
        teleportOrder = null;
    }

    Dictionary<EntityHandle, TeleportEventData> EventMap => teleportEvents ??= new();

    List<EntityHandle> EventOrder => teleportOrder ??= new();

    /// <summary>
    /// Gets the teleport debug query used during iteration.
    /// </summary>
    public QueryDescription TeleportQuery => teleportQuery;

    /// <summary>
    /// Gets the cached teleport debug events keyed by entity handle.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, TeleportEventData> TeleportEvents => EventMap;

    /// <summary>
    /// Gets the iteration order of teleport debug entities.
    /// </summary>
    public IReadOnlyList<EntityHandle> TeleportOrder => EventOrder;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<PlayerTeleportDebugEvent>();
        builder.AddAllReadOnly<FromCharacter>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetComponentLookup<PlayerTeleportDebugEvent>(isReadOnly: true);
            _ = facade.GetComponentLookup<FromCharacter>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        EnsureTrackingCollections();
        context.ForEachEntity(teleportQuery, ProcessTeleportEvent);
    }

    void ProcessTeleportEvent(EntityHandle eventEntity)
    {
        if (!EventMap.TryGetValue(eventEntity, out var eventData))
        {
            return;
        }

        if (!eventData.HasTeleportDebug)
        {
            return;
        }

        if (!eventData.HasFromCharacter)
        {
            return;
        }

        if (familiarResolver == null)
        {
            return;
        }

        EntityHandle? familiar = familiarResolver(eventData.OwnerCharacter);
        if (familiar is not EntityHandle familiarHandle)
        {
            return;
        }

        familiarReturner?.Invoke(familiarHandle, eventData.OwnerCharacter);
    }

    void EnsureTrackingCollections()
    {
        _ = EventMap;
        _ = EventOrder;
    }

    /// <summary>
    /// Adds a teleport debug event to be processed during the next tick.
    /// </summary>
    /// <param name="eventData">Event data to add.</param>
    public void AddTeleportEvent(TeleportEventData eventData)
    {
        if (!EventMap.ContainsKey(eventData.EventEntity))
        {
            EventOrder.Add(eventData.EventEntity);
        }

        EventMap[eventData.EventEntity] = eventData;
    }

    /// <summary>
    /// Attempts to retrieve teleport debug data for a specific entity.
    /// </summary>
    /// <param name="eventEntity">Event entity handle.</param>
    /// <param name="eventData">Retrieved event data when available.</param>
    /// <returns><c>true</c> when the event data exists.</returns>
    public bool TryGetTeleportEvent(EntityHandle eventEntity, out TeleportEventData eventData)
    {
        return EventMap.TryGetValue(eventEntity, out eventData);
    }
}
