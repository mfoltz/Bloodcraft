using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Cobalt.Systems.Bloodline.BloodStatsSystem;
using static Cobalt.Systems.Bloodline.BloodStatsSystem.BloodStatManager;
using static Cobalt.Systems.Expertise.WeaponStatsSystem.WeaponStatManager;

namespace Cobalt.Systems.Bloodline
{
    public class BloodMasterySystem
    {
        private static readonly float BloodMasteryMultiplier = 1f; // mastery points multiplier from normal units
        private static readonly float BaseBloodMastery = 5; // base mastery points
        private static readonly int MaxBloodMasteryLevel = 99; // maximum level
        private static readonly float BloodMasteryConstant = 0.1f; // constant for calculating level from xp
        private static readonly int BloodMasteryXPPower = 2; // power for calculating level from xp
        private static readonly float VBloodMultiplier = 10; // mastery points multiplier from VBlood units

        public static void UpdateBloodMastery(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            if (Killer == Victim) return;
            if (entityManager.HasComponent<Minion>(Victim) || !Victim.Has<BloodConsumeSource>()) return;

            BloodConsumeSource bloodConsumeSource = Victim.Read<BloodConsumeSource>();
            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User User = entityManager.GetComponentData<User>(userEntity);
            ulong SteamID = User.PlatformId;

            //var VictimStats = entityManager.GetComponentData<UnitStats>(Victim);

            bool isVBlood;
            if (entityManager.HasComponent<VBloodConsumeSource>(Victim))
            {
                isVBlood = true;
            }
            else
            {
                isVBlood = false;
            }
            float BloodMasteryValue = bloodConsumeSource.BloodQuality * bloodConsumeSource.BloodQuality;
            if (isVBlood) BloodMasteryValue *= VBloodMultiplier;

            BloodMasteryValue *= BloodMasteryMultiplier;
            if (BloodMasteryValue.Equals(0) || BloodMasteryValue < 5)
            {
                BloodMasteryValue += BaseBloodMastery;
            }
            SetBloodMastery(User, BloodMasteryValue, entityManager);
        }

        public static void SetBloodMastery(User user, float Value, EntityManager entityManager)
        {
            ulong SteamID = user.PlatformId;
            bool isPlayerFound = DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var Mastery);
            float newExperience = Value + (isPlayerFound ? Mastery.Value : 0);
            int newLevel = ConvertXpToLevel(newExperience);
            bool leveledUp = isPlayerFound && newLevel > Mastery.Key;

            if (leveledUp && newLevel > MaxBloodMasteryLevel)
            {
                newLevel = MaxBloodMasteryLevel;
                newExperience = ConvertLevelToXp(MaxBloodMasteryLevel);
            }

            KeyValuePair<int, float> newMastery = new(newLevel, newExperience);
            if (isPlayerFound)
            {
                DataStructures.PlayerSanguimancy[SteamID] = newMastery;
            }
            else
            {
                DataStructures.PlayerSanguimancy.Add(SteamID, newMastery);
            }

            DataStructures.SavePlayerSanguimancy();
            NotifyPlayer(entityManager, user, Value, leveledUp, newLevel);
        }

        private static void NotifyPlayer(EntityManager entityManager, User user, float gainedXP, bool leveledUp, int newLevel)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP;  // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID);  // Calculate the current progress to the next level

            if (leveledUp)
            {
                // Directly using the newLevel parameter since it's already calculated
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"<color=red>Sanguimancy</color> improved to [<color=white>{newLevel}</color>]");
            }
            else
            {
                if (DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["BloodLogging"])
                {
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{gainedXP}</color> <color=red>sanguimancy</color> (<color=white>{levelProgress}%</color>)");
                }
            }
        }

        private static int ConvertXpToLevel(float xp)
        {
            return (int)(BloodMasteryConstant * Math.Sqrt(xp));
        }

        private static float ConvertLevelToXp(int level)
        {
            return (int)Math.Pow(level / BloodMasteryConstant, BloodMasteryXPPower);
        }

        private static int GetLevelProgress(ulong steamID)
        {
            if (!DataStructures.PlayerSanguimancy.TryGetValue(steamID, out var mastery))
                return 0; // Return 0 if no mastery data found

            float currentXP = mastery.Value;
            int currentLevel = ConvertXpToLevel(currentXP);
            int nextLevelXP = (int)ConvertLevelToXp(currentLevel + 1);
            // Correcting to show progress between the current and next level
            int percent = (int)((currentXP - ConvertLevelToXp(currentLevel)) / (nextLevelXP - ConvertLevelToXp(currentLevel)) * 100);
            return percent;
        }

        public static BloodStatType GetBloodStatTypeFromString(string statType)
        {
            foreach (BloodStatType type in Enum.GetValues(typeof(BloodStatType)))
            {
                if (statType.ToLower().Contains(type.ToString().ToLower()))
                {
                    return type;
                }
            }
            return BloodStatType.PassiveHealthRegen;
        }
    }
}