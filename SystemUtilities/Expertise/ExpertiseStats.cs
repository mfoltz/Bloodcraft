﻿using ProjectM;
using static Bloodcraft.Systems.Expertise.ExpertiseStats.WeaponStatManager;

namespace Bloodcraft.Systems.Expertise;

internal static class ExpertiseStats
{
    public class PlayerWeaponUtilities
    {
        public static bool ChooseStat(ulong steamId, ExpertiseUtilities.WeaponType weaponType, WeaponStatManager.WeaponStatType statType)
        {
            if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) || !weaponStats.TryGetValue(weaponType, out var Stats))
            {
                Stats = [];
                Core.DataStructures.PlayerWeaponStats[steamId][weaponType] = Stats;
            }

            if (Plugin.HardSynergies.Value)
            {
                if (!Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) || classes.Keys.Count == 0)
                {
                    return false;
                }
                List<int> playerClassStats = classes.First().Value.Item1;

                List<WeaponStatType> weaponStatTypes = playerClassStats.Select(value => (WeaponStatType)value).ToList();

                if (!weaponStatTypes.Contains(statType))
                {
                    return false;
                }
                if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= Plugin.ExpertiseStatChoices.Value || Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen per weapon, one stat type per weapon
                }
                Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
                Core.DataStructures.SavePlayerWeaponStats();
                return true;
            }

            if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= Plugin.ExpertiseStatChoices.Value || Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Contains(statType)) 
            {
                return false; // Only allow configured amount of stats to be chosen per weapon
            }

            Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
            Core.DataStructures.SavePlayerWeaponStats();
            return true;
        }
        public static void ResetStats(ulong steamId, ExpertiseUtilities.WeaponType weaponType)
        {
            if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
            {
                Stats.Clear();
                Core.DataStructures.SavePlayerWeaponStats();
            }
        }
    }
    public class WeaponStatManager
    {
        public enum WeaponStatType
        {
            MaxHealth, // 0
            MovementSpeed, // 1
            PrimaryAttackSpeed, // 2
            PhysicalLifeLeech, // 3
            SpellLifeLeech, // 4
            PrimaryLifeLeech, // 5
            PhysicalPower, // 6
            SpellPower, // 7
            PhysicalCritChance, // 8
            PhysicalCritDamage, // 9
            SpellCritChance, // 10
            SpellCritDamage // 11
        }

        public static readonly Dictionary<WeaponStatManager.WeaponStatType, string> StatFormatMap = new()
        { 
            { WeaponStatManager.WeaponStatType.MaxHealth, "integer" },
            { WeaponStatManager.WeaponStatType.MovementSpeed, "decimal" },
            { WeaponStatManager.WeaponStatType.PrimaryAttackSpeed, "percentage" },
            { WeaponStatManager.WeaponStatType.PhysicalLifeLeech, "percentage" },
            { WeaponStatManager.WeaponStatType.SpellLifeLeech, "percentage" },
            { WeaponStatManager.WeaponStatType.PrimaryLifeLeech, "percentage" },
            { WeaponStatManager.WeaponStatType.PhysicalPower, "integer" },
            { WeaponStatManager.WeaponStatType.SpellPower, "integer" },
            { WeaponStatManager.WeaponStatType.PhysicalCritChance, "percentage" },
            { WeaponStatManager.WeaponStatType.PhysicalCritDamage, "percentage" },
            { WeaponStatManager.WeaponStatType.SpellCritChance, "percentage" },
            { WeaponStatManager.WeaponStatType.SpellCritDamage, "percentage" }
        };

        public static readonly Dictionary<WeaponStatType, UnitStatType> WeaponStatMap = new()
        {
            { WeaponStatType.MaxHealth, UnitStatType.MaxHealth },
            { WeaponStatType.MovementSpeed, UnitStatType.MovementSpeed },
            { WeaponStatType.PrimaryAttackSpeed, UnitStatType.PrimaryAttackSpeed },
            { WeaponStatType.PhysicalLifeLeech, UnitStatType.PhysicalLifeLeech },
            { WeaponStatType.SpellLifeLeech, UnitStatType.SpellLifeLeech },
            { WeaponStatType.PrimaryLifeLeech, UnitStatType.PrimaryLifeLeech },
            { WeaponStatType.PhysicalPower, UnitStatType.PhysicalPower },
            { WeaponStatType.SpellPower, UnitStatType.SpellPower },
            { WeaponStatType.PhysicalCritChance, UnitStatType.PhysicalCriticalStrikeChance },
            { WeaponStatType.PhysicalCritDamage, UnitStatType.PhysicalCriticalStrikeDamage },
            { WeaponStatType.SpellCritChance, UnitStatType.SpellCriticalStrikeChance },
            { WeaponStatType.SpellCritDamage, UnitStatType.SpellCriticalStrikeDamage },
        };

        static readonly Dictionary<WeaponStatType, float> baseCaps = new()
        {
            {WeaponStatType.MaxHealth, Plugin.MaxHealth.Value},
            {WeaponStatType.MovementSpeed, Plugin.MovementSpeed.Value},
            {WeaponStatType.PrimaryAttackSpeed, Plugin.PrimaryAttackSpeed.Value},
            {WeaponStatType.PhysicalLifeLeech, Plugin.PhysicalLifeLeech.Value},
            {WeaponStatType.SpellLifeLeech, Plugin.SpellLifeLeech.Value},
            {WeaponStatType.PrimaryLifeLeech, Plugin.PrimaryLifeLeech.Value},
            {WeaponStatType.PhysicalPower, Plugin.PhysicalPower.Value},
            {WeaponStatType.SpellPower, Plugin.SpellPower.Value},
            {WeaponStatType.PhysicalCritChance, Plugin.PhysicalCritChance.Value},
            {WeaponStatType.PhysicalCritDamage, Plugin.PhysicalCritDamage.Value},
            {WeaponStatType.SpellCritChance, Plugin.SpellCritChance.Value},
            {WeaponStatType.SpellCritDamage, Plugin.SpellCritDamage.Value}
        };

        public static Dictionary<WeaponStatType, float> BaseCaps
        {
            get => baseCaps;
        }
    }
}
