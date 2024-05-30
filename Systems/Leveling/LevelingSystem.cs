﻿using Bloodcraft.Patches;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Steamworks;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Bloodcraft.Core;
using static Bloodcraft.Systems.Legacy.BloodSystem;

namespace Bloodcraft.Systems.Experience
{
    public class LevelingSystem
    {
        static readonly float UnitMultiplier = Plugin.UnitLevelingMultiplier.Value; // multipler for normal units
        static readonly float VBloodMultiplier = Plugin.VBloodLevelingMultiplier.Value; // multiplier for VBlood units
        static readonly float EXPConstant = 0.1f; // constant for calculating level from xp
        static readonly int EXPPower = 2; // power for calculating level from xp
        static readonly int MaxPlayerLevel = Plugin.MaxPlayerLevel.Value; // maximum level
        static readonly float GroupMultiplier = Plugin.GroupLevelingMultiplier.Value; // multiplier for group kills
        static readonly float LevelScalingMultiplier = Plugin.LevelScalingMultiplier.Value; // scaling multiplier for experience, increases experience at lower levels and tapers off as level increases

        static readonly PrefabGUID levelUpBuff = new(-1133938228);

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
            int groupMultiplier = 1;
            HashSet<Entity> participants = GetParticipants(killerEntity, userEntity); // want list of participants to process experience for
            if (participants.Count > 1) groupMultiplier = (int)GroupMultiplier; // if more than 1 participant, apply group multiplier
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

        static void ProcessExperienceGain(EntityManager entityManager, Entity killerEntity, Entity victimEntity, ulong SteamID, int groupMultiplier)
        {
            UnitLevel victimLevel = entityManager.GetComponentData<UnitLevel>(victimEntity);
            bool isVBlood = IsVBlood(entityManager, victimEntity);

            float gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);
            int currentLevel = Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData) ? xpData.Key : 0;
            gainedXP = ApplyScalingFactor(gainedXP, currentLevel);

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(SteamID, out var prestiges) && prestiges.TryGetValue(PrestigeSystem.PrestigeType.Experience, out var PrestigeData) && PrestigeData > 0)
            {
                float reductionFactor = 1.0f / (1 + PrestigeData * Plugin.PrestigeRatesReducer.Value);
                gainedXP *= reductionFactor;
            }


            UpdatePlayerExperience(SteamID, gainedXP * groupMultiplier);

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
        static float ApplyScalingFactor(float gainedXP, int currentLevel)
        {
            float k = LevelScalingMultiplier; // You can adjust this constant to control the tapering effect
            float scalingFactor = 1 / (1 + k * currentLevel);
            return gainedXP * scalingFactor;
        }

    }
}