using System;
using ProjectM;
using Stunlock.Core;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the <see cref="Systems.Quests.QuestTargetSystem"/> setup behaviour.
/// </summary>
public sealed class QuestTargetWork : ISystemWork
{
    /// <summary>
    /// Delegate invoked when a target entity should be added or updated in the cache.
    /// </summary>
    /// <param name="entity">Entity handle representing the quest target.</param>
    public delegate void TargetCacheUpdater(EntityHandle entity);

    /// <summary>
    /// Delegate invoked when a target entity should be tracked as blacklisted.
    /// </summary>
    /// <param name="entity">Entity handle representing the blacklisted target.</param>
    public delegate void BlacklistUpdater(EntityHandle entity);

    /// <summary>
    /// Delegate invoked when an imprisoned unit should be tracked.
    /// </summary>
    /// <param name="entity">Entity handle representing the imprisoned unit.</param>
    public delegate void ImprisonedUpdater(EntityHandle entity);

    /// <summary>
    /// Initial capacity assigned to the target unit cache.
    /// </summary>
    public const int TargetUnitsCapacity = 1024;

    /// <summary>
    /// Initial capacity assigned to the blacklisted unit cache.
    /// </summary>
    public const int BlacklistedUnitsCapacity = 256;

    /// <summary>
    /// Initial capacity assigned to the imprisoned unit cache.
    /// </summary>
    public const int ImprisonedUnitsCapacity = 512;

    static QueryDescription CreateTargetQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<PrefabGUID>();
        builder.AddAllReadOnly<Health>();
        builder.AddAllReadOnly<UnitLevel>();
        builder.AddAllReadOnly<UnitStats>();
        builder.AddAllReadOnly<Movement>();
        builder.AddAllReadOnly<AggroConsumer>();

        builder.AddNone(ComponentRequirements.ReadOnly<Minion>());
        builder.AddNone(ComponentRequirements.ReadOnly<DestroyOnSpawn>());
        builder.AddNone(ComponentRequirements.ReadOnly<Trader>());
        builder.AddNone(ComponentRequirements.ReadOnly<BlockFeedBuff>());

        builder.WithOptions(EntityQueryOptions.IncludeDisabled);
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateImprisonedQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<Buff>();
        builder.AddAllReadOnly<ImprisonedBuff>();
        builder.WithOptions(EntityQueryOptions.IncludeDisabled);
        // The production system keeps this query optional because imprisoned units
        // are processed opportunistically rather than gating the system update.
        return builder.Describe(requireForUpdate: false);
    }

    static readonly QueryDescription targetQuery = CreateTargetQuery();
    static readonly QueryDescription imprisonedQuery = CreateImprisonedQuery();

    readonly TargetCacheUpdater? targetUpdater;
    readonly BlacklistUpdater? blacklistUpdater;
    readonly ImprisonedUpdater? imprisonedUpdater;
    readonly Func<EntityHandle, bool>? targetFilter;
    readonly Func<EntityHandle, bool>? blacklistEvaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestTargetWork"/> class.
    /// </summary>
    public QuestTargetWork()
        : this(null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuestTargetWork"/> class.
    /// </summary>
    /// <param name="targetUpdater">Callback used when caching viable targets.</param>
    /// <param name="blacklistUpdater">Callback used when caching blacklisted targets.</param>
    /// <param name="imprisonedUpdater">Callback used when caching imprisoned targets.</param>
    /// <param name="targetFilter">Predicate controlling which targets are processed.</param>
    /// <param name="blacklistEvaluator">Predicate controlling which targets are marked as blacklisted.</param>
    public QuestTargetWork(
        TargetCacheUpdater? targetUpdater,
        BlacklistUpdater? blacklistUpdater,
        ImprisonedUpdater? imprisonedUpdater,
        Func<EntityHandle, bool>? targetFilter,
        Func<EntityHandle, bool>? blacklistEvaluator)
    {
        this.targetUpdater = targetUpdater;
        this.blacklistUpdater = blacklistUpdater;
        this.imprisonedUpdater = imprisonedUpdater;
        this.targetFilter = targetFilter;
        this.blacklistEvaluator = blacklistEvaluator;
    }

    /// <summary>
    /// Gets the primary target query description used by the work.
    /// </summary>
    public QueryDescription TargetQuery => targetQuery;

    /// <summary>
    /// Gets the imprisoned query description used by the work.
    /// </summary>
    public QueryDescription ImprisonedQuery => imprisonedQuery;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<PrefabGUID>();
        builder.AddAllReadOnly<Health>();
        builder.AddAllReadOnly<UnitLevel>();
        builder.AddAllReadOnly<UnitStats>();
        builder.AddAllReadOnly<Movement>();
        builder.AddAllReadOnly<AggroConsumer>();

        builder.AddNone(ComponentRequirements.ReadOnly<Minion>());
        builder.AddNone(ComponentRequirements.ReadOnly<DestroyOnSpawn>());
        builder.AddNone(ComponentRequirements.ReadOnly<Trader>());
        builder.AddNone(ComponentRequirements.ReadOnly<BlockFeedBuff>());

        builder.WithOptions(EntityQueryOptions.IncludeDisabled);
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetEntityTypeHandle();
            _ = facade.GetEntityStorageInfoLookup();
            _ = facade.GetComponentTypeHandle<PrefabGUID>(isReadOnly: true);
            _ = facade.GetComponentTypeHandle<Buff>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        if (targetUpdater != null || blacklistUpdater != null || targetFilter != null || blacklistEvaluator != null)
        {
            context.ForEachEntity(targetQuery, ProcessTarget);
        }

        if (imprisonedUpdater != null)
        {
            context.ForEachEntity(imprisonedQuery, entity => imprisonedUpdater?.Invoke(entity));
        }
    }

    void ProcessTarget(EntityHandle entity)
    {
        if (targetFilter != null && !targetFilter(entity))
        {
            return;
        }

        targetUpdater?.Invoke(entity);

        if (blacklistUpdater != null)
        {
            var isBlacklisted = blacklistEvaluator?.Invoke(entity) ?? false;
            if (isBlacklisted)
            {
                blacklistUpdater(entity);
            }
        }
    }
}
