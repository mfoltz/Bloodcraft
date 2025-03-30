using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Utilities.EntityQueries;
using static Bloodcraft.Utilities.Familiars;

namespace Bloodcraft.Services;
internal class PlayerService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _leveling = ConfigService.LevelingSystem;

    const float START_DELAY = 30f;
    const float ROUTINE_DELAY = 60f;

    static readonly WaitForSeconds _startDelay = new(START_DELAY);
    static readonly WaitForSeconds _delay = new(ROUTINE_DELAY);

    static readonly ComponentType[] _userComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<User>())
    ];

    static readonly ComponentType[] _familiarComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
        ComponentType.ReadOnly(Il2CppType.Of<TeamReference>()),
        ComponentType.ReadOnly(Il2CppType.Of<BlockFeedBuff>())
    ];

    static EntityQuery _onlineUserQuery;
    static EntityQuery _userQuery;
    // static EntityQuery _familiarQuery;

    static bool _shouldMigrate = true;
    static bool _shouldDestroy = true;
    static bool _rebuildCache = true;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdPlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdPlayerInfoCache => _steamIdPlayerInfoCache;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdOnlinePlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdOnlinePlayerInfoCache => _steamIdOnlinePlayerInfoCache;

    static readonly ConcurrentDictionary<int, ulong> _userIndexSteamIdCache = [];
    public static IReadOnlyDictionary<int, ulong> UserIndexSteamIdCache => _userIndexSteamIdCache;
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    public PlayerService()
    {
        _userQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _userComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        _onlineUserQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _userComponent,
        });

        /*
        _familiarQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _familiarComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });
        */

        PlayerServiceRoutine().Start();
    }

    static readonly int[] _typeIndices = [0];
    static IEnumerator PlayerServiceRoutine()
    {
        // yield return _startDelay;

        if (_rebuildCache) BuildPlayerInfoCache();
        if (_shouldDestroy) DestroyFamiliars();
        if (_shouldMigrate) MigrateFamiliarPrestigeData();

        while (true)
        {
            _steamIdOnlinePlayerInfoCache.Clear();
            _userIndexSteamIdCache.Clear();

            yield return QueryResultStreamAsync(
                _onlineUserQuery,
                _userComponent,
                _typeIndices,
                stream =>
                {
                    try
                    {
                        using (stream)
                        {
                            foreach (QueryResult result in stream.GetResults())
                            {
                                Entity userEntity = result.Entity;
                                User user = result.ResolveComponentData<User>();
                                Entity playerCharacter = user.LocalCharacter.GetEntityOnServer();

                                ulong steamId = user.PlatformId;
                                PlayerInfo playerInfo = new(userEntity, playerCharacter, user);

                                _steamIdPlayerInfoCache[steamId] = playerInfo;

                                if (user.IsConnected)
                                {
                                    _steamIdOnlinePlayerInfoCache[steamId] = playerInfo;
                                    _userIndexSteamIdCache[user.Index] = steamId;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Log.LogWarning($"[PlayerService] PlayerServiceRoutine() - {ex}");
                    }
                }
            );

            yield return _delay;
        }
    }
    static void BuildPlayerInfoCache()
    {
        NativeArray<Entity> userEntities = _userQuery.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity userEntity in userEntities)
            {
                if (!userEntity.Exists()) continue;

                User user = userEntity.GetUser();
                Entity character = user.LocalCharacter.GetEntityOnServer();

                ulong steamId = user.PlatformId;
                string playerName = user.CharacterName.Value;

                _steamIdPlayerInfoCache[steamId] = new PlayerInfo(userEntity, character, user);

                if (user.IsConnected)
                {
                    _steamIdOnlinePlayerInfoCache[steamId] = new PlayerInfo(userEntity, character, user);
                    _userIndexSteamIdCache[user.Index] = steamId;
                }

                if (_leveling)
                {
                    int level = LevelingSystem.GetLevel(steamId);
                    bool hasPrestiged = PrestigeManager.HasPrestiged(steamId);
                    Progression.PlayerProgressionCacheManager.UpdatePlayerProgression(steamId, level, hasPrestiged);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[PlayerService] BuildPlayerInfoCache() - {ex}");
        }
        finally
        {
            userEntities.Dispose();
        }

        _rebuildCache = false;
    }
    static void DestroyFamiliars()
    {
        try
        {
            foreach (var keyValuePair in SteamIdPlayerInfoCache)
            {
                ulong steamId = keyValuePair.Key;
                Entity playerCharacter = keyValuePair.Value.CharEntity;

                ActiveFamiliarData activeFamiliarData = ActiveFamiliarManager.GetActiveFamiliarData(steamId);

                Entity servant = activeFamiliarData.Servant;
                Entity familiar = activeFamiliarData.Familiar;

                if (servant.Exists())
                {
                    FamiliarBindingSystem.RemoveDropTable(servant);
                    StatChangeUtility.KillOrDestroyEntity(EntityManager, servant, playerCharacter, playerCharacter, Core.ServerTime, StatChangeReason.Default, true);
                }

                if (familiar.Exists())
                {
                    FamiliarBindingSystem.RemoveDropTable(familiar);
                    StatChangeUtility.KillOrDestroyEntity(EntityManager, familiar, playerCharacter, playerCharacter, Core.ServerTime, StatChangeReason.Default, true);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[PlayerService] DestroyFamiliars() - {ex}");
        }

        _shouldDestroy = false;
    }
    static void MigrateFamiliarPrestigeData()
    {
        foreach (var keyValuePair in SteamIdPlayerInfoCache)
        {
            ulong steamId = keyValuePair.Key;

            var oldPrestigeData = FamiliarPrestigeManager.LoadFamiliarPrestigeData(steamId);
            if (oldPrestigeData != null) FamiliarPrestigeManager_V2.MigrateToV2(oldPrestigeData, steamId);
        }

        _shouldMigrate = false;
    }
    public static void HandleConnection(ulong steamId, PlayerInfo playerInfo)
    {
        _steamIdOnlinePlayerInfoCache.TryAdd(steamId, playerInfo);
        _steamIdPlayerInfoCache.TryAdd(steamId, playerInfo);
    }
    public static void HandleDisconnection(ulong steamId)
    {
        _steamIdOnlinePlayerInfoCache.TryRemove(steamId, out _);
    }
    public static PlayerInfo GetPlayerInfo(string playerName)
    {
        return SteamIdPlayerInfoCache.FirstOrDefault(kvp => kvp.Value.User.CharacterName.Value.ToLower() == playerName.ToLower()).Value;
    }
    public static ulong? GetSteamId(int userIndex)
    {
        return UserIndexSteamIdCache.TryGetValue(userIndex, out ulong steamId) ? steamId : null;
    }
}
