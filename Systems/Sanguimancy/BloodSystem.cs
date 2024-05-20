using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Cobalt.Systems.Sanguimancy.BloodStats.BloodStatManager;

namespace Cobalt.Systems.Sanguimancy
{
    public class BloodSystem
    {
        private static readonly int UnitMultiplier = Plugin.UnitBloodMultiplier.Value; // base mastery points
        public static readonly int MaxBloodLevel = Plugin.MaxBloodLevel.Value; // maximum level
        private static readonly float BloodMasteryConstant = 0.1f; // constant for calculating level from xp
        private static readonly int BloodXPPower = 2; // power for calculating level from xp
        private static readonly int VBloodMultiplier = Plugin.VBloodBloodMultiplier.Value; // mastery points multiplier from VBlood units

        public static void UpdateSanguimancy(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = Core.Server.EntityManager;
            if (Killer == Victim) return;
            if (entityManager.HasComponent<Minion>(Victim) || !Victim.Has<BloodConsumeSource>()) return;

            BloodConsumeSource bloodConsumeSource = Victim.Read<BloodConsumeSource>();
            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User User = entityManager.GetComponentData<User>(userEntity);

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
            else
            {
                BloodMasteryValue *= UnitMultiplier;
            }
            if (BloodMasteryValue.Equals(0) || BloodMasteryValue < 5)
            {
                BloodMasteryValue += UnitMultiplier;
            }
            SetBloodMastery(User, BloodMasteryValue, entityManager);
        }

        public static void SetBloodMastery(User user, float Value, EntityManager entityManager)
        {
            ulong SteamID = user.PlatformId;
            bool isPlayerFound = Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var Mastery);
            float newExperience = Value + (isPlayerFound ? Mastery.Value : 0);
            int newLevel = ConvertXpToLevel(newExperience);
            bool leveledUp = isPlayerFound && newLevel > Mastery.Key;

            if (leveledUp && newLevel > MaxBloodLevel)
            {
                newLevel = MaxBloodLevel;
                newExperience = ConvertLevelToXp(MaxBloodLevel);
            }

            KeyValuePair<int, float> newMastery = new(newLevel, newExperience);
            if (isPlayerFound)
            {
                Core.DataStructures.PlayerSanguimancy[SteamID] = newMastery;
            }
            else
            {
                Core.DataStructures.PlayerSanguimancy.Add(SteamID, newMastery);
            }

            Core.DataStructures.SavePlayerSanguimancy();
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
                if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["SanguimancyLogging"])
                {
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, $"+<color=yellow>{gainedXP}</color> <color=red>sanguimancy</color> (<color=white>{levelProgress}%</color>)");
                }
            }
        }

        private static int ConvertXpToLevel(float xp)
        {
            return (int)(BloodMasteryConstant * Math.Sqrt(xp));
        }

        public static float ConvertLevelToXp(int level)
        {
            return (int)Math.Pow(level / BloodMasteryConstant, BloodXPPower);
        }

        private static int GetLevelProgress(ulong steamID)
        {
            if (!Core.DataStructures.PlayerSanguimancy.TryGetValue(steamID, out var mastery))
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