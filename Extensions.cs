using Bloodcraft.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft;
internal static class Extensions
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    
    public delegate void ActionRef<T>(ref T item);
    public static void With<T>(this Entity entity, ActionRef<T> action) where T : struct
    {
        T item = entity.ReadRW<T>();
        action(ref item);
        EntityManager.SetComponentData(entity, item);
    }
    
    public unsafe static void Write<T>(this Entity entity, T componentData) where T : struct
    {
        // Get the ComponentType for T
        var ct = new ComponentType(Il2CppType.Of<T>());

        // Marshal the component data to a byte array
        byte[] byteArray = StructureToByteArray(componentData);

        // Get the size of T
        int size = Marshal.SizeOf<T>();

        // Create a pointer to the byte array
        fixed (byte* p = byteArray)
        {
            // Set the component data
            EntityManager.SetComponentDataRaw(entity, ct.TypeIndex, p, size);
        }
    }
    public static byte[] StructureToByteArray<T>(T structure) where T : struct
    {
        int size = Marshal.SizeOf(structure);
        byte[] byteArray = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(structure, ptr, true);
        Marshal.Copy(ptr, byteArray, 0, size);
        Marshal.FreeHGlobal(ptr);

        return byteArray;
    }
    public unsafe static T ReadRW<T>(this Entity entity) where T : struct
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        void* componentDataRawRW = EntityManager.GetComponentDataRawRW(entity, ct.TypeIndex);
        T componentData = Marshal.PtrToStructure<T>(new IntPtr(componentDataRawRW));
        return componentData;
    }
    public unsafe static T Read<T>(this Entity entity) where T : struct
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        void* rawPointer = EntityManager.GetComponentDataRawRO(entity, ct.TypeIndex);
        T componentData = Marshal.PtrToStructure<T>(new IntPtr(rawPointer));
        return componentData;
    }
    public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.GetBuffer<T>(entity);
    }
    public static bool TryGetComponent<T>(this Entity entity, out T componentData) where T : struct
    {
        componentData = default;
        if (entity.Has<T>())
        {
            componentData = entity.Read<T>();
            return true;
        }
        return false;
    }
    public static bool Has<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        return EntityManager.HasComponent(entity, ct);
    }
    public static string LookupName(this PrefabGUID prefabGUID)
    {
        return (PrefabCollectionSystem.PrefabGuidToNameDictionary.ContainsKey(prefabGUID)
            ? PrefabCollectionSystem.PrefabGuidToNameDictionary[prefabGUID] + " " + prefabGUID : "Guid Not Found").ToString();
    }
    public static string GetPrefabName(this PrefabGUID itemPrefabGUID)
    {
        return LocalizationService.GetPrefabName(itemPrefabGUID);
    }
    public static void LogComponentTypes(this Entity entity)
    {
        NativeArray<ComponentType>.Enumerator enumerator = EntityManager.GetComponentTypes(entity).GetEnumerator();
        Core.Log.LogInfo("===");
        while (enumerator.MoveNext())
        {
            ComponentType current = enumerator.Current;
            Core.Log.LogInfo($"{current}");
        }
        Core.Log.LogInfo("===");
    }
    public static void Add<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        EntityManager.AddComponent(entity, ct);
    }
    public static void Remove<T>(this Entity entity)
    {
        var ct = new ComponentType(Il2CppType.Of<T>());
        EntityManager.RemoveComponent(entity, ct);
    }
    public static Entity GetOwner(this Entity entity)
    {
        return ServerGameManager.GetOwner(entity);
    }
    public static bool FollowingPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;
        if (entity.Has<Follower>())
        {
            Follower follower = entity.Read<Follower>();
            Entity followed = follower.Followed._Value;
            if (followed.Has<PlayerCharacter>())
            {
                player = followed;
                return true;
            }
        }
        return false;
    }
    public static bool HasPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;
        if (entity.Has<PlayerCharacter>())
        {
            player = entity;
            return true;
        }
        return false;
    }
    public static bool IsVampire(this Entity entity)
    {
        if (entity.Has<VampireTag>())
        {
            return true;
        }
        return false;
    }
    public static Entity GetBuffTarget(this Entity entity)
    {
        return CreateGameplayEventServerUtility.GetBuffTarget(EntityManager, entity);
    }
    public static Entity GetPrefabEntity(this Entity entity)
    {
        return ServerGameManager.GetPrefabEntity(entity.Read<PrefabGUID>());
    }
    public static Entity GetSpellTarget(this Entity entity)
    {
        return CreateGameplayEventServerUtility.GetSpellTarget(EntityManager, entity);
    }
    public static Entity GetTeamEntity(this Entity entity)
    {
        if (entity.Has<TeamReference>())
        {
            return entity.Read<TeamReference>().Value._Value;
        }
        return Entity.Null;
    }
    public static bool Exists(this Entity entity)
    {
        return EntityManager.Exists(entity);
    }
    public static bool Disabled(this Entity entity)
    {
        return entity.Has<Disabled>();
    }
    public static ulong GetSteamId(this Entity entity)
    {
        return entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
    }
}