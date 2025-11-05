# System Factory Usage Guide

This document describes how to work with the system factory helpers.

## Query holders and resource initialisers

The `SystemWorkBuilder` exposes query helpers that return lightweight holder objects.
These holders are initialised during the builder's resource initialiser pass and are
cleared automatically during `OnDestroy`.

```csharp
var descriptor = QueryDescriptor.Create()
    .WithAll<Movement>()
    .IncludeDisabled();

var builder = new SystemWorkBuilder()
    .WithQuery(descriptor);

SystemWorkBuilder.QueryHandleHolder primaryQuery =
    builder.WithPrimaryQuery(requireForUpdate: true);
SystemWorkBuilder.QueryHandleHolder secondaryQuery =
    builder.WithQuery(ref descriptor);
```

The holders surface the underlying `QueryHandle` through the `Handle` property. When the
system shuts down the builder-managed `OnDestroy` sets the holder back to `null`,
ensuring callers never interact with a disposed handle. Additional native allocations
should still be registered via `SystemContext.RegisterDisposable` where appropriate.

## Persistent native containers

Systems built on `VSystemBase` tend to retain caches across multiple updates, making them ideal candidates for persistent `Native*` containers instead of managed collections. The quest target system demonstrates this pattern by storing per-prefab caches inside `NativeParallelMultiHashMap` and `NativeParallelHashSet` fields that survive between frames without hitting the GC.【F:Factory/Quests/QuestTargetSystem.Work.cs†L45-L138】 Because `VSystemBase` automatically disposes every `IDisposable` registered through the system context during teardown, these native containers can be kept alive for the full lifetime of the system without bespoke cleanup code.【F:Factory/VSystemBase.cs†L84-L90】【F:Factory/VSystemBase.cs†L192-L199】【F:Factory/VSystemBase.cs†L279-L289】 Persistent containers also remain burst-friendly and safe to use from jobs, whereas managed collections would be off-limits during DOTS execution.

Use [`SystemWorkBuilder.WithNativeContainer`](SystemWorkBuilder.cs#L601-L634) to allocate persistent containers once during the builder's `OnCreate` pass and surface them through strongly typed holders. The helper automatically registers the container for disposal, so the system shuts down cleanly without manual teardown code.

```csharp
SystemWorkBuilder.NativeContainerHolder<NativeParallelHashSet<Entity>> _handled;

public Work()
{
    var builder = new SystemWorkBuilder();

    _handled = builder.WithNativeContainer(_ =>
    {
        // Allocate the persistent container a single time during OnCreate.
        return new NativeParallelHashSet<Entity>(512, Allocator.Persistent);
    });

    builder.OnUpdate(context =>
    {
        ref var handled = ref _handled.Container;
        handled.Clear();

        // Use handled across updates without re-allocating.
    });

    _implementation = builder.Build();
    // Native containers registered with the builder are disposed automatically on teardown.
}
```

The holders expose a `Container` reference, making it easy to clear or refresh caches at the start of each update. Because the builder takes responsibility for disposal you avoid sprinkling manual `Dispose()` calls across lifecycle hooks, and the scaffold emitted by the system work generator now includes comments that highlight exactly where to perform allocation, refresh, and cleanup steps.

## Example work class

The following example demonstrates how to create a system that tracks minions while
leveraging the builder helpers to manage queries and lookups.

```csharp
public sealed class TrackMinionsSystem : VSystemBase<TrackMinionsSystem.Work>
{
    public sealed class Work : ISystemWork
    {
        readonly ISystemWork _implementation;
        readonly SystemWorkBuilder.QueryHandleHolder _trackedMinions;
        readonly SystemWorkBuilder.ComponentLookupHandle<Movement> _movementLookup;

        public Work()
        {
            var descriptor = QueryDescriptor.Create()
                .WithAll<Movement>()
                .IncludeDisabled();

            var builder = new SystemWorkBuilder()
                .WithQuery(descriptor);

            _trackedMinions = builder.WithPrimaryQuery(requireForUpdate: true);
            _movementLookup = builder.WithLookup<Movement>(isReadOnly: true);

            builder.OnUpdate(context =>
            {
                var queryHandle = _trackedMinions.Handle;
                if (queryHandle == null || queryHandle.IsDisposed)
                {
                    return;
                }

                SystemWorkBuilder.ForEachEntity(context, queryHandle, iterator =>
                {
                    if (!iterator.TryGetComponent(_movementLookup, out var movement))
                        return;

                    // Perform work here.
                });
            });

            _implementation = builder.Build();
        }

        public void Build(ref EntityQueryBuilder builder) =>
            _implementation.Build(ref builder);
        public void OnCreate(SystemContext context) =>
            _implementation.OnCreate(context);
        public void OnUpdate(SystemContext context) =>
            _implementation.OnUpdate(context);
        public void OnDestroy(SystemContext context) =>
            _implementation.OnDestroy(context);
    }
}
```

The `TryGetComponent` helper automatically verifies that the entity still exists before
returning the requested component, eliminating the need for explicit
`context.Exists` checks inside the iteration loop.

## Builder-driven systems

`SystemWorkBuilder` can also be used to drive systems directly. In the following example
the builder constructs the work implementation while surfacing handles to the system
for reuse during updates.

```csharp
public sealed class BuilderDrivenSystem : VSystemBase<ISystemWork>
{
    readonly SystemWorkBuilder.ComponentLookupHandle<Movement> _movementLookup;
    readonly SystemWorkBuilder.ComponentTypeHandleHandle<Movement> _movementHandle;

    public BuilderDrivenSystem()
        : base(CreateWork(out var artifacts))
    {
        _movementLookup = artifacts.MovementLookup;
        _movementHandle = artifacts.MovementHandle;
    }

    static ISystemWork CreateWork(out BuilderArtifacts artifacts)
    {
        var descriptor = QueryDescriptor.Create()
            .WithAll<Movement>();

        var builder = new SystemWorkBuilder()
            .WithQuery(descriptor);

        var primaryQuery = builder.WithPrimaryQuery();
        var movementLookup = builder.WithLookup<Movement>(isReadOnly: true);
        var movementHandle = builder.WithComponentTypeHandle<Movement>(isReadOnly: true);

        builder.OnUpdate(context =>
        {
            var queryHandle = primaryQuery.Handle;
            if (queryHandle == null || queryHandle.IsDisposed)
            {
                return;
            }

            var system = (BuilderDrivenSystem)context.System;

            SystemWorkBuilder.ForEachChunk(context, queryHandle)
                .WithReadOnlyComponent(system._movementHandle)
                .ForEach((chunkContext, movementArray) =>
                {
                    var entities = chunkContext.Entities;

                    for (int i = 0; i < chunkContext.Count; ++i)
                    {
                        var entity = entities[i];

                        if (!chunkContext.TryGetComponent(system._movementLookup, entity, out var movement))
                        {
                            continue;
                        }

                        // Perform work here with the validated movement component.
                    }
                });
        });

        artifacts = new BuilderArtifacts(movementLookup, movementHandle);
        return builder.Build();
    }

    sealed class BuilderArtifacts
    {
        public BuilderArtifacts(
            SystemWorkBuilder.ComponentLookupHandle<Movement> movementLookup,
            SystemWorkBuilder.ComponentTypeHandleHandle<Movement> movementHandle)
        {
            MovementLookup = movementLookup;
            MovementHandle = movementHandle;
        }

        public SystemWorkBuilder.ComponentLookupHandle<Movement> MovementLookup { get; }
        public SystemWorkBuilder.ComponentTypeHandleHandle<Movement> MovementHandle { get; }
    }
}
```

Within chunk iteration the same helper guarantees that the component is available for
the current entity index before the loop processes it further.

## Throttling expensive updates

`SystemWorkBuilder` can gate `OnUpdate` delegates so they only run on a desired cadence.
Use `WithUpdateInterval` to supply a custom time provider, or lean on the convenience
wrappers when sampling server time or the fixed tick counter.

```csharp
var builder = new SystemWorkBuilder()
    .WithQuery(descriptor)
    .WithServerTimeInterval(TimeSpan.FromSeconds(15))
    .WithFixedServerTickInterval(20);

var expensiveQuery = builder.WithPrimaryQuery(requireForUpdate: true);

builder.OnUpdate(context =>
{
    // This block runs at most once every 15 seconds and every twentieth tick.
    var queryHandle = expensiveQuery.Handle;
    if (queryHandle == null || queryHandle.IsDisposed)
    {
        return;
    }

    SystemWorkBuilder.ForEachEntity(context, queryHandle, iterator =>
    {
        // Perform throttled work here.
    });
});
```

The helper registers its internal state with `SystemContext.Registrar.Register`, ensuring
the gate refreshes alongside lookups and other builder-managed resources. The fixed
tick overload leverages the same refresh pass to count server ticks, letting you express
cadence in either seconds or discrete updates.

## Spawn-tag queries

Use <code>IncludeSpawnTag()</code> when a query should target entities that are still marked with the spawn tag. This is useful
for systems that need to process freshly spawned prefabs before other conversions strip the marker.

```csharp
var spawnDescriptor = QueryDescriptor.Create()
    .WithAll<HarvestableResource>()
    .IncludeSpawnTag();

var builder = new SystemWorkBuilder()
    .WithQuery(spawnDescriptor);
```

## Responding to spawn and destroy tags

`SystemWorkBuilder.ForEachSpawned` and `SystemWorkBuilder.ForEachDestroyed` construct
temporary queries that already include the necessary DOTS options to observe spawn- and
destroy-tagged entities. They execute actions through the builder's disposal-safe
iteration pipeline, ensuring native arrays are disposed automatically.

```csharp
builder.OnUpdate(context =>
{
    SystemWorkBuilder.ForEachSpawned(
        context,
        descriptor => descriptor
            .WithAll<Minion>()
            .WithAll<EntityOwner>(),
        iterator =>
        {
            var entity = iterator.Entity;
            // Inspect freshly spawned minions here.
        });

    SystemWorkBuilder.ForEachDestroyed(
        context,
        descriptor => descriptor.WithAll<MyComponent>(),
        iterator =>
        {
            // Schedule cleanup that must run during ECB playback.
            context.EnqueueDestroyTagCleanup(ecb =>
                ecb.RemoveComponent<DestroyTag>(iterator.Entity));
        });
});
```

Harmony patches or other consumers that only have access to an `EntityManager` can use
the overloads that accept a descriptor configuration delegate instead of a
`SystemContext` instance.

## Singleton gating

Systems that should only run when a DOTS singleton exists can register a dedicated query using
<code>RequireSingleton&lt;TSingleton&gt;()</code>. The builder automatically marks the query as required for update and includes the
necessary <code>EntityQueryOptions.IncludeSystems</code> flag.

```csharp
var singletonQuery = builder.RequireSingleton<GameSettingsSystem.Singleton>();

builder.OnUpdate(context =>
{
    var handle = singletonQuery.Handle;
    if (handle == null || handle.IsDisposed)
    {
        return;
    }

    // Access the singleton components safely here.
});
```

## Migration notes

* Replace direct calls to `context.WithQuery`/`context.CreateQuery` with the new builder
  helpers. The holders automatically reset during `OnDestroy`, so manual `_query = null;`
  assignments are no longer necessary.
* The descriptor-based `WithQuery` overload accepts a `disposeOnDestroy` flag that
  determines whether the created query is registered with `SystemContext.RegisterDisposable`.
  Pass `false` only when another owner will handle the lifecycle.
* Existing lookup and handle helpers continue to work unchanged. They should still be
  created through the builder to participate in the refresh pipeline.
