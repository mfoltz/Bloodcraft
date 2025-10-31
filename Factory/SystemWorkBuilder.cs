#nullable enable

using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Bloodcraft.Factory;

/// <summary>
/// Provides a fluent builder for constructing lightweight <see cref="ISystemWork"/> implementations.
/// </summary>
public sealed class SystemWorkBuilder
{
    /// <summary>
    /// Represents a callback used to configure an <see cref="EntityQueryBuilder"/>.
    /// </summary>
    /// <param name="builder">The builder receiving the configuration.</param>
    public delegate void QueryConfigurator(ref EntityQueryBuilder builder);

    readonly List<QueryConfigurator> _queryConfigurators = new();
    readonly List<Action<SystemContext>> _resourceInitializers = new();

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
    /// Registers a query configuration callback executed during <see cref="ISystemWork.Build"/>.
    /// </summary>
    /// <param name="configurator">Callback that mutates the builder.</param>
    /// <returns>The current builder instance.</returns>
    public SystemWorkBuilder WithQuery(Action<EntityQueryBuilder> configurator)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        return WithQuery((ref EntityQueryBuilder builder) =>
        {
            var local = builder;
            configurator(local);
            builder = local;
        });
    }

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
    /// Registers and exposes a <see cref="ComponentLookup{T}"/> refreshed each update.
    /// </summary>
    /// <typeparam name="T">Component type to lookup.</typeparam>
    /// <param name="isReadOnly">Whether the lookup operates in read-only mode.</param>
    /// <returns>A handle that surfaces the refreshed lookup.</returns>
    public ComponentLookupHandle<T> WithLookup<T>(bool isReadOnly = false)
    {
        var handle = new ComponentLookupHandle<T>();

        _resourceInitializers.Add(context =>
        {
            handle.Current = context.System.GetComponentLookup<T>(isReadOnly);
            context.Registrar.Register(system => handle.Current.Update(system));
        });

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

        _resourceInitializers.Add(context =>
        {
            handle.Current = context.System.GetBufferLookup<T>(isReadOnly);
            context.Registrar.Register(system => handle.Current.Update(system));
        });

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

    sealed class DelegateSystemWork : ISystemWork
    {
        readonly QueryConfigurator[] _queryConfigurators;
        readonly Action<SystemContext>[] _resourceInitializers;
        readonly bool _requireForUpdate;
        readonly Action<SystemContext>? _onCreate;
        readonly Action<SystemContext>? _onStartRunning;
        readonly Action<SystemContext>? _onUpdate;
        readonly Action<SystemContext>? _onStopRunning;
        readonly Action<SystemContext>? _onDestroy;

        public DelegateSystemWork(
            QueryConfigurator[] queryConfigurators,
            Action<SystemContext>[] resourceInitializers,
            bool requireForUpdate,
            Action<SystemContext>? onCreate,
            Action<SystemContext>? onStartRunning,
            Action<SystemContext>? onUpdate,
            Action<SystemContext>? onStopRunning,
            Action<SystemContext>? onDestroy)
        {
            _queryConfigurators = queryConfigurators ?? Array.Empty<QueryConfigurator>();
            _resourceInitializers = resourceInitializers ?? Array.Empty<Action<SystemContext>>();
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

        public void OnDestroy(SystemContext context) =>
            _onDestroy?.Invoke(context);
    }
}
