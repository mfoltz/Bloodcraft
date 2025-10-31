// This file documents the intended usage for the system factory helpers. It is excluded from compilation.
//
// Example:
//
// public sealed class TrackMinionsSystem : VSystemBase<TrackMinionsSystem.Work>
// {
//     public sealed class Work : ISystemWork
//     {
//         ComponentLookup<Movement> _movementLookup;
//         QueryHandle _trackedMinions;
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
//             context.Registrar.Register(system =>
//             {
//                 _movementLookup = system.GetComponentLookup<Movement>(true);
//             });
//         }
//
//         public void OnUpdate(SystemContext context)
//         {
//             _trackedMinions.ForEachEntity(entity =>
//             {
//                 if (!context.Exists(entity))
//                     return;
//
//                 var movement = _movementLookup[entity];
//                 // Perform work here.
//             });
//         }
//
//         public void OnDestroy(SystemContext context)
//         {
//             _trackedMinions = null;
//         }
//     }
// }

// Example using the fluent builder:
//
// public sealed class BuilderDrivenSystem : VSystemBase<ISystemWork>
// {
//     readonly SystemWorkBuilder.ComponentLookupHandle<Movement> _movementLookup;
//
//     public BuilderDrivenSystem()
//         : base(CreateWork(out _movementLookup))
//     {
//     }
//
//     static ISystemWork CreateWork(out SystemWorkBuilder.ComponentLookupHandle<Movement> movementLookup)
//     {
//         var builder = new SystemWorkBuilder()
//             .WithQuery((ref EntityQueryBuilder q) =>
//             {
//                 q.AddAll(ComponentType.ReadOnly(Il2CppType.Of<Movement>()));
//             });
//
//         movementLookup = builder.WithLookup<Movement>(true);
//
//         builder.OnUpdate(context =>
//         {
//             var query = context.WithQuery(context.Query);
//             query.ForEachEntity(entity =>
//             {
//                 var movement = movementLookup.Lookup[entity];
//                 // Perform work here.
//             });
//         });
//
//         return builder.Build();
//     }
// }
