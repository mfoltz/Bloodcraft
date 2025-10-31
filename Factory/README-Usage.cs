// This file documents the intended usage for the system factory helpers. It is excluded from compilation.
//
// Example:
//
// public sealed class TrackMinionsSystem : VSystemBase<TrackMinionsSystem.Work>
// {
//     public sealed class Work : ISystemWork
//     {
//         SystemWorkBuilder.ComponentLookupHandle<Movement> _movementLookup;
//         QueryHandle _trackedMinions;
//         NativeParallelHashSet<Entity> _trackedEntities;
//
//         public void Build(ref EntityQueryBuilder builder)
//         {
//             builder.AddAll(ComponentType.ReadOnly(Il2CppType.Of<Movement>()));
//             builder.WithOptions(EntityQueryOptions.IncludeDisabled);
//         }
//
//         public void OnCreate(SystemContext context)
//         {
//             _trackedMinions = context.WithQuery(context.Query);
//
//             _movementLookup = SystemWorkBuilder.CreateLookup<Movement>(context, isReadOnly: true);
//
//             _trackedEntities = new NativeParallelHashSet<Entity>(256, Allocator.Persistent);
//             context.RegisterDisposable(_trackedEntities);
//         }
//
//         public void OnUpdate(SystemContext context)
//         {
//             SystemWorkBuilder.ForEachEntity(context, _trackedMinions, iterator =>
//             {
//                 if (!context.Exists(iterator.Entity))
//                     return;
//
//                 var movement = iterator.GetLookup(_movementLookup)[iterator.Entity];
//                 // Perform work here.
//             });
//         }
//
//         public void OnDestroy(SystemContext context)
//         {
//             _trackedMinions = null;
//
//             if (_trackedEntities.IsCreated)
//             {
//                 _trackedEntities.Clear();
//                 _trackedEntities = default;
//             }
//         }
//     }
// }

// Example using the fluent builder:
//
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
//         var builder = new SystemWorkBuilder()
//             .WithQuery(static (ref EntityQueryBuilder queryBuilder) =>
//             {
//                 queryBuilder.AddAll(ComponentType.ReadOnly(Il2CppType.Of<Movement>()));
//             })
//             .RequireForUpdate(true);
//
//         var movementLookup = builder.WithLookup<Movement>(isReadOnly: true);
//         var movementHandle = builder.WithComponentTypeHandle<Movement>(isReadOnly: true);
//         QueryHandle movementQuery = null;
//
//         builder.OnCreate(context =>
//         {
//             movementQuery = context.WithQuery(context.Query, requireForUpdate: true);
//         });
//
//         builder.OnUpdate(context =>
//         {
//             if (movementQuery == null)
//             {
//                 return;
//             }
//
//             var system = (BuilderDrivenSystem)context.System;
//
//             SystemWorkBuilder.ForEachChunk(context, movementQuery, chunk =>
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
//         builder.OnDestroy(context =>
//         {
//             movementQuery = null;
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

// -----------------------------------------------------------------------------
// System work generator helper
// -----------------------------------------------------------------------------
// The repository ships with an interactive generator that scaffolds builder-based
// or work-class implementations. Execute it from the repository root:
//
//     dotnet run --project Bloodcraft.csproj -- --system-work
//
// The tool prompts for the system name, primary components, and any optional
// lookup/handle requirements. It then prints either a fluent builder snippet or
// a partial work class skeleton that follows the patterns demonstrated above.
// Copy the emitted code into your system and tailor the TODO sections to your
// specific logic.
// -----------------------------------------------------------------------------
// Additional examples
// -----------------------------------------------------------------------------
// The quest target system (Systems/Quests/QuestTargetSystemBase.cs paired with
// Factory/Quests/QuestTargetSystem.Work.cs) showcases coordinating multiple
// query handles while maintaining native container caches across updates.
