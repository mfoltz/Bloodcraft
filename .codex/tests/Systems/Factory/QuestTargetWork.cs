using ProjectM;
using Stunlock.Core;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the <see cref="Systems.Quests.QuestTargetSystem"/> setup behaviour.
/// </summary>
public readonly struct QuestTargetWork : ISystemWork
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
        return builder.Describe(requireForUpdate: true);
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
    public void Build(ref TestEntityQueryBuilder builder)
    {
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
        context.Registrar.Register(facade =>
        {
            _ = facade.GetEntityTypeHandle();
            _ = facade.GetEntityStorageInfoLookup();
            _ = facade.GetComponentTypeHandle<PrefabGUID>(isReadOnly: true);
            _ = facade.GetComponentTypeHandle<Buff>(isReadOnly: true);
        });
    }
}
