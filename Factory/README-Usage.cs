// This file documents the intended usage for the system factory helpers. It is excluded from compilation.
//
// -----------------------------------------------------------------------------
// Query holders and resource initialisers
// -----------------------------------------------------------------------------
// The SystemWorkBuilder now exposes query helpers that return lightweight holder
// objects. These holders are initialised during the builder's resource
// initialiser pass and cleared automatically during OnDestroy:
//
//     var descriptor = QueryDescriptor.Create()
//         .WithAll<Movement>()
//         .IncludeDisabled();
//
//     var builder = new SystemWorkBuilder()
//         .WithQuery(descriptor);
//
//     SystemWorkBuilder.QueryHandleHolder primaryQuery =
//         builder.WithPrimaryQuery(requireForUpdate: true);
//     SystemWorkBuilder.QueryHandleHolder secondaryQuery =
//         builder.WithQuery(ref descriptor);
//
// The holders surface the underlying QueryHandle through the Handle property.
// When the system shuts down the builder-managed OnDestroy sets the holder back
// to null, ensuring callers never interact with a disposed handle. Additional
// native allocations should still be registered via
// SystemContext.RegisterDisposable where appropriate.
//
// -----------------------------------------------------------------------------
// Example work class
// -----------------------------------------------------------------------------
// public sealed class TrackMinionsSystem : VSystemBase<TrackMinionsSystem.Work>
// {
//     public sealed class Work : ISystemWork
//     {
//         readonly ISystemWork _implementation;
//         readonly SystemWorkBuilder.QueryHandleHolder _trackedMinions;
//         readonly SystemWorkBuilder.ComponentLookupHandle<Movement> _movementLookup;
//
//         public Work()
//         {
//             var descriptor = QueryDescriptor.Create()
//                 .WithAll<Movement>()
//                 .IncludeDisabled();
//
//             var builder = new SystemWorkBuilder()
//                 .WithQuery(descriptor);
//
//             _trackedMinions = builder.WithPrimaryQuery(requireForUpdate: true);
//             _movementLookup = builder.WithLookup<Movement>(isReadOnly: true);
//
//             builder.OnUpdate(context =>
//             {
//                 var queryHandle = _trackedMinions.Handle;
//                 if (queryHandle == null || queryHandle.IsDisposed)
//                 {
//                     return;
//                 }
//
//                 SystemWorkBuilder.ForEachEntity(context, queryHandle, iterator =>
//                 {
//                     if (!context.Exists(iterator.Entity))
//                         return;
//
//                     var movement = iterator.GetLookup(_movementLookup)[iterator.Entity];
//                     // Perform work here.
//                 });
//             });
//
//             _implementation = builder.Build();
//         }
//
//         public void Build(ref EntityQueryBuilder builder) =>
//             _implementation.Build(ref builder);
//         public void OnCreate(SystemContext context) =>
//             _implementation.OnCreate(context);
//         public void OnUpdate(SystemContext context) =>
//             _implementation.OnUpdate(context);
//         public void OnDestroy(SystemContext context) =>
//             _implementation.OnDestroy(context);
//     }
// }
//
// -----------------------------------------------------------------------------
// Builder-driven systems
// -----------------------------------------------------------------------------
// public sealed class BuilderDrivenSystem : VSystemBase<ISystemWork>
// {
//     readonly SystemWorkBuilder.ComponentLookupHandle<Movement> _movementLookup;
//     readonly SystemWorkBuilder.ComponentTypeHandleHandle<Movement> _movementHandle;
//
//     public BuilderDrivenSystem()
//         : base(CreateWork(out var artifacts))
//     {
//         _movementLookup = artifacts.MovementLookup;
//         _movementHandle = artifacts.MovementHandle;
//     }
//
//     static ISystemWork CreateWork(out BuilderArtifacts artifacts)
//     {
//         var descriptor = QueryDescriptor.Create()
//             .WithAll<Movement>();
//
//         var builder = new SystemWorkBuilder()
//             .WithQuery(descriptor);
//
//         var primaryQuery = builder.WithPrimaryQuery();
//         var movementLookup = builder.WithLookup<Movement>(isReadOnly: true);
//         var movementHandle = builder.WithComponentTypeHandle<Movement>(isReadOnly: true);
//
//         builder.OnUpdate(context =>
//         {
//             var queryHandle = primaryQuery.Handle;
//             if (queryHandle == null || queryHandle.IsDisposed)
//             {
//                 return;
//             }
//
//             var system = (BuilderDrivenSystem)context.System;
//
//             SystemWorkBuilder.ForEachChunk(context, queryHandle, chunk =>
//             {
//                 var movementArray = chunk.GetNativeArray(system._movementHandle);
//                 var entities = chunk.Entities;
//
//                 for (int i = 0; i < chunk.Count; ++i)
//                 {
//                     var entity = entities[i];
//                     var movement = movementArray[i];
//                     // Perform work here.
//                 }
//             });
//         });
//
//         artifacts = new BuilderArtifacts(movementLookup, movementHandle);
//         return builder.Build();
//     }
//
//     sealed class BuilderArtifacts
//     {
//         public BuilderArtifacts(
//             SystemWorkBuilder.ComponentLookupHandle<Movement> movementLookup,
//             SystemWorkBuilder.ComponentTypeHandleHandle<Movement> movementHandle)
//         {
//             MovementLookup = movementLookup;
//             MovementHandle = movementHandle;
//         }
//
//         public SystemWorkBuilder.ComponentLookupHandle<Movement> MovementLookup { get; }
//         public SystemWorkBuilder.ComponentTypeHandleHandle<Movement> MovementHandle { get; }
//     }
// }
//
// -----------------------------------------------------------------------------
// Migration notes
// -----------------------------------------------------------------------------
// * Replace direct calls to context.WithQuery/context.CreateQuery with the new
//   builder helpers. The holders automatically reset during OnDestroy, so manual
//   `_query = null;` assignments are no longer necessary.
// * The descriptor-based WithQuery overload accepts a disposeOnDestroy flag that
//   determines whether the created query is registered with
//   SystemContext.RegisterDisposable. Pass false only when another owner will
//   handle the lifecycle.
// * Existing lookup and handle helpers continue to work unchanged. They should
//   still be created through the builder to participate in the refresh pipeline.
