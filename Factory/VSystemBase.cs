using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Factory;

/// <summary>
/// Provides a reusable base implementation for DOTS <see cref="SystemBase"/> workflows that delegate
/// their behavior to strongly typed work objects.
/// </summary>
/// <typeparam name="TWork">Work definition executed by the system.</typeparam>
public abstract class VSystemBase<TWork> : SystemBase, IRegistrar
    where TWork : class, ISystemWork, new()
{
    readonly List<Action<SystemBase>> _refreshActions = new();

    EntityTypeHandle _entityTypeHandle;
    EntityStorageInfoLookup _entityStorageInfoLookup;
    EntityQuery _query;

    protected TWork Work { get; private set; } = new();

    /// <summary>
    /// Gets the entity type handle refreshed each update.
    /// </summary>
    protected ref EntityTypeHandle EntityTypeHandle => ref _entityTypeHandle;

    /// <summary>
    /// Gets the entity storage info lookup refreshed each update.
    /// </summary>
    protected ref EntityStorageInfoLookup EntityStorageInfoLookup => ref _entityStorageInfoLookup;

    /// <summary>
    /// Gets the entity query backing the system.
    /// </summary>
    protected EntityQuery Query => _query;

    public override void OnCreate()
    {
        base.OnCreate();

        BuildQuery();

        if (Work.RequireForUpdate)
            RequireForUpdate(_query);

        RefreshEntityHandles();

        Work.OnCreate(CreateContext());
    }

    public override void OnStartRunning()
    {
        base.OnStartRunning();
        Work.OnStartRunning(CreateContext());
    }

    public override void OnStopRunning()
    {
        Work.OnStopRunning(CreateContext());
        base.OnStopRunning();
    }

    public override void OnDestroy()
    {
        Work.OnDestroy(CreateContext());
        _refreshActions.Clear();
        base.OnDestroy();
    }

    public override void OnUpdate()
    {
        RefreshEntityHandles();
        RunRefreshActions();
        Work.OnUpdate(CreateContext());
    }

    /// <summary>
    /// Executes the supplied callback with a temporary entity array for the provided query.
    /// </summary>
    protected void WithTempEntities(EntityQuery query, Action<NativeArray<Entity>> action)
    {
        if (query == default)
            throw new ArgumentNullException(nameof(query));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var entities = query.ToEntityArray(Allocator.Temp);
        try
        {
            action(entities);
        }
        finally
        {
            if (entities.IsCreated)
                entities.Dispose();
        }
    }

    /// <summary>
    /// Iterates every entity in the query using a temporary array allocation.
    /// </summary>
    protected void ForEachEntity(EntityQuery query, Action<Entity> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        WithTempEntities(query, entities =>
        {
            for (int i = 0; i < entities.Length; ++i)
            {
                action(entities[i]);
            }
        });
    }

    /// <summary>
    /// Executes the supplied callback with a temporary archetype chunk array for the provided query.
    /// </summary>
    protected void WithTempChunks(EntityQuery query, Action<NativeArray<ArchetypeChunk>> action)
    {
        if (query == default)
            throw new ArgumentNullException(nameof(query));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
        try
        {
            action(chunks);
        }
        finally
        {
            if (chunks.IsCreated)
                chunks.Dispose();
        }
    }

    /// <summary>
    /// Iterates every chunk in the query using a temporary array allocation.
    /// </summary>
    protected void ForEachChunk(EntityQuery query, Action<ArchetypeChunk> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        WithTempChunks(query, chunks =>
        {
            for (int i = 0; i < chunks.Length; ++i)
            {
                action(chunks[i]);
            }
        });
    }

    /// <summary>
    /// Determines whether the entity still exists according to the latest storage lookup.
    /// </summary>
    protected new bool Exists(Entity entity) => _entityStorageInfoLookup.Exists(entity);

    void IRegistrar.Register(Action<SystemBase> refreshAction)
    {
        if (refreshAction == null)
            throw new ArgumentNullException(nameof(refreshAction));

        _refreshActions.Add(refreshAction);
    }

    void BuildQuery()
    {
        var builder = new EntityQueryBuilder(Allocator.Temp);
        try
        {
            Work.Build(ref builder);
            _query = EntityManager.CreateEntityQuery(ref builder);
        }
        finally
        {
            builder.Dispose();
        }
    }

    void RefreshEntityHandles()
    {
        _entityTypeHandle = GetEntityTypeHandle();
        _entityStorageInfoLookup = GetEntityStorageInfoLookup();
    }

    void RunRefreshActions()
    {
        if (_refreshActions.Count == 0)
            return;

        foreach (var action in _refreshActions)
        {
            action(this);
        }
    }

    SystemContext CreateContext() => new(
        this,
        EntityManager,
        _query,
        _entityTypeHandle,
        _entityStorageInfoLookup,
        this,
        WithTempEntities,
        ForEachEntity,
        WithTempChunks,
        ForEachChunk,
        Exists);
}
