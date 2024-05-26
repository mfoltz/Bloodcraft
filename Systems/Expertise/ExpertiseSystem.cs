using Bloodcraft.Patches;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Systems.Expertise
{
    public class ExpertiseSystem
    {
        static readonly float UnitMultiplier = Plugin.UnitExpertiseMultiplier.Value; // Expertise points multiplier from normal units
        static readonly int MaxExpertiseLevel = Plugin.MaxExpertiseLevel.Value; // maximum level
        static readonly float VBloodMultiplier = Plugin.VBloodExpertiseMultiplier.Value; // Expertise points multiplier from VBlood units
        static readonly float ExpertiseConstant = 0.1f; // constant for calculating level from xp
        static readonly int ExpertisePower = 2; // power for calculating level from xp
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
            Unarmed,
            FishingPole
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
        static float CalculateExpertiseValue(UnitStats VictimStats, bool isVBlood)
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
            if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["ExpertiseLogging"])
            {
                message = $"+<color=yellow>{gainedXP}</color> <color=#c0c0c0>{weaponType.ToString().ToLower()}</color> <color=#FFC0CB>expertise</color> (<color=white>{levelProgress}%</color>)";
                ServerChatUtils.SendSystemMessageToClient(entityManager, user, message);
            }
        }
        
        public static int GetLevelProgress(ulong steamID, IExpertiseHandler handler)
        {
            float currentXP = GetXp(steamID, handler);
            int currentLevelXP = ConvertLevelToXp(GetLevel(steamID, handler));
            int nextLevelXP = ConvertLevelToXp(GetLevel(steamID, handler) + 1);

            double neededXP = nextLevelXP - currentLevelXP;
            double earnedXP = nextLevelXP - currentXP;
            return 100 - (int)Math.Ceiling(earnedXP / neededXP * 100);
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
        static float GetXp(ulong steamID, IExpertiseHandler handler)
        {
            var xpData = handler.GetExpertiseData(steamID);
            return xpData.Value;
        }
        static int GetLevel(ulong steamID, IExpertiseHandler handler)
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
                else if (weaponCheck.Contains("fishingpole"))
                {
                    return WeaponType.FishingPole;
                }
            }
            throw new InvalidOperationException("Unrecognized weapon type");
        }
    }
}