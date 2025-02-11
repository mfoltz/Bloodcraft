using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Utilities.Misc;

namespace Bloodcraft.Services;
internal class PlayerService // this is basically a worse version of the PlayerService from KindredCommands, if you're here looking for good examples to follow :p
{
    static EntityManager EntityManager => Core.EntityManager;

    // static readonly bool _performance = ConfigService.PerformanceAuditing;
    // static readonly Regex _regex = new(@"^\d+");

    static readonly WaitForSeconds _delay = new(60f);

    static readonly ComponentType[] _userComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<User>()),
    ];

    static EntityQuery _userQuery;
    static EntityQuery _allUsersQuery;

    static bool _migratedPlayerBools = false;
    static bool _migratedFamiliarPrestige = false;
    static bool _playersCached = false;

    public static readonly ConcurrentDictionary<ulong, PlayerInfo> PlayerCache = [];
    public static readonly ConcurrentDictionary<ulong, PlayerInfo> OnlineCache = [];
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
    public PlayerService()
    {
        EntityQueryDesc entityQueryDesc = new()
        {
            All = _userComponent,
            Options = EntityQueryOptions.IncludeDisabled
        };

        _allUsersQuery = EntityManager.CreateEntityQuery(entityQueryDesc);

        entityQueryDesc = new()
        {
            All = _userComponent,
        };

        _userQuery = EntityManager.CreateEntityQuery(entityQueryDesc);

        PlayerServiceRoutine().Start();
    }
    static IEnumerator PlayerServiceRoutine()
    {
        while (true)
        {
            OnlineCache.Clear();
            var players = Queries.GetEntitiesEnumerable(_playersCached ? _userQuery : _allUsersQuery);

            players
                .Select(userEntity =>
                {
                    User user = userEntity.Read<User>();
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
                    PlayerCache[kvp.Key] = kvp.Value;

                    if (kvp.Value.User.IsConnected)
                    {
                        OnlineCache[kvp.Key] = kvp.Value;
                    }
                });

            // should probably organize the migrating stuff elsewhere, just convenient to put here for now since using PlayerInfo for it
            if (!_migratedPlayerBools && File.Exists(DataService.PlayerPersistence.JsonFilePaths.PlayerBoolsJson))
            {
                List<PlayerInfo> playerCache = new(PlayerCache.Values);

                foreach (PlayerInfo playerInfo in playerCache)
                {
                    ulong steamId = playerInfo.User.PlatformId;

                    if (PlayerBoolsManager.TryMigrateBools(steamId))
                    {
                        // Core.Log.LogInfo($"Migrated player bools for {playerInfo.User.CharacterName.Value}! (should only happen once per player)");
                    }
                    else
                    {
                        // Core.Log.LogInfo($"No bools to migrate for {playerInfo.User.CharacterName.Value}! (already migrated or didn't have bools data before now)");
                    }
                }

                if (!DataService.PlayerDictionaries._playerBools.Any())
                {
                    if (File.Exists(DataService.PlayerPersistence.JsonFilePaths.PlayerBoolsJson))
                    {
                        // File.Delete(DataService.PlayerPersistence.JsonFilePaths.PlayerBoolsJson);
                        // Core.Log.LogInfo($"No entries remaining in old bools file: {DataService.PlayerDictionaries._playerBools.Count}");
                    }
                }
                else
                {
                    // Core.Log.LogInfo($"Entries remaining in old bools file: {DataService.PlayerDictionaries._playerBools.Count}");

                    if (File.Exists(DataService.PlayerPersistence.JsonFilePaths.PlayerBoolsJson))
                    {
                        // File.Delete(DataService.PlayerPersistence.JsonFilePaths.PlayerBoolsJson);
                    }
                }

                _migratedPlayerBools = true;
            }
            
            if (!_migratedFamiliarPrestige)
            {
                List<PlayerInfo> playerCache = new(PlayerCache.Values);

                foreach (PlayerInfo playerInfo in playerCache)
                {
                    var oldPrestigeData = FamiliarPrestigeManager.LoadFamiliarPrestigeData(playerInfo.User.PlatformId);
                    if (oldPrestigeData != null) FamiliarPrestigeManager_V2.MigrateToV2(oldPrestigeData, playerInfo.User.PlatformId);
                }

                /*
                string path = ConfigService.ConfigInitialization.DirectoryPaths[7];
                foreach (string file in Directory.EnumerateFiles(path, "*.json"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    Match match = _regex.Match(fileName);

                    if (match.Success)
                    {
                        ulong steamId = ulong.Parse(match.Value);
                        var oldPrestigeData = FamiliarPrestigeManager.LoadFamiliarPrestigeData(steamId);

                        if (oldPrestigeData != null) FamiliarPrestigeManager_V2.MigrateToV2(oldPrestigeData, steamId);
                    }
                }
                */

                _migratedFamiliarPrestige = true;
            }

            // if (_performance) Core.LogPerformanceStats();
            if (!_playersCached) _playersCached = true;

            yield return _delay;
        }
    }
    public static PlayerInfo GetPlayerInfo(string playerName)
    {
        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Value.User.CharacterName.Value.ToLower() == playerName.ToLower()).Value;
        return playerInfo;
    }
}
