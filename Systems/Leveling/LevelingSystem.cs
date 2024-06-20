using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.LocalizationService;

namespace Bloodcraft.Systems.Experience;
public class LevelingSystem
{
    static readonly float UnitMultiplier = Plugin.UnitLevelingMultiplier.Value; // multipler for normal units
    static readonly float VBloodMultiplier = Plugin.VBloodLevelingMultiplier.Value; // multiplier for VBlood units
    static readonly float EXPConstant = 0.1f; // constant for calculating level from xp
    static readonly float EXPPower = 2f; // power for calculating level from xp
    static readonly int MaxPlayerLevel = Plugin.MaxPlayerLevel.Value; // maximum level
    static readonly float GroupMultiplier = Plugin.GroupLevelingMultiplier.Value; // multiplier for group kills
    static readonly float LevelScalingMultiplier = Plugin.LevelScalingMultiplier.Value; //
    static readonly float UnitSpawnerMultiplier = Plugin.UnitSpawnerMultiplier.Value;
    static readonly float WarEventMultiplier = Plugin.WarEventMultiplier.Value;
    static readonly float ExpShareDistance = Plugin.ExpShareDistance.Value;
    static readonly bool PlayerAlliances = Plugin.PlayerAlliances.Value;

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
        { PlayerClasses.BloodKnight, (Plugin.BloodKnightWeapon.Value, Plugin.BloodKnightBlood.Value) },
        { PlayerClasses.DemonHunter, (Plugin.DemonHunterWeapon.Value, Plugin.DemonHunterBlood.Value) },
        { PlayerClasses.VampireLord, (Plugin.VampireLordWeapon.Value, Plugin.VampireLordBlood.Value) },
        { PlayerClasses.ShadowBlade, (Plugin.ShadowBladeWeapon.Value, Plugin.ShadowBladeBlood.Value) },
        { PlayerClasses.ArcaneSorcerer, (Plugin.ArcaneSorcererWeapon.Value, Plugin.ArcaneSorcererBlood.Value) },
        { PlayerClasses.DeathMage, (Plugin.DeathMageWeapon.Value, Plugin.DeathMageBlood.Value) }
    };

    public static readonly Dictionary<PlayerClasses, string> ClassPrestigeBuffsMap = new()
    {
        { PlayerClasses.BloodKnight, Plugin.BloodKnightBuffs.Value },
        { PlayerClasses.DemonHunter, Plugin.DemonHunterBuffs.Value },
        { PlayerClasses.VampireLord, Plugin.VampireLordBuffs.Value },
        { PlayerClasses.ShadowBlade, Plugin.ShadowBladeBuffs.Value },
        { PlayerClasses.ArcaneSorcerer, Plugin.ArcaneSorcererBuffs.Value },
        { PlayerClasses.DeathMage, Plugin.DeathMageBuffs.Value }
    };

    public static readonly Dictionary<PlayerClasses, string> ClassSpellsMap = new()
    {
        { PlayerClasses.BloodKnight, Plugin.BloodKnightSpells.Value },
        { PlayerClasses.DemonHunter, Plugin.DemonHunterSpells.Value },
        { PlayerClasses.VampireLord, Plugin.VampireLordSpells.Value },
        { PlayerClasses.ShadowBlade, Plugin.ShadowBladeSpells.Value },
        { PlayerClasses.ArcaneSorcerer, Plugin.ArcaneSorcererSpells.Value },
        { PlayerClasses.DeathMage, Plugin.DeathMageSpells.Value }
    };

    public static readonly Dictionary<PlayerClasses, PrefabGUID> ClassApplyBuffOnDamageDealtMap = new()
    {
        { PlayerClasses.BloodKnight, new(-1246704569) }, // leech
        { PlayerClasses.DemonHunter, new(-1576512627) }, // static
        { PlayerClasses.VampireLord, new(27300215) }, // chill
        { PlayerClasses.ShadowBlade, new(348724578) }, // ignite
        { PlayerClasses.ArcaneSorcerer, new(1723455773) }, // weaken
        { PlayerClasses.DeathMage, new(-1246704569) } // condemn
    };

    public static void UpdateLeveling(Entity killerEntity, Entity victimEntity)
    {
        EntityManager entityManager = Core.EntityManager;
        if (!IsValidVictim(entityManager, victimEntity)) return;
        HandleExperienceUpdate(entityManager, killerEntity, victimEntity);
    }

    static bool IsValidVictim(EntityManager entityManager, Entity victimEntity)
    {
        return !entityManager.HasComponent<Minion>(victimEntity) && entityManager.HasComponent<UnitLevel>(victimEntity);
    }

    static void HandleExperienceUpdate(EntityManager entityManager, Entity killerEntity, Entity victimEntity)
    {
        PlayerCharacter player = entityManager.GetComponentData<PlayerCharacter>(killerEntity);
        Entity userEntity = player.UserEntity;
        float groupMultiplier = 1;

        if (IsVBlood(entityManager, victimEntity))
        {
            ProcessExperienceGain(entityManager, killerEntity, victimEntity, userEntity.Read<User>().PlatformId, 1); // override multiplier since this should just be a solo kill and skip getting participants for vbloods
            return;
        }

        HashSet<Entity> participants = GetParticipants(killerEntity, userEntity); // want list of participants to process experience for
        if (participants.Count > 1) groupMultiplier = GroupMultiplier; // if more than 1 participant, apply group multiplier
        foreach (Entity participant in participants)
        {
            ulong steamId = participant.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId; // participants are character entities
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData) && xpData.Key >= MaxPlayerLevel) continue; // Check if already at max level
            ProcessExperienceGain(entityManager, participant, victimEntity, steamId, groupMultiplier);
        }
    }

    static HashSet<Entity> GetParticipants(Entity killer, Entity userEntity)
    {
        float3 killerPosition = killer.Read<Translation>().Value;
        User killerUser = userEntity.Read<User>();
        HashSet<Entity> players = [killer];

        // check for exp sharing group here too
        /*
        if (PlayerAlliances && Core.DataStructures.PlayerAlliances.TryGetValue(killerUser.PlatformId, out var group)) //check if group leader, add members
        {
            foreach (string name in group)
            {
                if (PlayerService.playerCache.TryGetValue(name, out var player)) // userEntities from player cache
                {
                    //Core.Log.LogInfo($"Adding {name} to participants if online...");
                    if (!player.Read<User>().IsConnected) continue;
                    var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<Translation>().Value);
                    if (distance > ExpShareDistance) continue;
                    players.Add(player.Read<User>().LocalCharacter._Entity);
                }
            }
        }
        else if (PlayerAlliances) // if not leader but in alliance, add members
        {
            foreach (var groupEntry in Core.DataStructures.PlayerAlliances)
            {
                if (groupEntry.Value.Contains(killerUser.CharacterName.Value))
                {
                    foreach (string name in groupEntry.Value)
                    {
                        if (PlayerService.playerCache.TryGetValue(name, out var player))
                        {
                            if (!player.Read<User>().IsConnected) continue;
                            var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<Translation>().Value);
                            if (distance > ExpShareDistance) continue;
                            players.Add(player.Read<User>().LocalCharacter._Entity);
                        }    
                    }
                    break;
                }
            }
        }
        */
        if (PlayerAlliances)
        {
            foreach (var groupEntry in Core.DataStructures.PlayerAlliances)
            {
                if (groupEntry.Value.Contains(killerUser.CharacterName.Value))
                {
                    foreach (string name in groupEntry.Value)
                    {
                        if (PlayerService.playerCache.TryGetValue(name, out var player))
                        {
                            //Core.Log.LogInfo($"Adding {name} to participants if online...");
                            if (!player.Read<User>().IsConnected) continue;
                            var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<Translation>().Value);
                            if (distance > ExpShareDistance) continue;
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
            if (distance > ExpShareDistance) continue;
            players.Add(player);
        }
        return players;
    }

    static void ProcessExperienceGain(EntityManager entityManager, Entity killerEntity, Entity victimEntity, ulong SteamID, float groupMultiplier)
    {
        UnitLevel victimLevel = entityManager.GetComponentData<UnitLevel>(victimEntity);
        Health health = entityManager.GetComponentData<Health>(victimEntity);

        bool isVBlood = IsVBlood(entityManager, victimEntity);
        int additionalXP = (int)(health.MaxHealth._Value / 2.5f);
        float gainedXP = CalculateExperienceGained(victimLevel.Level._Value, isVBlood);
        //Core.Log.LogInfo($"Gained XP before nerfing unit spawner: {gainedXP}");

        //Core.Log.LogInfo($"Gained XP before adding bonus from health of unit: {gainedXP}");
        gainedXP += additionalXP;
        //Core.Log.LogInfo($"Gained XP after adding bonus from health of unit: {gainedXP}");
        int currentLevel = Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData) ? xpData.Key : 0;

        gainedXP = ApplyScalingFactor(gainedXP, currentLevel, victimLevel.Level._Value);
        //Core.Log.LogInfo($"Gained XP after applying scaling factor: {gainedXP}");
        if (Core.DataStructures.PlayerPrestiges.TryGetValue(SteamID, out var prestiges) && prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
        {
            float expReductionFactor = 1 - (Plugin.LevelingPrestigeReducer.Value * PrestigeData);
            gainedXP *= expReductionFactor;
        }
        //Core.Log.LogInfo($"Gained XP after reducing for prestige: {gainedXP}");
        if (UnitSpawnerMultiplier < 1 && victimEntity.Has<IsMinion>() && victimEntity.Read<IsMinion>().Value)
        {
            gainedXP *= UnitSpawnerMultiplier;
            if (gainedXP == 0) return;
        }
        if (WarEventMultiplier < 1 && victimEntity.Has<SpawnBuffElement>())
        {
            // nerf experience gain from war event units
            var spawnBuffElement = victimEntity.ReadBuffer<SpawnBuffElement>();
            for (int i = 0; i < spawnBuffElement.Length; i++)
            {
                //Core.Log.LogInfo(spawnBuffElement[i].Buff.GetPrefabName());
                //Core.Log.LogInfo(spawnBuffElement[i].Buff.GuidHash);
                if (spawnBuffElement[i].Buff.Equals(warEventTrash))
                {
                    //Core.Log.LogInfo($"Gained XP before nerfing warevent: {gainedXP}");
                    gainedXP *= WarEventMultiplier;
                    break;
                }
            }
        }
        gainedXP *= groupMultiplier;
        UpdatePlayerExperience(SteamID, gainedXP);

        CheckAndHandleLevelUp(killerEntity, SteamID, gainedXP, currentLevel);
    }

    static bool IsVBlood(EntityManager entityManager, Entity victimEntity)
    {
        return entityManager.HasComponent<VBloodConsumeSource>(victimEntity);
    }

    static float CalculateExperienceGained(int victimLevel, bool isVBlood)
    {
        int baseXP = victimLevel;
        if (isVBlood) return baseXP * VBloodMultiplier;
        return baseXP * UnitMultiplier;
    }

    static void UpdatePlayerExperience(ulong SteamID, float gainedXP)
    {
        // Retrieve the current experience and level from the player's data structure.
        if (!Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData))
        {
            xpData = new KeyValuePair<int, float>(0, 0); // Initialize if not present
        }

        // Calculate new experience amount
        float newExperience = xpData.Value + gainedXP;

        // Check and update the level based on new experience
        int newLevel = ConvertXpToLevel(newExperience);
        if (newLevel > MaxPlayerLevel)
        {
            newLevel = MaxPlayerLevel; // Cap the level at the maximum
            newExperience = ConvertLevelToXp(MaxPlayerLevel); // Adjust the XP to the max level's XP
        }

        // Update the level and experience in the data structure
        Core.DataStructures.PlayerExperience[SteamID] = new KeyValuePair<int, float>(newLevel, newExperience);

        // Save the experience data
        Core.DataStructures.SavePlayerExperience();
    }

    static void CheckAndHandleLevelUp(Entity characterEntity, ulong SteamID, float gainedXP, int currentLevel)
    {
        EntityManager entityManager = Core.EntityManager;
        Entity userEntity = characterEntity.Read<PlayerCharacter>().UserEntity;

        bool leveledUp = CheckForLevelUp(SteamID, currentLevel);
        //Plugin.Log.LogInfo($"Leveled up: {leveledUp}");
        if (leveledUp)
        {
            //Plugin.Log.LogInfo("Applying level up buff...");
            DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = levelUpBuff,
            };
            FromCharacter fromCharacter = new()
            {
                Character = characterEntity,
                User = userEntity,
            };
            // apply level up buff here
            debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);

            if (Plugin.SoftSynergies.Value || Plugin.HardSynergies.Value) ApplyClassBuffsAtThresholds(characterEntity, SteamID, debugEventsSystem, fromCharacter); // get prestige level before this and do prestigeClassBuff version of method with if/else
        }
        NotifyPlayer(entityManager, userEntity, SteamID, (int)gainedXP, leveledUp);
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

    static void NotifyPlayer(EntityManager entityManager, Entity userEntity, ulong SteamID, int gainedXP, bool leveledUp)
    {
        User user = entityManager.GetComponentData<User>(userEntity);
        if (leveledUp)
        {
            int newLevel = Core.DataStructures.PlayerExperience[SteamID].Key;
            GearOverride.SetLevel(userEntity.Read<User>().LocalCharacter._Entity);
            if (newLevel < MaxPlayerLevel) HandleServerReply(entityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
        }
        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["ExperienceLogging"])
        {
            int levelProgress = GetLevelProgress(SteamID);
            HandleServerReply(entityManager, user, $"+<color=yellow>{gainedXP}</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)");
        }
    }

    public static int ConvertXpToLevel(float xp)
    {
        // Assuming a basic square root scaling for experience to level conversion
        return (int)(EXPConstant * Math.Sqrt(xp));
    }

    public static int ConvertLevelToXp(int level)
    {
        // Reversing the formula used in ConvertXpToLevel for consistency
        return (int)Math.Pow(level / EXPConstant, EXPPower);
    }

    static float GetXp(ulong SteamID)
    {
        if (Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData)) return xpData.Value;
        return 0;
    }

    static int GetLevel(ulong SteamID)
    {
        return ConvertXpToLevel(GetXp(SteamID));
    }

    public static int GetLevelProgress(ulong SteamID)
    {
        float currentXP = GetXp(SteamID);
        int currentLevelXP = ConvertLevelToXp(GetLevel(SteamID));
        int nextLevelXP = ConvertLevelToXp(GetLevel(SteamID) + 1);

        double neededXP = nextLevelXP - currentLevelXP;
        double earnedXP = nextLevelXP - currentXP;

        return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
    }

    static float ApplyScalingFactor(float gainedXP, int currentLevel, int victimLevel)
    {
        float k = LevelScalingMultiplier; // You can adjust this constant to control the tapering effect
        int levelDifference = currentLevel - victimLevel;
        //float scalingFactor =
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

    static void ApplyClassBuffsAtThresholds(Entity characterEntity, ulong SteamID, DebugEventsSystem debugEventsSystem, FromCharacter fromCharacter)
    {
        ServerGameManager serverGameManager = Core.ServerGameManager;
        var buffs = GetClassBuffs(SteamID);
        //int levelStep = 20;
        if (buffs.Count == 0) return;
        int levelStep = MaxPlayerLevel / buffs.Count;

        int playerLevel = Core.DataStructures.PlayerExperience[SteamID].Key;
        if (playerLevel % levelStep == 0 && playerLevel / levelStep <= buffs.Count)
        {
            int buffIndex = playerLevel / levelStep - 1;

            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = new(buffs[buffIndex])
            };
            debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            if (serverGameManager.TryGetBuff(characterEntity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity firstBuff)) // if present, modify based on prestige level
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

    static void HandleBloodBuff(Entity buff, int prestigeLevel = 0)
    {
        // so at every prestige need to take away and reapply buff with new values
        //Core.Log.LogInfo($"Handling blood buff... {buff.Read<PrefabGUID>().GetPrefabName()}");

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
            buff.Write(bruteAttackSpeedBonus);
            return;
        }
    }

    public static bool HandleClassChangeItem(ChatCommandContext ctx, Dictionary<PlayerClasses, (List<int>, List<int>)> classes, ulong steamId)
    {
        PrefabGUID item = new(Plugin.ChangeClassItem.Value);
        int quantity = Plugin.ChangeClassItemQuantity.Value;

        if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, ctx.User.LocalCharacter._Entity, out var inventoryEntity) ||
            Core.ServerGameManager.GetInventoryItemCount(inventoryEntity, item) < quantity)
        {
            HandleReply(ctx, $"You do not have the required item to change classes ({item.GetPrefabName()}x{quantity})");
            return false;
        }

        if (!Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
        {
            HandleReply(ctx, $"Failed to remove the required item ({item.GetPrefabName()}x{quantity})");
            return false;
        }

        RemoveClassBuffs(ctx, steamId);

        return true;
    }

    public static void UpdateClassData(Entity character, PlayerClasses parsedClassType, Dictionary<PlayerClasses, (List<int>, List<int>)> classes, ulong steamId)
    {
        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
        var weaponConfigEntry = ClassWeaponBloodMap[parsedClassType].Item1;
        var bloodConfigEntry = ClassWeaponBloodMap[parsedClassType].Item2;
        var classWeaponStats = Core.ParseConfigString(weaponConfigEntry);
        var classBloodStats = Core.ParseConfigString(bloodConfigEntry);

        classes[parsedClassType] = (classWeaponStats, classBloodStats);

        Core.DataStructures.PlayerClasses[steamId] = classes;
        Core.DataStructures.SavePlayerClasses();

        FromCharacter fromCharacter = new()
        {
            Character = character,
            User = character.Read<PlayerCharacter>().UserEntity,
        };

        ApplyClassBuffs(character, steamId, debugEventsSystem, fromCharacter);
    }

    public static void ApplyClassBuffs(Entity character, ulong steamId, DebugEventsSystem debugEventsSystem, FromCharacter fromCharacter)
    {
        ServerGameManager serverGameManager = Core.ServerGameManager;
        var buffs = GetClassBuffs(steamId);

        if (buffs.Count == 0) return;
        int levelStep = MaxPlayerLevel / buffs.Count;

        int playerLevel = Core.DataStructures.PlayerExperience[steamId].Key;
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

                debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                if (serverGameManager.TryGetBuff(character, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity firstBuff))
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
                        firstBuff.Write(new Buff_Persists_Through_Death());
                    }
                    if (firstBuff.Has<LifeTime>())
                    {
                        LifeTime lifeTime = firstBuff.Read<LifeTime>();
                        lifeTime.Duration = -1;
                        lifeTime.EndAction = LifeTimeEndAction.None;
                        firstBuff.Write(lifeTime);
                    }
                    //Core.Log.LogInfo($"Proceeding to class check for on hit extra buff components to add...");
                    /*
                    if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) && classes.Keys.Count > 0)
                    {
                        PlayerClasses playerClass = classes.Keys.FirstOrDefault();
                        switch (i)
                        {
                            case 0:
                                Core.Log.LogInfo($"Applying on damage dealt debuff components for {playerClass}...");
                                if (!firstBuff.Has<Buff_ApplyBuffOnDamageTypeDealt_DataShared>()) firstBuff.Add<Buff_ApplyBuffOnDamageTypeDealt_DataShared>();
                                Buff_ApplyBuffOnDamageTypeDealt_DataShared applyOnHit = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[new(-429891372)].Read<Buff_ApplyBuffOnDamageTypeDealt_DataShared>();
                                applyOnHit.ProcBuff = ClassApplyBuffOnDamageDealtMap[playerClass];
                                applyOnHit.ProcChance = 1f;
                                firstBuff.Write(applyOnHit);
                                Core.Log.LogInfo($"Applied {ClassApplyBuffOnDamageDealtMap[playerClass].LookupName()} to blood buff {applyBuffDebugEvent.BuffPrefabGUID.LookupName()} for {playerClass}");
                                break;

                            case 1:
                                break;

                            case 2:
                                break;

                            case 3:
                                break;
                        }
                    }
                    */
                }
            }
        }
    }

    public static void RemoveClassBuffs(ChatCommandContext ctx, ulong steamId)
    {
        List<int> buffs = GetClassBuffs(steamId);
        var buffSpawner = BuffUtility.BuffSpawner.Create(Core.ServerGameManager);
        var entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();

        if (buffs.Count == 0) return;

        for (int i = 0; i < buffs.Count; i++)
        {
            if (buffs[i] == 0) continue;
            PrefabGUID buffPrefab = new(buffs[i]);
            if (Core.ServerGameManager.TryGetBuff(ctx.Event.SenderCharacterEntity, buffPrefab.ToIdentifier(), out var buff))
            {
                BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, buffPrefab.ToIdentifier(), ctx.Event.SenderCharacterEntity);
            }
        }
    }
    public static Dictionary<PrefabGUID, int> GetSpellPrefabs()
    {
        Dictionary<PrefabGUID, int> spellPrefabs = [];
        foreach (LevelingSystem.PlayerClasses playerClass in Enum.GetValues(typeof(LevelingSystem.PlayerClasses)))
        {
            if (!string.IsNullOrEmpty(LevelingSystem.ClassSpellsMap[playerClass])) Core.ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]).Select((x, index) => new { Prefab = new PrefabGUID(x), Index = index }).ToList().ForEach(x => spellPrefabs.TryAdd(x.Prefab, x.Index));
        }
        return spellPrefabs;
    }
    public static List<int> GetClassBuffs(ulong steamId)
    {
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) && classes.Keys.Count > 0)
        {
            var playerClass = classes.Keys.FirstOrDefault();
            return Core.ParseConfigString(LevelingSystem.ClassPrestigeBuffsMap[playerClass]);
        }
        return [];
    }

    public static List<int> GetClassSpells(ulong steamId)
    {
        if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) && classes.Keys.Count > 0)
        {
            var playerClass = classes.Keys.FirstOrDefault();
            return Core.ParseConfigString(LevelingSystem.ClassSpellsMap[playerClass]);
        }
        return [];
    }

    public class AllianceUtilities
    {
        public static bool CheckClanLeadership(ChatCommandContext ctx, Entity ownerClanEntity)
        {
            if (ownerClanEntity.Equals(Entity.Null))
            {
                return true;
            }

            Entity userEntity = ctx.Event.SenderUserEntity;
            return userEntity.TryGetComponent(out ClanRole clanRole) && !clanRole.Value.Equals(ClanRoleEnum.Leader);
        }

        public static void HandleClanAlliance(ChatCommandContext ctx, ulong ownerId, string name)
        {
            if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId))
            {
                Core.DataStructures.PlayerAlliances[ownerId] = [];
            }

            HashSet<string> alliance = Core.DataStructures.PlayerAlliances[ownerId];
            HashSet<string> members = [];
            Entity clanEntity = PlayerService.GetClanByName(name);

            if (clanEntity.Equals(Entity.Null))
            {
                HandleReply(ctx, "Clan/leader not found...");
                return;
            }

            if (!TryAddClanMembers(ctx, ownerId, clanEntity, members))
            {
                return;
            }

            AddMembersToAlliance(ctx, alliance, members);
        }

        public static bool TryAddClanMembers(ChatCommandContext ctx, ulong ownerId, Entity clanEntity, HashSet<string> members)
        {
            var clanBuffer = clanEntity.ReadBuffer<ClanMemberStatus>();
            int leaderIndex = GetClanLeaderIndex(clanBuffer);

            if (leaderIndex == -1)
            {
                HandleReply(ctx, "Couldn't find clan leader to verify consent.");
                return false;
            }

            var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
            for (int i = 0; i < userBuffer.Length; i++)
            {
                var users = userBuffer[i];
                User user = users.UserEntity.Read<User>();

                if (i == leaderIndex && !IsClanLeaderInvitesEnabled(user))
                {
                    HandleReply(ctx, "Clan leader does not have alliances invites enabled.");
                    return false;
                }
                members.Add(user.CharacterName.Value);
            }
            return true;
        }
        public static void RemoveClanFromAlliance(ChatCommandContext ctx, HashSet<string> alliance, string clanName)
        {
            List<string> removed = [];
            Entity clanEntity = PlayerService.GetClanByName(clanName);

            if (clanEntity.Equals(Entity.Null))
            {
                HandleReply(ctx, "Clan not found...");
                return;
            }

            foreach (string memberName in alliance)
            {
                string playerKey = PlayerService.playerCache.Keys.FirstOrDefault(key => key.Equals(memberName, StringComparison.OrdinalIgnoreCase));
                if (PlayerService.playerCache.TryGetValue(playerKey, out var player))
                {
                    Entity playerClanEntity = player.Read<User>().ClanEntity._Entity;
                    ClanTeam clanTeam = playerClanEntity.Read<ClanTeam>();
                    if (clanTeam.Name.Value.Equals(clanName, StringComparison.OrdinalIgnoreCase))
                    {
                        alliance.Remove(memberName);
                        removed.Add(memberName);
                    }
                }
            }
            string replyMessage = removed.Count > 0 ? string.Join(", ", removed.Select(member => $"<color=green>{member}</color>")) : "No members from clan found to remove.";
            if (removed.Count > 0) replyMessage += " removed from alliance.";
            HandleReply(ctx, replyMessage);
            Core.DataStructures.SavePlayerAlliances();
        }
        public static int GetClanLeaderIndex(DynamicBuffer<ClanMemberStatus> clanBuffer)
        {
            for (int i = 0; i < clanBuffer.Length; i++)
            {
                if (clanBuffer[i].ClanRole.Equals(ClanRoleEnum.Leader))
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool IsClanLeaderInvitesEnabled(User user)
        {
            return Core.DataStructures.PlayerBools.TryGetValue(user.PlatformId, out var bools) && bools["Grouping"];
        }

        public static void AddMembersToAlliance(ChatCommandContext ctx, HashSet<string> alliance, HashSet<string> members)
        {
            if (members.Count > 0 && alliance.Count + members.Count < Plugin.MaxAllianceSize.Value)
            {
                string membersAdded = string.Join(", ", members.Select(member => $"<color=green>{member}</color>"));
                alliance.UnionWith(members);
                HandleReply(ctx, $"{membersAdded} were added to the alliance.");
                Core.DataStructures.SavePlayerAlliances();
            }
            else if (members.Count == 0)
            {
                HandleReply(ctx, "Couldn't find any clan members to add.");
            }
            else
            {
                HandleReply(ctx, "Alliance would exceed max size by adding found clan members.");
            }
        }

        public static void HandlePlayerAlliance(ChatCommandContext ctx, ulong ownerId, string name)
        {
            if (PlayerService.playerCache.TryGetValue(name, out Entity player))
            {
                if (player.Equals(Entity.Null))
                {
                    HandleReply(ctx, "Player not found...");
                    return;
                }

                User foundUser = player.Read<User>();
                if (foundUser.PlatformId == ownerId)
                {
                    HandleReply(ctx, "Player not found...");
                    return;
                }

                string playerName = foundUser.CharacterName.Value;
                if (IsPlayerEligibleForAlliance(foundUser, ownerId, playerName))
                {
                    AddPlayerToAlliance(ctx, ownerId, playerName);
                }
                else
                {
                    HandleReply(ctx, $"<color=green>{playerName}</color> does not have alliances enabled or is already in an alliance.");
                }
            }
            else
            {
                HandleReply(ctx, "Player not found...");
            }  
        }

        public static bool IsPlayerEligibleForAlliance(User foundUser, ulong ownerId, string playerName)
        {
            if (Core.DataStructures.PlayerBools.TryGetValue(foundUser.PlatformId, out var bools) && bools["Grouping"])
            {
                if (!Core.DataStructures.PlayerAlliances.ContainsKey(foundUser.PlatformId) && (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId) || !Core.DataStructures.PlayerAlliances[ownerId].Contains(playerName)))
                {
                    bools["Grouping"] = false;
                    Core.DataStructures.SavePlayerBools();
                    return true;
                }
            }
            return false;
        }

        public static void AddPlayerToAlliance(ChatCommandContext ctx, ulong ownerId, string playerName)
        {
            if (!Core.DataStructures.PlayerAlliances.ContainsKey(ownerId))
            {
                Core.DataStructures.PlayerAlliances[ownerId] = [];
            }

            string ownerName = ctx.Event.User.CharacterName.Value;
            HashSet<string> alliance = Core.DataStructures.PlayerAlliances[ownerId];

            if (alliance.Count < Plugin.MaxAllianceSize.Value && !alliance.Contains(playerName))
            {
                alliance.Add(playerName);

                if (!alliance.Contains(ownerName)) // add owner to alliance for simplified processing elsewhere
                {
                    alliance.Add(ownerName);
                }

                Core.DataStructures.SavePlayerAlliances();
                HandleReply(ctx, $"<color=green>{playerName}</color> added to alliance.");
            }
            else
            {
                HandleReply(ctx, $"Alliance is full or <color=green>{playerName}</color> is already in the alliance.");
            }
        }
        public static void RemovePlayerFromAlliance(ChatCommandContext ctx, HashSet<string> alliance, string playerName)
        {
            if (alliance.FirstOrDefault(n => n.Equals(playerName, StringComparison.OrdinalIgnoreCase)) != null)
            {
                alliance.Remove(playerName);
                Core.DataStructures.SavePlayerAlliances();
                HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> removed from alliance.");
            }
            else
            {
                HandleReply(ctx, $"<color=green>{char.ToUpper(playerName[0]) + playerName[1..].ToLower()}</color> not found in alliance.");
            }
        }
        public static void ListPersonalAllianceMembers(ChatCommandContext ctx, Dictionary<ulong, HashSet<string>> playerAlliances)
        {
            ulong ownerId = ctx.Event.User.PlatformId;
            string playerName = ctx.Event.User.CharacterName.Value;
            HashSet<string> members = playerAlliances.ContainsKey(ownerId) ? playerAlliances[ownerId] : playerAlliances.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();
            string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")) : "No members in alliance.";
            HandleReply(ctx, replyMessage);
        }

        public static void ListAllianceMembersByName(ChatCommandContext ctx, string name, Dictionary<ulong, HashSet<string>> playerAlliances)
        {
            string playerKey = PlayerService.playerCache.Keys.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(playerKey) && PlayerService.playerCache.TryGetValue(playerKey, out var player))
            {
                ulong steamId = player.Read<User>().PlatformId;
                string playerName = player.Read<User>().CharacterName.Value;
                HashSet<string> members = playerAlliances.ContainsKey(steamId) ? playerAlliances[steamId] : playerAlliances.Where(groupEntry => groupEntry.Value.Contains(playerName)).SelectMany(groupEntry => groupEntry.Value).ToHashSet();
                string replyMessage = members.Count > 0 ? string.Join(", ", members.Select(member => $"<color=green>{member}</color>")) : "No members in alliance.";
                HandleReply(ctx, replyMessage);
            }
            else
            {
                foreach (var groupEntry in playerAlliances)
                {
                    playerKey = groupEntry.Value.FirstOrDefault(key => key.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (!string.IsNullOrEmpty(playerKey))
                    {
                        string replyMessage = groupEntry.Value.Count > 0 ? string.Join(", ", groupEntry.Value.Select(member => $"<color=green>{member}</color>")) : "No members in alliance.";
                        HandleReply(ctx, replyMessage);
                        return;
                    }
                }
            }
        }
    }
}