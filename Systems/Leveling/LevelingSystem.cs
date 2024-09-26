using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using VampireCommandFramework;
using static Bloodcraft.Patches.DeathEventListenerSystemPatch;
using static Bloodcraft.Services.PlayerService;
using User = ProjectM.Network.User;

namespace Bloodcraft.Systems.Leveling;
internal static class LevelingSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    const float EXPConstant = 0.1f; // constant for calculating level from xp
    const float EXPPower = 2f; // power for calculating level from xp

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID levelUpBuff = new(-1133938228);
    static readonly PrefabGUID warEventTrash = new(2090187901);

    static readonly AssetGuid assetGuid = AssetGuid.FromString("4210316d-23d4-4274-96f5-d6f0944bd0bb"); // experience
    static readonly float3 color = new(1.0f, 0.7529f, 0.7961f);
    public enum PlayerClasses
    {
        BloodKnight,
        DemonHunter,
        VampireLord,
        ShadowBlade,
        ArcaneSorcerer,
        DeathMage
    }

    public static readonly Dictionary<PlayerClasses, (string, string)> ClassWeaponBloodMap = new()
    {
        { PlayerClasses.BloodKnight, (ConfigService.BloodKnightWeapon, ConfigService.BloodKnightBlood) },
        { PlayerClasses.DemonHunter, (ConfigService.DemonHunterWeapon, ConfigService.DemonHunterBlood) },
        { PlayerClasses.VampireLord, (ConfigService.VampireLordWeapon, ConfigService.VampireLordBlood) },
        { PlayerClasses.ShadowBlade, (ConfigService.ShadowBladeWeapon, ConfigService.ShadowBladeBlood) },
        { PlayerClasses.ArcaneSorcerer, (ConfigService.ArcaneSorcererWeapon, ConfigService.ArcaneSorcererBlood) },
        { PlayerClasses.DeathMage, (ConfigService.DeathMageWeapon, ConfigService.DeathMageBlood) }
    };

    public static readonly Dictionary<PlayerClasses, (List<int>, List<int>)> ClassWeaponBloodEnumMap = new()
    {
        { PlayerClasses.BloodKnight, (ConfigUtilities.ParseConfigString(ConfigService.BloodKnightWeapon), ConfigUtilities.ParseConfigString(ConfigService.BloodKnightBlood)) },
        { PlayerClasses.DemonHunter, (ConfigUtilities.ParseConfigString(ConfigService.DemonHunterWeapon), ConfigUtilities.ParseConfigString(ConfigService.DemonHunterBlood)) },
        { PlayerClasses.VampireLord, (ConfigUtilities.ParseConfigString(ConfigService.VampireLordWeapon), ConfigUtilities.ParseConfigString(ConfigService.VampireLordBlood)) },
        { PlayerClasses.ShadowBlade, (ConfigUtilities.ParseConfigString(ConfigService.ShadowBladeWeapon), ConfigUtilities.ParseConfigString(ConfigService.ShadowBladeBlood)) },
        { PlayerClasses.ArcaneSorcerer, (ConfigUtilities.ParseConfigString(ConfigService.ArcaneSorcererWeapon), ConfigUtilities.ParseConfigString(ConfigService.ArcaneSorcererBlood)) },
        { PlayerClasses.DeathMage, (ConfigUtilities.ParseConfigString(ConfigService.DeathMageWeapon), ConfigUtilities.ParseConfigString(ConfigService.DeathMageBlood)) }
    };

    public static readonly Dictionary<PlayerClasses, string> ClassBuffMap = new()
    {
        { PlayerClasses.BloodKnight, ConfigService.BloodKnightBuffs },
        { PlayerClasses.DemonHunter, ConfigService.DemonHunterBuffs },
        { PlayerClasses.VampireLord, ConfigService.VampireLordBuffs },
        { PlayerClasses.ShadowBlade, ConfigService.ShadowBladeBuffs },
        { PlayerClasses.ArcaneSorcerer, ConfigService.ArcaneSorcererBuffs },
        { PlayerClasses.DeathMage, ConfigService.DeathMageBuffs }
    };

    public static readonly Dictionary<PlayerClasses, string> ClassSpellsMap = new()
    {
        { PlayerClasses.BloodKnight, ConfigService.BloodKnightSpells },
        { PlayerClasses.DemonHunter, ConfigService.DemonHunterSpells },
        { PlayerClasses.VampireLord, ConfigService.VampireLordSpells },
        { PlayerClasses.ShadowBlade, ConfigService.ShadowBladeSpells },
        { PlayerClasses.ArcaneSorcerer, ConfigService.ArcaneSorcererSpells },
        { PlayerClasses.DeathMage, ConfigService.DeathMageSpells }
    };

    public static readonly Dictionary<PlayerClasses, PrefabGUID> ClassOnHitDebuffMap = new() // tier 1
    {
        { PlayerClasses.BloodKnight, new(-1246704569) }, //leech
        { PlayerClasses.DemonHunter, new(-1576512627) }, //static
        { PlayerClasses.VampireLord, new(27300215) }, // chill
        { PlayerClasses.ShadowBlade, new(348724578) }, // ignite
        { PlayerClasses.ArcaneSorcerer, new(1723455773) }, // weaken
        { PlayerClasses.DeathMage, new(-325758519) } // condemn
    };

    public static readonly Dictionary<PlayerClasses, PrefabGUID> ClassOnHitEffectMap = new() // tier 2
    {

        { PlayerClasses.BloodKnight, new(2085766220) }, // lesser bloodrage
        { PlayerClasses.DemonHunter, new(-737425100) }, // lesser stormshield
        { PlayerClasses.VampireLord, new(620130895) }, // lesser frozenweapon
        { PlayerClasses.ShadowBlade, new(763939566) }, // lesser powersurge
        { PlayerClasses.ArcaneSorcerer, new(1433921398) }, // lesser aegis
        { PlayerClasses.DeathMage, new(-2071441247) } // guardian block :p
    };
    public static void OnUpdate(object sender, DeathEventArgs deathEvent)
    {
        ProcessExperience(deathEvent.Source, deathEvent.Target);
    }
    public static void ProcessExperience(Entity killerEntity, Entity victimEntity)
    {
        PlayerCharacter player = killerEntity.Read<PlayerCharacter>();
        Entity userEntity = player.UserEntity;
        float groupMultiplier = 1;

        if (IsVBlood(victimEntity))
        {
            ProcessExperienceGain(killerEntity, victimEntity, userEntity.Read<User>().PlatformId, 1); // override multiplier since this should just be a solo kill and skip getting participants for vbloods since they're all in the event list from VBloodSystem if involved in same kill
            return;
        }

        HashSet<Entity> participants = GetParticipants(killerEntity, userEntity); // want list of participants to process experience for
        if (participants.Count > 1) groupMultiplier = ConfigService.GroupLevelingMultiplier; // if more than 1 participant, apply group multiplier
        foreach (Entity participant in participants)
        {
            ulong steamId = participant.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId; // participants are character entities
            if (steamId.TryGetPlayerExperience(out var xpData) && xpData.Key >= ConfigService.MaxLevel) continue; // Check if already at max level
            ProcessExperienceGain(participant, victimEntity, steamId, groupMultiplier);
        }
    }
    public static HashSet<Entity> GetParticipants(Entity killer, Entity userEntity)
    {
        float3 killerPosition = killer.Read<Translation>().Value;
        User killerUser = userEntity.Read<User>();
        HashSet<Entity> players = [killer];

        if (ConfigService.PlayerParties)
        {
            foreach (var groupEntry in DataService.PlayerDictionaries.playerParties)
            {
                if (groupEntry.Value.Contains(killerUser.CharacterName.Value))
                {
                    foreach (string name in groupEntry.Value)
                    {
                        if (name.TryGetPlayerInfo(out PlayerInfo playerInfo))
                        {
                            if (!playerInfo.User.IsConnected) continue;
                            var distance = UnityEngine.Vector3.Distance(killerPosition, playerInfo.CharEntity.Read<Translation>().Value);
                            if (distance > ConfigService.ExpShareDistance) continue;
                            players.Add(playerInfo.CharEntity);
                        }
                    }
                    break;
                }
            }
        }

        if (killerUser.ClanEntity._Entity.Equals(Entity.Null)) return players;

        Entity clanEntity = killerUser.ClanEntity._Entity;
        var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
        for (int i = 0; i < userBuffer.Length; i++) // add clan members
        {
            var users = userBuffer[i];
            User user = users.UserEntity.Read<User>();
            if (!user.IsConnected) continue;
            Entity player = user.LocalCharacter._Entity;
            var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<Translation>().Value);
            if (distance > ConfigService.ExpShareDistance) continue;
            players.Add(player);
        }

        return players;
    }
    static void ProcessExperienceGain(Entity killerEntity, Entity victimEntity, ulong SteamID, float groupMultiplier)
    {
        UnitLevel victimLevel = victimEntity.Read<UnitLevel>();
        Health health = victimEntity.Read<Health>();

        bool isVBlood = IsVBlood(victimEntity);
        int additionalXP = (int)(health.MaxHealth._Value / 2.5f);
        float gainedXP = GetBaseExperience(victimLevel.Level._Value, isVBlood);

        gainedXP += additionalXP;
        int currentLevel = SteamID.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;

        if (currentLevel >= ConfigService.MaxLevel) return;

        gainedXP = ApplyScalingFactor(gainedXP, currentLevel, victimLevel.Level._Value);

        if (SteamID.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
        {
            int exoLevel = prestiges.TryGetValue(PrestigeType.Exo, out var exo) ? exo : 0;
            float expReductionFactor = 1 - ConfigService.LevelingPrestigeReducer * PrestigeData;
            if (exoLevel == 0)
            {
                gainedXP *= expReductionFactor;
            }
        }

        if (ConfigService.UnitSpawnerMultiplier < 1 && victimEntity.Has<IsMinion>() && victimEntity.Read<IsMinion>().Value)
        {
            gainedXP *= ConfigService.UnitSpawnerMultiplier;
            if (gainedXP == 0) return;
        }

        if (ConfigService.WarEventMultiplier < 1 && victimEntity.Has<SpawnBuffElement>())
        {
            var spawnBuffElement = victimEntity.ReadBuffer<SpawnBuffElement>();
            for (int i = 0; i < spawnBuffElement.Length; i++)
            {
                if (spawnBuffElement[i].Buff.Equals(warEventTrash))
                {
                    gainedXP *= ConfigService.WarEventMultiplier;
                    break;
                }
            }
        }

        if (ConfigService.DocileUnitMultiplier < 1 && victimEntity.Has<AggroConsumer>() && !isVBlood)
        {
            if (victimEntity.Read<AggroConsumer>().AlertDecayPerSecond == 99)
            {
                gainedXP *= 0.2f;
            }
        }

        gainedXP *= groupMultiplier;
        int rested = 0;
        if (ConfigService.RestedXPSystem) gainedXP = AddRestedXP(SteamID, gainedXP, ref rested);

        SaveExperience(SteamID, gainedXP);
        CheckAndHandleLevelUp(killerEntity, victimEntity, SteamID, gainedXP, currentLevel, rested);
    }
    static float AddRestedXP(ulong steamId, float gainedXP, ref int rested)
    {
        if (steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
        {
            float restedXP = restedData.Value;
            float bonusXP = Math.Min(gainedXP, restedXP);
            float totalXP = gainedXP + bonusXP;
            restedXP -= bonusXP;

            steamId.SetPlayerRestedXP(new KeyValuePair<DateTime, float>(restedData.Key, restedXP));
            rested = (int)bonusXP;

            return totalXP;
        }
        return gainedXP;
    }
    public static void ProcessQuestExperienceGain(User user, Entity victim, int multiplier)
    {
        ulong SteamID = user.PlatformId;
        Entity character = user.LocalCharacter._Entity;
        int currentLevel = SteamID.TryGetPlayerExperience(out var xpData) ? xpData.Key : 0;
        float gainedXP = ConvertLevelToXp(currentLevel) * 0.03f * multiplier;

        SaveExperience(SteamID, gainedXP);
        CheckAndHandleLevelUp(character, victim, SteamID, gainedXP, currentLevel);
    }
    static bool IsVBlood(Entity victimEntity)
    {
        return victimEntity.Has<VBloodConsumeSource>();
    }
    static float GetBaseExperience(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * ConfigService.VBloodLevelingMultiplier;
        return baseXP * ConfigService.UnitLevelingMultiplier;
    }
    static void SaveExperience(ulong SteamID, float gainedXP)
    {
        if (!SteamID.TryGetPlayerExperience(out var xpData))
        {
            xpData = new KeyValuePair<int, float>(0, 0); // Initialize if not present
        }

        float newExperience = xpData.Value + gainedXP;
        int newLevel = ConvertXpToLevel(newExperience);

        if (newLevel > ConfigService.MaxLevel)
        {
            newLevel = ConfigService.MaxLevel; // Cap the level at the maximum
            newExperience = ConvertLevelToXp(ConfigService.MaxLevel); // Adjust the XP to the max level's XP
        }

        SteamID.SetPlayerExperience(new KeyValuePair<int, float>(newLevel, newExperience));
    }
    static void CheckAndHandleLevelUp(Entity characterEntity, Entity victim, ulong SteamID, float gainedXP, int currentLevel, int restedXP = 0)
    {
        Entity userEntity = characterEntity.Read<PlayerCharacter>().UserEntity;

        bool leveledUp = CheckForLevelUp(SteamID, currentLevel);
        if (leveledUp)
        {
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = levelUpBuff,
            };

            FromCharacter fromCharacter = new()
            {
                Character = characterEntity,
                User = userEntity,
            };

            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            if (Classes) ApplyClassBuffAtThresholds(characterEntity, SteamID, fromCharacter);
        }
        NotifyPlayer(userEntity, victim, SteamID, (int)gainedXP, leveledUp, restedXP);
    }
    static bool CheckForLevelUp(ulong SteamID, int currentLevel)
    {
        int newLevel = ConvertXpToLevel(GetXp(SteamID));
        if (newLevel > currentLevel)
        {
            return true;
        }
        return false;
    }
    static void NotifyPlayer(Entity userEntity, Entity victim, ulong steamId, int gainedXP, bool leveledUp, int restedXP)
    {
        User user = userEntity.Read<User>();
        Entity character = user.LocalCharacter._Entity;

        if (leveledUp)
        {
            int newLevel = GetLevel(steamId);
            SetLevel(character);

            if (newLevel <= ConfigService.MaxLevel) LocalizationService.HandleServerReply(EntityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
            if (PlayerUtilities.GetPlayerBool(steamId, "Reminders") && Classes && !ClassUtilities.HasClass(steamId))
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Don't forget to choose a class! Use <color=white>'.class l'</color> to view choices and see what they have to offer with <color=white>'.class lb [Class]'</color> (buffs), <color=white>'.class lsp [Class]'</color> (spells), and <color=white>'.class lst [Class]'</color> (synergies). (toggle reminders with <color=white>'.remindme'</color>)");
            }
        }

        if (PlayerUtilities.GetPlayerBool(steamId, "ExperienceLogging"))
        {
            //Core.Log.LogInfo($"Player {user.CharacterName.Value} gained {gainedXP} rested {restedXP} leveled up {leveledUp} progress {GetLevelProgress(SteamID)}");
            int levelProgress = GetLevelProgress(steamId);
            string message = restedXP > 0 ? $"+<color=yellow>{gainedXP}</color> <color=green>rested</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)" : $"+<color=yellow>{gainedXP}</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)";
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }

        /*
        if (PlayerUtilities.GetPlayerBool(steamId, "ScrollingText"))
        {
            float3 position = victim.Read<Translation>().Value;
            float3 adjacentPosition = new(position.x + 0.05f, position.y - 0.05f, position.z - 0.01f);

            EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
            Entity sctEntity = ScrollingCombatTextMessage.Create(EntityManager, entityCommandBuffer, assetGuidLeveling, adjacentPosition, color, character, gainedXP, default, userEntity);
        }
        */
    }
    public static int ConvertXpToLevel(float xp)
    {
        return (int)(EXPConstant * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / EXPConstant, EXPPower);
    }
    static float GetXp(ulong SteamID)
    {
        if (SteamID.TryGetPlayerExperience(out var xpData))
        {
            return xpData.Value;
        }
        return 0f;
    }
    public static int GetLevel(ulong SteamID)
    {
        if (SteamID.TryGetPlayerExperience(out var xpData))
        {
            return xpData.Key;
        }
        return 0;
    }
    static int GetLevelFromXp(ulong SteamID)
    {
        return ConvertXpToLevel(GetXp(SteamID));
    }
    public static int GetLevelProgress(ulong SteamID)
    {
        float currentXP = GetXp(SteamID);
        int currentLevelXP = ConvertLevelToXp(GetLevelFromXp(SteamID));
        int nextLevelXP = ConvertLevelToXp(GetLevelFromXp(SteamID) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }
    static float ApplyScalingFactor(float gainedXP, int currentLevel, int victimLevel)
    {
        float k = ConfigService.LevelScalingMultiplier;
        int levelDifference = currentLevel - victimLevel;
        if (k <= 0) return gainedXP;
        float scalingFactor = levelDifference > 0 ? MathF.Exp(-k * levelDifference) : 1.0f;
        return gainedXP * scalingFactor;
    }
    public static bool TryParseClassName(string className, out PlayerClasses parsedClassType)
    {
        // Attempt to parse the className string to the PlayerClasses enum.
        if (Enum.TryParse(className, true, out parsedClassType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PlayerClasses enum value containing the input string.
        parsedClassType = Enum.GetValues(typeof(PlayerClasses))
                             .Cast<PlayerClasses>()
                             .FirstOrDefault(ct => ct.ToString().Contains(className, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedClassType.Equals(default(PlayerClasses)))
        {
            return true; // Found a matching enum value
        }

        // If no match is found, return false and set the out parameter to default value.
        parsedClassType = default;
        return false; // Parsing failed
    }
    public static void SetLevel(Entity player)
    {
        ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            int playerLevel = xpData.Key;
            Equipment equipment = player.Read<Equipment>();

            equipment.ArmorLevel._Value = 0f;
            equipment.SpellLevel._Value = 0f;
            equipment.WeaponLevel._Value = playerLevel;
            player.Write(equipment);
        }
    }
    static void ApplyClassBuffAtThresholds(Entity characterEntity, ulong SteamID, FromCharacter fromCharacter)
    {
        var buffs = ClassUtilities.GetClassBuffs(SteamID);

        if (buffs.Count == 0) return;

        int levelStep = ConfigService.MaxLevel / buffs.Count;
        int playerLevel = GetLevel(SteamID);

        if (playerLevel % levelStep == 0 && playerLevel / levelStep <= buffs.Count)
        {
            int buffIndex = playerLevel / levelStep - 1;

            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = new(buffs[buffIndex])
            };

            if (ServerGameManager.HasBuff(characterEntity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier())) return;
            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);

            /*
            if (ServerGameManager.TryGetBuff(characterEntity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
            {
                BuffUtilities.HandleBloodBuff(buff);

                if (buff.Has<RemoveBuffOnGameplayEvent>())
                {
                    buff.Remove<RemoveBuffOnGameplayEvent>();
                }
                if (buff.Has<RemoveBuffOnGameplayEventEntry>())
                {
                    buff.Remove<RemoveBuffOnGameplayEventEntry>();
                }
                if (buff.Has<CreateGameplayEventsOnSpawn>())
                {
                    buff.Remove<CreateGameplayEventsOnSpawn>();
                }
                if (buff.Has<GameplayEventListeners>())
                {
                    buff.Remove<GameplayEventListeners>();
                }
                if (!buff.Has<Buff_Persists_Through_Death>())
                {
                    buff.Add<Buff_Persists_Through_Death>();
                }
                if (buff.Has<LifeTime>())
                {
                    LifeTime lifeTime = buff.Read<LifeTime>();
                    lifeTime.Duration = -1;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                    buff.Write(lifeTime);
                }
            }
            */
        }
    }
    public static void ResetRestedXP(ulong steamId)
    {
        if (steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
        {
            restedData = new KeyValuePair<DateTime, float>(restedData.Key, 0);
            steamId.SetPlayerRestedXP(restedData);
        }
    }
    public class PartyUtilities
    {
        public static void HandlePlayerParty(ChatCommandContext ctx, ulong ownerId, string name)
        {
            string playerKey = PlayerCache.Keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(playerKey) && playerKey.TryGetPlayerInfo(out PlayerInfo playerInfo))
            {
                if (playerInfo.User.PlatformId == ownerId)
                {
                    LocalizationService.HandleReply(ctx, "Can't add yourself to your own party.");
                    return;
                }

                string playerName = playerInfo.User.CharacterName.Value;
                if (IsPlayerEligibleForParty(playerInfo.User.PlatformId, playerName))
                {
                    AddPlayerToParty(ctx, ownerId, playerName);
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> does not have parties enabled or is already in a party.");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Player not found...");
            }
        }
        public static bool IsPlayerEligibleForParty(ulong steamId, string playerName)
        {
            if (PlayerUtilities.GetPlayerBool(steamId, "Grouping"))
            {
                if (!steamId.TryGetPlayerParties(out var parties) && !DataService.PlayerDictionaries.playerParties.Values.Any(party => party.Equals(playerName)))
                {
                    PlayerUtilities.SetPlayerBool(steamId, "Grouping", false);
                    return true;
                }
            }
            return false;
        }
        public static void AddPlayerToParty(ChatCommandContext ctx, ulong ownerId, string playerName)
        {
            if (!ownerId.TryGetPlayerParties(out var _))
            {
                ownerId.SetPlayerParties([]);
            }

            string ownerName = ctx.Event.User.CharacterName.Value;
            if (ownerId.TryGetPlayerParties(out var party) && party.Count < ConfigService.MaxPartySize && !party.Contains(playerName))
            {
                party.Add(playerName);

                if (!party.Contains(ownerName)) // add owner to alliance for simplified processing elsewhere
                {
                    party.Add(ownerName);
                }

                ownerId.SetPlayerParties(party);
                LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> added to party.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Party is full or <color=green>{playerName}</color> is already in the party.");
            }
        }
        public static void RemovePlayerFromParty(ChatCommandContext ctx, HashSet<string> party, string playerName)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            string playerKey = PlayerCache.Keys.FirstOrDefault(key => key.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(playerKey) && party.FirstOrDefault(n => n.Equals(playerKey)) != null)
            {
                party.Remove(playerKey);
                steamId.SetPlayerParties(party);
                LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> removed from party.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> not found in party.");
            }
        }
        public static void ListPartyMembers(ChatCommandContext ctx, Dictionary<ulong, HashSet<string>> playerParties)
        {
            ulong ownerId = ctx.Event.User.PlatformId;
            string playerName = ctx.Event.User.CharacterName.Value;
            HashSet<string> members = playerParties.ContainsKey(ownerId) ? playerParties[ownerId] : playerParties.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();
            string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")) : "No members in party.";
            LocalizationService.HandleReply(ctx, replyMessage);
        }
    }
}