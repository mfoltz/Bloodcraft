using Bloodcraft.Services;
using static Bloodcraft.Systems.Leveling.PrestigeSystem;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Utilities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Systems.Experience;
internal static class LevelingSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    
    static PlayerService PlayerService => Core.PlayerService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    const float EXPConstant = 0.1f; // constant for calculating level from xp
    const float EXPPower = 2f; // power for calculating level from xp
    static bool Classes => ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID levelUpBuff = new(-1133938228);
    static readonly PrefabGUID warEventTrash = new(2090187901);
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

    public static readonly Dictionary<PlayerClasses, string> ClassPrestigeBuffsMap = new()
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
    public static void UpdateLeveling(Entity killerEntity, Entity victimEntity)
    {
        if (!IsValidVictim(victimEntity)) return;
        HandleExperienceUpdate(killerEntity, victimEntity);
    }
    static bool IsValidVictim(Entity victimEntity)
    {
        return !victimEntity.Has<Minion>() && victimEntity.Has<UnitLevel>();
    }
    static void HandleExperienceUpdate(Entity killerEntity, Entity victimEntity)
    {
        PlayerCharacter player = killerEntity.Read<PlayerCharacter>();
        Entity userEntity = player.UserEntity;
        float groupMultiplier = 1;

        if (IsVBlood(victimEntity))
        {
            ProcessExperienceGain(killerEntity, victimEntity, userEntity.Read<User>().PlatformId, 1); // override multiplier since this should just be a solo kill and skip getting participants for vbloods
            return;
        }
        
        HashSet<Entity> participants = GetParticipants(killerEntity, userEntity); // want list of participants to process experience for
        if (participants.Count > 1) groupMultiplier = ConfigService.GroupLevelingMultiplier; // if more than 1 participant, apply group multiplier
        foreach (Entity participant in participants)
        {
            ulong steamId = participant.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId; // participants are character entities
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData) && xpData.Key >= ConfigService.MaxLevel) continue; // Check if already at max level
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
            foreach (var groupEntry in Core.DataStructures.PlayerParties)
            {
                if (groupEntry.Value.Contains(killerUser.CharacterName.Value))
                {
                    foreach (string name in groupEntry.Value)
                    {
                        if (PlayerService.UserCache.TryGetValue(name, out var player))
                        {
                            if (!player.Read<User>().IsConnected) continue;
                            var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<Translation>().Value);
                            if (distance > ConfigService.ExpShareDistance) continue;
                            players.Add(player.Read<User>().LocalCharacter._Entity);
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
        float gainedXP = CalculateExperienceGained(victimLevel.Level._Value, isVBlood);

        gainedXP += additionalXP;
        int currentLevel = Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData) ? xpData.Key : 0;

        if (currentLevel >= ConfigService.MaxLevel) return;

        gainedXP = ApplyScalingFactor(gainedXP, currentLevel, victimLevel.Level._Value);
        
        if (Core.DataStructures.PlayerPrestiges.TryGetValue(SteamID, out var prestiges) && prestiges.TryGetValue(PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
        {
            int exoLevel = prestiges.TryGetValue(PrestigeType.Exo, out var exo) ? exo : 0;
            float expReductionFactor = 1 - (ConfigService.LevelingPrestigeReducer * PrestigeData);
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
        if (ConfigService.RestedXPSystem) gainedXP = HandleRestedXP(SteamID, gainedXP, ref rested);

        UpdatePlayerExperience(SteamID, gainedXP);
        CheckAndHandleLevelUp(killerEntity, SteamID, gainedXP, currentLevel, rested);
    }
    static float HandleRestedXP(ulong steamId, float gainedXP, ref int rested)
    {
        if (Core.DataStructures.PlayerRestedXP.TryGetValue(steamId, out var restedData) && restedData.Value > 0)
        {
            float restedXP = restedData.Value;
            float bonusXP = Math.Min(gainedXP, restedXP);
            float totalXP = gainedXP + bonusXP;
            restedXP -= bonusXP;

            Core.DataStructures.PlayerRestedXP[steamId] = new KeyValuePair<DateTime, float>(restedData.Key, restedXP);
            Core.DataStructures.SavePlayerRestedXP();

            rested = (int)bonusXP;
            return totalXP;
        }
        return gainedXP;
    }
    public static void ProcessQuestExperienceGain(User user, int multiplier)
    {
        ulong SteamID = user.PlatformId;
        Entity character = user.LocalCharacter._Entity;
        int currentLevel = Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData) ? xpData.Key : 0;
        float gainedXP = (float)ConvertLevelToXp(currentLevel) * 0.03f * multiplier;

        UpdatePlayerExperience(SteamID, gainedXP);
        CheckAndHandleLevelUp(character, SteamID, gainedXP, currentLevel);
    }
    static bool IsVBlood(Entity victimEntity)
    {
        return victimEntity.Has<VBloodConsumeSource>();
    }
    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * ConfigService.VBloodLevelingMultiplier;
        return baseXP * ConfigService.UnitLevelingMultiplier;
    }
    static void UpdatePlayerExperience(ulong SteamID, float gainedXP)
    {
        if (!Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData))
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

        Core.DataStructures.PlayerExperience[SteamID] = new KeyValuePair<int, float>(newLevel, newExperience);
        Core.DataStructures.SavePlayerExperience();
    }
    static void CheckAndHandleLevelUp(Entity characterEntity, ulong SteamID, float gainedXP, int currentLevel, int restedXP = 0)
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
            if (Classes) ApplyClassBuffsAtThresholds(characterEntity, SteamID, fromCharacter);
        }
        NotifyPlayer(userEntity, SteamID, (int)gainedXP, leveledUp, restedXP);
    }
    static bool CheckForLevelUp(ulong SteamID, int currentLevel)
    {
        int newLevel = ConvertXpToLevel(Core.DataStructures.PlayerExperience[SteamID].Value);
        if (newLevel > currentLevel)
        {
            return true;
        }
        return false;
    }
    static void NotifyPlayer(Entity userEntity, ulong SteamID, int gainedXP, bool leveledUp, int restedXP)
    {
        User user = userEntity.Read<User>();
        Entity character = user.LocalCharacter._Entity;

        if (leveledUp)
        {
            int newLevel = Core.DataStructures.PlayerExperience[SteamID].Key;
            SetLevel(character);

            if (newLevel <= ConfigService.MaxLevel) LocalizationService.HandleServerReply(EntityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
            if (GetPlayerBool(SteamID, "Reminders") && Classes && !HasClass(SteamID))
            {
                LocalizationService.HandleServerReply(EntityManager, user, $"Don't forget to choose a class! Use '.class l' to view choices and see what they have to offer with '.class lb [Class]' (buffs), '.class lsp [Class]' (spells), and '.class lst [Class]' (stat synergies).");
            }
        }

        if (GetPlayerBool(SteamID, "ExperienceLogging"))
        {
            int levelProgress = GetLevelProgress(SteamID);
            string message = restedXP > 0 ? $"+<color=yellow>{gainedXP}</color> <color=green>rested</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)" : $"+<color=yellow>{gainedXP}</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)";
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
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
        if (Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData)) return xpData.Value;
        return 0;
    }
    public static int GetLevel(ulong SteamID)
    {
        if (Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData)) return xpData.Key;
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
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
        {
            int playerLevel = xpData.Key;
            Equipment equipment = player.Read<Equipment>();

            equipment.ArmorLevel._Value = 0f;
            equipment.SpellLevel._Value = 0f;
            equipment.WeaponLevel._Value = playerLevel;
            player.Write(equipment);
        }
    }
    static void ApplyClassBuffsAtThresholds(Entity characterEntity, ulong SteamID, FromCharacter fromCharacter)
    {
        var buffs = GetClassBuffs(SteamID);
        //int levelStep = 20;
        if (buffs.Count == 0) return;
        int levelStep = ConfigService.MaxLevel / buffs.Count;

        int playerLevel = Core.DataStructures.PlayerExperience[SteamID].Key;
        if (playerLevel % levelStep == 0 && playerLevel / levelStep <= buffs.Count)
        {
            int buffIndex = playerLevel / levelStep - 1;

            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = new(buffs[buffIndex])
            };

            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            if (ServerGameManager.TryGetBuff(characterEntity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity firstBuff)) // if present, modify based on prestige level
            {
                //Core.Log.LogInfo($"Applied {applyBuffDebugEvent.BuffPrefabGUID.GetPrefabName()} for class buff, modifying...");

                HandleBloodBuff(firstBuff);

                if (firstBuff.Has<RemoveBuffOnGameplayEvent>())
                {
                    firstBuff.Remove<RemoveBuffOnGameplayEvent>();
                }
                if (firstBuff.Has<RemoveBuffOnGameplayEventEntry>())
                {
                    firstBuff.Remove<RemoveBuffOnGameplayEventEntry>();
                }
                if (firstBuff.Has<CreateGameplayEventsOnSpawn>())
                {
                    firstBuff.Remove<CreateGameplayEventsOnSpawn>();
                }
                if (firstBuff.Has<GameplayEventListeners>())
                {
                    firstBuff.Remove<GameplayEventListeners>();
                }
                if (!firstBuff.Has<Buff_Persists_Through_Death>())
                {
                    firstBuff.Add<Buff_Persists_Through_Death>();
                }
                if (firstBuff.Has<LifeTime>())
                {
                    LifeTime lifeTime = firstBuff.Read<LifeTime>();
                    lifeTime.Duration = -1;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                    firstBuff.Write(lifeTime);
                }
            }
        }
    }
    public static void HandleBloodBuff(Entity buff)
    {
        if (buff.Has<BloodBuff_HealReceivedProc_DataShared>())
        {
            var healReceivedProc = buff.Read<BloodBuff_HealReceivedProc_DataShared>();
            // modifications
            healReceivedProc.RequiredBloodPercentage = 0;
            buff.Write(healReceivedProc);
            return;
        }

        if (buff.Has<BloodBuffScript_Brute_HealthRegenBonus>())
        {
            var bruteHealthRegenBonus = buff.Read<BloodBuffScript_Brute_HealthRegenBonus>();
            // modifications
            bruteHealthRegenBonus.RequiredBloodPercentage = 0;
            bruteHealthRegenBonus.MinHealthRegenIncrease = bruteHealthRegenBonus.MaxHealthRegenIncrease;
            buff.Write(bruteHealthRegenBonus);
            return;
        }

        if (buff.Has<BloodBuffScript_Brute_NulifyAndEmpower>())
        {
            var bruteNulifyAndEmpower = buff.Read<BloodBuffScript_Brute_NulifyAndEmpower>();
            // modifications
            bruteNulifyAndEmpower.RequiredBloodPercentage = 0;
            buff.Write(bruteNulifyAndEmpower);
            return;
        }

        if (buff.Has<BloodBuff_Brute_PhysLifeLeech_DataShared>())
        {
            var brutePhysLifeLeech = buff.Read<BloodBuff_Brute_PhysLifeLeech_DataShared>();
            // modifications
            brutePhysLifeLeech.RequiredBloodPercentage = 0;
            brutePhysLifeLeech.MinIncreasedPhysicalLifeLeech = brutePhysLifeLeech.MaxIncreasedPhysicalLifeLeech;
            buff.Write(brutePhysLifeLeech);
            return;
        }

        if (buff.Has<BloodBuff_Brute_RecoverOnKill_DataShared>())
        {
            var bruteRecoverOnKill = buff.Read<BloodBuff_Brute_RecoverOnKill_DataShared>();
            // modifications
            bruteRecoverOnKill.RequiredBloodPercentage = 0;
            bruteRecoverOnKill.MinHealingReceivedValue = bruteRecoverOnKill.MaxHealingReceivedValue;
            buff.Write(bruteRecoverOnKill);
            return;
        }

        if (buff.Has<BloodBuff_Creature_SpeedBonus_DataShared>())
        {
            var creatureSpeedBonus = buff.Read<BloodBuff_Creature_SpeedBonus_DataShared>();
            // modifications
            creatureSpeedBonus.RequiredBloodPercentage = 0;
            creatureSpeedBonus.MinMovementSpeedIncrease = creatureSpeedBonus.MaxMovementSpeedIncrease;
            buff.Write(creatureSpeedBonus);
            return;
        }

        if (buff.Has<BloodBuff_SunResistance_DataShared>())
        {
            var sunResistance = buff.Read<BloodBuff_SunResistance_DataShared>();
            // modifications
            sunResistance.RequiredBloodPercentage = 0;
            sunResistance.MinBonus = sunResistance.MaxBonus;
            buff.Write(sunResistance);
            return;
        }

        if (buff.Has<BloodBuffScript_Draculin_BloodMendBonus>())
        {
            var draculinBloodMendBonus = buff.Read<BloodBuffScript_Draculin_BloodMendBonus>();
            // modifications
            draculinBloodMendBonus.RequiredBloodPercentage = 0;
            draculinBloodMendBonus.MinBonusHealing = draculinBloodMendBonus.MaxBonusHealing;
            buff.Write(draculinBloodMendBonus);
            return;
        }

        if (buff.Has<Script_BloodBuff_CCReduction_DataShared>())
        {
            var bloodBuffCCReduction = buff.Read<Script_BloodBuff_CCReduction_DataShared>();
            // modifications
            bloodBuffCCReduction.RequiredBloodPercentage = 0;
            bloodBuffCCReduction.MinBonus = bloodBuffCCReduction.MaxBonus;
            buff.Write(bloodBuffCCReduction);
            return;
        }

        if (buff.Has<Script_BloodBuff_Draculin_ImprovedBite_DataShared>())
        {
            var draculinImprovedBite = buff.Read<Script_BloodBuff_Draculin_ImprovedBite_DataShared>();
            // modifications
            draculinImprovedBite.RequiredBloodPercentage = 0;
            buff.Write(draculinImprovedBite);
            return;
        }

        if (buff.Has<BloodBuffScript_LastStrike>())
        {
            var lastStrike = buff.Read<BloodBuffScript_LastStrike>();
            // modifications
            lastStrike.RequiredBloodQuality = 0;
            lastStrike.LastStrikeBonus_Min = lastStrike.LastStrikeBonus_Max;
            buff.Write(lastStrike);
            return;
        }

        if (buff.Has<BloodBuff_Draculin_SpeedBonus_DataShared>())
        {
            var draculinSpeedBonus = buff.Read<BloodBuff_Draculin_SpeedBonus_DataShared>();
            // modifications
            draculinSpeedBonus.RequiredBloodPercentage = 0;
            draculinSpeedBonus.MinMovementSpeedIncrease = draculinSpeedBonus.MaxMovementSpeedIncrease;
            buff.Write(draculinSpeedBonus);
            return;
        }

        if (buff.Has<BloodBuff_AllResistance_DataShared>())
        {
            var allResistance = buff.Read<BloodBuff_AllResistance_DataShared>();
            // modifications
            allResistance.RequiredBloodPercentage = 0;
            allResistance.MinBonus = allResistance.MaxBonus;
            buff.Write(allResistance);
            return;
        }

        if (buff.Has<BloodBuff_BiteToMutant_DataShared>())
        {
            var biteToMutant = buff.Read<BloodBuff_BiteToMutant_DataShared>();
            // modifications
            biteToMutant.RequiredBloodPercentage = 0;
            biteToMutant.MutantFaction = new(877850148); // slaves_rioters
            buff.Write(biteToMutant);
            return;
        }

        if (buff.Has<BloodBuff_BloodConsumption_DataShared>())
        {
            var bloodConsumption = buff.Read<BloodBuff_BloodConsumption_DataShared>();
            // modifications
            bloodConsumption.RequiredBloodPercentage = 0;
            bloodConsumption.MinBonus = bloodConsumption.MaxBonus;
            buff.Write(bloodConsumption);
            return;
        }

        if (buff.Has<BloodBuff_HealthRegeneration_DataShared>())
        {
            var healthRegeneration = buff.Read<BloodBuff_HealthRegeneration_DataShared>();
            // modifications
            healthRegeneration.RequiredBloodPercentage = 0;
            healthRegeneration.MinBonus = healthRegeneration.MaxBonus;
            buff.Write(healthRegeneration);
            return;
        }

        if (buff.Has<BloodBuff_ApplyMovementSpeedOnShapeshift_DataShared>())
        {
            var applyMovementSpeedOnShapeshift = buff.Read<BloodBuff_ApplyMovementSpeedOnShapeshift_DataShared>();
            // modifications
            applyMovementSpeedOnShapeshift.RequiredBloodPercentage = 0;
            applyMovementSpeedOnShapeshift.MinBonus = applyMovementSpeedOnShapeshift.MaxBonus;
            buff.Write(applyMovementSpeedOnShapeshift);
            return;
        }

        if (buff.Has<BloodBuff_PrimaryAttackLifeLeech_DataShared>())
        {
            var primaryAttackLifeLeech = buff.Read<BloodBuff_PrimaryAttackLifeLeech_DataShared>();
            // modifications
            primaryAttackLifeLeech.RequiredBloodPercentage = 0;
            primaryAttackLifeLeech.MinBonus = primaryAttackLifeLeech.MaxBonus;
            buff.Write(primaryAttackLifeLeech);
            return;
        }

        if (buff.Has<BloodBuff_PrimaryProc_FreeCast_DataShared>())
        {
            var primaryProcFreeCast = buff.Read<BloodBuff_PrimaryProc_FreeCast_DataShared>(); // scholar one I think
            // modifications
            primaryProcFreeCast.RequiredBloodPercentage = 0;
            primaryProcFreeCast.MinBonus = primaryProcFreeCast.MaxBonus;
            buff.Write(primaryProcFreeCast);
            return;
        }

        if (buff.Has<BloodBuff_Rogue_AttackSpeedBonus_DataShared>())
        {
            var rogueAttackSpeedBonus = buff.Read<BloodBuff_Rogue_AttackSpeedBonus_DataShared>();
            // modifications
            rogueAttackSpeedBonus.RequiredBloodPercentage = 0;
            buff.Write(rogueAttackSpeedBonus);
            if (buff.Has<BloodBuff_Rogue_SpeedBonus_DataShared>()) // dracula blood
            {
                var rogueSpeedBonus = buff.Read<BloodBuff_Rogue_SpeedBonus_DataShared>();
                rogueSpeedBonus.RequiredBloodPercentage = 0;
                buff.Write(rogueSpeedBonus);
                return;
            }
            return;
        }

        if (buff.Has<BloodBuff_CritAmplifyProc_DataShared>())
        {
            var critAmplifyProc = buff.Read<BloodBuff_CritAmplifyProc_DataShared>();
            // modifications
            critAmplifyProc.RequiredBloodPercentage = 0;
            critAmplifyProc.MinBonus = critAmplifyProc.MaxBonus;
            buff.Write(critAmplifyProc);
            return;
        }

        if (buff.Has<BloodBuff_PhysCritChanceBonus_DataShared>())
        {
            var physCritChanceBonus = buff.Read<BloodBuff_PhysCritChanceBonus_DataShared>();
            // modifications
            physCritChanceBonus.RequiredBloodPercentage = 0;
            physCritChanceBonus.MinPhysicalCriticalStrikeChance = physCritChanceBonus.MaxPhysicalCriticalStrikeChance;
            buff.Write(physCritChanceBonus);
            return;
        }

        if (buff.Has<BloodBuff_Rogue_SpeedBonus_DataShared>())
        {
            var rogueSpeedBonus = buff.Read<BloodBuff_Rogue_SpeedBonus_DataShared>();
            // modifications
            rogueSpeedBonus.RequiredBloodPercentage = 0;
            rogueSpeedBonus.MinMovementSpeedIncrease = rogueSpeedBonus.MaxMovementSpeedIncrease;
            buff.Write(rogueSpeedBonus);
            return;
        }

        if (buff.Has<BloodBuff_ReducedTravelCooldown_DataShared>())
        {
            var reducedTravelCooldown = buff.Read<BloodBuff_ReducedTravelCooldown_DataShared>();
            // modifications
            reducedTravelCooldown.RequiredBloodPercentage = 0;
            reducedTravelCooldown.MinBonus = reducedTravelCooldown.MaxBonus;
            buff.Write(reducedTravelCooldown);
            return;
        }

        if (buff.Has<BloodBuff_Scholar_SpellCooldown_DataShared>())
        {
            var scholarSpellCooldown = buff.Read<BloodBuff_Scholar_SpellCooldown_DataShared>();
            // modifications
            scholarSpellCooldown.RequiredBloodPercentage = 0;
            scholarSpellCooldown.MinCooldownReduction = scholarSpellCooldown.MaxCooldownReduction;
            buff.Write(scholarSpellCooldown);
            return;
        }

        if (buff.Has<BloodBuff_Scholar_SpellCritChanceBonus_DataShared>())
        {
            var scholarSpellCritChanceBonus = buff.Read<BloodBuff_Scholar_SpellCritChanceBonus_DataShared>();
            // modifications
            scholarSpellCritChanceBonus.RequiredBloodPercentage = 0;
            scholarSpellCritChanceBonus.MinSpellCriticalStrikeChance = scholarSpellCritChanceBonus.MaxSpellCriticalStrikeChance;
            buff.Write(scholarSpellCritChanceBonus);
            return;
        }

        if (buff.Has<BloodBuff_Scholar_SpellPowerBonus_DataShared>())
        {
            var scholarSpellPowerBonus = buff.Read<BloodBuff_Scholar_SpellPowerBonus_DataShared>();
            // modifications
            scholarSpellPowerBonus.RequiredBloodPercentage = 0;
            scholarSpellPowerBonus.MinSpellPowerIncrease = scholarSpellPowerBonus.MaxSpellPowerIncrease;
            buff.Write(scholarSpellPowerBonus);
            if (buff.Has<BloodBuff_Warrior_PhysDamageBonus_DataShared>()) // dracula blood
            {
                var warriorPhysDamageBonus = buff.Read<BloodBuff_Warrior_PhysDamageBonus_DataShared>();
                warriorPhysDamageBonus.RequiredBloodPercentage = 0;
                warriorPhysDamageBonus.MinPhysDamageIncrease = warriorPhysDamageBonus.MaxPhysDamageIncrease;
                buff.Write(warriorPhysDamageBonus);
                return;
            }
            return;
        }

        if (buff.Has<BloodBuff_SpellLifeLeech_DataShared>())
        {
            var spellLifeLeech = buff.Read<BloodBuff_SpellLifeLeech_DataShared>();
            // modifications
            spellLifeLeech.RequiredBloodPercentage = 0;
            spellLifeLeech.MinBonus = spellLifeLeech.MaxBonus;
            buff.Write(spellLifeLeech);
            return;
        }

        if (buff.Has<BloodBuff_Warrior_DamageReduction_DataShared>())
        {
            var warriorDamageReduction = buff.Read<BloodBuff_Warrior_DamageReduction_DataShared>();
            // modifications
            warriorDamageReduction.RequiredBloodPercentage = 0;
            warriorDamageReduction.MinDamageReduction = warriorDamageReduction.MaxDamageReduction;
            buff.Write(warriorDamageReduction);
            return;
        }

        if (buff.Has<BloodBuff_Warrior_PhysCritDamageBonus_DataShared>())
        {
            var warriorPhysCritDamageBonus = buff.Read<BloodBuff_Warrior_PhysCritDamageBonus_DataShared>();
            // modifications
            warriorPhysCritDamageBonus.RequiredBloodPercentage = 0;
            warriorPhysCritDamageBonus.MinWeaponCriticalStrikeDamageIncrease = warriorPhysCritDamageBonus.MaxWeaponCriticalStrikeDamageIncrease;
            buff.Write(warriorPhysCritDamageBonus);
            return;
        }

        if (buff.Has<BloodBuff_Warrior_PhysDamageBonus_DataShared>())
        {
            var warriorPhysDamageBonus = buff.Read<BloodBuff_Warrior_PhysDamageBonus_DataShared>();
            // modifications
            warriorPhysDamageBonus.RequiredBloodPercentage = 0;
            warriorPhysDamageBonus.MinPhysDamageIncrease = warriorPhysDamageBonus.MaxPhysDamageIncrease;
            buff.Write(warriorPhysDamageBonus);
            return;
        }

        if (buff.Has<BloodBuff_Warrior_PhysicalBonus_DataShared>())
        {
            var warriorPhysicalBonus = buff.Read<BloodBuff_Warrior_PhysicalBonus_DataShared>();
            // modifications
            warriorPhysicalBonus.RequiredBloodPercentage = 0;
            warriorPhysicalBonus.MinWeaponPowerIncrease = warriorPhysicalBonus.MaxWeaponPowerIncrease;
            buff.Write(warriorPhysicalBonus);
            return;
        }

        if (buff.Has<BloodBuff_Warrior_WeaponCooldown_DataShared>())
        {
            var warriorWeaponCooldown = buff.Read<BloodBuff_Warrior_WeaponCooldown_DataShared>();
            // modifications
            warriorWeaponCooldown.RequiredBloodPercentage = 0;
            warriorWeaponCooldown.MinCooldownReduction = warriorWeaponCooldown.MaxCooldownReduction;
            buff.Write(warriorWeaponCooldown);
            return;
        }

        if (buff.Has<BloodBuff_Brute_100_DataShared>())
        {
            var bruteEffect = buff.Read<BloodBuff_Brute_100_DataShared>();
            bruteEffect.RequiredBloodPercentage = 0;
            bruteEffect.MinHealthRegainPercentage = bruteEffect.MaxHealthRegainPercentage;
            buff.Write(bruteEffect);
            return;
        }

        if (buff.Has<BloodBuff_Rogue_100_DataShared>())
        {
            var rogueEffect = buff.Read<BloodBuff_Rogue_100_DataShared>();
            rogueEffect.RequiredBloodPercentage = 0;
            buff.Write(rogueEffect);
            return;
        }

        if (buff.Has<BloodBuff_Warrior_100_DataShared>())
        {
            var warriorEffect = buff.Read<BloodBuff_Warrior_100_DataShared>();
            warriorEffect.RequiredBloodPercentage = 0;
            buff.Write(warriorEffect);
            return;
        }

        if (buff.Has<BloodBuffScript_Scholar_MovementSpeedOnCast>())
        {
            var scholarEffect = buff.Read<BloodBuffScript_Scholar_MovementSpeedOnCast>();
            scholarEffect.RequiredBloodPercentage = 0;
            scholarEffect.ChanceToGainMovementOnCast_Min = scholarEffect.ChanceToGainMovementOnCast_Max;
            buff.Write(scholarEffect);
            return;
        }

        if (buff.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>())
        {
            var bruteAttackSpeedBonus = buff.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
            bruteAttackSpeedBonus.MinValue = bruteAttackSpeedBonus.MaxValue;
            bruteAttackSpeedBonus.RequiredBloodPercentage = 0;
            bruteAttackSpeedBonus.GearLevel = 0f;
            buff.Write(bruteAttackSpeedBonus);
            return;
        }   
    }
    public static bool HandleClassChangeItem(ChatCommandContext ctx, ulong steamId)
    {
        PrefabGUID item = new(ConfigService.ChangeClassItem);
        int quantity = ConfigService.ChangeClassQuantity;

        if (!InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out var inventoryEntity) ||
            ServerGameManager.GetInventoryItemCount(inventoryEntity, item) < quantity)
        {
            LocalizationService.HandleReply(ctx, $"You do not have the required item to change classes ({item.GetPrefabName()}x{quantity})");
            return false;
        }

        if (!ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
        {
            LocalizationService.HandleReply(ctx, $"Failed to remove the required item ({item.GetPrefabName()}x{quantity})");
            return false;
        }

        RemoveClassBuffs(ctx, steamId);

        return true;
    }
    public static void UpdateClassData(Entity character, PlayerClasses parsedClassType, Dictionary<PlayerClasses, (List<int>, List<int>)> classes, ulong steamId)
    {
        var weaponConfigEntry = ClassWeaponBloodMap[parsedClassType].Item1;
        var bloodConfigEntry = ClassWeaponBloodMap[parsedClassType].Item2;
        var classWeaponStats = ParseConfigString(weaponConfigEntry);
        var classBloodStats = ParseConfigString(bloodConfigEntry);

        classes[parsedClassType] = (classWeaponStats, classBloodStats);

        Core.DataStructures.PlayerClass[steamId] = classes;
        Core.DataStructures.SavePlayerClasses();

        FromCharacter fromCharacter = new()
        {
            Character = character,
            User = character.Read<PlayerCharacter>().UserEntity,
        };

        ApplyClassBuffs(character, steamId, fromCharacter);
    }
    public static void ApplyClassBuffs(Entity character, ulong steamId, FromCharacter fromCharacter)
    {
        var buffs = GetClassBuffs(steamId);

        if (buffs.Count == 0) return;
        int levelStep = ConfigService.MaxLevel / buffs.Count;

        int playerLevel = 0;

        if (ConfigService.LevelingSystem)
        {
            playerLevel = Core.DataStructures.PlayerExperience[steamId].Key;
        }
        else
        {
            Equipment equipment = character.Read<Equipment>();
            playerLevel = (int)equipment.GetFullLevel();
        }

        if (ConfigService.PrestigeSystem && Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) && prestigeData[PrestigeType.Experience] > 0)
        {
            playerLevel = ConfigService.MaxLevel;
        }

        int numBuffsToApply = playerLevel / levelStep;

        if (numBuffsToApply > 0 && numBuffsToApply <= buffs.Count)
        {
            //Core.Log.LogInfo($"Applying {numBuffsToApply} class buff(s) at level {playerLevel}");

            for (int i = 0; i < numBuffsToApply; i++)
            {
                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                {
                    BuffPrefabGUID = new(buffs[i])
                };
                if (!ServerGameManager.TryGetBuff(character, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity _))
                {
                    DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    if (ServerGameManager.TryGetBuff(character, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity firstBuff))
                    {
                        //Core.Log.LogInfo($"Applied {applyBuffDebugEvent.BuffPrefabGUID.LookupName()} for class buff");

                        HandleBloodBuff(firstBuff);

                        if (firstBuff.Has<RemoveBuffOnGameplayEvent>())
                        {
                            firstBuff.Remove<RemoveBuffOnGameplayEvent>();
                        }
                        if (firstBuff.Has<RemoveBuffOnGameplayEventEntry>())
                        {
                            firstBuff.Remove<RemoveBuffOnGameplayEventEntry>();
                        }
                        if (firstBuff.Has<CreateGameplayEventsOnSpawn>())
                        {
                            firstBuff.Remove<CreateGameplayEventsOnSpawn>();
                        }
                        if (firstBuff.Has<GameplayEventListeners>())
                        {
                            firstBuff.Remove<GameplayEventListeners>();
                        }
                        if (!firstBuff.Has<Buff_Persists_Through_Death>())
                        {
                            firstBuff.Add<Buff_Persists_Through_Death>();
                        }
                        if (firstBuff.Has<LifeTime>())
                        {
                            LifeTime lifeTime = firstBuff.Read<LifeTime>();
                            lifeTime.Duration = -1;
                            lifeTime.EndAction = LifeTimeEndAction.None;
                            firstBuff.Write(lifeTime);
                        }
                    }
                }
            }
        }
    }
    public static void RemoveClassBuffs(ChatCommandContext ctx, ulong steamId)
    {
        List<int> buffs = GetClassBuffs(steamId);
        var buffSpawner = BuffUtility.BuffSpawner.Create(ServerGameManager);
        var entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        if (buffs.Count == 0) return;

        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i] == 0) continue;
            PrefabGUID buffPrefab = new(buffs[i]);
            if (ServerGameManager.HasBuff(ctx.Event.SenderCharacterEntity, buffPrefab.ToIdentifier()))
            {
                BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, buffPrefab.ToIdentifier(), ctx.Event.SenderCharacterEntity);
            }
        }
    }
    public static Dictionary<int, int> GetSpellPrefabs()
    {
        Dictionary<int, int> spellPrefabs = [];
        foreach (LevelingSystem.PlayerClasses playerClass in Enum.GetValues(typeof(LevelingSystem.PlayerClasses)))
        {
            if (!string.IsNullOrEmpty(LevelingSystem.ClassSpellsMap[playerClass])) ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]).Select((x, index) => new { Hash = x, Index = index }).ToList().ForEach(x => spellPrefabs.TryAdd(x.Hash, x.Index));
        }
        return spellPrefabs;
    }
    public static List<int> GetClassBuffs(ulong steamId)
    {
        if (Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classes) && classes.Keys.Count > 0)
        {
            var playerClass = classes.Keys.FirstOrDefault();
            return ParseConfigString(LevelingSystem.ClassPrestigeBuffsMap[playerClass]);
        }
        return [];
    }
    public static PlayerClasses GetPlayerClass(ulong steamId)
    {
        return Core.DataStructures.PlayerClass[steamId].Keys.First();
    }
    public static bool HasClass(ulong steamId)
    {
        return Core.DataStructures.PlayerClass.ContainsKey(steamId) && Core.DataStructures.PlayerClass[steamId].Keys.Count > 0;
    }
    public static List<int> GetClassSpells(ulong steamId)
    {
        if (Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classes) && classes.Keys.Count > 0)
        {
            var playerClass = classes.Keys.FirstOrDefault();
            return ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]);
        }
        return [];
    }
    public static bool TryParseClass(string classType, out PlayerClasses parsedClassType)
    {
        // Attempt to parse the classType string to the PlayerClasses enum.
        if (Enum.TryParse(classType, true, out parsedClassType))
        {
            return true; // Successfully parsed
        }

        // If the initial parse failed, try to find a matching PlayerClasses enum value containing the input string.
        parsedClassType = Enum.GetValues(typeof(PlayerClasses))
                              .Cast<PlayerClasses>()
                              .FirstOrDefault(pc => pc.ToString().Contains(classType, StringComparison.OrdinalIgnoreCase));

        // Check if a valid enum value was found that contains the input string.
        if (!parsedClassType.Equals(default(PlayerClasses)))
        {
            return true; // Found a matching enum value
        }

        // If no match is found, return false and set the out parameter to default value.
        parsedClassType = default;
        return false; // Parsing failed
    }
    public static void ShowClassBuffs(ChatCommandContext ctx, PlayerClasses playerClass)
    {
        List<int> perks = ParseConfigString(LevelingSystem.ClassPrestigeBuffsMap[playerClass]);

        if (perks.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"{playerClass} buffs not found.");
            return;
        }

        int step = ConfigService.MaxLevel / perks.Count;

        var classBuffs = perks.Select((perk, index) =>
        {
            int level = (index + 1) * step;
            string prefab = new PrefabGUID(perk).LookupName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            return $"<color=white>{prefab}</color> at level <color=yellow>{level}</color>";
        }).ToList();

        for (int i = 0; i < classBuffs.Count; i += 6)
        {
            var batch = classBuffs.Skip(i).Take(6);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{playerClass} buffs: {replyMessage}");
        }
    }
    public static void ShowClassSpells(ChatCommandContext ctx, PlayerClasses playerClass)
    {
        List<int> perks = ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]);

        if (perks.Count == 0)
        {
            LocalizationService.HandleReply(ctx, $"{playerClass} spells not found.");
            return;
        }

        var classSpells = perks.Select(perk =>
        {
            string prefab = new PrefabGUID(perk).LookupName();
            int prefabIndex = prefab.IndexOf("Prefab");
            if (prefabIndex != -1)
            {
                prefab = prefab[..prefabIndex].TrimEnd();
            }
            return $"<color=white>{prefab}</color>";
        }).ToList();

        for (int i = 0; i < classSpells.Count; i += 6)
        {
            var batch = classSpells.Skip(i).Take(6);
            string replyMessage = string.Join(", ", batch);
            LocalizationService.HandleReply(ctx, $"{playerClass} spells: {replyMessage}");
        }
    }
    public static void ResetRestedXP(ulong steamId)
    { 
        if (Core.DataStructures.PlayerRestedXP.TryGetValue(steamId, out var restedData) && restedData.Value > 0)
        {
            restedData = new KeyValuePair<DateTime, float>(restedData.Key, 0);
            Core.DataStructures.PlayerRestedXP[steamId] = restedData;
            Core.DataStructures.SavePlayerRestedXP();
        }
    }
    public class PartyUtilities
    {
        public static void HandlePlayerParty(ChatCommandContext ctx, ulong ownerId, string name)
        {
            string playerKey = PlayerService.UserCache.Keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(playerKey) && PlayerService.UserCache.TryGetValue(playerKey, out Entity player))
            {
                if (player.Equals(Entity.Null))
                {
                    LocalizationService.HandleReply(ctx, "Player not found...");
                    return;
                }

                User foundUser = player.Read<User>();
                if (foundUser.PlatformId == ownerId)
                {
                    LocalizationService.HandleReply(ctx, "Can't add yourself to your own party.");
                    return;
                }

                string playerName = foundUser.CharacterName.Value;
                if (IsPlayerEligibleForParty(foundUser, playerName))
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
        public static bool IsPlayerEligibleForParty(User foundUser, string playerName)
        {
            if (Core.DataStructures.PlayerBools.TryGetValue(foundUser.PlatformId, out var bools) && bools["Grouping"])
            {
                if (!Core.DataStructures.PlayerParties.ContainsKey(foundUser.PlatformId) && !Core.DataStructures.PlayerParties.Values.Any(party => party.Equals(playerName)))
                {
                    bools["Grouping"] = false;
                    Core.DataStructures.SavePlayerBools();
                    return true;
                }
            }
            return false;
        }
        public static void AddPlayerToParty(ChatCommandContext ctx, ulong ownerId, string playerName)
        {
            if (!Core.DataStructures.PlayerParties.ContainsKey(ownerId))
            {
                Core.DataStructures.PlayerParties[ownerId] = [];
            }

            string ownerName = ctx.Event.User.CharacterName.Value;
            HashSet<string> party = Core.DataStructures.PlayerParties[ownerId];

            if (party.Count < ConfigService.MaxPartySize && !party.Contains(playerName))
            {
                party.Add(playerName);

                if (!party.Contains(ownerName)) // add owner to alliance for simplified processing elsewhere
                {
                    party.Add(ownerName);
                }

                Core.DataStructures.SavePlayerParties();
                LocalizationService.HandleReply(ctx, $"<color=green>{playerName}</color> added to party.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"Party is full or <color=green>{playerName}</color> is already in the party.");
            }
        }
        public static void RemovePlayerFromParty(ChatCommandContext ctx, HashSet<string> party, string playerName)
        {
            string playerKey = PlayerService.UserCache.Keys.FirstOrDefault(key => key.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(playerKey) && party.FirstOrDefault(n => n.Equals(playerKey)) != null)
            {
                party.Remove(playerKey);
                Core.DataStructures.SavePlayerParties();
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