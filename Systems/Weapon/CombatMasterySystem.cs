using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using ProjectM.Network;
using Steamworks;
using Unity.Entities;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;

namespace Cobalt.Systems.Weapon
{
    public class CombatMasterySystem
    {
        private static readonly float CombatMasteryMultiplier = 1; // mastery points multiplier from normal units
        private static readonly float CombatValueModifier = 4f;
        public static readonly float MaxCombatMasteryLevel = 99; // maximum level
        private static readonly float VBloodMultiplier = 10; // mastery points multiplier from VBlood units
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

        private static readonly Dictionary<WeaponType, string> masteryToFileKey = new()
        {
            { WeaponType.Sword, "SwordMastery" },
            { WeaponType.Axe, "AxeMastery" },
            { WeaponType.Mace, "MaceMastery" },
            { WeaponType.Spear, "SpearMastery" },
            { WeaponType.Crossbow, "CrossbowMastery" },
            { WeaponType.GreatSword, "GreatSwordMastery" },
            { WeaponType.Slashers, "SlashersMastery" },
            { WeaponType.Pistols, "PistolsMastery" },
            { WeaponType.Reaper, "ReaperMastery" },
            { WeaponType.Longbow, "LongbowMastery" },
            { WeaponType.Whip, "WhipMastery" },
            { WeaponType.Unarmed, "UnarmedMastery" }
        };

        /*
        public static readonly Dictionary<WeaponType, Dictionary<ulong, KeyValuePair<int, float>>> weaponMasteries = new()
        {
            { WeaponType.Sword, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Axe, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Mace, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Spear, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Crossbow, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.GreatSword, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Slashers, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Pistols, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Reaper, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Longbow, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Whip, new Dictionary<ulong, KeyValuePair<int, float>>() },
            { WeaponType.Unarmed, new Dictionary<ulong, KeyValuePair<int, float>>() }
        };
        */

        public static void UpdateCombatMastery(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            if (Killer == Victim) return;
            if (entityManager.HasComponent<Minion>(Victim)) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User User = entityManager.GetComponentData<User>(userEntity);
            ulong SteamID = User.PlatformId;
            PrefabGUID weapon = Killer.Read<Equipment>().WeaponSlotEntity._Entity.Read<PrefabGUID>();
            string weaponType = GetWeaponTypeFromPrefab(weapon).ToString();
            var VictimStats = entityManager.GetComponentData<UnitStats>(Victim);

            bool isVBlood;
            if (entityManager.HasComponent<VBloodConsumeSource>(Victim))
            {
                isVBlood = true;
            }
            else
            {
                isVBlood = false;
            }
            float CombatMasteryValue = (int)((VictimStats.SpellPower + VictimStats.PhysicalPower) / CombatValueModifier);
            if (isVBlood) CombatMasteryValue *= VBloodMultiplier;

            CombatMasteryValue *= CombatMasteryMultiplier;
            SetCombatMastery(SteamID, CombatMasteryValue, weaponType, entityManager, User);

            HandleUpdate(Killer, entityManager);
        }

        public static void HandleUpdate(Entity player, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<PlayerCharacter>(player)) return;
            Equipment equipment = player.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();
            var userEntity = player.Read<PlayerCharacter>().UserEntity;
            var steamId = userEntity.Read<User>().PlatformId;

            UnitStats stats = entityManager.GetComponentData<UnitStats>(player);
            Health health = entityManager.GetComponentData<Health>(player);
            UpdateStats(player, stats, health, steamId, weapon);
        }

        public static void UpdateStats(Entity player, UnitStats unitStats, Health health, ulong steamId, PrefabGUID weapon)
        {
            CombatMasterySystem.WeaponType weaponType = GetWeaponTypeFromPrefab(weapon);
            string weaponString = weaponType.ToString().ToLower();
            if (!player.Has<PlayerCharacter>())
            {
                Plugin.Log.LogInfo("No player character found for stats modifying...");
                return;
            }

            if (!DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponsStats) || !weaponsStats.TryGetValue(weaponString, out var masteryStats))
            {
                Plugin.Log.LogInfo("No stats found for this weapon.");
                return; // No mastery stats to check
            }

            if (DataStructures.weaponMasteryMap.TryGetValue(weaponString, out var playerMastery) && playerMastery.TryGetValue(steamId, out var mastery))
            {
                int playerLevel = ConvertXpToLevel(mastery.Value);
                float levelPercentage = playerLevel / MaxCombatMasteryLevel; // Calculate the percentage of the max level (99)

                foreach (var statType in masteryStats)
                {
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType stat = GetStatTypeFromString(statType.Key);
                    float baseCap = WeaponStatManager.WeaponFocusSystem.BaseCaps[stat];
                    float scaledCap = baseCap * levelPercentage; // Scale cap based on the player's level
                    float currentStatValue = masteryStats[statType.Key];
                    if (scaledCap > currentStatValue)
                    {
                        currentStatValue += scaledCap;
                        weaponsStats[weaponString][statType.Key] = currentStatValue;
                        DataStructures.SavePlayerWeaponStats();
                        ApplyStatIncrease(health, unitStats, stat, scaledCap);
                    }
                }
            }

            player.Write(unitStats); // Assuming there's at least one stat update
            player.Write(health); // Update player health
        }

        private static void ApplyStatIncrease(Health health, UnitStats unitStats, WeaponStatManager.WeaponFocusSystem.WeaponStatType statType, float increase)
        {
            switch (statType)
            {
                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.MaxHealth:
                    health.MaxHealth._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.CastSpeed:
                    unitStats.AttackSpeed._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.AttackSpeed:
                    unitStats.PrimaryAttackSpeed._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalPower:
                    unitStats.PhysicalPower._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellPower:
                    unitStats.SpellPower._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalCritChance:
                    unitStats.PhysicalCriticalStrikeChance._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalCritDamage:
                    unitStats.PhysicalCriticalStrikeDamage._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellCritChance:
                    unitStats.SpellCriticalStrikeChance._Value += increase; // Add increase directly
                    break;

                case WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellCritDamage:
                    unitStats.SpellCriticalStrikeDamage._Value += increase; // Add increase directly
                    break;

                default:
                    throw new ArgumentException("Unknown weapon stat type to apply");
            }
        }

        public static void SetCombatMastery(ulong steamID, float value, string weaponType, EntityManager entityManager, User user)
        {
            Plugin.Log.LogInfo($"{weaponType}");
            ulong steamId = user.PlatformId;
            if (DataStructures.weaponMasteryMap.TryGetValue(weaponType, out var playerMastery) && playerMastery.TryGetValue(steamId, out var mastery))
            {
                //bool isPlayerFound = masteryDictionary.TryGetValue(steamID, out var mastery);
                float newExperience = value + mastery.Value;
                int newLevel = ConvertXpToLevel(newExperience);
                bool leveledUp = newLevel > mastery.Key;

                if (leveledUp)
                {
                    if (newLevel > MaxCombatMasteryLevel)
                    {
                        newExperience = ConvertLevelToXp((int)MaxCombatMasteryLevel);
                        newLevel = (int)MaxCombatMasteryLevel;
                    }
                }

                playerMastery[steamID] = new KeyValuePair<int, float>(newLevel, newExperience);
                DataStructures.saveActions[weaponType]();

                NotifyPlayer(entityManager, user, weaponType, value, leveledUp, newLevel);
            }
            else
            {
                Plugin.Log.LogInfo($"Unable to update experience for {weaponType}");
            }
        }

        public static void NotifyPlayer(EntityManager entityManager, User user, string weaponType, float gainedXP, bool leveledUp, int newLevel)
        {
            ulong steamID = user.PlatformId;
            gainedXP = (int)gainedXP;  // Convert to integer if necessary
            int levelProgress = GetLevelProgress(steamID, weaponType);  // Calculate the current progress to the next level

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

        private static int GetLevelProgress(ulong steamID, string weaponType)
        {
            if (DataStructures.weaponMasteryMap.TryGetValue(weaponType, out var masteryDictionary) && masteryDictionary.TryGetValue(steamID, out var mastery))
            {
                float currentXP = mastery.Value;
                int currentLevel = ConvertXpToLevel(currentXP);
                int nextLevelXP = (int)ConvertLevelToXp(currentLevel + 1);
                return (int)((currentXP - ConvertLevelToXp(currentLevel)) / (nextLevelXP - ConvertLevelToXp(currentLevel)) * 100);
            }
            return 0; // Return 0 if no mastery data found
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

        public static WeaponStatManager.WeaponFocusSystem.WeaponStatType GetStatTypeFromString(string statType)
        {
            string statCheck = statType.ToLower();
            foreach (WeaponStatManager.WeaponFocusSystem.WeaponStatType type in Enum.GetValues(typeof(WeaponStatManager.WeaponFocusSystem.WeaponStatType)))
            {
                //Plugin.Log.LogInfo($"{weaponCheck}|{type.ToString().ToLower()}");
                // Convert the enum name to lower case and check if it is contained in the weapon GUID string
                if (statCheck.Contains(type.ToString().ToLower()))
                {
                    return type;
                }
            }
            return WeaponStatManager.WeaponFocusSystem.WeaponStatType.MaxHealth; // Return Unknown if no match is found
        }

        public static int ConvertXpToLevel(float xp)
        {
            // Using a hypothetical formula: level = constant * sqrt(xp)
            // You might need to adjust this based on your game's leveling curve
            return (int)(CombatMasteryConstant * Math.Sqrt(xp));
        }

        public static float ConvertLevelToXp(int level)
        {
            // Reverse the formula used in ConvertXpToLevel
            return (float)Math.Pow(level / CombatMasteryConstant, CombatMasteryXPPower);
        }
    }
}