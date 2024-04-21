using Bloodstone.API;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Shared;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Cobalt.Core.Toolbox
{
    public static class Utilities
    {
        public static Entity GetPrefabEntityByPrefabGUID(PrefabGUID prefabGUID, EntityManager entityManager)
        {
            try
            {
                PrefabCollectionSystem prefabCollectionSystem = entityManager.World.GetExistingSystem<PrefabCollectionSystem>();

                return prefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Error: {ex}");
                return Entity.Null;
            }
        }

        public static Il2CppSystem.Type Il2CppTypeGet(Type type)
        {
            return Il2CppSystem.Type.GetType(type.ToString());
        }

        public static ComponentType ComponentTypeGet(string component)
        {
            return ComponentType.ReadOnly(Il2CppSystem.Type.GetType(component));
        }

        // alternative for Entitymanager.HasComponent
        public static bool HasComponent<T>(Entity entity) where T : struct
        {
            return VWorld.Server.EntityManager.HasComponent(entity, ComponentTypeOther<T>());
        }

        // more convenient than Entitymanager.AddComponent
        public static bool AddComponent<T>(Entity entity) where T : struct
        {
            return VWorld.Server.EntityManager.AddComponent(entity, ComponentTypeOther<T>());
        }

        // alternative for Entitymanager.AddComponentData
        public static void AddComponentData<T>(Entity entity, T componentData) where T : struct
        {
            AddComponent<T>(entity);
            SetComponentData(entity, componentData);
        }

        // alternative for Entitymanager.RemoveComponent
        public static bool RemoveComponent<T>(Entity entity) where T : struct
        {
            return VWorld.Server.EntityManager.RemoveComponent(entity, ComponentTypeOther<T>());
        }

        // alternative for EntityMManager.GetComponentData
        public static unsafe T GetComponentData<T>(Entity entity) where T : struct
        {
            void* rawPointer = VWorld.Server.EntityManager.GetComponentDataRawRO(entity, ComponentTypeIndex<T>());
            return Marshal.PtrToStructure<T>(new IntPtr(rawPointer));
        }

        // alternative for EntityManager.SetComponentData
        public static unsafe void SetComponentData<T>(Entity entity, T componentData) where T : struct
        {
            var size = Marshal.SizeOf(componentData);
            //byte[] byteArray = new byte[size];
            var byteArray = StructureToByteArray(componentData);
            fixed (byte* data = byteArray)
            {
                //UnsafeUtility.CopyStructureToPtr(ref componentData, data);
                VWorld.Server.EntityManager.SetComponentDataRaw(entity, ComponentTypeIndex<T>(), data, size);
            }
        }

        private static ComponentType ComponentTypeOther<T>()
        {
            return new ComponentType(Il2CppType.Of<T>());
        }

        private static int ComponentTypeIndex<T>()
        {
            return ComponentTypeOther<T>().TypeIndex;
        }

        private static byte[] StructureToByteArray<T>(T structure) where T : struct
        {
            int size = Marshal.SizeOf(structure);
            byte[] byteArray = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, ptr, true);
                Marshal.Copy(ptr, byteArray, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return byteArray;
        }
    }

    public static class CastleTerritoryCache
    {
        public static Dictionary<int2, Entity> BlockTileToTerritory = [];
        public static int TileToBlockDivisor = 10;

        public static void Initialize()
        {
            var entities = Helper.GetEntitiesByComponentTypes<CastleTerritoryBlocks>();
            foreach (var entity in entities)
            {
                entity.LogComponentTypes();
                var buffer = entity.ReadBuffer<CastleTerritoryBlocks>();
                foreach (var block in buffer)
                {
                    //Plugin.Logger.LogInfo($"{block.BlockCoordinate}");
                    BlockTileToTerritory[block.BlockCoordinate] = entity;
                }
            }
        }

        public static bool TryGetCastleTerritory(Entity entity, out Entity territoryEntity)
        {
            if (entity.Has<TilePosition>())
            {
                return BlockTileToTerritory.TryGetValue(entity.Read<TilePosition>().Tile / TileToBlockDivisor, out territoryEntity);
            }
            territoryEntity = default;
            return false;
        }
    }

    public static class Il2cppService
    {
        public static Il2CppSystem.Type GetType<T>() => Il2CppType.Of<T>();

        public static unsafe T GetComponentDataAOT<T>(this EntityManager entityManager, Entity entity) where T : unmanaged
        {
            var type = TypeManager.GetTypeIndex(GetType<T>());
            var result = (T*)entityManager.GetComponentDataRawRW(entity, type);

            return *result;
        }

        public static NativeArray<Entity> GetEntitiesByComponentTypes<T1>(bool includeAll = false)
        {
            EntityQueryOptions options = includeAll ? EntityQueryOptions.IncludeAll : EntityQueryOptions.Default;

            EntityQueryDesc queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] {
                new ComponentType(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite)
            },
                Options = options
            };

            var query = VWorld.Server.EntityManager.CreateEntityQuery(queryDesc);
            var entities = query.ToEntityArray(Allocator.Temp);

            return entities;
        }

        public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2>(bool includeAll = false)
        {
            EntityQueryOptions options = includeAll ? EntityQueryOptions.IncludeAll : EntityQueryOptions.Default;

            EntityQueryDesc queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] {
                new ComponentType(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite),
                new ComponentType(Il2CppType.Of<T2>(), ComponentType.AccessMode.ReadWrite)
            },
                Options = options
            };

            var query = VWorld.Server.EntityManager.CreateEntityQuery(queryDesc);
            var entities = query.ToEntityArray(Allocator.Temp);

            return entities;
        }

        public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2, T3>(bool includeAll = false)
        {
            EntityQueryOptions options = includeAll ? EntityQueryOptions.IncludeAll : EntityQueryOptions.Default;

            EntityQueryDesc queryDesc = new EntityQueryDesc
            {
                All = new ComponentType[] {
                new ComponentType(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite),
                new ComponentType(Il2CppType.Of<T2>(), ComponentType.AccessMode.ReadWrite),
                new ComponentType(Il2CppType.Of<T3>(), ComponentType.AccessMode.ReadWrite)
            },
                Options = options
            };

            var query = VWorld.Server.EntityManager.CreateEntityQuery(queryDesc);
            var entities = query.ToEntityArray(Allocator.Temp);

            return entities;
        }
    }

    public static class SystemPatchUtil
    {
        public static void Destroy(Entity entity)
        {
            VWorld.Server.EntityManager.AddComponent<Disabled>(entity);
            DestroyUtility.CreateDestroyEvent(VWorld.Server.EntityManager, entity, DestroyReason.Default, DestroyDebugReason.ByScript);
        }

        public static void Disable(Entity entity)
        {
            VWorld.Server.EntityManager.AddComponent<Disabled>(entity);
            //DestroyUtility.CreateDestroyEvent(VWorld.Server.EntityManager, entity, DestroyReason.Default, DestroyDebugReason.ByScript);
        }

        public static void Enable(Entity entity)
        {
            VWorld.Server.EntityManager.RemoveComponent<Disabled>(entity);
            //DestroyUtility.CreateDestroyEvent(VWorld.Server.EntityManager, entity, DestroyReason.Default, DestroyDebugReason.ByScript);
        }
    }

    public static class NetworkedEntityUtil
    {
        private static readonly NetworkIdSystem _NetworkIdSystem = VWorld.Server.GetExistingSystem<NetworkIdSystem>();

        public static bool TryFindEntity(NetworkId networkId, out Entity entity)
        {
            return _NetworkIdSystem._NetworkIdToEntityMap.TryGetValue(networkId, out entity);
        }
    }
}