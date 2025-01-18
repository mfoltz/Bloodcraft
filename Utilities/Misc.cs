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
    public class ConcurrentList<T> : IEnumerable<T>
    {
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
    public enum ScrollingTextMessage
    {
        PlayerExperience,
        FamiliarExperience,
        ProfessionExperience,
        ProfessionYield
    }

    const string SCT_PLAYER = "PlayerXP";
    const string SCT_FAMILIAR = "FamiliarXP";
    const string SCT_PROFESSIONS = "ProfessionXP";
    const string SCT_YIELD = "BonusYield";

    public static readonly List<string> ScrollingTextNames =
    [
        SCT_PLAYER,
        SCT_FAMILIAR,
        SCT_PROFESSIONS,
        SCT_YIELD
    ];

    public static readonly Dictionary<string, ScrollingTextMessage> ScrollingTextNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { SCT_PLAYER, ScrollingTextMessage.PlayerExperience },
        { SCT_FAMILIAR, ScrollingTextMessage.FamiliarExperience },
        { SCT_PROFESSIONS, ScrollingTextMessage.ProfessionExperience },
        { SCT_YIELD, ScrollingTextMessage.ProfessionYield }
    };

    public static readonly Dictionary<ScrollingTextMessage, string> ScrollingTextBoolKeyMap = new()
    {
        { ScrollingTextMessage.PlayerExperience, PlayerBoolsManager.EXPERIENCE_LOG_KEY },
        { ScrollingTextMessage.FamiliarExperience, PlayerBoolsManager.SCT_FAMILIAR_KEY },
        { ScrollingTextMessage.ProfessionExperience, PlayerBoolsManager.SCT_PROFESSIONS_KEY },
        { ScrollingTextMessage.ProfessionYield, PlayerBoolsManager.SCT_YIELD_KEY }
    };
    public static class PlayerBoolsManager
    {
        public const string EXPERIENCE_LOG_KEY = "ExperienceLogging";
        public const string QUEST_LOG_KEY = "QuestLogging";
        public const string PROFESSION_LOG_KEY = "ProfessionLogging";
        public const string WEAPON_LOG_KEY = "WeaponLogging";
        public const string BLOOD_LOG_KEY = "BloodLogging";
        public const string SPELL_LOCK_KEY = "SpellLock";
        public const string SHIFT_LOCK_KEY = "ShiftLock";
        public const string PARTY_INVITE_KEY = "PartyInvite";
        public const string EMOTE_ACTIONS_KEY = "EmoteActions";
        public const string STARTER_KIT_KEY = "StarterKit";
        public const string VBLOOD_EMOTES_KEY = "VBloodEmotes";
        public const string SHINY_FAMILIARS_KEY = "ShinyFamiliars";
        public const string REMINDERS_KEY = "Reminders";
        public const string SCT_PLAYER_KEY = "PlayerExperienceSCT";
        public const string SCT_FAMILIAR_KEY = "FamiliarExperienceSCT";
        public const string SCT_PROFESSIONS_KEY = "ProfessionExperienceSCT";
        public const string SCT_YIELD_KEY = "ProfessionYieldSCT";
        public const string EXO_FORM_KEY = "ExoForm";
        public const string SHROUD_KEY = "Shroud";

        public static readonly Dictionary<string, bool> DefaultBools = new()
        {
            [EXPERIENCE_LOG_KEY] = false,
            [QUEST_LOG_KEY] = true,
            [PROFESSION_LOG_KEY] = false,
            [WEAPON_LOG_KEY] = false,
            [BLOOD_LOG_KEY] = false,
            [SPELL_LOCK_KEY] = false,
            [SHIFT_LOCK_KEY] = false,
            [PARTY_INVITE_KEY] = false,
            [EMOTE_ACTIONS_KEY] = false,
            [STARTER_KIT_KEY] = false,
            [VBLOOD_EMOTES_KEY] = true,
            [SHINY_FAMILIARS_KEY] = true,
            [REMINDERS_KEY] = true,
            [SCT_PLAYER_KEY] = true,
            [SCT_FAMILIAR_KEY] = true,
            [SCT_PROFESSIONS_KEY] = true,
            [SCT_YIELD_KEY] = true,
            [EXO_FORM_KEY] = false,
            [SHROUD_KEY] = true
        };

        static readonly Dictionary<string, string> _boolKeyConversionMap = new()
        {
            { "Emotes", EMOTE_ACTIONS_KEY },
            { "FamiliarVisual", SHINY_FAMILIARS_KEY },
            { "Kit", STARTER_KIT_KEY },
            { "ExpertiseLogging", WEAPON_LOG_KEY }
        };
        public static bool GetPlayerBool(ulong steamId, string boolKey)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            if (bools.TryGetValue(boolKey, out bool value))
            {
                return value;
            }
            else if (DefaultBools.TryGetValue(boolKey, out bool defaultValue))
            {
                bools[boolKey] = defaultValue;
                DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);

                return defaultValue;
            }

            return false;
        }
        public static void SetPlayerBool(ulong steamId, string boolKey, bool value)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);
            bools[boolKey] = value;

            DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);
        }
        public static void TogglePlayerBool(ulong steamId, string boolKey)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            if (bools.ContainsKey(boolKey))
            {
                bools[boolKey] = !bools[boolKey];
            }
            else
            {
                bools[boolKey] = !DefaultBools.TryGetValue(boolKey, out var defaultValue) || !defaultValue;
            }

            DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);
        }
        public static bool TryGetPlayerBool(ulong steamId, string boolKey, out bool value)
        {
            var bools = DataService.PlayerBoolsManager.LoadPlayerBools(steamId);

            if (bools.TryGetValue(boolKey, out value))
            {
                return true;
            }
            else if (DefaultBools.TryGetValue(boolKey, out var defaultValue))
            {
                value = defaultValue;

                bools[boolKey] = defaultValue;
                DataService.PlayerBoolsManager.SavePlayerBools(steamId, bools);

                return true;
            }

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
                    else if (_boolKeyConversionMap.TryGetValue(boolKey, out string convertedKey))
                    {
                        bools[convertedKey] = oldBools[boolKey];
                    }
                }

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
            List<List<string>> playerParties = DataService.PlayerDictionaries._playerParties.Values
                .Select(party => party.ToList())
                .ToList();

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
