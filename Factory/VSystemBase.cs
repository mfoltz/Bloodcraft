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
    where TWork : class, ISystemWork
{
    readonly List<Action<SystemBase>> _refreshActions = new();
    readonly List<IDisposable> _managedDisposables = new();
    readonly List<Action<EntityCommandBuffer>> _destroyTagCleanupActions = new();

    EntityTypeHandle _entityTypeHandle;
    EntityStorageInfoLookup _entityStorageInfoLookup;
    EntityQuery _query;

    EndSimulationEntityCommandBufferSystem? _destroyTagEcbSystem;

    protected TWork Work { get; }

    protected VSystemBase()
        : this(CreateDefaultWork())
    {
    }

    protected VSystemBase(TWork work)
    {
        Work = work ?? throw new ArgumentNullException(nameof(work));
    }

    protected VSystemBase(Func<TWork> workFactory)
        : this(CreateFromFactory(workFactory))
    {
    }

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
        _destroyTagCleanupActions.Clear();
        DisposeManagedResources();
        base.OnDestroy();
    }

    public override void OnUpdate()
    {
        RefreshEntityHandles();
        RunRefreshActions();
        Work.OnUpdate(CreateContext());
        RunDestroyTagCleanup();
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

    void RegisterDisposable(IDisposable disposable)
    {
        if (disposable == null)
            throw new ArgumentNullException(nameof(disposable));

        _managedDisposables.Add(disposable);
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
        Exists,
        EnqueueDestroyTagCleanup,
        RegisterDisposable);

    void EnqueueDestroyTagCleanup(Action<EntityCommandBuffer> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _destroyTagCleanupActions.Add(action);
    }

    void RunDestroyTagCleanup()
    {
        if (_destroyTagCleanupActions.Count == 0)
            return;

        var ecbSystem = GetDestroyTagEcbSystem();
        var commandBuffer = ecbSystem.CreateCommandBuffer();

        for (int i = 0; i < _destroyTagCleanupActions.Count; ++i)
        {
            _destroyTagCleanupActions[i]?.Invoke(commandBuffer);
        }

        ecbSystem.AddJobHandleForProducer(Dependency);
        _destroyTagCleanupActions.Clear();
    }

    EndSimulationEntityCommandBufferSystem GetDestroyTagEcbSystem()
    {
        _destroyTagEcbSystem ??= World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>()
            ?? World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();

        return _destroyTagEcbSystem;
    }

    void DisposeManagedResources()
    {
        if (_managedDisposables.Count == 0)
            return;

        for (int i = 0; i < _managedDisposables.Count; ++i)
        {
            _managedDisposables[i]?.Dispose();
        }

        _managedDisposables.Clear();
    }

    static TWork CreateDefaultWork()
    {
        var work = Activator.CreateInstance<TWork>();
        if (work == null)
            throw new InvalidOperationException($"Unable to create an instance of {typeof(TWork)}.");

        return work;
    }

    static TWork CreateFromFactory(Func<TWork> workFactory)
    {
        if (workFactory == null)
            throw new ArgumentNullException(nameof(workFactory));

        var work = workFactory();
        if (work == null)
            throw new InvalidOperationException("Work factory produced a null instance.");

        return work;
    }
}
