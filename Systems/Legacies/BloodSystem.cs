using Bloodcraft.Patches;
using Bloodcraft.Systems.Legacy;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Systems.Expertise.WeaponStats.WeaponStatManager;

namespace Bloodcraft.Systems.Legacy
{
    public class BloodSystem
    {
        private static readonly int UnitLegacyMultiplier = Plugin.UnitExpertiseMultiplier.Value; // Expertise points multiplier from normal units
        public static readonly int MaxBloodLevel = Plugin.MaxExpertiseLevel.Value; // maximum level
        private static readonly int VBloodLegacyMultiplier = Plugin.VBloodLegacyMultipler.Value; // Expertise points multiplier from VBlood units
        private static readonly float BloodConstant = 0.1f; // constant for calculating level from xp
        private static readonly int BloodPower = 2; // power for calculating level from xp

        public enum BloodType
        {
            Worker,
            Warrior,
            Scholar,
            Rogue,
            Mutant,
            VBlood,
            None,
            GateBoss,
            Draculin,
            DraculaTheImmortal,
            Creature,
            Brute
        }

        public static void UpdateLegacy(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = Core.EntityManager;
            if (Killer == Victim || entityManager.HasComponent<Minion>(Victim) || !Victim.Has<BloodConsumeSource>()) return;
            BloodConsumeSource bloodConsumeSource = Victim.Read<BloodConsumeSource>();
            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;

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
            float BloodValue = bloodConsumeSource.BloodQuality * bloodConsumeSource.BloodQuality;
            if (isVBlood) BloodValue *= VBloodLegacyMultiplier;
            else
            {
                BloodValue *= UnitLegacyMultiplier;
            }

            User user = entityManager.GetComponentData<User>(userEntity);
            ulong steamID = user.PlatformId;
            BloodSystem.BloodType bloodType = ModifyUnitStatBuffUtils.GetCurrentBloodType(Killer);
            if (bloodType.Equals(BloodType.None)) return;

            IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (handler != null)
            {
                // Check if the player leveled up
                var xpData = handler.GetLegacyData(steamID);
                float newExperience = xpData.Value + BloodValue;
                int newLevel = ConvertXpToLevel(newExperience);
                bool leveledUp = false;

                if (newLevel > xpData.Key)
                {
                    leveledUp = true;
                    if (newLevel > MaxBloodLevel)
                    {
                        newLevel = MaxBloodLevel;
                        newExperience = ConvertLevelToXp(MaxBloodLevel);
                    }
                }
                var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
                handler.UpdateLegacyData(steamID, updatedXPData);
                handler.SaveChanges();
                NotifyPlayer(entityManager, user, bloodType, BloodValue, leveledUp, newLevel, handler);
            }
        }

        public static void NotifyPlayer(EntityManager entityManager, User user, BloodSystem.BloodType bloodType, float gainedXP, bool leveledUp, int newLevel, IBloodHandler handler)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP; // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID, handler); // Calculate the current progress to the next level

            string message;

            if (leveledUp)
            {
                message = $"<color=red>{bloodType}</color> legacy improved to [<color=white>{newLevel}</color>]";
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
            }
            else
            {
                if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["BloodLogging"])
                {
                    message = $"+<color=yellow>{gainedXP}</color> <color=red>{bloodType}</color> lineage (<color=white>{levelProgress}%</color>)";
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
                }
            }
        }

        public static int GetLevelProgress(ulong steamID, IBloodHandler handler)
        {
            float currentXP = GetXp(steamID, handler);
            int currentLevel = GetLevel(steamID, handler);
            int nextLevelXP = ConvertLevelToXp(currentLevel + 1);
            //Plugin.Log.LogInfo($"Lv: {currentLevel} | xp: {currentXP} | toNext: {nextLevelXP}");
            int percent = (int)(currentXP / nextLevelXP * 100);
            return percent;
        }

        public static int ConvertXpToLevel(float xp)
        {
            // Assuming a basic square root scaling for experience to level conversion
            return (int)(BloodConstant * Math.Sqrt(xp));
        }

        public static int ConvertLevelToXp(int level)
        {
            // Reversing the formula used in ConvertXpToLevel for consistency
            return (int)Math.Pow(level / BloodConstant, BloodPower);
        }

        private static float GetXp(ulong steamID, IBloodHandler handler)
        {
            var xpData = handler.GetLegacyData(steamID);
            return xpData.Value;
        }

        private static int GetLevel(ulong steamID, IBloodHandler handler)
        {
            return ConvertXpToLevel(GetXp(steamID, handler));
        }

        public static BloodType GetBloodTypeFromPrefab(PrefabGUID blood)
        {
            string bloodCheck = blood.LookupName().ToString().ToLower();
            foreach (BloodType type in Enum.GetValues(typeof(BloodType)))
            {
                if (bloodCheck.Contains(type.ToString().ToLower()))
                {
                    return type;
                }
            }
            throw new InvalidOperationException("Unrecognized blood type");
        }
    }
}