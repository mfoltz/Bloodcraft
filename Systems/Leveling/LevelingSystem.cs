using Bloodcraft.Patches;
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
using static Bloodcraft.Core;

namespace Bloodcraft.Systems.Experience
{
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

            if (IsVBlood(Core.EntityManager, victimEntity))
            {
                ProcessExperienceGain(entityManager, killerEntity, victimEntity, userEntity.Read<User>().PlatformId, 1); // override multiplier since this should just be a solo kill and skip getting participants for vbloods
                return;
            }

            HashSet<Entity> participants = GetParticipants(killerEntity, userEntity); // want list of participants to process experience for
            if (participants.Count > 1) groupMultiplier = GroupMultiplier; // if more than 1 participant, apply group multiplier
            foreach (Entity participant in participants)
            {
                ulong steamId = participant.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData) && xpData.Key >= MaxPlayerLevel) continue; // Check if already at max level
                ProcessExperienceGain(entityManager, participant, victimEntity, steamId, groupMultiplier);
            }
        }
        static HashSet<Entity> GetParticipants(Entity killer, Entity userEntity)
        {
            float3 killerPosition = killer.Read<LocalToWorld>().Position;
            User killerUser = userEntity.Read<User>();
            HashSet<Entity> players = [killer];
            // check for exp sharing group here too
            if (Core.DataStructures.PlayerGroups.TryGetValue(killerUser.PlatformId, out var group)) //check if group leader, add members
            {
                foreach (Entity player in group)
                {
                    var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<LocalToWorld>().Position);
                    if (distance > 25f) continue;
                    players.Add(player);
                }
            }
            else
            {
                foreach (var groupEntry in Core.DataStructures.PlayerGroups)
                {
                    if (groupEntry.Value.Contains(killer) && groupEntry.Key != killerUser.PlatformId)
                    {
                        foreach (Entity player in groupEntry.Value)
                        {
                            var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<LocalToWorld>().Position);
                            if (distance > 25f) continue;
                            players.Add(player);
                        }
                        break;
                    }
                }
            }
            if (killerUser.ClanEntity._Entity.Equals(Entity.Null)) return players;
            Entity clanEntity = killerUser.ClanEntity._Entity;
            var userBuffer = clanEntity.ReadBuffer<SyncToUserBuffer>();
            for (int i = 0; i < userBuffer.Length; i++)
            {
                var users = userBuffer[i];
                User user = users.UserEntity.Read<User>();
                if (!user.IsConnected) continue;
                Entity player = user.LocalCharacter._Entity;
                var distance = UnityEngine.Vector3.Distance(killerPosition, player.Read<LocalToWorld>().Position);
                if (distance > 25f) continue;
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

                ApplyClassBuffsAtThresholds(characterEntity, SteamID, debugEventsSystem, fromCharacter); // get prestige level before this and do prestigeClassBuff version of method with if/else
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
                if (newLevel < MaxPlayerLevel) ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
            }
            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["ExperienceLogging"])
            {
                int levelProgress = GetLevelProgress(SteamID);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{gainedXP}</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)");
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
            int levelStep = MaxPlayerLevel / buffs.Count;
            int prestigeLevel = PrestigeSystem.GetExperiencePrestigeLevel(SteamID);
            int playerLevel = Core.DataStructures.PlayerExperience[SteamID].Key;
            if (playerLevel % levelStep == 0 && playerLevel / levelStep <= buffs.Count )
            {
                int buffIndex = playerLevel / levelStep - 1;

                
                /*
                if (prestigeLevel > 0) // if present, modify based on prestige level?
                {
                    ApplyBuffDebugEvent applyBuffDebugEvent = new()
                    {
                        BuffPrefabGUID = new(-429891372) // mugging powersurge for it's components, prefectly ethical
                    };

                    debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    if (serverGameManager.TryGetBuff(characterEntity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity firstBuff)) // if present, modify based on prestige level
                    {
                        Core.Log.LogInfo($"Applied {applyBuffDebugEvent.BuffPrefabGUID.LookupName()} for class buff, modifying...");

                        Buff buff = firstBuff.Read<Buff>();
                        buff.BuffType = BuffType.Parallel;
                        firstBuff.Write(buff);

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
                        if (Core.DataStructures.PlayerClasses.TryGetValue(SteamID, out var classes) && classes.Keys.Count > 0) // so basically if prestiged already and at the level threshold again, handle the buff matching the index and scale for prestige
                        {
                            PlayerClasses playerClass = classes.Keys.FirstOrDefault();
                            Buff_ApplyBuffOnDamageTypeDealt_DataShared onHitBuff = firstBuff.Read<Buff_ApplyBuffOnDamageTypeDealt_DataShared>();
                            onHitBuff.ProcBuff = ClassApplyBuffOnDamageDealtMap[playerClass];
                            onHitBuff.ProcChance = 1;
                            firstBuff.Write(onHitBuff);

                            Core.Log.LogInfo($"Applied {onHitBuff.ProcBuff.GetPrefabName()} to class buff, removing uneeded components...");

                            if (firstBuff.Has<Buff_EmpowerDamageDealtByType_DataShared>())
                            {
                                firstBuff.Remove<Buff_EmpowerDamageDealtByType_DataShared>();
                            }
                            if (firstBuff.Has<ModifyMovementSpeedBuff>())
                            {
                                firstBuff.Remove<ModifyMovementSpeedBuff>();
                            }
                            if (firstBuff.Has<CreateGameplayEventOnBuffReapply>())
                            {
                                firstBuff.Remove<CreateGameplayEventOnBuffReapply>();
                            }
                            if (firstBuff.Has<AdjustLifetimeOnGameplayEvent>())
                            {
                                firstBuff.Remove<AdjustLifetimeOnGameplayEvent>();
                            }
                            if (firstBuff.Has<ApplyBuffOnGameplayEvent>())
                            {
                                firstBuff.Remove<CreateGameplayEventsOnSpawn>();
                            }
                            if (firstBuff.Has<SpellModSetComponent>())
                            {
                                firstBuff.Remove<SpellModSetComponent>();
                            }
                            if (firstBuff.Has<ApplyBuffOnGameplayEvent>())
                            {
                                firstBuff.Remove<ApplyBuffOnGameplayEvent>();
                            }
                            if (firstBuff.Has<SpellModArithmetic>())
                            {
                                firstBuff.Remove<SpellModArithmetic>();
                            }

                           
                        }

                    }
                    // each class gets an applyBuffOnDamageTypeDealt effect? like BloodKnight gets one that has a chance to proc leech, that I could somewhat safely scale with prestige level easily
                    
                }
                */
                
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
            }

            if (buff.Has<BloodBuffScript_Brute_HealthRegenBonus>())
            {
                var bruteHealthRegenBonus = buff.Read<BloodBuffScript_Brute_HealthRegenBonus>();
                // modifications
                bruteHealthRegenBonus.RequiredBloodPercentage = 0;
                bruteHealthRegenBonus.MinHealthRegenIncrease = bruteHealthRegenBonus.MaxHealthRegenIncrease;
                buff.Write(bruteHealthRegenBonus);
            }

            if (buff.Has<BloodBuffScript_Brute_NulifyAndEmpower>())
            {
                var bruteNulifyAndEmpower = buff.Read<BloodBuffScript_Brute_NulifyAndEmpower>();
                // modifications
                bruteNulifyAndEmpower.RequiredBloodPercentage = 0;
                buff.Write(bruteNulifyAndEmpower);
            }

            if (buff.Has<BloodBuff_Brute_PhysLifeLeech_DataShared>())
            {
                var brutePhysLifeLeech = buff.Read<BloodBuff_Brute_PhysLifeLeech_DataShared>();
                // modifications
                brutePhysLifeLeech.RequiredBloodPercentage = 0;
                brutePhysLifeLeech.MinIncreasedPhysicalLifeLeech = brutePhysLifeLeech.MaxIncreasedPhysicalLifeLeech;
                buff.Write(brutePhysLifeLeech);
            }

            if (buff.Has<BloodBuff_Brute_RecoverOnKill_DataShared>())
            {
                var bruteRecoverOnKill = buff.Read<BloodBuff_Brute_RecoverOnKill_DataShared>();
                // modifications
                bruteRecoverOnKill.RequiredBloodPercentage = 0;
                bruteRecoverOnKill.MinHealingReceivedValue = bruteRecoverOnKill.MaxHealingReceivedValue;
                buff.Write(bruteRecoverOnKill);
            }

            if (buff.Has<BloodBuff_Creature_SpeedBonus_DataShared>())
            {
                var creatureSpeedBonus = buff.Read<BloodBuff_Creature_SpeedBonus_DataShared>();
                // modifications
                creatureSpeedBonus.RequiredBloodPercentage = 0;
                creatureSpeedBonus.MinMovementSpeedIncrease = creatureSpeedBonus.MaxMovementSpeedIncrease;
                buff.Write(creatureSpeedBonus);
            }

            if (buff.Has<BloodBuff_SunResistance_DataShared>())
            {
                var sunResistance = buff.Read<BloodBuff_SunResistance_DataShared>();
                // modifications
                sunResistance.RequiredBloodPercentage = 0;
                sunResistance.MinBonus = sunResistance.MaxBonus;
                buff.Write(sunResistance);
            }

            if (buff.Has<BloodBuffScript_Draculin_BloodMendBonus>())
            {
                var draculinBloodMendBonus = buff.Read<BloodBuffScript_Draculin_BloodMendBonus>();
                // modifications
                draculinBloodMendBonus.RequiredBloodPercentage = 0;
                draculinBloodMendBonus.MinBonusHealing = draculinBloodMendBonus.MaxBonusHealing;
                buff.Write(draculinBloodMendBonus);
            }
            
            if (buff.Has<Script_BloodBuff_CCReduction_DataShared>())
            {
                var bloodBuffCCReduction = buff.Read<Script_BloodBuff_CCReduction_DataShared>();
                // modifications
                bloodBuffCCReduction.RequiredBloodPercentage = 0;
                bloodBuffCCReduction.MinBonus = bloodBuffCCReduction.MaxBonus;
                buff.Write(bloodBuffCCReduction);
            }
            
            if (buff.Has<Script_BloodBuff_Draculin_ImprovedBite_DataShared>())
            {
                var draculinImprovedBite = buff.Read<Script_BloodBuff_Draculin_ImprovedBite_DataShared>();
                // modifications
                draculinImprovedBite.RequiredBloodPercentage = 0;
                buff.Write(draculinImprovedBite);
            }
            
            if (buff.Has<BloodBuffScript_LastStrike>())
            {
                var lastStrike = buff.Read<BloodBuffScript_LastStrike>();
                // modifications
                lastStrike.RequiredBloodQuality = 0;
                lastStrike.LastStrikeBonus_Min = lastStrike.LastStrikeBonus_Max;
                buff.Write(lastStrike);
            }

            if (buff.Has<BloodBuff_Draculin_SpeedBonus_DataShared>())
            {
                var draculinSpeedBonus = buff.Read<BloodBuff_Draculin_SpeedBonus_DataShared>();
                // modifications
                draculinSpeedBonus.RequiredBloodPercentage = 0;
                draculinSpeedBonus.MinMovementSpeedIncrease = draculinSpeedBonus.MaxMovementSpeedIncrease;
                buff.Write(draculinSpeedBonus);
            }

            if (buff.Has<BloodBuff_AllResistance_DataShared>())
            {
                var allResistance = buff.Read<BloodBuff_AllResistance_DataShared>();
                // modifications
                allResistance.RequiredBloodPercentage = 0;
                allResistance.MinBonus = allResistance.MaxBonus;
                buff.Write(allResistance);
            }

            if (buff.Has<BloodBuff_BiteToMutant_DataShared>())
            {
                var biteToMutant = buff.Read<BloodBuff_BiteToMutant_DataShared>();
                // modifications
                biteToMutant.RequiredBloodPercentage = 0;

                buff.Write(biteToMutant);
            }

            if (buff.Has<BloodBuff_BloodConsumption_DataShared>())
            {
                var bloodConsumption = buff.Read<BloodBuff_BloodConsumption_DataShared>();
                // modifications
                bloodConsumption.RequiredBloodPercentage = 0;
                bloodConsumption.MinBonus = bloodConsumption.MaxBonus;
                buff.Write(bloodConsumption);
            }        

            if (buff.Has<BloodBuff_HealthRegeneration_DataShared>())
            {
                var healthRegeneration = buff.Read<BloodBuff_HealthRegeneration_DataShared>();
                // modifications
                healthRegeneration.RequiredBloodPercentage = 0;
                healthRegeneration.MinBonus = healthRegeneration.MaxBonus;
                buff.Write(healthRegeneration);
            }

            if (buff.Has<BloodBuff_ApplyMovementSpeedOnShapeshift_DataShared>())
            {
                var applyMovementSpeedOnShapeshift = buff.Read<BloodBuff_ApplyMovementSpeedOnShapeshift_DataShared>();
                // modifications
                applyMovementSpeedOnShapeshift.RequiredBloodPercentage = 0;
                applyMovementSpeedOnShapeshift.MinBonus = applyMovementSpeedOnShapeshift.MaxBonus;
                buff.Write(applyMovementSpeedOnShapeshift);
            }

            if (buff.Has<BloodBuff_PrimaryAttackLifeLeech_DataShared>())
            {
                var primaryAttackLifeLeech = buff.Read<BloodBuff_PrimaryAttackLifeLeech_DataShared>();
                // modifications
                primaryAttackLifeLeech.RequiredBloodPercentage = 0;
                primaryAttackLifeLeech.MinBonus = primaryAttackLifeLeech.MaxBonus;
                buff.Write(primaryAttackLifeLeech);
            }
            
            if (buff.Has<BloodBuff_PrimaryProc_FreeCast_DataShared>())
            {
                var primaryProcFreeCast = buff.Read<BloodBuff_PrimaryProc_FreeCast_DataShared>(); // scholar one I think
                // modifications
                primaryProcFreeCast.RequiredBloodPercentage = 0;
                primaryProcFreeCast.MinBonus = primaryProcFreeCast.MaxBonus;
                buff.Write(primaryProcFreeCast);
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
                }
            }

            if (buff.Has<BloodBuff_CritAmplifyProc_DataShared>())
            {
                var critAmplifyProc = buff.Read<BloodBuff_CritAmplifyProc_DataShared>();
                // modifications
                critAmplifyProc.RequiredBloodPercentage = 0;
                critAmplifyProc.MinBonus = critAmplifyProc.MaxBonus;
                buff.Write(critAmplifyProc);
            }

            if (buff.Has<BloodBuff_PhysCritChanceBonus_DataShared>())
            {
                var physCritChanceBonus = buff.Read<BloodBuff_PhysCritChanceBonus_DataShared>();
                // modifications
                physCritChanceBonus.RequiredBloodPercentage = 0;
                physCritChanceBonus.MinPhysicalCriticalStrikeChance = physCritChanceBonus.MaxPhysicalCriticalStrikeChance;
                buff.Write(physCritChanceBonus);
            }

            if (buff.Has<BloodBuff_Rogue_SpeedBonus_DataShared>())
            {
                var rogueSpeedBonus = buff.Read<BloodBuff_Rogue_SpeedBonus_DataShared>();
                // modifications
                rogueSpeedBonus.RequiredBloodPercentage = 0;
                rogueSpeedBonus.MinMovementSpeedIncrease = rogueSpeedBonus.MaxMovementSpeedIncrease;
                buff.Write(rogueSpeedBonus);
            }

            if (buff.Has<BloodBuff_ReducedTravelCooldown_DataShared>())
            {
                var reducedTravelCooldown = buff.Read<BloodBuff_ReducedTravelCooldown_DataShared>();
                // modifications
                reducedTravelCooldown.RequiredBloodPercentage = 0;
                reducedTravelCooldown.MinBonus = reducedTravelCooldown.MaxBonus;
                buff.Write(reducedTravelCooldown);
            }
            if (buff.Has<BloodBuff_Scholar_SpellCooldown_DataShared>())
            {
                var scholarSpellCooldown = buff.Read<BloodBuff_Scholar_SpellCooldown_DataShared>();
                // modifications
                scholarSpellCooldown.RequiredBloodPercentage = 0;
                scholarSpellCooldown.MinCooldownReduction = scholarSpellCooldown.MaxCooldownReduction;
                buff.Write(scholarSpellCooldown);
            }
            if (buff.Has<BloodBuff_Scholar_SpellCritChanceBonus_DataShared>())
            {
                var scholarSpellCritChanceBonus = buff.Read<BloodBuff_Scholar_SpellCritChanceBonus_DataShared>();
                // modifications
                scholarSpellCritChanceBonus.RequiredBloodPercentage = 0;
                scholarSpellCritChanceBonus.MinSpellCriticalStrikeChance = scholarSpellCritChanceBonus.MaxSpellCriticalStrikeChance;
                buff.Write(scholarSpellCritChanceBonus);
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
                }
            }

            if (buff.Has<BloodBuff_SpellLifeLeech_DataShared>())
            {
                var spellLifeLeech = buff.Read<BloodBuff_SpellLifeLeech_DataShared>();
                // modifications
                spellLifeLeech.RequiredBloodPercentage = 0;
                spellLifeLeech.MinBonus = spellLifeLeech.MaxBonus;
                buff.Write(spellLifeLeech);
            }

            if (buff.Has<BloodBuff_Warrior_DamageReduction_DataShared>())
            {
                var warriorDamageReduction = buff.Read<BloodBuff_Warrior_DamageReduction_DataShared>();
                // modifications
                warriorDamageReduction.RequiredBloodPercentage = 0;
                warriorDamageReduction.MinDamageReduction = warriorDamageReduction.MaxDamageReduction;
                buff.Write(warriorDamageReduction);
            }

            if (buff.Has<BloodBuff_Warrior_PhysCritDamageBonus_DataShared>())
            {
                var warriorPhysCritDamageBonus = buff.Read<BloodBuff_Warrior_PhysCritDamageBonus_DataShared>();
                // modifications
                warriorPhysCritDamageBonus.RequiredBloodPercentage = 0;
                warriorPhysCritDamageBonus.MinWeaponCriticalStrikeDamageIncrease = warriorPhysCritDamageBonus.MaxWeaponCriticalStrikeDamageIncrease;
                buff.Write(warriorPhysCritDamageBonus);
            }

            if (buff.Has<BloodBuff_Warrior_PhysDamageBonus_DataShared>())
            {
                var warriorPhysDamageBonus = buff.Read<BloodBuff_Warrior_PhysDamageBonus_DataShared>();
                // modifications
                warriorPhysDamageBonus.RequiredBloodPercentage = 0;
                warriorPhysDamageBonus.MinPhysDamageIncrease = warriorPhysDamageBonus.MaxPhysDamageIncrease;
                buff.Write(warriorPhysDamageBonus);
            }

            if (buff.Has<BloodBuff_Warrior_PhysicalBonus_DataShared>())
            {
                var warriorPhysicalBonus = buff.Read<BloodBuff_Warrior_PhysicalBonus_DataShared>();
                // modifications
                warriorPhysicalBonus.RequiredBloodPercentage = 0;
                warriorPhysicalBonus.MinWeaponPowerIncrease = warriorPhysicalBonus.MaxWeaponPowerIncrease;
                buff.Write(warriorPhysicalBonus);
            }

            if (buff.Has<BloodBuff_Warrior_WeaponCooldown_DataShared>())
            {
                var warriorWeaponCooldown = buff.Read<BloodBuff_Warrior_WeaponCooldown_DataShared>();
                // modifications
                warriorWeaponCooldown.RequiredBloodPercentage = 0;
                warriorWeaponCooldown.MinCooldownReduction = warriorWeaponCooldown.MaxCooldownReduction;
                buff.Write(warriorWeaponCooldown);
            }

            if (buff.Has<BloodBuff_Brute_100_DataShared>())
            {
                var bruteEffect = buff.Read<BloodBuff_Brute_100_DataShared>();
                bruteEffect.RequiredBloodPercentage = 0;
                bruteEffect.MinHealthRegainPercentage = bruteEffect.MaxHealthRegainPercentage;
                buff.Write(bruteEffect);
            }

            if (buff.Has<BloodBuff_Rogue_100_DataShared>())
            {
                var rogueEffect = buff.Read<BloodBuff_Rogue_100_DataShared>();
                rogueEffect.RequiredBloodPercentage = 0;
                buff.Write(rogueEffect);
            }

            if (buff.Has<BloodBuff_Warrior_100_DataShared>())
            {
                var warriorEffect = buff.Read<BloodBuff_Warrior_100_DataShared>();
                warriorEffect.RequiredBloodPercentage = 0;
                buff.Write(warriorEffect);
            }

            if (buff.Has<BloodBuffScript_Scholar_MovementSpeedOnCast>())
            {
                var scholarEffect = buff.Read<BloodBuffScript_Scholar_MovementSpeedOnCast>();
                scholarEffect.RequiredBloodPercentage = 0;
                scholarEffect.ChanceToGainMovementOnCast_Min = scholarEffect.ChanceToGainMovementOnCast_Max;
                buff.Write(scholarEffect);
            }
            if (buff.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>())
            {
                var bruteAttackSpeedBonus = buff.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                bruteAttackSpeedBonus.MinValue = bruteAttackSpeedBonus.MaxValue;
                bruteAttackSpeedBonus.RequiredBloodPercentage = 0;
                buff.Write(bruteAttackSpeedBonus);
            }
        }

        public static bool HandleClassChangeItem(ChatCommandContext ctx, Dictionary<PlayerClasses, (List<int>, List<int>)> classes, ulong steamId)
        {
            PrefabGUID item = new(Plugin.ChangeClassItem.Value);
            int quantity = Plugin.ChangeClassItemQuantity.Value;

            if (!InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, ctx.User.LocalCharacter._Entity, out var inventoryEntity) ||
                Core.ServerGameManager.GetInventoryItemCount(inventoryEntity, item) < quantity)
            {
                ctx.Reply($"You do not have the required item to change classes ({item.GetPrefabName()}x{quantity})");
                return false;
            }

            if (!Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
            {
                ctx.Reply($"Failed to remove the required item ({item.GetPrefabName()}x{quantity})");
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
            int levelStep = 20;

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
    }
}