using System;
using System.Collections.Generic;
using ProjectM;
using Unity.Entities;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test double that mirrors the familiar minion spawn handling patch.
/// </summary>
public sealed class FamiliarMinionSpawnWork : ISystemWork
{
    /// <summary>
    /// Delegate used to resolve the active familiar for a followed player.
    /// </summary>
    /// <param name="player">Player entity that owns the familiar.</param>
    /// <returns>The familiar entity when available.</returns>
    public delegate EntityHandle? ActiveFamiliarResolver(EntityHandle player);

    /// <summary>
    /// Delegate used to schedule lifetime expiry for tracked minions.
    /// </summary>
    /// <param name="minion">Minion entity whose lifetime should be limited.</param>
    /// <param name="lifetimeSeconds">Lifetime value in seconds.</param>
    public delegate void LifetimeScheduler(EntityHandle minion, float lifetimeSeconds);

    /// <summary>
    /// Represents cached data associated with a spawn entry.
    /// </summary>
    /// <param name="Minion">Spawned minion entity.</param>
    /// <param name="Owner">Owner associated with the minion.</param>
    /// <param name="OwnerExists">Indicates whether the owner exists.</param>
    /// <param name="FollowedPlayer">Optional player followed by the owner.</param>
    /// <param name="OwnerHasBlockFeedBuff">Indicates whether the owner has a <see cref="BlockFeedBuff"/> component.</param>
    /// <param name="OwnerDisabled">Indicates whether the owner is disabled.</param>
    public readonly record struct SpawnEntry(
        EntityHandle Minion,
        EntityHandle Owner,
        bool OwnerExists = true,
        EntityHandle? FollowedPlayer = null,
        bool OwnerHasBlockFeedBuff = false,
        bool OwnerDisabled = false);

    /// <summary>
    /// Lifetime applied to familiar minions.
    /// </summary>
    public const float FamiliarMinionLifetimeSeconds = 30f;

    static QueryDescription CreateSpawnQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<EntityOwner>();
        builder.AddAllReadOnly<Minion>();
        builder.AddAllReadOnly<SpawnTag>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription spawnQuery = CreateSpawnQuery();

    readonly ActiveFamiliarResolver? familiarResolver;
    readonly LifetimeScheduler? lifetimeScheduler;

    Dictionary<EntityHandle, SpawnEntry>? spawnEntries;
    List<EntityHandle>? spawnOrder;
    Dictionary<EntityHandle, HashSet<EntityHandle>>? familiarMinions;
    HashSet<EntityHandle>? destroyedMinions;
    Dictionary<EntityHandle, EntityHandle>? reassignedOwners;

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarMinionSpawnWork"/> class.
    /// </summary>
    public FamiliarMinionSpawnWork()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarMinionSpawnWork"/> class.
    /// </summary>
    /// <param name="familiarResolver">Optional resolver used to fetch active familiars.</param>
    /// <param name="lifetimeScheduler">Optional scheduler used to limit minion lifetimes.</param>
    public FamiliarMinionSpawnWork(
        ActiveFamiliarResolver? familiarResolver,
        LifetimeScheduler? lifetimeScheduler)
    {
        this.familiarResolver = familiarResolver;
        this.lifetimeScheduler = lifetimeScheduler;
        spawnEntries = null;
        spawnOrder = null;
        familiarMinions = null;
        destroyedMinions = null;
        reassignedOwners = null;
    }

    Dictionary<EntityHandle, SpawnEntry> EntryMap => spawnEntries ??= new();

    List<EntityHandle> EntryOrder => spawnOrder ??= new();

    Dictionary<EntityHandle, HashSet<EntityHandle>> FamiliarMinionMap => familiarMinions ??= new();

    HashSet<EntityHandle> DestroyedSet => destroyedMinions ??= new();

    Dictionary<EntityHandle, EntityHandle> OwnerMap => reassignedOwners ??= new();

    /// <summary>
    /// Gets the spawn query used during iteration.
    /// </summary>
    public QueryDescription SpawnQuery => spawnQuery;

    /// <summary>
    /// Gets the cached spawn entries.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, SpawnEntry> SpawnEntries => EntryMap;

    /// <summary>
    /// Gets the iteration order of spawn entities.
    /// </summary>
    public IReadOnlyList<EntityHandle> SpawnOrder => EntryOrder;

    /// <summary>
    /// Gets the tracked familiar minion map.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, HashSet<EntityHandle>> FamiliarMinions => FamiliarMinionMap;

    /// <summary>
    /// Gets the set of destroyed minion entities.
    /// </summary>
    public IReadOnlyCollection<EntityHandle> DestroyedMinionEntities => DestroyedSet;

    /// <summary>
    /// Gets the reassigned minion owners.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, EntityHandle> ReassignedOwners => OwnerMap;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<EntityOwner>();
        builder.AddAllReadOnly<Minion>();
        builder.AddAllReadOnly<SpawnTag>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetComponentLookup<EntityOwner>(isReadOnly: false);
            _ = facade.GetComponentLookup<Minion>(isReadOnly: true);
            _ = facade.GetComponentLookup<BlockFeedBuff>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        EnsureTrackingCollections();
        context.ForEachEntity(spawnQuery, ProcessSpawn);
    }

    void ProcessSpawn(EntityHandle minion)
    {
        if (!EntryMap.TryGetValue(minion, out var entry))
        {
            return;
        }

        if (!entry.OwnerExists)
        {
            return;
        }

        if (entry.FollowedPlayer is EntityHandle player)
        {
            if (familiarResolver?.Invoke(player) is EntityHandle familiar)
            {
                TrackFamiliarMinion(familiar, minion);
                OwnerMap[minion] = player;
            }

            return;
        }

        if (entry.OwnerHasBlockFeedBuff)
        {
            TrackFamiliarMinion(entry.Owner, minion);
            return;
        }

        if (entry.OwnerDisabled)
        {
            DestroyedSet.Add(minion);
        }
    }

    void TrackFamiliarMinion(EntityHandle familiar, EntityHandle minion)
    {
        if (!FamiliarMinionMap.TryGetValue(familiar, out var minions))
        {
            minions = new HashSet<EntityHandle>();
            FamiliarMinionMap[familiar] = minions;
        }

        minions.Add(minion);
        lifetimeScheduler?.Invoke(minion, FamiliarMinionLifetimeSeconds);
    }

    void EnsureTrackingCollections()
    {
        _ = FamiliarMinionMap;
        _ = DestroyedSet;
        _ = OwnerMap;
    }

    /// <summary>
    /// Adds a spawn entry to be processed during the next tick.
    /// </summary>
    /// <param name="entry">Entry to add.</param>
    public void AddSpawnEntry(SpawnEntry entry)
    {
        if (!EntryMap.ContainsKey(entry.Minion))
        {
            EntryOrder.Add(entry.Minion);
        }

        EntryMap[entry.Minion] = entry;
    }

    /// <summary>
    /// Attempts to retrieve a spawn entry by entity handle.
    /// </summary>
    /// <param name="minion">Minion entity handle.</param>
    /// <param name="entry">Retrieved entry.</param>
    public bool TryGetSpawnEntry(EntityHandle minion, out SpawnEntry entry)
    {
        return EntryMap.TryGetValue(minion, out entry);
    }
}
