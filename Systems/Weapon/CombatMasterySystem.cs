using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;

namespace Cobalt.Systems.Weapon
{
    public class CombatMasterySystem
    {
        private static readonly float CombatMasteryMultiplier = 1; // mastery points multiplier from normal units
        private static readonly float CombatValueModifier = 4f;
        private static readonly int MaxCombatMastery = 10000; // maximum stored mastery points
        private static readonly float VBloodMultiplier = 10; // mastery points multiplier from VBlood units
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
            Whip
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
            { WeaponType.Whip, "WhipMastery" }
        };

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
            { WeaponType.Whip, new Dictionary<ulong, KeyValuePair<int, float>>() }
        };
        public static void UpdateCombatMastery(Entity Killer, Entity Victim)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            if (Killer == Victim) return;
            if (entityManager.HasComponent<Minion>(Victim)) return;

            Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(Killer).UserEntity;
            User User = entityManager.GetComponentData<User>(userEntity);
            ulong SteamID = User.PlatformId;
            PrefabGUID weapon = Killer.Read<Equipment>().WeaponSlotEntity._Entity.Read<PrefabGUID>();
            WeaponType weaponType = GetWeaponTypeFromPrefab(weapon);
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
            SetCombatMastery(SteamID, CombatMasteryValue, weaponType);

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && bools["CombatLogging"])
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, User, $"+<color=yellow>{CombatMasteryValue}</color> <color=white>{weaponType}</color> <color=#BDD0D7>proficiency</color>");
            }
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
            if (!player.Has<PlayerCharacter>())
            {
                Plugin.Log.LogInfo("No player character found for stats modifying...");
                return;
            }

            Equipment equipment = player.Read<Equipment>();
            PrefabGUID weaponGUID = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();

            if (!DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponsStats) || !weaponsStats.TryGetValue(weaponGUID, out var masteryStats))
            {
                Plugin.Log.LogInfo("No stats found for this weapon.");
                return; // No mastery stats to check
            }

            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.MaxHealth))
                UpdateStatIfIncreased(ref health.MaxHealth, masteryStats.MaxHealth, health.MaxHealth._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.AttackSpeed))
                UpdateStatIfIncreased(ref unitStats.AttackSpeed, masteryStats.AttackSpeed, unitStats.AttackSpeed._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.CastSpeed))
                UpdateStatIfIncreased(ref unitStats.PrimaryAttackSpeed, masteryStats.CastSpeed, unitStats.PrimaryAttackSpeed._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalPower))
                UpdateStatIfIncreased(ref unitStats.PhysicalPower, masteryStats.PhysicalPower, unitStats.PhysicalPower._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellPower))
                UpdateStatIfIncreased(ref unitStats.SpellPower, masteryStats.SpellPower, unitStats.SpellPower._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalCritChance))
                UpdateStatIfIncreased(ref unitStats.PhysicalCriticalStrikeChance, masteryStats.PhysicalCritChance, unitStats.PhysicalCriticalStrikeChance._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalCritDamage))
                UpdateStatIfIncreased(ref unitStats.PhysicalCriticalStrikeDamage, masteryStats.PhysicalCritDamage, unitStats.PhysicalCriticalStrikeDamage._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellCritChance))
                UpdateStatIfIncreased(ref unitStats.SpellCriticalStrikeChance, masteryStats.SpellCritChance, unitStats.SpellCriticalStrikeChance._Value);
            if (masteryStats.ChosenStats.Contains(WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellCritDamage))
                UpdateStatIfIncreased(ref unitStats.SpellCriticalStrikeDamage, masteryStats.SpellCritDamage, unitStats.SpellCriticalStrikeDamage._Value);
            player.Write(unitStats); // Assuming there's at least one stat update
        }

        public static void UpdateStatIfIncreased(ref ModifiableFloat currentStat, float masteryIncrease, float currentStatValue)
        {
            float newStatValue = currentStatValue + masteryIncrease;
            if (newStatValue > currentStat._Value)
            {
                currentStat._Value = newStatValue;
            }
        }

        public static void SetCombatMastery(ulong steamID, float value, WeaponType weaponType)
        {
            if (weaponMasteries.TryGetValue(weaponType, out var masteryDictionary))
            {
                bool isPlayerFound = masteryDictionary.TryGetValue(steamID, out var mastery);
                if (isPlayerFound)
                {
                    float newValue = value + mastery.Value;
                    if (newValue > MaxCombatMastery)
                    {
                        newValue = MaxCombatMastery;
                    }
                    masteryDictionary[steamID] = new KeyValuePair<int, float>(mastery.Key, newValue);
                }
                else
                {
                    masteryDictionary.Add(steamID, new KeyValuePair<int, float>(0, value));
                }

                // Save the updated mastery data to the appropriate JSON file
                DataStructures.SaveData(masteryDictionary, masteryToFileKey[weaponType]);
            }
        }
        public static WeaponType GetWeaponTypeFromPrefab(PrefabGUID weapon)
        {
            string weaponCheck = weapon.ToString().ToLower();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
            {
                // Convert the enum name to lower case and check if it is contained in the weapon GUID string
                if (weaponCheck.Contains(type.ToString().ToLower()))
                {
                    return type;
                }
            }
            return WeaponType.Sword; // Return Unknown if no match is found
        }
    }
}