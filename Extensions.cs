using Bloodcraft.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft;
internal static class Extensions
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
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
    public static DynamicBuffer<T> AddBuffer<T>(this Entity entity) where T : struct
    {
        return EntityManager.AddBuffer<T>(entity);
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
    public static bool TryRemoveComponent<T>(this Entity entity) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.Remove<T>();

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

        enumerator.Dispose();
    }
    public static void Add<T>(this Entity entity)
    {
        EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Remove<T>(this Entity entity)
    {
        EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static Entity GetOwner(this Entity entity)
    {
        return ServerGameManager.GetOwner(entity);
    }
    public static bool TryGetFollowedPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;

        if (entity.Has<Follower>())
        {
            Follower follower = entity.Read<Follower>();
            Entity followed = follower.Followed._Value;

            if (followed.IsPlayer())
            {
                player = followed;

                return true;
            }
        }

        return false;
    }
    public static bool TryGetPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;

        if (entity.Has<PlayerCharacter>())
        {
            player = entity;

            return true;
        }

        return false;
    }
    public static bool IsPlayer(this Entity entity)
    {
        if (entity.Has<VampireTag>())
        {
            return true;
        }

        return false;
    }
    public static bool IsDifferentPlayer(this Entity entity, Entity target)
    {
        if (entity.IsPlayer() && target.IsPlayer() && !entity.Equals(target))
        {
            return true;
        }

        return false;
    }
    public static bool IsFollowingPlayer(this Entity entity)
    {
        if (entity.Has<Follower>())
        {
            Follower follower = entity.Read<Follower>();
            if (follower.Followed._Value.IsPlayer())
            {
                return true;
            }
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
    public static bool TryGetTeamEntity(this Entity entity, out Entity teamEntity)
    {
        teamEntity = Entity.Null;

        if (entity.TryGetComponent(out TeamReference teamReference))
        {
            Entity teamReferenceEntity = teamReference.Value._Value;
            
            if (teamReferenceEntity.Exists())
            {
                teamEntity = teamReferenceEntity;

                return true;
            }
        }

        return false;
    }
    public static bool Exists(this Entity entity)
    {
        return EntityManager.Exists(entity);
    }
    public static bool IsDisabled(this Entity entity)
    {
        return entity.Has<Disabled>();
    }
    public static bool IsVBlood(this Entity entity)
    {
        return entity.Has<VBloodUnit>();
    }
    public static ulong GetSteamId(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter))
        {
            return playerCharacter.UserEntity.Read<User>().PlatformId;
        }
        else if (entity.TryGetComponent(out User user))
        {
            return user.PlatformId;
        }

        return 0;
    }
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }
    public static bool TryGetPlayerInfo(this ulong steamId, out PlayerInfo playerInfo)
    {
        if (PlayerCache.TryGetValue(steamId, out playerInfo)) return true;
        else if (OnlineCache.TryGetValue(steamId, out playerInfo)) return true;

        return false;
    }
    public static PrefabGUID GetPrefabGUID(this Entity entity)
    {
        if (entity.TryGetComponent(out PrefabGUID prefabGUID)) return prefabGUID;
        return PrefabGUID.Empty;
    }
    public static Entity GetUserEntity(this Entity character)
    {
        if (character.TryGetComponent(out PlayerCharacter playerCharacter)) return playerCharacter.UserEntity;
        return Entity.Null;
    }
    public static User GetUser(this Entity entity)
    {
        User user = User.Empty;

        if (entity.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.UserEntity.TryGetComponent(out user)) return user;
        else if (entity.TryGetComponent(out user)) return user;

        return user;
    }
    public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGUID)
    {
        return ServerGameManager.HasBuff(entity, buffPrefabGUID.ToIdentifier());
    }
    public static bool TryGetBuff(this Entity entity, PrefabGUID buffPrefabGUID, out Entity buffEntity)
    {
        if (ServerGameManager.TryGetBuff(entity, buffPrefabGUID.ToIdentifier(), out buffEntity))
        {
            return true;
        }

        return false;
    }
    public static unsafe bool TryGetBuffer<T>(this Entity entity, out DynamicBuffer<T> dynamicBuffer) where T : struct
    {
        if (ServerGameManager.TryGetBuffer(entity, out dynamicBuffer))
        {
            return true;
        }

        return false;
    }
    public static void Remove<T>(this Entity entity, EntityCommandBuffer entityCommandBuffer)
    {
        entityCommandBuffer.RemoveComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static float3 GetAimPosition(this Entity entity)
    {
        if (entity.TryGetComponent(out EntityInput entityInput))
        {
            return entityInput.AimPosition;
        }

        return float3.zero;
    }
    public static bool TryGetPosition(this Entity entity, out float3 position)
    {
        position = float3.zero;

        if (entity.TryGetComponent(out Translation translation))
        {
            position = translation.Value;

            return true;
        }

        return false;
    }
    public static float3 GetPosition(this Entity entity)
    {
        if (entity.TryGetComponent(out Translation translation))
        {
            return translation.Value;
        }

        return float3.zero;
    }
    public static bool TryGetMatch(this HashSet<(ulong, ulong)> hashSet, ulong value, out (ulong, ulong) matchingPair)
    {
        matchingPair = default;

        foreach (var pair in hashSet)
        {
            if (pair.Item1 == value || pair.Item2 == value)
            {
                matchingPair = pair;

                return true;
            }
        }

        return false;
    }
    public static bool TryGetMatchPairInfo(this (ulong, ulong) matchPair, out (PlayerInfo, PlayerInfo) matchPairInfo)
    {
        matchPairInfo = default;

        ulong playerOne = matchPair.Item1;
        ulong playerTwo = matchPair.Item2;

        if (playerOne.TryGetPlayerInfo(out PlayerInfo playerOneInfo) && playerTwo.TryGetPlayerInfo(out PlayerInfo playerTwoInfo))
        {
            matchPairInfo = (playerOneInfo, playerTwoInfo);

            return true;
        }

        return false;
    }
    public static void Destroy(this Entity entity)
    {
        if (entity.Exists()) DestroyUtility.Destroy(EntityManager, entity);
    }
}