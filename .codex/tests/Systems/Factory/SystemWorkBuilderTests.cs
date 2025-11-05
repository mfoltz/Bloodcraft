using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Xunit;
using FactorySystemContext = Bloodcraft.Factory.SystemContext;
using FactorySystemWorkBuilder = Bloodcraft.Factory.SystemWorkBuilder;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class SystemWorkBuilderTests : IDisposable
{
    readonly World world;
    readonly StubSystem system;

    public SystemWorkBuilderTests()
    {
        world = new World(nameof(SystemWorkBuilderTests));
        system = world.CreateSystemManaged<StubSystem>();
    }

    [Fact]
    public void WithNativeContainer_RegistersForDisposal()
    {
        var capturedDisposables = new List<IDisposable>();
        var builder = new FactorySystemWorkBuilder();

        var nativeHolder = builder.WithNativeContainer(_ =>
            new NativeParallelHashSet<int>(capacity: 4, Allocator.Persistent));
        var managedHolder = builder.WithDisposable(_ => new TrackingDisposable());

        var work = builder.Build();
        var context = CreateContext(capturedDisposables);

        work.OnCreate(context);

        Assert.True(nativeHolder.TryGetValue(out var nativeContainer));
        Assert.True(nativeContainer.IsCreated);
        Assert.True(managedHolder.TryGetInstance(out var managedInstance));
        Assert.NotNull(managedInstance);
        Assert.False(managedInstance!.IsDisposed);

        // Simulate the managed disposal pass running independently of OnDestroy.
        foreach (var disposable in capturedDisposables)
        {
            disposable.Dispose();
        }

        Assert.False(nativeHolder.TryGetValue(out _));
        Assert.False(nativeContainer.IsCreated);
        Assert.True(managedInstance.IsDisposed);
        Assert.False(managedHolder.TryGetInstance(out _));

        // The holder disposals are idempotent.
        foreach (var disposable in capturedDisposables)
        {
            disposable.Dispose();
        }

        work.OnDestroy(context);
    }

    FactorySystemContext CreateContext(List<IDisposable> capturedDisposables)
    {
        return new FactorySystemContext(
            system,
            world.EntityManager,
            world.EntityManager.UniversalQuery,
            system.GetEntityTypeHandle(),
            system.GetEntityStorageInfoLookup(),
            system,
            (_, __) => { },
            (_, __) => { },
            (_, __) => { },
            (_, __) => { },
            _ => false,
            _ => { },
            disposable => capturedDisposables.Add(disposable));
    }

    public void Dispose()
    {
        if (world.IsCreated)
        {
            world.Dispose();
        }
    }

    sealed class StubSystem : SystemBase, Bloodcraft.Factory.IRegistrar
    {
        public void Register(Action<SystemBase> refreshAction)
        {
            // No-op: refresh actions are not required for these tests.
        }

        public override void OnUpdate()
        {
        }
    }

    sealed class TrackingDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
