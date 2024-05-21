using Cobalt.Hooks;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using static Cobalt.Systems.Expertise.WeaponStats.WeaponStatManager;

namespace Cobalt.Systems.Expertise
{
    public class ExpertiseSystem
    {
        private static readonly int UnitMultiplier = Plugin.UnitExpertiseMultiplier.Value; // Expertise points multiplier from normal units
        public static readonly int MaxExpertiseLevel = Plugin.MaxExpertiseLevel.Value; // maximum level
        private static readonly int VBloodMultiplier = Plugin.VBloodExpertiseMultiplier.Value; // Expertise points multiplier from VBlood units
        private static readonly float ExpertiseConstant = 0.1f; // constant for calculating level from xp
        private static readonly int ExpertisePower = 2; // power for calculating level from xp

        public enum WeaponType
        {
            Sword,
            Axe,
            Mace,
            Spear,
            Crossbow,
            GreatSword,
            Slashers,
            Pistols,
            Reaper,
            Longbow,
            Whip,
            Unarmed
        }

        public static void UpdateExpertise(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = Core.EntityManager;
            if (Killer == Victim || entityManager.HasComponent<Minion>(Victim)) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User user = entityManager.GetComponentData<User>(userEntity);
            ulong steamID = user.PlatformId;
            ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(Killer);

            if (weaponType.Equals(WeaponType.Unarmed) && !Plugin.Sanguimancy.Value) return; // check for sanguimancy setting

            if (entityManager.HasComponent<UnitStats>(Victim))
            {
                var VictimStats = entityManager.GetComponentData<UnitStats>(Victim);
                float ExpertiseValue = CalculateExpertiseValue(VictimStats, entityManager.HasComponent<VBloodConsumeSource>(Victim));

                IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
                if (handler != null)
                {
                    // Check if the player leveled up
                    var xpData = handler.GetExpertiseData(steamID);
                    float newExperience = xpData.Value + ExpertiseValue;
                    int newLevel = ConvertXpToLevel(newExperience);
                    bool leveledUp = false;

                    if (newLevel > xpData.Key)
                    {
                        leveledUp = true;
                        if (newLevel > MaxExpertiseLevel)
                        {
                            newLevel = MaxExpertiseLevel;
                            newExperience = ConvertLevelToXp(MaxExpertiseLevel);
                        }
                    }
                    var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
                    handler.UpdateExpertiseData(steamID, updatedXPData);
                    handler.SaveChanges();
                    NotifyPlayer(entityManager, user, weaponType, ExpertiseValue, leveledUp, newLevel, handler);
                }
            }
        }

        private static float CalculateExpertiseValue(UnitStats VictimStats, bool isVBlood)
        {
            float ExpertiseValue = VictimStats.SpellPower + VictimStats.PhysicalPower;
            if (isVBlood) return ExpertiseValue * VBloodMultiplier;
            return ExpertiseValue * UnitMultiplier;
        }

        public static void NotifyPlayer(EntityManager entityManager, User user, ExpertiseSystem.WeaponType weaponType, float gainedXP, bool leveledUp, int newLevel, IExpertiseHandler handler)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP; // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID, handler); // Calculate the current progress to the next level

            string message;

            if (leveledUp)
            {
                Entity character = user.LocalCharacter._Entity;
                Equipment equipment = character.Read<Equipment>();
                message = $"<color=#c0c0c0>{weaponType}</color> improved to [<color=white>{newLevel}</color>]";
                if (!weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed)) GearOverride.SetWeaponItemLevel(equipment, newLevel, Core.Server.EntityManager);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
            }
            else
            {
                if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["ExpertiseLogging"])
                {
                    message = $"+<color=yellow>{gainedXP}</color> <color=#c0c0c0>{weaponType}</color> expertise (<color=white>{levelProgress}%</color>)";
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
                }
            }
        }

        public static int GetLevelProgress(ulong steamID, IExpertiseHandler handler)
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
            return (int)(ExpertiseConstant * Math.Sqrt(xp));
        }

        public static int ConvertLevelToXp(int level)
        {
            // Reversing the formula used in ConvertXpToLevel for consistency
            return (int)Math.Pow(level / ExpertiseConstant, ExpertisePower);
        }

        private static float GetXp(ulong steamID, IExpertiseHandler handler)
        {
            var xpData = handler.GetExpertiseData(steamID);
            return xpData.Value;
        }

        private static int GetLevel(ulong steamID, IExpertiseHandler handler)
        {
            return ConvertXpToLevel(GetXp(steamID, handler));
        }

        public static WeaponType GetWeaponTypeFromPrefab(PrefabGUID weapon)
        {
            string weaponCheck = weapon.LookupName().ToString().ToLower();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                if (weaponCheck.Contains(type.ToString().ToLower()) && !weaponCheck.Contains("great"))
                {
                    return type;
                }
                else if (weaponCheck.Contains("great"))
                {
                    return WeaponType.GreatSword;
                }
            }

            throw new InvalidOperationException("Unrecognized weapon type");
        }

        public static WeaponStatType GetWeaponStatTypeFromString(string statType)
        {
            foreach (WeaponStatType type in Enum.GetValues(typeof(WeaponStatType)))
            {
                if (statType.ToLower().Contains(type.ToString().ToLower()))
                {
                    return type;
                }
            }

            throw new InvalidOperationException("Unrecognized stat type");
        }
    }
}