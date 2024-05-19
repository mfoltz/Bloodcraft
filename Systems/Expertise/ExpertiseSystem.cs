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
        public static readonly int MaxWeaponExpertiseLevel = Plugin.MaxExpertiseLevel.Value; // maximum level
        private static readonly int VBloodMultiplier = Plugin.VBloodExpertiseMultiplier.Value; // Expertise points multiplier from VBlood units
        private static readonly float WeaponExpertiseConstant = 0.1f; // constant for calculating level from xp
        private static readonly int WeaponExpertiseXPPower = 2; // power for calculating level from xp

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

        public static void UpdateWeaponExpertise(EntityManager entityManager, Entity Killer, Entity Victim)
        {
            if (Killer == Victim || entityManager.HasComponent<Minion>(Victim)) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User user = entityManager.GetComponentData<User>(userEntity);
            ulong steamID = user.PlatformId;
            ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(Killer);

            if (entityManager.HasComponent<UnitStats>(Victim))
            {
                var VictimStats = entityManager.GetComponentData<UnitStats>(Victim);
                float WeaponExpertiseValue = CalculateExpertiseValue(VictimStats, entityManager.HasComponent<VBloodConsumeSource>(Victim));

                IWeaponExpertiseHandler handler = WeaponExpertiseHandlerFactory.GetWeaponExpertiseHandler(weaponType);
                if (handler != null)
                {
                    // Check if the player leveled up
                    var xpData = handler.GetExperienceData(steamID);
                    float newExperience = xpData.Value + WeaponExpertiseValue;
                    int newLevel = ConvertXpToLevel(newExperience);
                    bool leveledUp = false;

                    if (newLevel > xpData.Key)
                    {
                        leveledUp = true;
                        if (newLevel > MaxWeaponExpertiseLevel)
                        {
                            newLevel = MaxWeaponExpertiseLevel;
                            newExperience = ConvertLevelToXp(MaxWeaponExpertiseLevel);
                        }
                    }
                    var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
                    handler.UpdateExperienceData(steamID, updatedXPData);
                    handler.SaveChanges();
                    NotifyPlayer(entityManager, user, weaponType, WeaponExpertiseValue, leveledUp, newLevel, handler);
                }
            }
        }

        private static float CalculateExpertiseValue(UnitStats VictimStats, bool isVBlood)
        {
            float WeaponExpertiseValue = VictimStats.SpellPower + VictimStats.PhysicalPower;
            if (isVBlood) return WeaponExpertiseValue * VBloodMultiplier;
            return WeaponExpertiseValue * UnitMultiplier;
        }

        public static void NotifyPlayer(EntityManager entityManager, User user, ExpertiseSystem.WeaponType weaponType, float gainedXP, bool leveledUp, int newLevel, IWeaponExpertiseHandler handler)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP; // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID, handler); // Calculate the current progress to the next level

            string message;

            if (leveledUp)
            {
                Entity character = user.LocalCharacter._Entity;
                Equipment equipment = character.Read<Equipment>();
                message = $"{weaponType} improved to [<color=white>{newLevel}</color>]";
                GearOverride.SetWeaponItemLevel(equipment, newLevel, Core.Server.EntityManager);
                //GearOverride.SetLevel(user.LocalCharacter._Entity, VWorld.Server.EntityManager);
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
            }
            else
            {
                if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["ExpertiseLogging"])
                {
                    message = $"+<color=yellow>{gainedXP}</color> {weaponType} expertise (<color=white>{levelProgress}%</color>)";
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
                }
            }
        }

        public static int GetLevelProgress(ulong steamID, IWeaponExpertiseHandler handler)
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
            return (int)(WeaponExpertiseConstant * Math.Sqrt(xp));
        }

        public static int ConvertLevelToXp(int level)
        {
            // Reversing the formula used in ConvertXpToLevel for consistency
            return (int)Math.Pow(level / WeaponExpertiseConstant, WeaponExpertiseXPPower);
        }

        private static float GetXp(ulong steamID, IWeaponExpertiseHandler handler)
        {
            var xpData = handler.GetExperienceData(steamID);
            return xpData.Value;
        }

        private static int GetLevel(ulong steamID, IWeaponExpertiseHandler handler)
        {
            return ConvertXpToLevel(GetXp(steamID, handler));
        }

        public static WeaponType GetWeaponTypeFromPrefab(PrefabGUID weapon)
        {
            if (weapon.GuidHash.Equals(0)) return WeaponType.Unarmed; // Return Unarmed if no weapon is equipped
            string weaponCheck = weapon.LookupName().ToString().ToLower();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                //Plugin.Log.LogInfo($"{weaponCheck}|{type.ToString().ToLower()}");
                // Convert the enum name to lower case and check if it is contained in the weapon GUID string
                if (weaponCheck.Contains(type.ToString().ToLower()) && !weaponCheck.Contains("great"))
                {
                    return type;
                }
                else
                {
                    if (weaponCheck.Contains("great"))
                    {
                        return WeaponType.GreatSword;
                    }
                }
            }
            return WeaponType.Unarmed; // Return Unknown if no match is found
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
            return WeaponStatType.PhysicalPower;
        }
    }
}