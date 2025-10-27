using Bloodcraft.Interfaces;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using System;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.EntityQueries;
using PrestigeManager = Bloodcraft.Systems.Leveling.PrestigeManager;

namespace Bloodcraft.Services;
internal class PlayerService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;

    static readonly ComponentType[] _userAllComponents = InitializeUserComponents();

    static QueryDesc _userQueryDesc;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdPlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdPlayerInfoCache => _steamIdPlayerInfoCache;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdOnlinePlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdOnlinePlayerInfoCache => _steamIdOnlinePlayerInfoCache;
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    public PlayerService()
    {
        _userQueryDesc = EntityManager.CreateQueryDesc(_userAllComponents, options: EntityQueryOptions.IncludeDisabled);
        BuildPlayerInfoCache();
    }
    static ComponentType[] InitializeUserComponents()
    {
        try
        {
            return
            [
                ComponentType.ReadOnly(Il2CppType.Of<User>())
            ];
        }
        catch (Exception ex) when (IsMissingNativeLibrary(ex))
        {
            return Array.Empty<ComponentType>();
        }
    }
    static bool IsMissingNativeLibrary(Exception exception)
    {
        for (Exception current = exception; ; )
        {
            if (current is DllNotFoundException)
            {
                return true;
            }

            if (current.InnerException is not Exception next)
            {
                return false;
            }

            current = next;
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
                }

                if (_leveling)
                {
                    int level = LevelingSystem.GetLevel(steamId);
                    bool hasPrestiged = PrestigeManager.HasPrestiged(steamId);
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
    }
    public static void HandleConnection(ulong steamId, PlayerInfo playerInfo)
    {
        _steamIdOnlinePlayerInfoCache[steamId] = playerInfo;
        _steamIdPlayerInfoCache[steamId] = playerInfo;
    }
    public static void HandleDisconnection(ulong steamId)
    {
        _steamIdOnlinePlayerInfoCache.TryRemove(steamId, out _);

        EclipseService.TryRemovePreRegistration(steamId);
        EclipseService.TryUnregisterUser(steamId);
    }
    public static PlayerInfo GetPlayerInfo(string playerName)
    {
        return SteamIdPlayerInfoCache.FirstOrDefault(kvp => string.Equals(kvp.Value.User.CharacterName.Value,
            playerName, StringComparison.CurrentCultureIgnoreCase)).Value;
    }
}
