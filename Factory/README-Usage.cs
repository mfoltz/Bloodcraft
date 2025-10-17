// This file documents the intended usage for the system factory helpers. It is excluded from compilation.
//
// Example:
//
// public sealed class TrackMinionsSystem : VSystemBase<TrackMinionsSystem.Work>
// {
//     public sealed class Work : ISystemWork
//     {
//         ComponentLookup<Movement> _movementLookup;
//
//         public void Build(ref EntityQueryBuilder builder)
//         {
//             builder.AddAll(ComponentType.ReadOnly(Il2CppType.Of<Movement>()));
//             builder.WithOptions(EntityQueryOptions.IncludeDisabled);
//         }
//
//         public void OnCreate(SystemContext context)
//         {
//             context.Registrar.Register(system =>
//             {
//                 _movementLookup = system.GetComponentLookup<Movement>(true);
//             });
//         }
//
//         public void OnUpdate(SystemContext context)
//         {
//             context.ForEachEntity(context.Query, entity =>
//             {
//                 if (!context.Exists(entity))
//                     return;
//
//                 var movement = _movementLookup[entity];
//                 // Perform work here.
//             });
//         }
//     }
// }
