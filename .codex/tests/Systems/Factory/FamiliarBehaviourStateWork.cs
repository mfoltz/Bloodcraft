using System;
using System.Collections.Generic;
using ProjectM;
using ProjectM.Behaviours;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition that mirrors the familiar behaviour-state orchestration implemented by
/// <see cref="Patches.BehaviourStateChangedSystemPatch"/>.
/// </summary>
public sealed class FamiliarBehaviourStateWork : ISystemWork
{
    /// <summary>
    /// Delegate invoked when the familiar should command its minions to follow again.
    /// </summary>
    /// <param name="familiar">Familiar entity handle being processed.</param>
    public delegate void FamiliarMinionHandler(EntityHandle familiar);

    /// <summary>
    /// Delegate invoked when the familiar should return to its owning player.
    /// </summary>
    /// <param name="owner">Player character entity handle.</param>
    /// <param name="familiar">Familiar entity handle being processed.</param>
    public delegate void FamiliarReturnHandler(EntityHandle owner, EntityHandle familiar);

    /// <summary>
    /// Represents cached data associated with a behaviour-state changed event.
    /// </summary>
    /// <param name="EventEntity">Entity that raised the state changed event.</param>
    /// <param name="TargetEntity">Familiar entity affected by the state change.</param>
    /// <param name="NewState">State being transitioned to.</param>
    /// <param name="FollowedPlayer">Owning player resolved through the follower component.</param>
    /// <param name="HasEventComponent">Indicates whether the source entity still has the event component.</param>
    /// <param name="TargetHasBehaviourState">Indicates whether the target exposes a <see cref="BehaviourTreeState"/> component.</param>
    /// <param name="TargetHasBlockFeedBuff">Indicates whether the target has an attached <see cref="BlockFeedBuff"/>.</param>
    /// <param name="TargetHasFollower">Indicates whether the target still has its follower component.</param>
    public readonly record struct BehaviourStateEventData(
        EntityHandle EventEntity,
        EntityHandle TargetEntity,
        GenericEnemyState NewState,
        EntityHandle? FollowedPlayer = null,
        bool HasEventComponent = true,
        bool TargetHasBehaviourState = true,
        bool TargetHasBlockFeedBuff = true,
        bool TargetHasFollower = true);

    static QueryDescription CreateBehaviourStateChangedQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadWrite<BehaviourTreeStateChangedEvent>();
        builder.AddAllReadWrite<BehaviourTreeState>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription behaviourStateChangedQuery = CreateBehaviourStateChangedQuery();

    readonly FamiliarMinionHandler? familiarMinionHandler;
    readonly FamiliarReturnHandler? familiarReturner;

    Dictionary<EntityHandle, BehaviourStateEventData>? behaviourEvents;
    List<EntityHandle>? behaviourEventOrder;
    Dictionary<EntityHandle, GenericEnemyState>? behaviourStates;

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarBehaviourStateWork"/> class.
    /// </summary>
    public FamiliarBehaviourStateWork()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarBehaviourStateWork"/> class.
    /// </summary>
    /// <param name="familiarMinionHandler">Optional delegate invoked for return-to-follow transitions.</param>
    /// <param name="familiarReturner">Optional delegate invoked for idle transitions.</param>
    public FamiliarBehaviourStateWork(
        FamiliarMinionHandler? familiarMinionHandler,
        FamiliarReturnHandler? familiarReturner)
    {
        this.familiarMinionHandler = familiarMinionHandler;
        this.familiarReturner = familiarReturner;
        behaviourEvents = null;
        behaviourEventOrder = null;
        behaviourStates = null;
    }

    Dictionary<EntityHandle, BehaviourStateEventData> EventMap => behaviourEvents ??= new();

    List<EntityHandle> EventOrder => behaviourEventOrder ??= new();

    Dictionary<EntityHandle, GenericEnemyState> StateMap => behaviourStates ??= new();

    /// <summary>
    /// Gets the behaviour-state changed query used during iteration.
    /// </summary>
    public QueryDescription BehaviourStateChangedQuery => behaviourStateChangedQuery;

    /// <summary>
    /// Gets the cached behaviour events keyed by their source entity handle.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, BehaviourStateEventData> BehaviourEvents => EventMap;

    /// <summary>
    /// Gets the iteration order of behaviour event entities.
    /// </summary>
    public IReadOnlyList<EntityHandle> BehaviourEventOrder => EventOrder;

    /// <summary>
    /// Gets the cached behaviour state values for familiar entities.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, GenericEnemyState> FamiliarBehaviourStates => StateMap;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadWrite<BehaviourTreeStateChangedEvent>();
        builder.AddAllReadWrite<BehaviourTreeState>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetComponentLookup<BehaviourTreeStateChangedEvent>(isReadOnly: false);
            _ = facade.GetComponentLookup<BehaviourTreeState>(isReadOnly: false);
            _ = facade.GetComponentLookup<BlockFeedBuff>(isReadOnly: true);
            _ = facade.GetComponentLookup<Follower>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        EnsureCollections();
        context.ForEachEntity(behaviourStateChangedQuery, ProcessBehaviourEvent);
    }

    void ProcessBehaviourEvent(EntityHandle eventEntity)
    {
        if (!EventMap.TryGetValue(eventEntity, out var eventData))
        {
            return;
        }

        if (!eventData.HasEventComponent)
        {
            return;
        }

        if (!eventData.TargetHasBlockFeedBuff)
        {
            return;
        }

        if (!eventData.TargetHasFollower)
        {
            return;
        }

        if (eventData.FollowedPlayer is not EntityHandle player)
        {
            return;
        }

        if (!eventData.TargetHasBehaviourState)
        {
            return;
        }

        if (!StateMap.TryGetValue(eventData.TargetEntity, out _))
        {
            return;
        }

        switch (eventData.NewState)
        {
            case GenericEnemyState.Return:
            {
                StateMap[eventData.TargetEntity] = GenericEnemyState.Follow;
                var updatedEvent = eventData with { NewState = GenericEnemyState.Follow };
                EventMap[eventEntity] = updatedEvent;
                familiarMinionHandler?.Invoke(eventData.TargetEntity);
                break;
            }

            case GenericEnemyState.Idle:
            {
                familiarReturner?.Invoke(player, eventData.TargetEntity);
                break;
            }
        }
    }

    void EnsureCollections()
    {
        _ = EventMap;
        _ = EventOrder;
        _ = StateMap;
    }

    /// <summary>
    /// Adds a behaviour-state event to be processed during the next tick.
    /// </summary>
    /// <param name="eventData">Event data to add.</param>
    public void AddBehaviourEvent(BehaviourStateEventData eventData)
    {
        EventMap[eventData.EventEntity] = eventData;

        if (!EventOrder.Contains(eventData.EventEntity))
        {
            EventOrder.Add(eventData.EventEntity);
        }
    }

    /// <summary>
    /// Attempts to retrieve the cached event data for the specified source entity.
    /// </summary>
    /// <param name="eventEntity">Event entity handle being queried.</param>
    /// <param name="eventData">When this method returns, contains the cached event data if available.</param>
    /// <returns><c>true</c> when the event exists in the cache.</returns>
    public bool TryGetBehaviourEvent(EntityHandle eventEntity, out BehaviourStateEventData eventData)
    {
        return EventMap.TryGetValue(eventEntity, out eventData);
    }

    /// <summary>
    /// Records the current behaviour state value for the specified familiar.
    /// </summary>
    /// <param name="familiar">Familiar entity handle.</param>
    /// <param name="state">Behaviour state value to cache.</param>
    public void SetBehaviourState(EntityHandle familiar, GenericEnemyState state)
    {
        StateMap[familiar] = state;
    }

    /// <summary>
    /// Attempts to retrieve the cached behaviour state for the specified familiar.
    /// </summary>
    /// <param name="familiar">Familiar entity handle.</param>
    /// <param name="state">When this method returns, contains the cached state value if available.</param>
    /// <returns><c>true</c> when the familiar has an associated state.</returns>
    public bool TryGetBehaviourState(EntityHandle familiar, out GenericEnemyState state)
    {
        return StateMap.TryGetValue(familiar, out state);
    }
}
