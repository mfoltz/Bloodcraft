using System;
using System.Collections.Generic;
using System.Reflection;
using Bloodcraft.Factory;
using Unity.Entities;

namespace Bloodcraft.Tests.Systems.Factory.Integration;

/// <summary>
/// Provides a lightweight Unity DOTS world for exercising <see cref="VSystemBase{TWork}"/> implementations under test.
/// </summary>
/// <typeparam name="TWork">Work definition executed by the system.</typeparam>
public sealed class FactoryWorldHarness<TWork> : IDisposable
    where TWork : class, Bloodcraft.Factory.ISystemWork, new()
{
    bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FactoryWorldHarness{TWork}"/> class.
    /// </summary>
    /// <param name="workFactory">Optional factory used to construct the work instance.</param>
    public FactoryWorldHarness(Func<TWork>? workFactory = null)
    {
        TestVSystem<TWork>.WorkFactoryOverride = workFactory;
        World = new World($"FactoryWorldHarness<{typeof(TWork).Name}>");
        try
        {
            System = World.CreateSystemManaged<TestVSystem<TWork>>();
        }
        finally
        {
            TestVSystem<TWork>.WorkFactoryOverride = null;
        }

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            World.DefaultGameObjectInjectionWorld = World;
        }
    }

    /// <summary>
    /// Gets the Unity world hosting the system under test.
    /// </summary>
    public World World { get; }

    /// <summary>
    /// Gets the entity manager associated with the hosted world.
    /// </summary>
    public EntityManager EntityManager => World.EntityManager;

    /// <summary>
    /// Gets the executing test system.
    /// </summary>
    public TestVSystem<TWork> System { get; }

    /// <summary>
    /// Gets the work instance executed by the system.
    /// </summary>
    public TWork Work => System.WorkInstance;

    /// <summary>
    /// Advances the system by a single update.
    /// </summary>
    public void Update()
    {
        System.Update();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        if (World.IsCreated)
        {
            if (World.DefaultGameObjectInjectionWorld == World)
            {
                World.DefaultGameObjectInjectionWorld = null;
            }

            World.Dispose();
        }
    }
}

/// <summary>
/// Specialized <see cref="VSystemBase{TWork}"/> exposing internals required for integration testing.
/// </summary>
/// <typeparam name="TWork">Work definition executed by the system.</typeparam>
public sealed class TestVSystem<TWork> : VSystemBase<TWork>, IRefreshRegistrationContext
    where TWork : class, Bloodcraft.Factory.ISystemWork, new()
{
    static readonly FieldInfo WorkField = typeof(VSystemBase<TWork>).GetField("<Work>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
    static readonly FieldInfo RefreshActionsField = typeof(VSystemBase<TWork>).GetField("_refreshActions", BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// Gets or sets the factory used to create the work instance before <see cref="OnCreate()"/> executes.
    /// </summary>
    internal static Func<TWork>? WorkFactoryOverride { get; set; }

    /// <summary>
    /// Gets the active work instance.
    /// </summary>
    public TWork WorkInstance => Work;

    /// <summary>
    /// Gets a read-only view of the refresh actions registered by the work instance.
    /// </summary>
    public IReadOnlyList<Action<SystemBase>> RefreshActions
    {
        get
        {
            if (RefreshActionsField.GetValue(this) is not List<Action<SystemBase>> actions)
                return Array.Empty<Action<SystemBase>>();

            return actions;
        }
    }

    /// <summary>
    /// Gets the current entity type handle.
    /// </summary>
    public ref Unity.Entities.EntityTypeHandle EntityTypeHandleRef => ref base.EntityTypeHandle;

    /// <summary>
    /// Gets the current entity storage info lookup.
    /// </summary>
    public ref Unity.Entities.EntityStorageInfoLookup EntityStorageInfoLookupRef => ref base.EntityStorageInfoLookup;

    /// <summary>
    /// Exposes <see cref="SystemBase.GetComponentLookup{T}(bool)"/> for assertions.
    /// </summary>
    public new Unity.Entities.ComponentLookup<TComponent> GetComponentLookup<TComponent>(bool isReadOnly = false)
    {
        return base.GetComponentLookup<TComponent>(isReadOnly);
    }

    /// <summary>
    /// Exposes <see cref="SystemBase.GetBufferLookup{T}(bool)"/> for assertions.
    /// </summary>
    public new Unity.Entities.BufferLookup<TBuffer> GetBufferLookup<TBuffer>(bool isReadOnly = false)
    {
        return base.GetBufferLookup<TBuffer>(isReadOnly);
    }

    /// <summary>
    /// Exposes <see cref="SystemBase.GetComponentTypeHandle{T}(bool)"/> for assertions.
    /// </summary>
    public new Unity.Entities.ComponentTypeHandle<TComponent> GetComponentTypeHandle<TComponent>(bool isReadOnly = false)
    {
        return base.GetComponentTypeHandle<TComponent>(isReadOnly);
    }

    /// <summary>
    /// Exposes <see cref="SystemBase.GetBufferTypeHandle{T}(bool)"/> for assertions.
    /// </summary>
    public new Unity.Entities.BufferTypeHandle<TBuffer> GetBufferTypeHandle<TBuffer>(bool isReadOnly = false)
    {
        return base.GetBufferTypeHandle<TBuffer>(isReadOnly);
    }

    /// <inheritdoc />
    public override void OnCreate()
    {
        if (WorkFactoryOverride != null)
        {
            ReplaceWork(WorkFactoryOverride());
            WorkFactoryOverride = null;
        }

        base.OnCreate();
    }

    /// <summary>
    /// Replaces the active work instance using reflection to bypass the private setter.
    /// </summary>
    /// <param name="work">Work instance that should be executed by the system.</param>
    void ReplaceWork(TWork work)
    {
        if (work == null)
            throw new ArgumentNullException(nameof(work));

        WorkField.SetValue(this, work);
    }

    /// <inheritdoc />
    ISystemFacade IRefreshRegistrationContext.CreateFacade()
    {
        return new SystemFacadeAdapter();
    }

    sealed class SystemFacadeAdapter : ISystemFacade
    {
        public SystemFacadeAdapter()
        {
        }

        public EntityTypeHandle GetEntityTypeHandle() => new();

        public EntityStorageInfoLookup GetEntityStorageInfoLookup() => new();

        public ComponentLookup<TComponent> GetComponentLookup<TComponent>(bool isReadOnly = false)
        {
            return new ComponentLookup<TComponent>(isReadOnly);
        }

        public BufferLookup<TBuffer> GetBufferLookup<TBuffer>(bool isReadOnly = false)
        {
            return new BufferLookup<TBuffer>(isReadOnly);
        }

        public ComponentTypeHandle<TComponent> GetComponentTypeHandle<TComponent>(bool isReadOnly = false)
        {
            return new ComponentTypeHandle<TComponent>(isReadOnly);
        }

        public BufferTypeHandle<TBuffer> GetBufferTypeHandle<TBuffer>(bool isReadOnly = false)
        {
            return new BufferTypeHandle<TBuffer>(isReadOnly);
        }
    }
}
