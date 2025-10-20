using System;
using System.Collections.Generic;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Represents the level of access a work definition requires for a component type.
/// </summary>
public enum ComponentAccessMode
{
    /// <summary>
    /// The component is accessed for reading and writing.
    /// </summary>
    ReadWrite,

    /// <summary>
    /// The component is accessed in a read-only fashion.
    /// </summary>
    ReadOnly,
}

/// <summary>
/// Mirrors the Unity DOTS entity query options that tests may need to express.
/// </summary>
[Flags]
public enum EntityQueryOptions
{
    /// <summary>
    /// The default query behaviour.
    /// </summary>
    None = 0,

    /// <summary>
    /// Include disabled entities in the query results.
    /// </summary>
    IncludeDisabled = 1 << 0,

    /// <summary>
    /// Include prefab entities in the query results.
    /// </summary>
    IncludePrefab = 1 << 1,

    /// <summary>
    /// Apply write group filtering to the query.
    /// </summary>
    FilterWriteGroup = 1 << 2,
}

/// <summary>
/// Lightweight representation of a Unity DOTS <c>ComponentType</c> used by test infrastructure.
/// </summary>
/// <param name="ElementType">Type requested by the query.</param>
/// <param name="AccessMode">Access level required for the component.</param>
public readonly record struct ComponentType(Type ElementType, ComponentAccessMode AccessMode)
{
    /// <summary>
    /// Creates a read-only component type descriptor for the specified component.
    /// </summary>
    public static ComponentType ReadOnly<TComponent>() => new(typeof(TComponent), ComponentAccessMode.ReadOnly);

    /// <summary>
    /// Creates a read-only component type descriptor for the specified component type.
    /// </summary>
    public static ComponentType ReadOnly(Type componentType) => new(componentType, ComponentAccessMode.ReadOnly);

    /// <summary>
    /// Creates a read-write component type descriptor for the specified component.
    /// </summary>
    public static ComponentType ReadWrite<TComponent>() => new(typeof(TComponent), ComponentAccessMode.ReadWrite);

    /// <summary>
    /// Creates a read-write component type descriptor for the specified component type.
    /// </summary>
    public static ComponentType ReadWrite(Type componentType) => new(componentType, ComponentAccessMode.ReadWrite);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{ElementType.Name} ({AccessMode})";
    }
}

/// <summary>
/// Helper factory methods used when specifying query component requirements.
/// </summary>
public static class ComponentRequirements
{
    /// <summary>
    /// Describes a read-only component requirement.
    /// </summary>
    public static ComponentType ReadOnly<TComponent>() => ComponentType.ReadOnly<TComponent>();

    /// <summary>
    /// Describes a read-only component requirement.
    /// </summary>
    public static ComponentType ReadOnly(Type componentType) => ComponentType.ReadOnly(componentType);

    /// <summary>
    /// Describes a read-write component requirement.
    /// </summary>
    public static ComponentType ReadWrite<TComponent>() => ComponentType.ReadWrite<TComponent>();

    /// <summary>
    /// Describes a read-write component requirement.
    /// </summary>
    public static ComponentType ReadWrite(Type componentType) => ComponentType.ReadWrite(componentType);
}

/// <summary>
/// Lightweight entity query builder used to exercise work definitions inside tests.
/// </summary>
public sealed class TestEntityQueryBuilder
{
    readonly List<ComponentType> all = new();
    readonly List<ComponentType> any = new();
    readonly List<ComponentType> none = new();

    /// <summary>
    /// Gets the collection of all component requirements.
    /// </summary>
    public IReadOnlyList<ComponentType> All => all;

    /// <summary>
    /// Gets the collection of any component requirements.
    /// </summary>
    public IReadOnlyList<ComponentType> Any => any;

    /// <summary>
    /// Gets the collection of none component requirements.
    /// </summary>
    public IReadOnlyList<ComponentType> None => none;

    /// <summary>
    /// Gets the options applied to the query.
    /// </summary>
    public EntityQueryOptions Options { get; private set; }

    /// <summary>
    /// Adds a component requirement that must be present on matching entities.
    /// </summary>
    public void AddAll(ComponentType requirement)
    {
        all.Add(requirement);
    }

    /// <summary>
    /// Adds a component requirement where at least one must be present on matching entities.
    /// </summary>
    public void AddAny(ComponentType requirement)
    {
        any.Add(requirement);
    }

    /// <summary>
    /// Adds a component requirement that must be absent on matching entities.
    /// </summary>
    public void AddNone(ComponentType requirement)
    {
        none.Add(requirement);
    }

    /// <summary>
    /// Adds a read-only component requirement that must be present on matching entities.
    /// </summary>
    public void AddAllReadOnly<TComponent>() => AddAll(ComponentRequirements.ReadOnly<TComponent>());

    /// <summary>
    /// Adds a read-write component requirement that must be present on matching entities.
    /// </summary>
    public void AddAllReadWrite<TComponent>() => AddAll(ComponentRequirements.ReadWrite<TComponent>());

    /// <summary>
    /// Applies an option to the query definition.
    /// </summary>
    public void WithOptions(EntityQueryOptions options)
    {
        Options |= options;
    }

    /// <summary>
    /// Produces an immutable description of the query state collected so far.
    /// </summary>
    public void Describe(out ComponentType[] allComponents, out ComponentType[] anyComponents, out ComponentType[] noneComponents, out EntityQueryOptions options)
    {
        allComponents = all.ToArray();
        anyComponents = any.ToArray();
        noneComponents = none.ToArray();
        options = Options;
    }

    /// <summary>
    /// Produces an immutable description of the query state collected so far.
    /// </summary>
    public QueryDescription Describe(bool requireForUpdate)
    {
        Describe(out var allComponents, out var anyComponents, out var noneComponents, out var options);
        return new QueryDescription(allComponents, anyComponents, noneComponents, options, requireForUpdate);
    }
}

/// <summary>
/// Immutable snapshot of a work definition's query configuration.
/// </summary>
/// <param name="All">Components that must be present on matching entities.</param>
/// <param name="Any">Components where at least one must be present.</param>
/// <param name="None">Components that must be absent.</param>
/// <param name="Options">Query options applied during construction.</param>
/// <param name="RequireForUpdate">Indicates whether the owning system should require the query for updates.</param>
public sealed record QueryDescription(
    IReadOnlyList<ComponentType> All,
    IReadOnlyList<ComponentType> Any,
    IReadOnlyList<ComponentType> None,
    EntityQueryOptions Options,
    bool RequireForUpdate)
{
    /// <summary>
    /// Gets an empty query description.
    /// </summary>
    public static QueryDescription Empty { get; } = new(
        Array.Empty<ComponentType>(),
        Array.Empty<ComponentType>(),
        Array.Empty<ComponentType>(),
        EntityQueryOptions.None,
        true);
}

/// <summary>
/// Interface describing how a work object builds its entity query.
/// </summary>
public interface IQuerySpec
{
    /// <summary>
    /// Describes the component lists and options that form the entity query definition.
    /// </summary>
    /// <param name="all">Components that must be present on matching entities.</param>
    /// <param name="any">Components where at least one must be present.</param>
    /// <param name="none">Components that must be absent.</param>
    /// <param name="options">Additional options applied to the query.</param>
    void DescribeQuery(out ComponentType[] all, out ComponentType[] any, out ComponentType[] none, out EntityQueryOptions options);

    /// <summary>
    /// Indicates whether the constructed query must be required for update.
    /// </summary>
    bool RequireForUpdate => true;
}

/// <summary>
/// Describes the API surface that refresh actions may use.
/// </summary>
public interface ISystemFacade
{
    EntityTypeHandle GetEntityTypeHandle();

    EntityStorageInfoLookup GetEntityStorageInfoLookup();

    ComponentLookup<TComponent> GetComponentLookup<TComponent>(bool isReadOnly = false);

    BufferLookup<TBuffer> GetBufferLookup<TBuffer>(bool isReadOnly = false);

    ComponentTypeHandle<TComponent> GetComponentTypeHandle<TComponent>(bool isReadOnly = false);

    BufferTypeHandle<TBuffer> GetBufferTypeHandle<TBuffer>(bool isReadOnly = false);
}

/// <summary>
/// Supplies a registration surface for per-update refresh actions.
/// </summary>
public interface IRegistrar
{
    /// <summary>
    /// Registers a refresh action that runs at the beginning of each update.
    /// </summary>
    /// <param name="refreshAction">Action invoked before the work executes.</param>
    void Register(Action<ISystemFacade> refreshAction);
}

/// <summary>
/// Represents a lightweight entity handle for tests.
/// </summary>
/// <param name="Id">Unique identifier associated with the entity.</param>
public readonly record struct EntityHandle(int Id);

/// <summary>
/// Represents a lightweight archetype chunk handle for tests.
/// </summary>
/// <param name="Id">Unique identifier associated with the chunk.</param>
public readonly record struct ArchetypeChunkHandle(int Id);

/// <summary>
/// Supplies contextual information to work instances during lifecycle events.
/// </summary>
public readonly struct SystemContext
{
    readonly Action<QueryDescription, Action<IReadOnlyList<EntityHandle>>> withTempEntities;
    readonly Action<QueryDescription, Action<EntityHandle>> forEachEntity;
    readonly Action<QueryDescription, Action<IReadOnlyList<ArchetypeChunkHandle>>> withTempChunks;
    readonly Action<QueryDescription, Action<ArchetypeChunkHandle>> forEachChunk;
    readonly Func<EntityHandle, bool> exists;

    /// <summary>
    /// Initializes a new instance of the <see cref="SystemContext"/> struct.
    /// </summary>
    /// <param name="query">Primary query associated with the work instance.</param>
    /// <param name="registrar">Registrar used to schedule per-update refresh actions.</param>
    /// <param name="withTempEntities">Delegate for performing operations with temporary entity handles.</param>
    /// <param name="forEachEntity">Delegate for iterating entities in a query.</param>
    /// <param name="withTempChunks">Delegate for performing operations with temporary chunks.</param>
    /// <param name="forEachChunk">Delegate for iterating chunks in a query.</param>
    /// <param name="exists">Predicate used to determine whether an entity still exists.</param>
    public SystemContext(
        QueryDescription query,
        IRegistrar registrar,
        Action<QueryDescription, Action<IReadOnlyList<EntityHandle>>>? withTempEntities = null,
        Action<QueryDescription, Action<EntityHandle>>? forEachEntity = null,
        Action<QueryDescription, Action<IReadOnlyList<ArchetypeChunkHandle>>>? withTempChunks = null,
        Action<QueryDescription, Action<ArchetypeChunkHandle>>? forEachChunk = null,
        Func<EntityHandle, bool>? exists = null)
    {
        Query = query ?? throw new ArgumentNullException(nameof(query));
        Registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        this.withTempEntities = withTempEntities ?? ((_, _) => { });
        this.forEachEntity = forEachEntity ?? ((_, _) => { });
        this.withTempChunks = withTempChunks ?? ((_, _) => { });
        this.forEachChunk = forEachChunk ?? ((_, _) => { });
        this.exists = exists ?? (_ => true);
    }

    /// <summary>
    /// Gets the primary query associated with the work instance.
    /// </summary>
    public QueryDescription Query { get; }

    /// <summary>
    /// Gets the registrar used to schedule per-update refresh actions.
    /// </summary>
    public IRegistrar Registrar { get; }

    /// <summary>
    /// Executes the supplied callback with a temporary collection of entity handles for the specified query.
    /// </summary>
    /// <param name="query">Query to use when collecting entities.</param>
    /// <param name="action">Action invoked with the temporary entity handles.</param>
    public void WithTempEntities(QueryDescription query, Action<IReadOnlyList<EntityHandle>> action)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        withTempEntities(query, action);
    }

    /// <summary>
    /// Iterates each entity in the specified query.
    /// </summary>
    /// <param name="query">Query to use when collecting entities.</param>
    /// <param name="action">Action invoked for every entity handle.</param>
    public void ForEachEntity(QueryDescription query, Action<EntityHandle> action)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        forEachEntity(query, action);
    }

    /// <summary>
    /// Executes the supplied callback with a temporary collection of chunk handles for the specified query.
    /// </summary>
    /// <param name="query">Query to use when collecting chunks.</param>
    /// <param name="action">Action invoked with the temporary chunk handles.</param>
    public void WithTempChunks(QueryDescription query, Action<IReadOnlyList<ArchetypeChunkHandle>> action)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        withTempChunks(query, action);
    }

    /// <summary>
    /// Iterates each chunk in the specified query.
    /// </summary>
    /// <param name="query">Query to use when collecting chunks.</param>
    /// <param name="action">Action invoked for every chunk handle.</param>
    public void ForEachChunk(QueryDescription query, Action<ArchetypeChunkHandle> action)
    {
        if (query == null)
            throw new ArgumentNullException(nameof(query));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        forEachChunk(query, action);
    }

    /// <summary>
    /// Determines whether the entity currently exists.
    /// </summary>
    /// <param name="entity">Entity handle to evaluate.</param>
    public bool Exists(EntityHandle entity)
    {
        return exists(entity);
    }
}

/// <summary>
/// Defines the lifecycle hooks executed by system work objects.
/// </summary>
public interface ISystemWork : IQuerySpec
{
    /// <summary>
    /// Invoked when the owning system is being initialised.
    /// </summary>
    /// <param name="registrar">Registrar used to schedule refresh actions.</param>
    /// <param name="context">The active system context.</param>
    void Setup(IRegistrar registrar, in SystemContext context);

    /// <summary>
    /// Invoked during each update tick.
    /// </summary>
    /// <param name="context">The active system context.</param>
    void Tick(in SystemContext context);

    /// <summary>
    /// Invoked when the owning system is destroyed.
    /// </summary>
    /// <param name="context">The active system context.</param>
    void Destroy(in SystemContext context) { }
}

/// <summary>
/// Represents a recorded lookup request made during refresh registration.
/// </summary>
/// <param name="ElementType">Type associated with the lookup.</param>
/// <param name="IsReadOnly">Indicates whether the lookup was requested in read-only mode.</param>
public readonly record struct LookupRequest(Type ElementType, bool IsReadOnly);

/// <summary>
/// Represents a recorded type handle request made during refresh registration.
/// </summary>
/// <param name="ElementType">Type associated with the handle.</param>
/// <param name="IsReadOnly">Indicates whether the handle was requested in read-only mode.</param>
public readonly record struct TypeHandleRequest(Type ElementType, bool IsReadOnly);

/// <summary>
/// Captures refresh registrations for later inspection inside tests.
/// </summary>
public sealed class RecordingRegistrar : IRegistrar
{
    readonly List<Action<ISystemFacade>> refreshActions = new();
    readonly List<LookupRequest> componentLookups = new();
    readonly List<LookupRequest> bufferLookups = new();
    readonly List<TypeHandleRequest> componentTypeHandles = new();
    readonly List<TypeHandleRequest> bufferTypeHandles = new();

    /// <summary>
    /// Gets the number of times the entity type handle was requested.
    /// </summary>
    public int EntityTypeHandleRequests { get; private set; }

    /// <summary>
    /// Gets the number of times the entity storage lookup was requested.
    /// </summary>
    public int EntityStorageInfoLookupRequests { get; private set; }

    /// <summary>
    /// Gets the recorded component lookup requests.
    /// </summary>
    public IReadOnlyList<LookupRequest> ComponentLookups => componentLookups;

    /// <summary>
    /// Gets the recorded buffer lookup requests.
    /// </summary>
    public IReadOnlyList<LookupRequest> BufferLookups => bufferLookups;

    /// <summary>
    /// Gets the recorded component type handle requests.
    /// </summary>
    public IReadOnlyList<TypeHandleRequest> ComponentTypeHandles => componentTypeHandles;

    /// <summary>
    /// Gets the recorded buffer type handle requests.
    /// </summary>
    public IReadOnlyList<TypeHandleRequest> BufferTypeHandles => bufferTypeHandles;

    /// <summary>
    /// Gets the number of registered refresh actions.
    /// </summary>
    public int RegistrationCount => refreshActions.Count;

    /// <inheritdoc />
    public void Register(Action<ISystemFacade> refreshAction)
    {
        if (refreshAction == null)
            throw new ArgumentNullException(nameof(refreshAction));

        refreshActions.Add(refreshAction);
    }

    /// <summary>
    /// Executes all registered refresh actions using a recording system facade.
    /// </summary>
    public void InvokeRegistrations()
    {
        if (refreshActions.Count == 0)
            return;

        var facade = new RecordingSystemFacade(this);
        foreach (var action in refreshActions)
        {
            action(facade);
        }
    }

    void RecordEntityTypeHandle()
    {
        EntityTypeHandleRequests++;
    }

    void RecordEntityStorageInfoLookup()
    {
        EntityStorageInfoLookupRequests++;
    }

    void RecordComponentLookup(Type elementType, bool isReadOnly)
    {
        componentLookups.Add(new LookupRequest(elementType, isReadOnly));
    }

    void RecordBufferLookup(Type elementType, bool isReadOnly)
    {
        bufferLookups.Add(new LookupRequest(elementType, isReadOnly));
    }

    void RecordComponentTypeHandle(Type elementType, bool isReadOnly)
    {
        componentTypeHandles.Add(new TypeHandleRequest(elementType, isReadOnly));
    }

    void RecordBufferTypeHandle(Type elementType, bool isReadOnly)
    {
        bufferTypeHandles.Add(new TypeHandleRequest(elementType, isReadOnly));
    }

    sealed class RecordingSystemFacade : ISystemFacade
    {
        readonly RecordingRegistrar registrar;

        public RecordingSystemFacade(RecordingRegistrar registrar)
        {
            this.registrar = registrar;
        }

        public EntityTypeHandle GetEntityTypeHandle()
        {
            registrar.RecordEntityTypeHandle();
            return new EntityTypeHandle();
        }

        public EntityStorageInfoLookup GetEntityStorageInfoLookup()
        {
            registrar.RecordEntityStorageInfoLookup();
            return new EntityStorageInfoLookup();
        }

        public ComponentLookup<TComponent> GetComponentLookup<TComponent>(bool isReadOnly = false)
        {
            registrar.RecordComponentLookup(typeof(TComponent), isReadOnly);
            return new ComponentLookup<TComponent>(isReadOnly);
        }

        public BufferLookup<TBuffer> GetBufferLookup<TBuffer>(bool isReadOnly = false)
        {
            registrar.RecordBufferLookup(typeof(TBuffer), isReadOnly);
            return new BufferLookup<TBuffer>(isReadOnly);
        }

        public ComponentTypeHandle<TComponent> GetComponentTypeHandle<TComponent>(bool isReadOnly = false)
        {
            registrar.RecordComponentTypeHandle(typeof(TComponent), isReadOnly);
            return new ComponentTypeHandle<TComponent>(isReadOnly);
        }

        public BufferTypeHandle<TBuffer> GetBufferTypeHandle<TBuffer>(bool isReadOnly = false)
        {
            registrar.RecordBufferTypeHandle(typeof(TBuffer), isReadOnly);
            return new BufferTypeHandle<TBuffer>(isReadOnly);
        }
    }
}

/// <summary>
/// Represents a stub entity type handle.
/// </summary>
public readonly record struct EntityTypeHandle;

/// <summary>
/// Represents a stub entity storage info lookup.
/// </summary>
public readonly record struct EntityStorageInfoLookup;

/// <summary>
/// Represents a stub component lookup.
/// </summary>
/// <param name="IsReadOnly">Whether the lookup was requested in read-only mode.</param>
public readonly record struct ComponentLookup<TComponent>(bool IsReadOnly);

/// <summary>
/// Represents a stub buffer lookup.
/// </summary>
/// <param name="IsReadOnly">Whether the lookup was requested in read-only mode.</param>
public readonly record struct BufferLookup<TBuffer>(bool IsReadOnly);

/// <summary>
/// Represents a stub component type handle.
/// </summary>
/// <param name="IsReadOnly">Whether the handle was requested in read-only mode.</param>
public readonly record struct ComponentTypeHandle<TComponent>(bool IsReadOnly);

/// <summary>
/// Represents a stub buffer type handle.
/// </summary>
/// <param name="IsReadOnly">Whether the handle was requested in read-only mode.</param>
public readonly record struct BufferTypeHandle<TBuffer>(bool IsReadOnly);

/// <summary>
/// Convenience helpers for interacting with work definitions under test.
/// </summary>
public static class FactoryTestUtilities
{
    /// <summary>
    /// Describes the query definition produced by the specified work type.
    /// </summary>
    /// <typeparam name="TWork">Work type being evaluated.</typeparam>
    public static QueryDescription DescribeQuery<TWork>()
        where TWork : struct, ISystemWork
    {
        TWork work = new();
        work.DescribeQuery(out var all, out var any, out var none, out var options);
        return new QueryDescription(all, any, none, options, work.RequireForUpdate);
    }

    /// <summary>
    /// Creates a <see cref="SystemContext"/> using the provided registrar and optional delegates.
    /// </summary>
    /// <param name="registrar">Registrar used to capture refresh registrations.</param>
    /// <param name="query">Optional query description to associate with the context.</param>
    /// <param name="withTempEntities">Delegate for temporary entity iteration.</param>
    /// <param name="forEachEntity">Delegate for entity enumeration.</param>
    /// <param name="withTempChunks">Delegate for temporary chunk iteration.</param>
    /// <param name="forEachChunk">Delegate for chunk enumeration.</param>
    /// <param name="exists">Predicate for determining whether an entity handle is still valid.</param>
    public static SystemContext CreateContext(
        IRegistrar registrar,
        QueryDescription? query = null,
        Action<QueryDescription, Action<IReadOnlyList<EntityHandle>>>? withTempEntities = null,
        Action<QueryDescription, Action<EntityHandle>>? forEachEntity = null,
        Action<QueryDescription, Action<IReadOnlyList<ArchetypeChunkHandle>>>? withTempChunks = null,
        Action<QueryDescription, Action<ArchetypeChunkHandle>>? forEachChunk = null,
        Func<EntityHandle, bool>? exists = null)
    {
        return new SystemContext(
            query ?? QueryDescription.Empty,
            registrar,
            withTempEntities,
            forEachEntity,
            withTempChunks,
            forEachChunk,
            exists);
    }

    /// <summary>
    /// Instantiates a work object for the specified type.
    /// </summary>
    /// <typeparam name="TWork">Work type to instantiate.</typeparam>
    public static TWork CreateWork<TWork>()
        where TWork : struct, ISystemWork
    {
        return new TWork();
    }
}
