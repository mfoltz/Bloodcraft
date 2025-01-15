using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Expertise.WeaponManager;
using User = ProjectM.Network.User;

namespace Bloodcraft.Utilities;
internal static class Misc
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _parties = ConfigService.PlayerParties;

    static readonly float _shareDistance = ConfigService.ExpShareDistance;

    static readonly PrefabGUID _draculaVBlood = new(-327335305);

    [Serializable]
    public class ConcurrentList<T> : IEnumerable<T>
    {
        [NonSerialized]
        readonly object _lock = new();

        readonly List<T> _list = [];
        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }
        public void Remove(T item)
        {
            lock (_lock)
            {
                _list.Remove(item);
            }
        }
        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _list.Contains(item);
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock)
            {
                return _list.ToList().GetEnumerator();
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool Any(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                return _list.Any(predicate);
            }
        }
        public List<T> ToList()
        {
            lock (_lock)
            {
                return new List<T>(_list);
            }
        }
        public int Count()
        {
            lock (_lock)
            {
                return _list.Count;
            }
        }
    }
    public static class PlayerBoolsManager
    {
        public static readonly Dictionary<string, bool> DefaultBools = new()
        {
            ["ExperienceLogging"] = false,
            ["QuestLogging"] = true,
            ["ProfessionLogging"] = false,
            ["ExpertiseLogging"] = false,
            ["BloodLogging"] = false,
            ["FamiliarLogging"] = false,
            ["SpellLock"] = false,
            ["ShiftLock"] = false,
            ["Grouping"] = false,
            ["Emotes"] = false,
            // ["Binding"] = false, using dictionary in patch with paired ulong and prefabGUID then removing after matching probably makes more sense than using bool here
            ["Kit"] = false,
            ["VBloodEmotes"] = true,
            ["FamiliarVisual"] = true,
            ["ShinyChoice"] = false,
            ["Reminders"] = true,
            ["ScrollingText"] = true,
            ["ExoForm"] = false,
            ["Shroud"] = true
        };
        public static bool GetPlayerBool(ulong steamId, string boolKey)
        {
            // Load player preferences
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            // Check if the key exists
            if (bools.TryGetValue(boolKey, out bool value))
            {
                return value;
            }

            // If key doesn't exist, use the default value
            if (DefaultBools.TryGetValue(boolKey, out bool defaultValue))
            {
                // Optionally, add the default key to the player's file
                bools[boolKey] = defaultValue;
                DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);

                return defaultValue;
            }

            // Return false if key is completely unknown
            return false;
        }

        public static void SetPlayerBool(ulong steamId, string boolKey, bool value)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            // Update the value
            bools[boolKey] = value;

            // Save back to file
            DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);
        }

        public static void TogglePlayerBool(ulong steamId, string boolKey)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            // Toggle the value if the key exists
            if (bools.ContainsKey(boolKey))
            {
                bools[boolKey] = !bools[boolKey];
            }
            else
            {
                // If key doesn't exist, initialize it with default (or true)
                bools[boolKey] = !DefaultBools.TryGetValue(boolKey, out var defaultValue) || !defaultValue;
            }

            // Save back to file
            DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);
        }

        public static bool TryGetPlayerBool(ulong steamId, string boolKey, out bool value)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            // Attempt to get the value
            if (bools.TryGetValue(boolKey, out value))
            {
                return true;
            }

            // Use default if available
            if (DefaultBools.TryGetValue(boolKey, out var defaultValue))
            {
                value = defaultValue;

                // Optionally, update the player's preferences
                bools[boolKey] = defaultValue;
                DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);

                return true;
            }

            // Key doesn't exist
            value = false;
            return false;
        }
        public static bool TryMigrateBools(ulong steamId)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            if (DataService.PlayerDictionaries._playerBools.TryRemove(steamId, out var oldBools))
            {
                foreach (string boolKey in oldBools.Keys)
                {
                    if (bools.ContainsKey(boolKey)) bools[boolKey] = oldBools[boolKey];
                }

                // steamId.SetPlayerBools([]); // delete old bools and save so doesn't try to migrate again
                DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);

                return true;
            }

            return false;
        }
    }
    public static HashSet<Entity> GetDeathParticipants(Entity source)
    {
        float3 sourcePosition = source.Read<Translation>().Value;
        User sourceUser = source.GetUser();
        string playerName = sourceUser.CharacterName.Value;

        Entity clanEntity = sourceUser.ClanEntity.GetEntityOnServer();
        HashSet<Entity> players = [source]; // use hashset to prevent double gains processing

        if (_parties)
        {
            List<ConcurrentList<string>> playerParties = new([.. DataService.PlayerDictionaries._playerParties.Values]);

            foreach (ConcurrentList<string> party in playerParties)
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
    public static string FormatTimespan(TimeSpan timeSpan)
    {
        string timeString = timeSpan.ToString(@"mm\:ss");
        return timeString;
    }
    public static string FormatBloodStatValue(float value)
    {
        string bonusString = (value * 100).ToString("F0") + "%";
        return bonusString;
    }
    public static string FormatWeaponStatValue(WeaponStats.WeaponStatType statType, float value)
    {
        string formattedBonus = WeaponStats.WeaponStatFormats[statType] switch
        {
            "integer" => ((int)value).ToString(),
            "decimal" => value.ToString("F2"),
            "percentage" => (value * 100).ToString("F0") + "%",
            _ => value.ToString(),
        };
        return formattedBonus;
    }

    /*
    public static bool EarnedPermaShroud()
    {
        if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(ShroudBuff) && !character.HasBuff(ShroudBuff)
    && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(ShroudBuff))
        {
            BuffUtilities.ApplyPermanentBuff(character, ShroudBuff);
        }
    }
    
    public static void InitializeChanceModifiers()
    {
        List<PlayerInfo> playerInfos = new(PlayerCache.Values);

        foreach (PlayerInfo playerInfo in playerInfos)
        {
            if (playerInfo.UserEntity.TryGetComponent(out UserStats userStats))
            {
                int vBloodKills = userStats.VBloodKills;

                FamiliarUnlockSystem.Modifiers[playerInfo.User.PlatformId] = CalculateBonusChance(vBloodKills);
            }
        }
    }
    static float CalculateBonusChance(int vBloodKills)
    {
        // WIP
        return 0f;
    }
    */
}
