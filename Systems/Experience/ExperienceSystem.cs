using Cobalt.Core;
using Cobalt.Hooks;
using Cobalt.Systems.Expertise;
using ProjectM;
using ProjectM.Network;
using Steamworks;
using Stunlock.Core;
using Unity.Entities;

namespace Cobalt.Systems.Experience
{
    public class ExperienceSystem
    {
        public static readonly float EXPMultiplier = 5; // multipler for normal units
        public static readonly float VBloodMultiplier = 15; // multiplier for VBlood units
        public static readonly float EXPConstant = 0.1f; // constant for calculating level from xp
        public static readonly int EXPPower = 2; // power for calculating level from xp
        public static readonly int MaxLevel = 90; // maximum level

        private static readonly PrefabGUID levelUpBuff = new(-1133938228);

        public static void EXPMonitor(Entity killerEntity, Entity victimEntity)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            if (!IsValidVictim(entityManager, victimEntity)) return;
            UpdateEXP(entityManager, killerEntity, victimEntity);
        }

        private static bool IsValidVictim(EntityManager entityManager, Entity victimEntity)
        {
            return !entityManager.HasComponent<Minion>(victimEntity) && entityManager.HasComponent<UnitLevel>(victimEntity);
        }

        private static void UpdateEXP(EntityManager entityManager, Entity killerEntity, Entity victimEntity)
        {
            PlayerCharacter player = entityManager.GetComponentData<PlayerCharacter>(killerEntity);
            Entity userEntity = player.UserEntity;
            User user = entityManager.GetComponentData<User>(userEntity);
            ulong steamId = user.PlatformId;

            if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData) && xpData.Key >= MaxLevel) return; // Check if already at max level

            ProcessExperienceGain(entityManager, killerEntity, victimEntity, steamId);
        }

        private static void ProcessExperienceGain(EntityManager entityManager, Entity killerEntity, Entity victimEntity, ulong SteamID)
        {
            UnitLevel victimLevel = entityManager.GetComponentData<UnitLevel>(victimEntity);
            bool isVBlood = IsVBlood(entityManager, victimEntity);

            int gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);
            int currentLevel = DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData) ? xpData.Key : 0;
            UpdatePlayerExperience(SteamID, gainedXP);

            CheckAndHandleLevelUp(killerEntity, SteamID, gainedXP, currentLevel);
        }

        private static bool IsVBlood(EntityManager entityManager, Entity victimEntity)
        {
            return entityManager.HasComponent<VBloodConsumeSource>(victimEntity);
        }

        private static int CalculateExperienceGained(int victimLevel, bool isVBlood)
        {
            int baseXP = victimLevel;
            if (isVBlood) baseXP *= (int)VBloodMultiplier;
            return (int)(baseXP * EXPMultiplier);
        }

        private static void UpdatePlayerExperience(ulong SteamID, int gainedXP)
        {
            // Retrieve the current experience and level from the player's data structure.
            if (!DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData))
            {
                xpData = new KeyValuePair<int, float>(0, 0); // Initialize if not present
            }

            // Calculate new experience amount
            float newExperience = xpData.Value + gainedXP;

            // Check and update the level based on new experience
            int newLevel = ConvertXpToLevel(newExperience);
            if (newLevel > MaxLevel)
            {
                newLevel = MaxLevel; // Cap the level at the maximum
                newExperience = ConvertLevelToXp(MaxLevel); // Adjust the XP to the max level's XP
            }

            // Update the level and experience in the data structure
            DataStructures.PlayerExperience[SteamID] = new KeyValuePair<int, float>(newLevel, newExperience);

            // Save the experience data
            DataStructures.SavePlayerExperience();
        }

        private static void CheckAndHandleLevelUp(Entity characterEntity, ulong SteamID, int gainedXP, int currentLevel)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            Entity userEntity = characterEntity.Read<PlayerCharacter>().UserEntity;

            bool leveledUp = CheckForLevelUp(SteamID, currentLevel);
            //Plugin.Log.LogInfo($"Leveled up: {leveledUp}");
            if (leveledUp)
            {
                //Plugin.Log.LogInfo("Applying level up buff...");
                DebugEventsSystem debugEventsSystem = VWorld.Server.GetExistingSystemManaged<DebugEventsSystem>();
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
            NotifyPlayer(entityManager, userEntity, SteamID, gainedXP, leveledUp);
        }

        private static bool CheckForLevelUp(ulong SteamID, int currentLevel)
        {
            int newLevel = ConvertXpToLevel(DataStructures.PlayerExperience[SteamID].Value);
            if (newLevel > currentLevel)
            {
                return true;
            }
            return false;
        }

        private static void NotifyPlayer(EntityManager entityManager, Entity userEntity, ulong SteamID, int gainedXP, bool leveledUp)
        {
            User user = entityManager.GetComponentData<User>(userEntity);
            if (leveledUp)
            {
                int newLevel = DataStructures.PlayerExperience[SteamID].Key;
                GearOverride.SetLevel(userEntity.Read<User>().LocalCharacter._Entity);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
            }
            else
            {
                if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["ExperienceLogging"])
                {
                    int levelProgress = GetLevelProgress(SteamID);
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{gainedXP}</color> <color=#FFC0CB>experience</color> (<color=white>{levelProgress}%</color>)");
                }
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

        private static float GetXp(ulong SteamID)
        {
            if (DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData)) return xpData.Value;
            else
            {
                return 0;
            }
        }

        private static int GetLevel(ulong SteamID)
        {
            return ConvertXpToLevel(GetXp(SteamID));
        }

        private static int GetLevelProgress(ulong SteamID)
        {
            float currentXP = GetXp(SteamID);
            int currentLevelXP = ConvertLevelToXp(GetLevel(SteamID));
            int nextLevelXP = ConvertLevelToXp(GetLevel(SteamID) + 1);

            double neededXP = nextLevelXP - currentLevelXP;
            double earnedXP = nextLevelXP - currentXP;

            return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
        }
    }
}