#nullable enable

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Factory;

/// <summary>
/// Provides a fluent builder for constructing lightweight <see cref="ISystemWork"/> implementations.
/// </summary>
/// <remarks>
/// When allocating persistent native containers inside lifecycle callbacks, use
/// <see cref="SystemContext.RegisterDisposable(System.IDisposable)"/> to ensure the resources are automatically disposed.
/// </remarks>
public sealed class SystemWorkBuilder
{
    /// <summary>
    /// Represents a callback used to configure an <see cref="EntityQueryBuilder"/>.
    /// </summary>
    /// <param name="builder">The builder receiving the configuration.</param>
    public delegate void QueryConfigurator(ref EntityQueryBuilder builder);

    readonly List<QueryConfigurator> _queryConfigurators = new();
    readonly List<Action<SystemContext>> _resourceInitializers = new();
    readonly List<Action<SystemContext>> _resourceTeardowns = new();

    Action<SystemContext>? _onCreate;
    Action<SystemContext>? _onStartRunning;
    Action<SystemContext>? _onUpdate;
    Action<SystemContext>? _onStopRunning;
    Action<SystemContext>? _onDestroy;

    bool _requireForUpdate = true;

    /// <summary>
    /// Registers a query configuration callback executed during <see cref="ISystemWork.Build"/>.
    /// </summary>
    /// <param name="configurator">Callback that mutates the builder.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder WithQuery(QueryConfigurator configurator)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        _queryConfigurators.Add(configurator);
        return this;
    }

    /// <summary>
    /// Registers a descriptor executed during <see cref="ISystemWork.Build"/>.
    /// </summary>
    /// <param name="descriptor">Descriptor describing the query requirements.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder WithQuery(QueryDescriptor descriptor)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        _queryConfigurators.Add(descriptor.Configure);

        if (descriptor.TryGetRequireForUpdate(out bool requireForUpdate))
        {
            _requireForUpdate = requireForUpdate;
        }

        return this;
    }

    /// <summary>
    /// Registers a handle that exposes the system's primary query.
    /// </summary>
    /// <param name="requireForUpdate">Whether to mark the primary query as required for update.</param>
    /// <returns>A holder exposing the wrapped query handle.</returns>
    public QueryHandleHolder WithPrimaryQuery(bool requireForUpdate = true)
    {
        var holder = new QueryHandleHolder();

        _requireForUpdate = requireForUpdate;

        _resourceInitializers.Add(context =>
        {
            if (context.System == null)
                throw new ArgumentNullException(nameof(context));

            holder.Current = new QueryHandle(
                context.Query,
                ownsQuery: false,
                (query, action) => context.WithTempEntities(query, action),
                (query, action) => context.ForEachEntity(query, action),
                (query, action) => context.WithTempChunks(query, action),
                (query, action) => context.ForEachChunk(query, action));

            if (requireForUpdate)
            {
                context.System.RequireForUpdate(context.Query);
            }
        });

        _resourceTeardowns.Add(_ => holder.Current = null);

        return holder;
    }

    /// <summary>
    /// Registers a holder that creates a new query from the supplied descriptor during <see cref="ISystemWork.OnCreate"/>.
    /// </summary>
    /// <param name="descriptor">Descriptor describing the query requirements.</param>
    /// <param name="disposeOnDestroy">Whether the created query should be disposed when the system is destroyed.</param>
    /// <returns>A holder exposing the wrapped query handle.</returns>
    public QueryHandleHolder WithQuery(ref QueryDescriptor descriptor, bool disposeOnDestroy = true)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        var holder = new QueryHandleHolder();
        var descriptorCopy = descriptor;

        _resourceInitializers.Add(context =>
        {
            if (context.System == null)
                throw new ArgumentNullException(nameof(context));

            var builder = new EntityQueryBuilder(Allocator.Temp);

            try
            {
                descriptorCopy.Configure(ref builder);
                var query = context.EntityManager.CreateEntityQuery(ref builder);

                if (descriptorCopy.TryGetRequireForUpdate(out bool requireForUpdate) && requireForUpdate)
                {
                    context.System.RequireForUpdate(query);
                }

                var handle = new QueryHandle(
                    query,
                    disposeOnDestroy,
                    (entityQuery, action) => context.WithTempEntities(entityQuery, action),
                    (entityQuery, action) => context.ForEachEntity(entityQuery, action),
                    (entityQuery, action) => context.WithTempChunks(entityQuery, action),
                    (entityQuery, action) => context.ForEachChunk(entityQuery, action));

                if (disposeOnDestroy)
                {
                    context.RegisterDisposable(handle);
                }

                holder.Current = handle;
            }
            finally
            {
                builder.Dispose();
            }
        });

        _resourceTeardowns.Add(_ => holder.Current = null);

        return holder;
    }

    /// <summary>
    /// <summary>
    /// Sets whether the constructed query should be required for update.
    /// </summary>
    /// <param name="requireForUpdate">Whether to mark the query as required.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder RequireForUpdate(bool requireForUpdate)
    {
        _requireForUpdate = requireForUpdate;
        return this;
    }

    /// <summary>
    /// Registers an <see cref="ISystemWork.OnCreate"/> delegate.
    /// </summary>
    /// <param name="action">Delegate invoked during creation.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder OnCreate(Action<SystemContext> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _onCreate += action;
        return this;
    }

    /// <summary>
    /// Registers an <see cref="ISystemWork.OnStartRunning"/> delegate.
    /// </summary>
    /// <param name="action">Delegate invoked when the system starts running.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder OnStartRunning(Action<SystemContext> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _onStartRunning += action;
        return this;
    }

    /// <summary>
    /// Registers an <see cref="ISystemWork.OnUpdate"/> delegate.
    /// </summary>
    /// <param name="action">Delegate invoked every update.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder OnUpdate(Action<SystemContext> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _onUpdate += action;
        return this;
    }

    /// <summary>
    /// Registers an <see cref="ISystemWork.OnStopRunning"/> delegate.
    /// </summary>
    /// <param name="action">Delegate invoked when the system stops running.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder OnStopRunning(Action<SystemContext> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _onStopRunning += action;
        return this;
    }

    /// <summary>
    /// Registers an <see cref="ISystemWork.OnDestroy"/> delegate.
    /// </summary>
    /// <param name="action">Delegate invoked when the system is destroyed.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder OnDestroy(Action<SystemContext> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _onDestroy += action;
        return this;
    }

    /// <summary>
    /// Exposes a managed <see cref="QueryHandle"/> instance to the caller.
    /// </summary>
    public sealed class QueryHandleHolder
    {
        QueryHandle? _handle;

        internal QueryHandle? Current
        {
            get => _handle;
            set => _handle = value;
        }

        /// <summary>
        /// Gets the current query handle, if available.
        /// </summary>
        public QueryHandle? Handle => _handle;
    }

    /// <summary>
    /// Registers and exposes a <see cref="ComponentLookup{T}"/> refreshed each update.
    /// </summary>
    /// <typeparam name="T">Component type to lookup.</typeparam>
    /// <param name="isReadOnly">Whether the lookup operates in read-only mode.</param>
    /// <returns>A handle that surfaces the refreshed lookup.</returns>
    public ComponentLookupHandle<T> WithLookup<T>(bool isReadOnly = false)
    {
        var handle = new ComponentLookupHandle<T>();

        _resourceInitializers.Add(context => InitializeLookupHandle(context, handle, isReadOnly));

        return handle;
    }

    /// <summary>
    /// Registers and exposes a <see cref="BufferLookup{T}"/> refreshed each update.
    /// </summary>
    /// <typeparam name="T">Buffer type to lookup.</typeparam>
    /// <param name="isReadOnly">Whether the lookup operates in read-only mode.</param>
    /// <returns>A handle that surfaces the refreshed lookup.</returns>
    public BufferLookupHandle<T> WithBuffer<T>(bool isReadOnly = false)
    {
        var handle = new BufferLookupHandle<T>();

        _resourceInitializers.Add(context => InitializeBufferLookupHandle(context, handle, isReadOnly));

        return handle;
    }

    /// <summary>
    /// Registers and exposes a <see cref="ComponentTypeHandle{T}"/> refreshed each update.
    /// </summary>
    /// <typeparam name="T">Component type for the handle.</typeparam>
    /// <param name="isReadOnly">Whether the handle operates in read-only mode.</param>
    /// <returns>A handle that surfaces the refreshed type handle.</returns>
    public ComponentTypeHandleHandle<T> WithComponentTypeHandle<T>(bool isReadOnly = false)
        where T : unmanaged
    {
        var handle = new ComponentTypeHandleHandle<T>();

        _resourceInitializers.Add(context => InitializeComponentTypeHandle(context, handle, isReadOnly));

        return handle;
    }

    /// <summary>
    /// Registers and exposes a <see cref="BufferTypeHandle{T}"/> refreshed each update.
    /// </summary>
    /// <typeparam name="T">Buffer element type for the handle.</typeparam>
    /// <param name="isReadOnly">Whether the handle operates in read-only mode.</param>
    /// <returns>A handle that surfaces the refreshed type handle.</returns>
    public BufferTypeHandleHandle<T> WithBufferTypeHandle<T>(bool isReadOnly = false)
        where T : unmanaged
    {
        var handle = new BufferTypeHandleHandle<T>();

        _resourceInitializers.Add(context => InitializeBufferTypeHandle(context, handle, isReadOnly));

        return handle;
    }

    /// <summary>
    /// Finalises the builder into a concrete <see cref="ISystemWork"/> implementation.
    /// </summary>
    /// <returns>The constructed work instance.</returns>
    public ISystemWork Build()
    {
        return new DelegateSystemWork(
            _queryConfigurators.ToArray(),
            _resourceInitializers.ToArray(),
            _resourceTeardowns.ToArray(),
            _requireForUpdate,
            _onCreate,
            _onStartRunning,
            _onUpdate,
            _onStopRunning,
            _onDestroy);
    }

    /// <summary>
    /// Exposes a refreshed component lookup to the caller.
    /// </summary>
    /// <typeparam name="T">Component type being accessed.</typeparam>
    public sealed class ComponentLookupHandle<T>
    {
        ComponentLookup<T> _lookup;

        internal ComponentLookup<T> Current
        {
            get => _lookup;
            set => _lookup = value;
        }

        /// <summary>
        /// Gets the latest component lookup instance.
        /// </summary>
        public ComponentLookup<T> Lookup => _lookup;
    }

    /// <summary>
    /// Exposes a refreshed buffer lookup to the caller.
    /// </summary>
    /// <typeparam name="T">Buffer element type being accessed.</typeparam>
    public sealed class BufferLookupHandle<T>
    {
        BufferLookup<T> _lookup;

        internal BufferLookup<T> Current
        {
            get => _lookup;
            set => _lookup = value;
        }

        /// <summary>
        /// Gets the latest buffer lookup instance.
        /// </summary>
        public BufferLookup<T> Lookup => _lookup;
    }

    /// <summary>
    /// Exposes a refreshed component type handle to the caller.
    /// </summary>
    /// <typeparam name="T">Component type being accessed.</typeparam>
    public sealed class ComponentTypeHandleHandle<T>
        where T : unmanaged
    {
        ComponentTypeHandle<T> _handle;

        internal ComponentTypeHandle<T> Current
        {
            get => _handle;
            set => _handle = value;
        }

        /// <summary>
        /// Gets the latest component type handle instance.
        /// </summary>
        public ComponentTypeHandle<T> Handle => _handle;
    }

    /// <summary>
    /// Exposes a refreshed buffer type handle to the caller.
    /// </summary>
    /// <typeparam name="T">Buffer element type being accessed.</typeparam>
    public sealed class BufferTypeHandleHandle<T>
        where T : unmanaged
    {
        BufferTypeHandle<T> _handle;

        internal BufferTypeHandle<T> Current
        {
            get => _handle;
            set => _handle = value;
        }

        /// <summary>
        /// Gets the latest buffer type handle instance.
        /// </summary>
        public BufferTypeHandle<T> Handle => _handle;
    }

    /// <summary>
    /// Represents the context provided to chunk iteration callbacks.
    /// </summary>
    public readonly struct ChunkIterationContext
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="ChunkIterationContext"/> struct.
        /// </summary>
        /// <param name="context">Active system context.</param>
        /// <param name="chunk">Chunk being processed.</param>
        /// <param name="entities">Entity array backing the chunk.</param>
        public ChunkIterationContext(SystemContext context, ArchetypeChunk chunk, NativeArray<Entity> entities)
        {
            Context = context;
            Chunk = chunk;
            Entities = entities;
        }

        /// <summary>
        /// Gets the active system context.
        /// </summary>
        public SystemContext Context { get; }

        /// <summary>
        /// Gets the chunk currently being processed.
        /// </summary>
        public ArchetypeChunk Chunk { get; }

        /// <summary>
        /// Gets the number of entities contained within the chunk.
        /// </summary>
        public int Count => Chunk.Count;

        /// <summary>
        /// Gets the native array of entities contained in the chunk.
        /// </summary>
        public NativeArray<Entity> Entities { get; }

        /// <summary>
        /// Gets a <see cref="NativeArray{T}"/> for the supplied component handle.
        /// </summary>
        public NativeArray<T> GetNativeArray<T>(ComponentTypeHandleHandle<T> handle)
            where T : unmanaged =>
            Chunk.GetNativeArray(handle.Handle);

        /// <summary>
        /// Gets a <see cref="BufferAccessor{T}"/> for the supplied buffer handle.
        /// </summary>
        public BufferAccessor<T> GetBufferAccessor<T>(BufferTypeHandleHandle<T> handle)
            where T : unmanaged =>
            Chunk.GetBufferAccessor(handle.Handle);

        /// <summary>
        /// Gets the latest <see cref="ComponentLookup{T}"/> for the supplied handle.
        /// </summary>
        public ComponentLookup<T> GetLookup<T>(ComponentLookupHandle<T> handle) => handle.Lookup;

        /// <summary>
        /// Gets the latest <see cref="BufferLookup{T}"/> for the supplied handle.
        /// </summary>
        public BufferLookup<T> GetLookup<T>(BufferLookupHandle<T> handle) => handle.Lookup;
    }

    /// <summary>
    /// Represents the context provided to entity iteration callbacks.
    /// </summary>
    public readonly struct EntityIterationContext
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="EntityIterationContext"/> struct.
        /// </summary>
        /// <param name="context">Active system context.</param>
        /// <param name="entity">Entity being processed.</param>
        public EntityIterationContext(SystemContext context, Entity entity)
        {
            Context = context;
            Entity = entity;
        }

        /// <summary>
        /// Gets the active system context.
        /// </summary>
        public SystemContext Context { get; }

        /// <summary>
        /// Gets the entity currently being processed.
        /// </summary>
        public Entity Entity { get; }

        /// <summary>
        /// Gets the latest <see cref="ComponentLookup{T}"/> for the supplied handle.
        /// </summary>
        public ComponentLookup<T> GetLookup<T>(ComponentLookupHandle<T> handle) => handle.Lookup;

        /// <summary>
        /// Gets the latest <see cref="BufferLookup{T}"/> for the supplied handle.
        /// </summary>
        public BufferLookup<T> GetLookup<T>(BufferLookupHandle<T> handle) => handle.Lookup;
    }

    /// <summary>
    /// Iterates each chunk in the supplied query while exposing strongly typed accessors.
    /// </summary>
    /// <param name="context">Active system context.</param>
    /// <param name="queryHandle">Query being iterated.</param>
    /// <param name="action">Action invoked per chunk.</param>
    public static void ForEachChunk(SystemContext context, QueryHandle queryHandle, Action<ChunkIterationContext> action)
    {
        if (context.System == null)
            throw new ArgumentNullException(nameof(context));
        if (queryHandle == null)
            throw new ArgumentNullException(nameof(queryHandle));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        queryHandle.WithTempChunks(chunks =>
        {
            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i];
                var entities = chunk.GetNativeArray(context.EntityTypeHandle);
                action(new ChunkIterationContext(context, chunk, entities));
            }
        });
    }

    /// <summary>
    /// Iterates each entity in the supplied query while exposing strongly typed accessors.
    /// </summary>
    /// <param name="context">Active system context.</param>
    /// <param name="queryHandle">Query being iterated.</param>
    /// <param name="action">Action invoked per entity.</param>
    public static void ForEachEntity(SystemContext context, QueryHandle queryHandle, Action<EntityIterationContext> action)
    {
        if (context.System == null)
            throw new ArgumentNullException(nameof(context));
        if (queryHandle == null)
            throw new ArgumentNullException(nameof(queryHandle));
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        queryHandle.WithTempEntities(entities =>
        {
            for (int i = 0; i < entities.Length; ++i)
            {
                action(new EntityIterationContext(context, entities[i]));
            }
        });
    }

    /// <summary>
    /// Creates a lookup handle that is automatically refreshed for the supplied context.
    /// </summary>
    /// <typeparam name="T">Component type being accessed.</typeparam>
    /// <param name="context">Active system context.</param>
    /// <param name="isReadOnly">Whether the lookup operates in read-only mode.</param>
    /// <returns>The hydrated lookup handle.</returns>
    public static ComponentLookupHandle<T> CreateLookup<T>(SystemContext context, bool isReadOnly = false)
    {
        if (context.System == null)
            throw new ArgumentNullException(nameof(context));

        var handle = new ComponentLookupHandle<T>();
        InitializeLookupHandle(context, handle, isReadOnly);
        return handle;
    }

    /// <summary>
    /// Creates a buffer lookup handle that is automatically refreshed for the supplied context.
    /// </summary>
    /// <typeparam name="T">Buffer element type being accessed.</typeparam>
    /// <param name="context">Active system context.</param>
    /// <param name="isReadOnly">Whether the lookup operates in read-only mode.</param>
    /// <returns>The hydrated buffer lookup handle.</returns>
    public static BufferLookupHandle<T> CreateBufferLookup<T>(SystemContext context, bool isReadOnly = false)
    {
        if (context.System == null)
            throw new ArgumentNullException(nameof(context));

        var handle = new BufferLookupHandle<T>();
        InitializeBufferLookupHandle(context, handle, isReadOnly);
        return handle;
    }

    /// <summary>
    /// Creates a component type handle that is automatically refreshed for the supplied context.
    /// </summary>
    /// <typeparam name="T">Component type being accessed.</typeparam>
    /// <param name="context">Active system context.</param>
    /// <param name="isReadOnly">Whether the handle operates in read-only mode.</param>
    /// <returns>The hydrated component type handle.</returns>
    public static ComponentTypeHandleHandle<T> CreateComponentTypeHandle<T>(SystemContext context, bool isReadOnly = false)
        where T : unmanaged
    {
        if (context.System == null)
            throw new ArgumentNullException(nameof(context));

        var handle = new ComponentTypeHandleHandle<T>();
        InitializeComponentTypeHandle(context, handle, isReadOnly);
        return handle;
    }

    /// <summary>
    /// Creates a buffer type handle that is automatically refreshed for the supplied context.
    /// </summary>
    /// <typeparam name="T">Buffer element type being accessed.</typeparam>
    /// <param name="context">Active system context.</param>
    /// <param name="isReadOnly">Whether the handle operates in read-only mode.</param>
    /// <returns>The hydrated buffer type handle.</returns>
    public static BufferTypeHandleHandle<T> CreateBufferTypeHandle<T>(SystemContext context, bool isReadOnly = false)
        where T : unmanaged
    {
        if (context.System == null)
            throw new ArgumentNullException(nameof(context));

        var handle = new BufferTypeHandleHandle<T>();
        InitializeBufferTypeHandle(context, handle, isReadOnly);
        return handle;
    }

    static void InitializeLookupHandle<T>(SystemContext context, ComponentLookupHandle<T> handle, bool isReadOnly)
    {
        handle.Current = context.System.GetComponentLookup<T>(isReadOnly);
        context.Registrar.Register(system =>
        {
            var lookup = handle.Current;
            lookup.Update(system);
            handle.Current = lookup;
        });
    }

    static void InitializeBufferLookupHandle<T>(SystemContext context, BufferLookupHandle<T> handle, bool isReadOnly)
    {
        handle.Current = context.System.GetBufferLookup<T>(isReadOnly);
        context.Registrar.Register(system =>
        {
            var lookup = handle.Current;
            lookup.Update(system);
            handle.Current = lookup;
        });
    }

    static void InitializeComponentTypeHandle<T>(SystemContext context, ComponentTypeHandleHandle<T> handle, bool isReadOnly)
        where T : unmanaged
    {
        handle.Current = context.System.GetComponentTypeHandle<T>(isReadOnly);
        context.Registrar.Register(system =>
        {
            handle.Current = system.GetComponentTypeHandle<T>(isReadOnly);
        });
    }

    static void InitializeBufferTypeHandle<T>(SystemContext context, BufferTypeHandleHandle<T> handle, bool isReadOnly)
        where T : unmanaged
    {
        handle.Current = context.System.GetBufferTypeHandle<T>(isReadOnly);
        context.Registrar.Register(system =>
        {
            handle.Current = system.GetBufferTypeHandle<T>(isReadOnly);
        });
    }

    sealed class DelegateSystemWork : ISystemWork
    {
        readonly QueryConfigurator[] _queryConfigurators;
        readonly Action<SystemContext>[] _resourceInitializers;
        readonly Action<SystemContext>[] _resourceTeardowns;
        readonly bool _requireForUpdate;
        readonly Action<SystemContext>? _onCreate;
        readonly Action<SystemContext>? _onStartRunning;
        readonly Action<SystemContext>? _onUpdate;
        readonly Action<SystemContext>? _onStopRunning;
        readonly Action<SystemContext>? _onDestroy;

        public DelegateSystemWork(
            QueryConfigurator[] queryConfigurators,
            Action<SystemContext>[] resourceInitializers,
            Action<SystemContext>[] resourceTeardowns,
            bool requireForUpdate,
            Action<SystemContext>? onCreate,
            Action<SystemContext>? onStartRunning,
            Action<SystemContext>? onUpdate,
            Action<SystemContext>? onStopRunning,
            Action<SystemContext>? onDestroy)
        {
            _queryConfigurators = queryConfigurators ?? Array.Empty<QueryConfigurator>();
            _resourceInitializers = resourceInitializers ?? Array.Empty<Action<SystemContext>>();
            _resourceTeardowns = resourceTeardowns ?? Array.Empty<Action<SystemContext>>();
            _requireForUpdate = requireForUpdate;
            _onCreate = onCreate;
            _onStartRunning = onStartRunning;
            _onUpdate = onUpdate;
            _onStopRunning = onStopRunning;
            _onDestroy = onDestroy;
        }

        public bool RequireForUpdate => _requireForUpdate;

        public void Build(ref EntityQueryBuilder builder)
        {
            for (int i = 0; i < _queryConfigurators.Length; ++i)
            {
                _queryConfigurators[i](ref builder);
            }
        }

        public void OnCreate(SystemContext context)
        {
            for (int i = 0; i < _resourceInitializers.Length; ++i)
            {
                _resourceInitializers[i](context);
            }

            _onCreate?.Invoke(context);
        }

        public void OnStartRunning(SystemContext context) =>
            _onStartRunning?.Invoke(context);

        public void OnUpdate(SystemContext context) =>
            _onUpdate?.Invoke(context);

        public void OnStopRunning(SystemContext context) =>
            _onStopRunning?.Invoke(context);

        public void OnDestroy(SystemContext context)
        {
            for (int i = 0; i < _resourceTeardowns.Length; ++i)
            {
                _resourceTeardowns[i](context);
            }

            _onDestroy?.Invoke(context);
        }
    }
}
