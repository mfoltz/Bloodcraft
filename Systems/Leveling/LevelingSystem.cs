using Cobalt.Hooks;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using static Cobalt.Core;

namespace Cobalt.Systems.Experience
{
    public class LevelingSystem
    {
        public static readonly int UnitMultiplier = Plugin.UnitLevelingMultiplier.Value; // multipler for normal units
        public static readonly int VBloodMultiplier = Plugin.VBloodLevelingMultiplier.Value; // multiplier for VBlood units
        public static readonly float EXPConstant = 0.1f; // constant for calculating level from xp
        public static readonly int EXPPower = 2; // power for calculating level from xp
        public static readonly int MaxLevel = Plugin.MaxPlayerLevel.Value; // maximum level
        public static readonly int GroupMultiplier = Plugin.GroupLevelingMultiplier.Value; // multiplier for group kills

        private static readonly PrefabGUID levelUpBuff = new(-1133938228);

        public static void EXPMonitor(Entity killerEntity, Entity victimEntity)
        {
            EntityManager entityManager = Core.Server.EntityManager;
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
            int groupMultiplier = 1;
            HashSet<Entity> participants = GetParticipants(killerEntity, userEntity); // want list of participants to process experience for
            if (participants.Count > 1) groupMultiplier = (int)GroupMultiplier; // if more than 1 participant, apply group multiplier
            foreach (Entity participant in participants)
            {
                ulong steamId = participant.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData) && xpData.Key >= MaxLevel) continue; // Check if already at max level
                ProcessExperienceGain(entityManager, participant, victimEntity, steamId, groupMultiplier);
            }
        }

        private static HashSet<Entity> GetParticipants(Entity killer, Entity userEntity)
        {
            float3 killerPosition = killer.Read<LocalToWorld>().Position;
            User killerUser = userEntity.Read<User>();
            HashSet<Entity> players = [killer];
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

        private static void ProcessExperienceGain(EntityManager entityManager, Entity killerEntity, Entity victimEntity, ulong SteamID, int groupMultiplier)
        {
            UnitLevel victimLevel = entityManager.GetComponentData<UnitLevel>(victimEntity);
            bool isVBlood = IsVBlood(entityManager, victimEntity);

            int gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);
            int currentLevel = Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData) ? xpData.Key : 0;
            UpdatePlayerExperience(SteamID, gainedXP * groupMultiplier);

            CheckAndHandleLevelUp(killerEntity, SteamID, gainedXP, currentLevel);
        }

        private static bool IsVBlood(EntityManager entityManager, Entity victimEntity)
        {
            return entityManager.HasComponent<VBloodConsumeSource>(victimEntity);
        }

        private static int CalculateExperienceGained(int victimLevel, bool isVBlood)
        {
            int baseXP = victimLevel;
            if (isVBlood) return baseXP * VBloodMultiplier;
            return baseXP * UnitMultiplier;
        }

        private static void UpdatePlayerExperience(ulong SteamID, int gainedXP)
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
            if (newLevel > MaxLevel)
            {
                newLevel = MaxLevel; // Cap the level at the maximum
                newExperience = ConvertLevelToXp(MaxLevel); // Adjust the XP to the max level's XP
            }

            // Update the level and experience in the data structure
            Core.DataStructures.PlayerExperience[SteamID] = new KeyValuePair<int, float>(newLevel, newExperience);

            // Save the experience data
            Core.DataStructures.SavePlayerExperience();
        }

        private static void CheckAndHandleLevelUp(Entity characterEntity, ulong SteamID, int gainedXP, int currentLevel)
        {
            EntityManager entityManager = Core.Server.EntityManager;
            Entity userEntity = characterEntity.Read<PlayerCharacter>().UserEntity;

            bool leveledUp = CheckForLevelUp(SteamID, currentLevel);
            //Plugin.Log.LogInfo($"Leveled up: {leveledUp}");
            if (leveledUp)
            {
                //Plugin.Log.LogInfo("Applying level up buff...");
                DebugEventsSystem debugEventsSystem = Core.Server.GetExistingSystemManaged<DebugEventsSystem>();
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
            int newLevel = ConvertXpToLevel(Core.DataStructures.PlayerExperience[SteamID].Value);
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
                int newLevel = Core.DataStructures.PlayerExperience[SteamID].Key;
                GearOverride.SetLevel(userEntity.Read<User>().LocalCharacter._Entity);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Congratulations, you've reached level <color=white>{newLevel}</color>!");
            }
            else
            {
                if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["ExperienceLogging"])
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
            if (Core.DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData)) return xpData.Value;
            else
            {
                return 0;
            }
        }

        private static int GetLevel(ulong SteamID)
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
    }
}