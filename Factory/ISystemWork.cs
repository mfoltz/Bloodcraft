using System;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Factory;

/// <summary>
/// Describes how a system constructs its primary <see cref="EntityQuery"/> definition.
/// </summary>
public interface IQuerySpec
{
    /// <summary>
    /// Configures the provided builder with the query's component and option requirements.
    /// </summary>
    /// <param name="builder">Builder that collects the query definition.</param>
    void Build(ref EntityQueryBuilder builder);

    /// <summary>
    /// Indicates whether the generated query must be required for update.
    /// </summary>
    bool RequireForUpdate => true;
}

/// <summary>
/// Provides a registration surface for per-frame refresh actions.
/// </summary>
public interface IRegistrar
{
    /// <summary>
    /// Registers a refresh action that runs at the beginning of each update.
    /// </summary>
    /// <param name="refreshAction">Action invoked before the work executes.</param>
    void Register(Action<SystemBase> refreshAction);
}

/// <summary>
/// Supplies contextual information to work instances during lifecycle events.
/// </summary>
public readonly struct SystemContext
{
    readonly Action<EntityQuery, Action<NativeArray<Entity>>> _withTempEntities;
    readonly Action<EntityQuery, Action<Entity>> _forEachEntity;
    readonly Action<EntityQuery, Action<NativeArray<ArchetypeChunk>>> _withTempChunks;
    readonly Action<EntityQuery, Action<ArchetypeChunk>> _forEachChunk;
    readonly Func<Entity, bool> _exists;

    public SystemContext(
        SystemBase system,
        EntityManager entityManager,
        EntityQuery query,
        EntityTypeHandle entityTypeHandle,
        EntityStorageInfoLookup entityStorageInfoLookup,
        IRegistrar registrar,
        Action<EntityQuery, Action<NativeArray<Entity>>> withTempEntities,
        Action<EntityQuery, Action<Entity>> forEachEntity,
        Action<EntityQuery, Action<NativeArray<ArchetypeChunk>>> withTempChunks,
        Action<EntityQuery, Action<ArchetypeChunk>> forEachChunk,
        Func<Entity, bool> exists)
    {
        System = system ?? throw new ArgumentNullException(nameof(system));
        EntityManager = entityManager;
        Query = query;
        EntityTypeHandle = entityTypeHandle;
        EntityStorageInfoLookup = entityStorageInfoLookup;
        Registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        _withTempEntities = withTempEntities ?? throw new ArgumentNullException(nameof(withTempEntities));
        _forEachEntity = forEachEntity ?? throw new ArgumentNullException(nameof(forEachEntity));
        _withTempChunks = withTempChunks ?? throw new ArgumentNullException(nameof(withTempChunks));
        _forEachChunk = forEachChunk ?? throw new ArgumentNullException(nameof(forEachChunk));
        _exists = exists ?? throw new ArgumentNullException(nameof(exists));
    }

    /// <summary>
    /// Gets the executing <see cref="SystemBase"/> instance.
    /// </summary>
    public SystemBase System { get; }

    /// <summary>
    /// Gets the world-level <see cref="EntityManager"/>.
    /// </summary>
    public EntityManager EntityManager { get; }

    /// <summary>
    /// Gets the primary <see cref="EntityQuery"/> constructed for the system.
    /// </summary>
    public EntityQuery Query { get; }

    /// <summary>
    /// Gets the cached <see cref="EntityTypeHandle"/>.
    /// </summary>
    public EntityTypeHandle EntityTypeHandle { get; }

    /// <summary>
    /// Gets the cached <see cref="EntityStorageInfoLookup"/>.
    /// </summary>
    public EntityStorageInfoLookup EntityStorageInfoLookup { get; }

    /// <summary>
    /// Gets the registrar used to schedule per-update refresh actions.
    /// </summary>
    public IRegistrar Registrar { get; }

    /// <summary>
    /// Executes an action with a temporary entity array for the specified query.
    /// </summary>
    public void WithTempEntities(EntityQuery query, Action<NativeArray<Entity>> action) =>
        _withTempEntities(query, action);

    /// <summary>
    /// Iterates each entity in the specified query.
    /// </summary>
    public void ForEachEntity(EntityQuery query, Action<Entity> action) =>
        _forEachEntity(query, action);

    /// <summary>
    /// Executes an action with a temporary chunk array for the specified query.
    /// </summary>
    public void WithTempChunks(EntityQuery query, Action<NativeArray<ArchetypeChunk>> action) =>
        _withTempChunks(query, action);

    /// <summary>
    /// Iterates each chunk in the specified query.
    /// </summary>
    public void ForEachChunk(EntityQuery query, Action<ArchetypeChunk> action) =>
        _forEachChunk(query, action);

    /// <summary>
    /// Determines whether the entity currently exists.
    /// </summary>
    public bool Exists(Entity entity) => _exists(entity);
}

/// <summary>
/// Defines the lifecycle hooks executed by <see cref="VSystemBase{TWork}"/> implementations.
/// </summary>
public interface ISystemWork : IQuerySpec
{
    /// <summary>
    /// Invoked when the owning system is created.
    /// </summary>
    /// <param name="context">The active system context.</param>
    void OnCreate(SystemContext context) { }

    /// <summary>
    /// Invoked when the owning system starts running.
    /// </summary>
    /// <param name="context">The active system context.</param>
    void OnStartRunning(SystemContext context) { }

    /// <summary>
    /// Invoked at the beginning of each update cycle.
    /// </summary>
    /// <param name="context">The active system context.</param>
    void OnUpdate(SystemContext context) { }

    /// <summary>
    /// Invoked when the owning system stops running.
    /// </summary>
    /// <param name="context">The active system context.</param>
    void OnStopRunning(SystemContext context) { }

    /// <summary>
    /// Invoked when the owning system is destroyed.
    /// </summary>
    /// <param name="context">The active system context.</param>
    void OnDestroy(SystemContext context) { }
}
