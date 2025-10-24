using System;
using System.Collections.Concurrent;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Services.LocalizationService;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft;
internal interface IEntityComponentOverrides
{
    bool TryRead<T>(Entity entity, out T value) where T : struct;

    bool TryHas(Entity entity, Type componentType, out bool has);
}

internal static class VExtensions
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly ConcurrentStack<IEntityComponentOverrides> ComponentOverrides = new();

    const string EMPTY_KEY = "LocalizationKey.Empty";
    const string PREFIX = "Entity(";
    const string CHAR = "CHAR_";

    const int LENGTH = 7;

    public delegate void WithRefHandler<T>(ref T item);
    public static void With<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        T item = entity.Read<T>();
        action(ref item);

        EntityManager.SetComponentData(entity, item);
    }
    public static void With<T>(this Entity entity, int index, WithRefHandler<T> action) where T : struct
    {
        if (!entity.TryGetBuffer<T>(out var buffer))
        {
            Core.Log.LogWarning($"Entity doesn't have DynamicBuffer<{typeof(T)}>!");
            return;
        }

        if (!buffer.IsIndexWithinRange(index))
        {
            Core.Log.LogWarning($"Index {index} out of range for DynamicBuffer<{typeof(T)}>! Length: {buffer.Length}");
            return;
        }

        var element = buffer[index];
        action(ref element);

        buffer[index] = element;
    }
    public static void AddWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        if (!entity.Has<T>())
        {
            entity.Add<T>();
        }

        entity.With(action);
    }
    public static void HasWith<T>(this Entity entity, WithRefHandler<T> action) where T : struct
    {
        if (entity.Has<T>())
        {
            entity.With(action);
        }
    }
    public unsafe static void Write<T>(this Entity entity, T componentData) where T : struct
    {
        EntityManager.SetComponentData(entity, componentData);
    }
    internal static IDisposable OverrideComponents(IEntityComponentOverrides overrides)
    {
        if (overrides is null)
        {
            throw new ArgumentNullException(nameof(overrides));
        }

        ComponentOverrides.Push(overrides);
        return new ComponentOverrideScope(overrides);
    }
    public static T Read<T>(this Entity entity) where T : struct
    {
        if (TryReadOverride(entity, out T value))
        {
            return value;
        }

        if (!ComponentOverrides.IsEmpty)
        {
            return default;
        }

        return EntityManager.GetComponentData<T>(entity);
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
    public static bool Has<T>(this Entity entity) where T : struct
    {
        if (TryHasOverride(entity, typeof(T), out bool has))
        {
            return has;
        }

        if (!ComponentOverrides.IsEmpty)
        {
            return false;
        }

        return EntityManager.HasComponent(entity, new(Il2CppType.Of<T>()));
    }
    static bool TryReadOverride<T>(Entity entity, out T value) where T : struct
    {
        foreach (IEntityComponentOverrides overrides in ComponentOverrides.ToArray())
        {
            if (overrides.TryRead(entity, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
    static bool TryHasOverride(Entity entity, Type componentType, out bool has)
    {
        foreach (IEntityComponentOverrides overrides in ComponentOverrides.ToArray())
        {
            if (overrides.TryHas(entity, componentType, out has))
            {
                return true;
            }
        }

        has = default;
        return false;
    }

    sealed class ComponentOverrideScope : IDisposable
    {
        readonly IEntityComponentOverrides overrides;
        bool disposed;

        public ComponentOverrideScope(IEntityComponentOverrides overrides)
        {
            this.overrides = overrides;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (!ComponentOverrides.TryPop(out IEntityComponentOverrides current) || !ReferenceEquals(current, overrides))
            {
                throw new InvalidOperationException("Component override stack is out of sync.");
            }
        }
    }
    public static bool IsCharacter(this PrefabGUID prefabGuid)
    {
        return prefabGuid.GetPrefabName().StartsWith(CHAR);
    }
    public static string GetPrefabName(this PrefabGUID prefabGuid, bool verbose = false)
    {
        if (PrefabGuidNames.TryGetValue(prefabGuid, out string prefabName))
            return verbose
                ? prefabName
                : $"{prefabName} {prefabGuid}";

        return EMPTY_KEY;
    }
    public static string GetSequenceName(this SequenceGUID sequenceGuid)
    {
        return SequenceGuidNames.TryGetValue(sequenceGuid, out string sequenceName) ? sequenceName: string.Empty;
    }
    public static string GetLocalizedName(this PrefabGUID prefabGuid)
    {
        string prefabName = GetNameFromPrefabGuid(prefabGuid);

        if (!string.IsNullOrEmpty(prefabName))
        {
            return prefabName;
        }

        if (PrefabGuidNames.TryGetValue(prefabGuid, out prefabName))
        {
            return prefabName;
        }

        return EMPTY_KEY;
    }
    public static void Add<T>(this Entity entity) where T : struct
    {
        if (!entity.Has<T>()) EntityManager.AddComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static void Remove<T>(this Entity entity) where T : struct
    {
        if (entity.Has<T>()) EntityManager.RemoveComponent(entity, new(Il2CppType.Of<T>()));
    }
    public static bool TryGetFollowedPlayer(this Entity entity, out Entity player)
    {
        player = Entity.Null;

        if (entity.TryGetComponent(out Follower follower))
        {
            if (follower.Followed._Value.TryGetPlayer(out player))
            {
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
        if (entity.Has<PlayerCharacter>())
        {
            return true;
        }

        return false;
    }
    public static bool IsFamiliar(this Entity entity)
    {
        if (entity.Has<BlockFeedBuff>())
        {
            return true;
        }

        return false;
    }
    public static bool IsFollowingPlayer(this Entity entity)
    {
        if (entity.Has<BlockFeedBuff>() && !entity.Has<Buff>() && !entity.Has<ServantEquipment>())
        {
            return true;
        }
        else if (entity.TryGetComponent(out Follower follower))
        {
            if (follower.Followed._Value.IsPlayer())
            {
                return true;
            }
        }

        return false;
    }
    public static bool TryGetAttached(this Entity entity, out Entity attached)
    {
        attached = Entity.Null;

        if (entity.TryGetComponent(out Attach attach) && attach.Parent.Exists())
        {
            attached = attach.Parent;
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
        // return ServerGameManager.GetPrefabEntity(entity.GetPrefabGuid());
        return entity.Exists() ? ServerGameManager.GetPrefabEntity(entity.GetPrefabGuid()) : Entity.Null;
    }
    public static Entity GetPrefabEntity(this PrefabGUID prefabGuid)
    {
        return prefabGuid.HasValue() ? ServerGameManager.GetPrefabEntity(prefabGuid) : Entity.Null;
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
        return entity.HasValue() && entity.IndexWithinCapacity() && EntityManager.Exists(entity);
    }
    public static bool HasValue(this Entity entity)
    {
        return entity != Entity.Null;
    }
    public static bool IndexWithinCapacity(this Entity entity)
    {
        string entityStr = entity.ToString();
        ReadOnlySpan<char> span = entityStr.AsSpan();

        if (!span.StartsWith(PREFIX)) return false;
        span = span[LENGTH..];

        int colon = span.IndexOf(':');
        if (colon <= 0) return false;

        ReadOnlySpan<char> tail = span[(colon + 1)..];

        int closeRel = tail.IndexOf(')');
        if (closeRel <= 0) return false;

        // Parse numbers
        if (!int.TryParse(span[..colon], out int index)) return false;
        if (!int.TryParse(tail[..closeRel], out _)) return false;

        // Single unsigned capacity check
        int capacity = EntityManager.EntityCapacity;
        bool isValid = (uint)index < (uint)capacity;

        if (!isValid)
        {
            // Core.Log.LogWarning($"Entity index out of range! ({index}>{capacity})");
        }

        return isValid;
    }
    public static bool IsDisabled(this Entity entity)
    {
        return entity.Has<Disabled>();
    }
    public static bool IsVBlood(this Entity entity)
    {
        return entity.Has<VBloodConsumeSource>();
    }
    public static bool IsGateBoss(this Entity entity)
    {
        return entity.Has<VBloodUnit>() && !entity.Has<VBloodConsumeSource>();
    }
    public static bool IsVBloodOrGateBoss(this Entity entity)
    {
        return entity.Has<VBloodUnit>();
    }
    public static bool IsLegendary(this Entity entity)
    {
        return entity.Has<LegendaryItemInstance>();
    }
    public static bool HasSpellLevel(this Entity entity)
    {
        return entity.Has<SpellLevel>();
    }
    public static bool IsMounter(this Entity entity)
    {
        return entity.Has<UnitMounter>();
    }
    public static bool IsAncestralWeapon(this Entity entity)
    {
        return entity.Has<LegendaryItemInstance>() && !entity.IsMagicSource();
    }
    public static bool IsShardNecklace(this Entity entity)
    {
        return entity.Has<LegendaryItemInstance>() && entity.IsMagicSource();
    }
    public static bool IsMagicSource(this Entity entity)
    {
        return entity.TryGetComponent(out EquippableData equippableData) && equippableData.EquipmentType.Equals(EquipmentType.MagicSource);
    }
    public static ulong GetSteamId(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter))
        {
            return playerCharacter.UserEntity.GetUser().PlatformId;
        }
        else if (entity.TryGetComponent(out User user))
        {
            return user.PlatformId;
        }

        return 0;
    }
    public static NetworkId GetNetworkId(this Entity entity)
    {
        if (entity.TryGetComponent(out NetworkId networkId))
        {
            return networkId;
        }

        return NetworkId.Empty;
    }
    public static bool TryGetPlayerInfo(this ulong steamId, out PlayerInfo playerInfo)
    {
        if (SteamIdPlayerInfoCache.TryGetValue(steamId, out playerInfo)) return true;
        else if (SteamIdOnlinePlayerInfoCache.TryGetValue(steamId, out playerInfo)) return true;

        return false;
    }
    public static PrefabGUID GetPrefabGuid(this Entity entity)
    {
        if (entity.TryGetComponent(out PrefabGUID prefabGuid)) return prefabGuid;

        return PrefabGUID.Empty;
    }
    public static int GetGuidHash(this Entity entity)
    {
        if (entity.TryGetComponent(out PrefabGUID prefabGUID)) return prefabGUID.GuidHash;

        return PrefabGUID.Empty.GuidHash;
    }
    public static Entity GetUserEntity(this Entity entity)
    {
        if (entity.TryGetComponent(out PlayerCharacter playerCharacter)) return playerCharacter.UserEntity;
        else if (entity.Has<User>()) return entity;

        return Entity.Null;
    }
    public static Entity GetOwner(this Entity entity, bool trueOwner = false)
    {
        if (!entity.Exists())
            return Entity.Null;
        else if (trueOwner && VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, EntityManager, out Entity result))
            return result;
        else
            return ServerGameManager.GetOwner(entity);
    }
    public static User GetUser(this Entity entity)
    {
        if (entity.TryGetComponent(out User user)) return user;
        else if (entity.TryGetComponent(out PlayerCharacter playerCharacter) && playerCharacter.UserEntity.TryGetComponent(out user)) return user;

        return User.Empty;
    }
    public static bool HasBuff(this Entity entity, PrefabGUID buffPrefabGuid)
    {
        return ServerGameManager.HasBuff(entity, buffPrefabGuid.ToIdentifier());
    }
    public static bool HasBuff<T>(this Entity entity)
    {
        return BuffUtility.HasBuff<T>(EntityManager, entity);
    }
    public static unsafe bool TryGetBuffer<T>(this Entity entity, out DynamicBuffer<T> dynamicBuffer) where T : struct
    {
        if (ServerGameManager.TryGetBuffer(entity, out dynamicBuffer))
        {
            return true;
        }

        dynamicBuffer = default;
        return false;
    }
    public static float3 GetAimPosition(this Entity entity)
    {
        if (entity.TryGetComponent(out EntityInput entityInput))
        {
            return entityInput.AimPosition;
        }

        return float3.zero;
    }
    public static float3 GetPosition(this Entity entity)
    {
        if (entity.TryGetComponent(out Translation translation))
        {
            return translation.Value;
        }

        return float3.zero;
    }
    public static int2 GetTileCoord(this Entity entity)
    {
        if (entity.TryGetComponent(out TilePosition tilePosition))
        {
            return tilePosition.Tile;
        }

        return int2.zero;
    }
    public static int GetUnitLevel(this Entity entity)
    {
        if (entity.TryGetComponent(out UnitLevel unitLevel))
        {
            return unitLevel.Level._Value;
        }

        return 0;
    }
    public static float GetMaxDurability(this Entity entity)
    {
        if (entity.TryGetComponent(out Durability durability))
        {
            return durability.MaxDurability;
        }

        return 0;
    }
    public static float GetDurability(this Entity entity)
    {
        if (entity.TryGetComponent(out Durability durability))
        {
            return durability.Value;
        }

        return 0;
    }
    public static float GetMaxHealth(this Entity entity)
    {
        if (entity.TryGetComponent(out Health health))
        {
            return health.MaxHealth._Value;
        }

        return 0;
    }
    public static Blood GetBlood(this Entity entity)
    {
        if (entity.TryGetComponent(out Blood blood))
        {
            return blood;
        }

        throw new InvalidOperationException("Entity does not have Blood!");
    }
    public static AiMoveSpeeds GetMoveSpeeds(this Entity entity)
    {
        if (entity.TryGetComponent(out AiMoveSpeeds aiMoveSpeeds))
        {
            return aiMoveSpeeds;
        }

        throw new InvalidOperationException("Entity does not have Blood!");
    }
    public static EntityInput GetInput(this Entity entity)
    {
        return ServerGameManager.GetInput(entity);
    }
    public static (float physicalPower, float spellPower) GetPowerTuple(this Entity entity)
    {
        if (entity.TryGetComponent(out UnitStats unitStats))
        {
            return (unitStats.PhysicalPower._Value, unitStats.SpellPower._Value);
        }

        return (0f, 0f);
    }
    public static bool IsUnitSpawnerSpawned(this Entity entity) // only works paired with UnitSpawnerSystem patch which sets IsMinion to true in prefix
    {
        if (entity.TryGetComponent(out IsMinion isMinion) && isMinion.Value)
        {
            return true;
        }

        return false;
    }
    public static bool IsStackableBuff(this Entity entity, out int maxStacks)
    {
        maxStacks = 1;

        if (entity.TryGetComponent(out Buff buff))
        {
            maxStacks = buff.MaxStacks;
            return buff.IncreaseStacks;
        }

        return false;
    }
    public static Entity Create(this ComponentType[] components)
    {
        return EntityManager.CreateEntity(components);
    }
    public static void Destroy(this Entity entity, bool immediate = false)
    {
        if (!entity.Exists()) return;

        bool isBuff = entity.IsBuff(); // should probably check if this actually matters or not but like... later >_>

        if (immediate && !isBuff)
            EntityManager.DestroyEntity(entity);
        else if (isBuff)
            DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.TryRemoveBuff);
        else
            DestroyUtility.Destroy(EntityManager, entity);
    }
    public static bool IsBuff(this Entity entity)
    {
        return entity.Has<Buff>();
    }
    public static void SetTeam(this Entity entity, Entity teamSource)
    {
        if (entity.Has<Team>() && entity.Has<TeamReference>() && teamSource.TryGetComponent(out Team sourceTeam) && teamSource.TryGetComponent(out TeamReference sourceTeamReference))
        {
            Entity teamRefEntity = sourceTeamReference.Value._Value;
            int teamId = sourceTeam.Value;

            entity.With((ref TeamReference teamReference) => teamReference.Value._Value = teamRefEntity);

            entity.With((ref Team team) => team.Value = teamId);
        }
    }
    public static void SetPosition(this Entity entity, float3 position)
    {
        if (entity.Has<Translation>())
        {
            entity.With((ref Translation translation) => translation.Value = position);
        }

        if (entity.Has<LastTranslation>())
        {
            entity.With((ref LastTranslation lastTranslation) => lastTranslation.Value = position);
        }
    }
    public static void SetFaction(this Entity entity, PrefabGUID factionPrefabGuid)
    {
        if (entity.Has<FactionReference>())
        {
            entity.With((ref FactionReference factionReference) => factionReference.FactionGuid._Value = factionPrefabGuid);
        }
    }
    public static bool IsAllied(this Entity entity, Entity player)
    {
        return ServerGameManager.IsAllies(entity, player);
    }
    public static bool IsDreadful(this Entity entity)
    {
        return entity.GetPrefabGuid().Equals(PrefabGUIDs.CHAR_Legion_DreadHorn_Lesser,
            PrefabGUIDs.CHAR_Legion_Dreadhorn);
    }
    public static bool IsPlayerOwned(this Entity entity)
    {
        if (entity.TryGetComponent(out EntityOwner entityOwner))
        {
            return entityOwner.Owner.IsPlayer();
        }

        return false;
    }
    public static void CastAbility(this Entity entity, PrefabGUID abilityGroup)
    {
        bool isPlayer = entity.IsPlayer();

        CastAbilityServerDebugEvent castAbilityServerDebugEvent = new()
        {
            AbilityGroup = abilityGroup,
            Who = entity.GetNetworkId()
        };

        FromCharacter fromCharacter = new()
        {
            Character = entity,
            User = isPlayer ? entity.GetUserEntity() : entity
        };

        int userIndex = isPlayer ? entity.GetUser().Index : 0;
        DebugEventsSystem.CastAbilityServerDebugEvent(userIndex, ref castAbilityServerDebugEvent, ref fromCharacter);
    }
    public static bool IsIndexWithinRange<T>(this DynamicBuffer<T> buffer, int index) where T : struct
    {
        return index >= 0 && index < buffer.Length;
    }
    public static NativeAccessor<Entity> ToEntityArrayAccessor(this EntityQuery entityQuery, Allocator allocator = Allocator.Temp)
    {
        NativeArray<Entity> entities = entityQuery.ToEntityArray(allocator);
        return new(entities);
    }
    public static NativeAccessor<T> ToComponentDataArrayAccessor<T>(this EntityQuery entityQuery, Allocator allocator = Allocator.Temp) where T : unmanaged
    {
        NativeArray<T> components = entityQuery.ToComponentDataArray<T>(allocator);
        return new(components);
    }
}