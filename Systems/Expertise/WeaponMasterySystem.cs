using Cobalt.Core;
using Cobalt.Systems.WeaponMastery;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Cobalt.Systems.Weapon.WeaponStatsSystem.WeaponStatManager;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;

namespace Cobalt.Systems.Weapon
{
    public class WeaponMasterySystem
    {
        private static readonly float CombatMasteryMultiplier = 1; // mastery points multiplier from normal units
        private static readonly float UnitMultiplier = 6f; // exp gain from normal units
        public static readonly int MaxCombatMasteryLevel = 99; // maximum level
        private static readonly float VBloodMultiplier = 15; // mastery points multiplier from VBlood units
        private static readonly float CombatMasteryConstant = 0.1f; // constant for calculating level from xp
        private static readonly int CombatMasteryXPPower = 2; // power for calculating level from xp

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

        public static void UpdateCombatMastery(EntityManager entityManager, Entity Killer, Entity Victim)
        {
            if (Killer == Victim || entityManager.HasComponent<Minion>(Victim)) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User user = entityManager.GetComponentData<User>(userEntity);
            ulong steamID = user.PlatformId;
            PrefabGUID weaponPrefab = entityManager.GetComponentData<Equipment>(Killer).WeaponSlotEntity._Entity.Read<PrefabGUID>();
            string weaponType = GetWeaponTypeFromPrefab(weaponPrefab).ToString();

            if (entityManager.HasComponent<UnitStats>(Victim))
            {
                var VictimStats = entityManager.GetComponentData<UnitStats>(Victim);
                float CombatMasteryValue = CalculateMasteryValue(VictimStats, entityManager.HasComponent<VBloodConsumeSource>(Victim));

                IWeaponMasteryHandler handler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
                if (handler != null)
                {
                    // Check if the player leveled up
                    var xpData = handler.GetExperienceData(steamID);
                    float newExperience = xpData.Value + CombatMasteryValue;
                    int newLevel = ConvertXpToLevel(newExperience);
                    bool leveledUp = false;

                    if (newLevel > xpData.Key)
                    {
                        leveledUp = true;
                        if (newLevel > MaxCombatMasteryLevel)
                        {
                            newLevel = MaxCombatMasteryLevel;
                            newExperience = ConvertLevelToXp(MaxCombatMasteryLevel);
                        }
                    }
                    var updatedXPData = new KeyValuePair<int, float>(newLevel, newExperience);
                    handler.UpdateExperienceData(steamID, updatedXPData);
                    handler.SaveChanges();
                    NotifyPlayer(entityManager, user, weaponType, CombatMasteryValue, leveledUp, newLevel, handler);
                }
            }
        }

        private static float CalculateMasteryValue(UnitStats VictimStats, bool isVBlood)
        {
            float CombatMasteryValue = (VictimStats.SpellPower + VictimStats.PhysicalPower) / UnitMultiplier;
            if (isVBlood)
            {
                CombatMasteryValue *= VBloodMultiplier;
            }
            return CombatMasteryValue * CombatMasteryMultiplier;
        }

        public static void NotifyPlayer(EntityManager entityManager, User user, string weaponType, float gainedXP, bool leveledUp, int newLevel, IWeaponMasteryHandler handler)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP; // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID, handler); // Calculate the current progress to the next level

            string message;

            if (leveledUp)
            {
                message = $"{weaponType} improved to [<color=white>{newLevel}</color>]";
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
            }
            else
            {
                if (DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["CombatLogging"])
                {
                    message = $"+<color=yellow>{gainedXP}</color> {weaponType.ToLower()} mastery (<color=white>{levelProgress}%</color>)";
                    ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
                }
            }
        }

        private static int GetLevelProgress(ulong steamID, IWeaponMasteryHandler handler)
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
            return (int)(CombatMasteryConstant * Math.Sqrt(xp));
        }

        public static int ConvertLevelToXp(int level)
        {
            // Reversing the formula used in ConvertXpToLevel for consistency
            return (int)Math.Pow(level / CombatMasteryConstant, CombatMasteryXPPower);
        }

        private static float GetXp(ulong steamID, IWeaponMasteryHandler handler)
        {
            var xpData = handler.GetExperienceData(steamID);
            return xpData.Value;
        }

        private static int GetLevel(ulong steamID, IWeaponMasteryHandler handler)
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
            return WeaponStatType.MaxHealth;
        }
    }
}