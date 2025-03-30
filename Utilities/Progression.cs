using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Progression.PlayerProgressionCacheManager;
using User = ProjectM.Network.User;

namespace Bloodcraft.Utilities;
internal static class Progression
{
    static SystemService SystemService => Core.SystemService;
    static UserActivityGridSystem UserActivityGridSystem => SystemService.UserActivityGridSystem;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _isPvE = _gameMode.Equals(GameModeType.PvE);

    static readonly bool _expShare = ConfigService.ExpShare;
    static readonly int _shareLevelRange = ConfigService.ExpShareLevelRange;
    static readonly float _shareDistance = ConfigService.ExpShareDistance;

    static readonly PrefabGUID _pveCombatBuff = new(581443919);
    static readonly PrefabGUID _draculaVBlood = new(-327335305);

    const float EXP_CONSTANT = 0.1f;
    const float EXP_POWER = 2f;
    public static int ConvertXpToLevel(float xp)
    {
        return (int)(EXP_CONSTANT * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / EXP_CONSTANT, EXP_POWER);
    }
    public class PlayerProgressionCacheManager
    {
        public class PlayerProgressionData(int level, bool hasPrestiged)
        {
            public int Level { get; set; } = level;
            public bool HasPrestiged { get; set; } = hasPrestiged;
        }
        public static IReadOnlyList<ulong> IgnoreShared => DataService.PlayerDictionaries._ignoreSharedExperience;

        static readonly ConcurrentDictionary<ulong, PlayerProgressionData> _playerProgressionCache = [];
        public static IReadOnlyDictionary<ulong, PlayerProgressionData> PlayerProgressionCache => _playerProgressionCache;
        public static void UpdatePlayerProgression(ulong steamId, int level, bool hasPrestiged)
        {
            if (_playerProgressionCache.ContainsKey(steamId))
            {
                _playerProgressionCache[steamId] = new PlayerProgressionData(level, hasPrestiged);
            }
            else
            {
                _playerProgressionCache.TryAdd(steamId, new PlayerProgressionData(level, hasPrestiged));
            }
        }
        public static void UpdatePlayerProgressionLevel(ulong steamId, int level)
        {
            if (_playerProgressionCache.TryGetValue(steamId, out PlayerProgressionData playerProgressionData))
            {
                playerProgressionData.Level = level;
            }
            else
            {
                UpdatePlayerProgression(steamId, level, false);
            }
        }
        public static void UpdatePlayerProgressionPrestige(ulong steamId, bool hasPrestiged)
        {
            if (_playerProgressionCache.TryGetValue(steamId, out PlayerProgressionData playerProgressionData))
            {
                playerProgressionData.HasPrestiged = hasPrestiged;
            }
            else
            {
                UpdatePlayerProgression(steamId, 1, hasPrestiged);
            }
        }
        public static PlayerProgressionData GetProgressionCacheData(ulong steamId)
        {
            if (_playerProgressionCache.TryGetValue(steamId, out var data))
                return data;

            int level = LevelingSystem.GetLevel(steamId);
            bool hasPrestiged = steamId.TryGetPlayerPrestiges(out var prestiges)
                                && prestiges.TryGetValue(PrestigeType.Experience, out int prestigeCount)
                                && prestigeCount >= 1;

            data = new PlayerProgressionData(level, hasPrestiged);
            _playerProgressionCache[steamId] = data;
            return data;
        }
    }
    public static HashSet<Entity> GetDeathParticipantsV2(Entity source)
    {
        float3 position = source.GetPosition();
        User sourceUser = source.GetUser();

        var sourceProgression = GetProgressionCacheData(sourceUser.PlatformId);
        int sourceLevel = sourceProgression.Level;

        HashSet<Entity> players = [source];

        if (!_expShare)
        {
            return players;
        }

        try
        {
            UserActivityGrid userActivityGrid = UserActivityGridSystem.GetUserActivityGrid();
            UserBitMask128 userBitMask = userActivityGrid.GetUsersInRadius(position, _shareDistance);
            UserBitMask128.Enumerable usersInRange = userBitMask.GetUsers();

            foreach (int userIndex in usersInRange)
            {
                ulong? steamId = GetSteamId(userIndex);

                if (!steamId.HasValue || IgnoreShared.Contains(steamId.Value)) continue;
                if (!steamId.Value.TryGetPlayerInfo(out PlayerInfo playerInfo)) continue;
                if (!playerInfo.CharEntity.HasBuff(_pveCombatBuff)) continue;

                var targetProgression = GetProgressionCacheData(steamId.Value);

                if (_isPvE)
                {
                    if (targetProgression.HasPrestiged || _shareLevelRange.Equals(0) || source.IsAllies(playerInfo.CharEntity))
                    {
                        players.Add(playerInfo.CharEntity);
                    }
                    else if (Math.Abs(sourceLevel - targetProgression.Level) <= _shareLevelRange)
                    {
                        players.Add(playerInfo.CharEntity);
                    }
                }
                else if (source.IsAllies(playerInfo.CharEntity))
                {
                    players.Add(playerInfo.CharEntity);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"Error getting users in range from activity grid: {e}");
        }

        return players;
    }
    public static List<PlayerInfo> GetUsersNearPosition(float3 position, float radius)
    {
        List<PlayerInfo> playerInfos = [];

        try
        {
            UserActivityGrid userActivityGrid = UserActivityGridSystem.GetUserActivityGrid();
            UserBitMask128 userBitMask = userActivityGrid.GetUsersInRadius(position, radius);
            UserBitMask128.Enumerable usersInRange = userBitMask.GetUsers();

            foreach (int userIndex in usersInRange)
            {
                ulong? steamId = GetSteamId(userIndex);

                if (!steamId.HasValue) continue;
                if (!steamId.Value.TryGetPlayerInfo(out PlayerInfo playerInfo)) continue;
                
                playerInfos.Add(playerInfo);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"Error getting users in range from activity grid: {e}");
        }

        return playerInfos;
    }
    public static HashSet<Entity> GetDeathParticipants(Entity source)
    {
        float3 sourcePosition = source.GetPosition();
        User sourceUser = source.GetUser();

        // string playerName = sourceUser.CharacterName.Value;
        int playerLevel = LevelingSystem.GetLevel(sourceUser.PlatformId);
        HashSet<Entity> players = [source];

        try
        {
            UserActivityGrid userActivityGrid = UserActivityGridSystem.GetUserActivityGrid();
            UserBitMask128 userBitMask = userActivityGrid.GetUsersInRadius(sourcePosition, _shareDistance);
            UserBitMask128.Enumerable usersInRange = userBitMask.GetUsers();

            Core.Log.LogInfo($"Users in range of deathEvent - {usersInRange._Mask.Count}");

            foreach (int userIndex in usersInRange)
            {
                ulong? steamId = GetSteamId(userIndex);

                if (steamId.HasValue && steamId.Value.TryGetPlayerInfo(out PlayerInfo playerInfo))
                {
                    int targetLevel = LevelingSystem.GetLevel(steamId.Value);

                    if (!playerInfo.CharEntity.HasBuff(_pveCombatBuff)) continue;
                    else if (steamId.Value.TryGetPlayerPrestiges(out var playerPrestiges)
                        && playerPrestiges.TryGetValue(PrestigeType.Experience, out int prestiges)
                        && prestiges >= 1) players.Add(playerInfo.CharEntity);
                    else if (Math.Abs(playerLevel - targetLevel) > _shareLevelRange) continue;
                    else players.Add(playerInfo.CharEntity);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"Error getting users in range from activity grid: {e}");
        }

        /*
        Entity clanEntity = sourceUser.ClanEntity.GetEntityOnServer();

        if (_parties)
        {
            List<List<string>> playerParties = [..DataService.PlayerDictionaries._playerParties.Values.Select(party => party.ToList())];

            foreach (List<string> party in playerParties)
            {
                if (party.Contains(playerName)) // find party with death source player name
                {
                    foreach (string partyMember in party)
                    {
                        PlayerInfo playerInfo = GetPlayerInfo(partyMember);

                        if (playerInfo.User.IsConnected && playerInfo.CharEntity.TryGetPosition(out float3 targetPosition))
                        {
                            float distance = UnityEngine.Vector3.Distance(sourcePosition, targetPosition);

                            if (distance > _shareDistance) continue;
                            else players.Add(playerInfo.CharEntity);
                        }
                    }

                    break; // break to avoid cases where there might be more than one party with same character name to account for checks that would prevent that happening failing
                }
            }
        }

        if (!clanEntity.Exists()) return players;
        else if (ServerGameManager.TryGetBuffer<SyncToUserBuffer>(clanEntity, out var clanUserBuffer) && !clanUserBuffer.IsEmpty)
        {
            foreach (SyncToUserBuffer clanUser in clanUserBuffer)
            {
                if (clanUser.UserEntity.TryGetComponent(out User user))
                {
                    Entity player = user.LocalCharacter.GetEntityOnServer();

                    if (user.IsConnected && player.TryGetPosition(out float3 targetPosition))
                    {
                        float distance = UnityEngine.Vector3.Distance(sourcePosition, targetPosition);

                        if (distance > _shareDistance) continue;
                        else players.Add(player);
                    }
                }
            }
        }
        */

        return players;
    }
    public static bool ConsumedDracula(Entity userEntity)
    {
        if (userEntity.TryGetComponent(out ProgressionMapper progressionMapper))
        {
            Entity progressionEntity = progressionMapper.ProgressionEntity.GetEntityOnServer();

            if (progressionEntity.TryGetBuffer<UnlockedVBlood>(out var buffer))
            {
                foreach (UnlockedVBlood unlockedVBlood in buffer)
                {
                    if (unlockedVBlood.VBlood.Equals(_draculaVBlood))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    const int LEVEL_FACTOR = 2;
    public static int GetSimulatedLevel(Entity userEntity)
    {
        if (userEntity.TryGetComponent(out ProgressionMapper progressionMapper))
        {
            Entity progressionEntity = progressionMapper.ProgressionEntity.GetEntityOnServer();

            if (progressionEntity.TryGetBuffer<UnlockedVBlood>(out var buffer))
            {
                return buffer.Length * LEVEL_FACTOR;
            }
        }

        return 0;
    }
}
