using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Services;
internal class PlayerService // this is basically a worse version of the PlayerService from KindredCommands, if you're here looking for good examples to follow :p
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds _delay = new(60);

    static readonly ComponentType[] _userComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<User>()),
    ];

    static EntityQuery _userQuery;

    public static readonly Dictionary<ulong, PlayerInfo> PlayerCache = [];
    public static readonly Dictionary<ulong, PlayerInfo> OnlineCache = [];
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    public PlayerService()
    {
        _userQuery = EntityManager.CreateEntityQuery(_userComponent);
        Core.StartCoroutine(PlayerCacheRoutine());
    }
    static IEnumerator PlayerCacheRoutine()
    {
        while (true)
        {
            PlayerCache.Clear();
            OnlineCache.Clear();

            var players = Queries.GetEntitiesEnumerable(_userQuery);

            players
                .Select(userEntity =>
                {
                    User user = userEntity.ReadRO<User>();
                    string playerName = user.CharacterName.Value;
                    ulong steamId = user.PlatformId;
                    Entity character = user.LocalCharacter.GetEntityOnServer();

                    return new
                    {
                        SteamIdEntry = new KeyValuePair<ulong, PlayerInfo>(
                            steamId, new PlayerInfo(userEntity, character, user))
                    };
                })
                .SelectMany(entry => new[] { entry.SteamIdEntry })
                .GroupBy(entry => entry.Key)
                .ToDictionary(group => group.Key, group => group.First().Value)
                .ForEach(kvp =>
                {
                    PlayerCache[kvp.Key] = kvp.Value; // Add to PlayerCache

                    if (kvp.Value.User.IsConnected) // Add to OnlinePlayerCache if connected
                    {
                        OnlineCache[kvp.Key] = kvp.Value;
                    }
                });

            yield return _delay;
        }
    }
    public static PlayerInfo GetPlayerInfo(string playerName)
    {
        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Value.User.CharacterName.Value.ToLower() == playerName.ToLower()).Value;
        return playerInfo;
    }
}
