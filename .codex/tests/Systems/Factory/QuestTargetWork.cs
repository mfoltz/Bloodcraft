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

        registrar.Register(static system =>
        {
            if (system is not IRefreshRegistrationContext refreshContext)
                throw new InvalidOperationException("The registrar provided a system that does not support refresh facade creation.");

            var facade = refreshContext.CreateFacade();
            _ = facade.GetEntityTypeHandle();
            _ = facade.GetEntityStorageInfoLookup();
            _ = facade.GetComponentTypeHandle<PrefabGUID>(isReadOnly: true);
            _ = facade.GetComponentTypeHandle<Buff>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
    }
}
