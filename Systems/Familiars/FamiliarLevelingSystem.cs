using Bloodcraft.Patches;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Core;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft.Systems.Familiars
{
    public class FamiliarLevelingSystem
    {
        static readonly float UnitMultiplier = Plugin.UnitFamiliarMultiplier.Value; // multipler for normal units
        static readonly float VBloodMultiplier = Plugin.VBloodFamiliarMultiplier.Value; // multiplier for VBlood units
        static readonly float EXPConstant = 0.1f; // constant for calculating level from xp
        static readonly int EXPPower = 2; // power for calculating level from xp
        static readonly int MaxFamiliarLevel = Plugin.MaxFamiliarLevel.Value; // maximum level

        static readonly PrefabGUID levelUpBuff = new(-1133938228);

        public static void UpdateFamiliar(Entity familiarEntity, Entity victimEntity)
        {
            EntityManager entityManager = Core.EntityManager;
            if (!IsValidVictim(entityManager, victimEntity)) return;
            HandleExperienceUpdate(entityManager, familiarEntity, victimEntity);
        }

        static bool IsValidVictim(EntityManager entityManager, Entity victimEntity)
        {
            return !entityManager.HasComponent<Minion>(victimEntity) && entityManager.HasComponent<UnitLevel>(victimEntity);
        }

        static void HandleExperienceUpdate(EntityManager entityManager, Entity familiarEntity, Entity victimEntity)
        {
            if (!entityManager.HasComponent<Follower>(familiarEntity)) return;

            Follower followerComponent = entityManager.GetComponentData<Follower>(familiarEntity);
            Entity playerEntity = followerComponent.Followed._Value;

            if (!entityManager.HasComponent<PlayerCharacter>(playerEntity)) return;

            PlayerCharacter player = entityManager.GetComponentData<PlayerCharacter>(playerEntity);
            Entity userEntity = player.UserEntity;
            ulong steamId = userEntity.Read<User>().PlatformId;
            PrefabGUID familiarUnit = familiarEntity.Read<PrefabGUID>();
            int familiarId = familiarUnit.GuidHash;
            if (GetFamiliarExperience(steamId, familiarId).Value >= MaxFamiliarLevel) return; // Check if already at max level

            ProcessExperienceGain(entityManager, familiarEntity, victimEntity, steamId, familiarId);
        }

        static void ProcessExperienceGain(EntityManager entityManager, Entity familiarEntity, Entity victimEntity, ulong steamID, int familiarId)
        {
            UnitLevel victimLevel = entityManager.GetComponentData<UnitLevel>(victimEntity);
            bool isVBlood = IsVBlood(entityManager, victimEntity);

            int gainedXP = CalculateExperienceGained(victimLevel.Level, isVBlood);

            int currentLevel = ConvertXpToLevel(GetFamiliarExperience(steamID, familiarId).Value);

            AddOrUpdateFamiliarExperience(steamID, familiarId, gainedXP);

            CheckAndHandleLevelUp(familiarEntity, steamID, gainedXP, currentLevel, familiarId);
        }
        static bool IsVBlood(EntityManager entityManager, Entity victimEntity)
        {
            return entityManager.HasComponent<VBloodConsumeSource>(victimEntity);
        }
        static int CalculateExperienceGained(int victimLevel, bool isVBlood)
        {
            int baseXP = victimLevel;
            if (isVBlood) return (int)(baseXP * VBloodMultiplier);
            return (int)(baseXP * UnitMultiplier);
        }

        static void AddOrUpdateFamiliarExperience(ulong playerId, int familiarId, float experience)
        {
            if (!FamiliarExperience.ContainsKey(playerId))
            {
                FamiliarExperience[playerId] = FamiliarExperienceManager.LoadFamiliarExperience(playerId);
            }

            var playerData = FamiliarExperience[playerId];
            if (playerData.FamiliarExperience.ContainsKey(familiarId))
            {
                var existingData = playerData.FamiliarExperience[familiarId];
                playerData.FamiliarExperience[familiarId] = new KeyValuePair<int, float>(existingData.Key, existingData.Value + experience);
            }
            else
            {
                playerData.FamiliarExperience[familiarId] = new KeyValuePair<int, float>(0, experience);
            }

            FamiliarExperienceManager.SaveFamiliarExperience(playerId, playerData);
        }

        static KeyValuePair<int, float> GetFamiliarExperience(ulong playerId, int familiarId)
        {
            if (!FamiliarExperience.ContainsKey(playerId))
            {
                FamiliarExperience[playerId] = FamiliarExperienceManager.LoadFamiliarExperience(playerId);
            }

            var playerData = FamiliarExperience[playerId];
            return playerData.FamiliarExperience.ContainsKey(familiarId) ? playerData.FamiliarExperience[familiarId] : new KeyValuePair<int, float>(0, 0f);
        }

        static void CheckAndHandleLevelUp(Entity familiarEntity, ulong steamID, int gainedXP, int currentLevel, int familiarId)
        {
            EntityManager entityManager = Core.EntityManager;
            Entity userEntity = familiarEntity.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity;

            bool leveledUp = CheckForLevelUp(steamID, familiarEntity.Index, currentLevel);
            if (leveledUp)
            {
                DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                {
                    BuffPrefabGUID = levelUpBuff,
                };
                FromCharacter fromCharacter = new()
                {
                    Character = familiarEntity,
                    User = userEntity,
                };
                debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            }
            NotifyPlayer(entityManager, userEntity, steamID, familiarId, gainedXP, leveledUp);
        }

        static bool CheckForLevelUp(ulong steamID, int familiarId, int currentLevel)
        {
            int newLevel = ConvertXpToLevel(GetFamiliarExperience(steamID, familiarId).Value);
            return newLevel > currentLevel;
        }

        static void NotifyPlayer(EntityManager entityManager, Entity userEntity, ulong steamID, int familiarId, int gainedXP, bool leveledUp)
        {
            User user = entityManager.GetComponentData<User>(userEntity);
            if (leveledUp)
            {
                int newLevel = ConvertXpToLevel(GetFamiliarExperience(steamID, familiarId).Value);
                GearOverride.SetLevel(userEntity.Read<User>().LocalCharacter._Entity);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"Your familiar reached level <color=white>{newLevel}</color>!");
            }
            if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["FamiliarLogging"])
            {
                int levelProgress = GetLevelProgress(steamID, familiarId);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{gainedXP}</color> <color=#FFC0CB>experience</color> for familiar (<color=white>{levelProgress}%</color>)");
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

        static float GetXp(ulong steamID, int familiarId)
        {
            return GetFamiliarExperience(steamID, familiarId).Value;
        }

        static int GetLevel(ulong SteamID, int familiarId)
        {
            return ConvertXpToLevel(GetXp(SteamID, familiarId));
        }

        public static int GetLevelProgress(ulong SteamID, int familiarId)
        {
            float currentXP = GetXp(SteamID, familiarId);
            int currentLevelXP = ConvertLevelToXp(GetLevel(SteamID, familiarId));
            int nextLevelXP = ConvertLevelToXp(GetLevel(SteamID, familiarId) + 1);

            double neededXP = nextLevelXP - currentLevelXP;
            double earnedXP = nextLevelXP - currentXP;

            return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
        }
    }
}