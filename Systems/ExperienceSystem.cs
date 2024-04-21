using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Entities;

namespace Cobalt.Systems
{
    public class ExperienceSystem
    {
        public static readonly float EXPMultiplier = 3; // multipler for normal units
        public static readonly float VBloodMultiplier = 10; // multiplier for VBlood units
        public static readonly float EXPConstant = 0.1f; // constant for calculating level from xp
        public static readonly int EXPPower = 2; // power for calculating level from xp
        public static readonly int MaxLevel = 90; // maximum level

        private static readonly PrefabGUID vBloodType = new(1557174542);

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
            ulong SteamID = user.PlatformId;

            if (!DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData))
            {
                xpData = new KeyValuePair<int, float>(0, 0); // Initialize if not present
            }

            if (xpData.Key >= MaxLevel) return; // Check if already at max level

            ProcessExperienceGain(entityManager, killerEntity, victimEntity, SteamID);
        }

        private static void ProcessExperienceGain(EntityManager entityManager, Entity killerEntity, Entity victimEntity, ulong SteamID)
        {
            UnitLevel victimLevel = entityManager.GetComponentData<UnitLevel>(victimEntity);
            bool isVBlood = IsVBlood(entityManager, victimEntity);

            int gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);

            UpdatePlayerExperience(SteamID, gainedXP);

            CheckAndHandleLevelUp(killerEntity, SteamID, gainedXP);
        }

        private static bool IsVBlood(EntityManager entityManager, Entity victimEntity)
        {
            return entityManager.HasComponent<BloodConsumeSource>(victimEntity) && entityManager.GetComponentData<BloodConsumeSource>(victimEntity).UnitBloodType.Equals(vBloodType);
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

        private static void CheckAndHandleLevelUp(Entity characterEntity, ulong SteamID, int gainedXP)
        {
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
            EntityManager entityManager = VWorld.Server.EntityManager;
            Entity userEntity = characterEntity.Read<PlayerCharacter>().UserEntity;
            int currentLevel = DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData) ? xpData.Key : 0;
            bool leveledUp = CheckForLevelUp(SteamID, currentLevel);
            if (leveledUp)
            {
                // apply level up buff here
                //Helper.BuffPlayerByName(name, levelUpBuff);
                serverGameManager.InstantiateBuffEntityImmediate(userEntity, characterEntity, levelUpBuff);
            }
            NotifyPlayer(entityManager, userEntity, SteamID, gainedXP, leveledUp);
        }

        private static bool CheckForLevelUp(ulong SteamID, int currentLevel)
        {
            int newLevel = ConvertXpToLevel(DataStructures.PlayerExperience[SteamID].Value);
            if (newLevel > currentLevel)
            {
                DataStructures.PlayerExperience[SteamID] = new KeyValuePair<int, float>(newLevel, DataStructures.PlayerExperience[SteamID].Value);

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
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Congratulations! You've reached level <color=yellow>{newLevel}</color>!");
            }
            else
            {
                int levelProgress = GetLevelProgress(SteamID);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"You gained <color=white>{gainedXP}</color> experience points. You are now <color=yellow>{levelProgress}</color>% towards the next level.");
            }
        }

        private static int ConvertXpToLevel(float xp)
        {
            // Assuming a basic square root scaling for experience to level conversion
            return (int)(EXPConstant * Math.Sqrt(xp));
        }

        private static int ConvertLevelToXp(int level)
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