using Bloodcraft.Interfaces;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Utilities.EntityQueries;
using PrestigeManager = Bloodcraft.Systems.Leveling.PrestigeManager;

namespace Bloodcraft.Services;
internal class PlayerService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _eclipse = ConfigService.Eclipse;

    // const float START_DELAY = 30f;
    const float ROUTINE_DELAY = 60f;

    // static readonly WaitForSeconds _startDelay = new(START_DELAY);
    static readonly WaitForSeconds _delay = new(ROUTINE_DELAY);

    static readonly ComponentType[] _userAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<User>())
    ];

    static QueryDesc _onlineUserQueryDesc;
    static QueryDesc _userQueryDesc;

    static bool _rebuildCache = true;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdPlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdPlayerInfoCache => _steamIdPlayerInfoCache;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdOnlinePlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdOnlinePlayerInfoCache => _steamIdOnlinePlayerInfoCache;

    static readonly ConcurrentDictionary<int, ulong> _userIndexSteamIdCache = [];
    static IReadOnlyDictionary<int, ulong> UserIndexSteamIdCache => _userIndexSteamIdCache;
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    public PlayerService()
    {
        _userQueryDesc = EntityManager.CreateQueryDesc(_userAllComponents, options: EntityQueryOptions.IncludeDisabled);
        _onlineUserQueryDesc = EntityManager.CreateQueryDesc(_userAllComponents);

        // PlayerServiceRoutine().Start();
        BuildPlayerInfoCache();
    }

    static readonly int[] _typeIndices = [0];
    static IEnumerator PlayerServiceRoutine()
    {
        if (_rebuildCache) BuildPlayerInfoCache();

        while (true)
        {
            _steamIdOnlinePlayerInfoCache.Clear();
            _userIndexSteamIdCache.Clear();

            yield return QueryResultStreamAsync(
                _onlineUserQueryDesc,
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
        NativeArray<Entity> userEntities = _userQueryDesc.EntityQuery.ToEntityArray(Allocator.Temp);

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

                    // Core.Log.LogWarning($"[PlayerService.BuildPlayerInfoCache] {steamId} - {playerName} - Level: {level} - HasPrestiged: {hasPrestiged}");
                    Progression.PlayerProgressionCacheManager.UpdatePlayerProgression(steamId, level, hasPrestiged);
                }

                if (_exoForm)
                {
                    bool hasExoPrestiged = PrestigeManager.HasExoPrestiged(steamId);

                    if (hasExoPrestiged && steamId.TryGetPlayerShapeshift(out ShapeshiftType shapeshift))
                    {
                        Shapeshifts.ShapeshiftCache.SetShapeshiftBuff(steamId, shapeshift);
                    }
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
    public static void HandleConnection(ulong steamId, PlayerInfo playerInfo)
    {
        // Core.Log.LogWarning($"[PlayerService.HandleConnection] {steamId}");

        _steamIdOnlinePlayerInfoCache[steamId] = playerInfo;
        _steamIdPlayerInfoCache[steamId] = playerInfo;
        _userIndexSteamIdCache[playerInfo.User.Index] = steamId;

        // if (_eclipse && EclipseService.PendingRegistration.TryGetValue(steamId, out string version)) EclipseService.HandleRegistration(playerInfo, steamId, version);
    }
    public static void HandleDisconnection(ulong steamId, int userIndex)
    {
        // Core.Log.LogWarning($"[PlayerService.HandleDisconnection] {steamId}");

        _steamIdOnlinePlayerInfoCache.TryRemove(steamId, out _);
        _userIndexSteamIdCache.TryRemove(userIndex, out _);

        if (_eclipse) EclipseService.TryRemovePreRegistration(steamId);
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
