using Bloodcraft.Services;
using ProjectM;
using static Bloodcraft.SystemUtilities.Expertise.ExpertiseStats.WeaponStatManager;

namespace Bloodcraft.SystemUtilities.Expertise;
internal static class ExpertiseStats
{
    static ConfigService ConfigService => Core.ConfigService;
    public class PlayerWeaponUtilities
    {
        public static bool ChooseStat(ulong steamId, ExpertiseHandler.WeaponType weaponType, WeaponStatType statType)
        {
            if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) || !weaponStats.TryGetValue(weaponType, out var Stats))
            {
                Stats = [];
                Core.DataStructures.PlayerWeaponStats[steamId][weaponType] = Stats;
            }

            if (ConfigService.HardSynergies)
            {
                if (!Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classes) || classes.Keys.Count == 0)
                {
                    return false;
                }

                List<int> playerClassStats = classes.First().Value.Item1;
                List<WeaponStatType> weaponStatTypes = playerClassStats.Select(value => (WeaponStatType)value).ToList();

                if (!weaponStatTypes.Contains(statType))
                {
                    return false;
                }

                if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= ConfigService.ExpertiseStatChoices || Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen per weapon, one stat type per weapon
                }

                Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
                Core.DataStructures.SavePlayerWeaponStats();
                return true;
            }

            if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= ConfigService.ExpertiseStatChoices || Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Contains(statType)) 
            {
                return false; // Only allow configured amount of stats to be chosen per weapon
            }

            Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
            Core.DataStructures.SavePlayerWeaponStats();
            return true;
        }
        public static void ResetStats(ulong steamId, ExpertiseHandler.WeaponType weaponType)
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

        public static readonly Dictionary<WeaponStatType, string> WeaponStatFormats = new()
        { 
            { WeaponStatType.MaxHealth, "integer" },
            { WeaponStatType.MovementSpeed, "decimal" },
            { WeaponStatType.PrimaryAttackSpeed, "percentage" },
            { WeaponStatType.PhysicalLifeLeech, "percentage" },
            { WeaponStatType.SpellLifeLeech, "percentage" },
            { WeaponStatType.PrimaryLifeLeech, "percentage" },
            { WeaponStatType.PhysicalPower, "integer" },
            { WeaponStatType.SpellPower, "integer" },
            { WeaponStatType.PhysicalCritChance, "percentage" },
            { WeaponStatType.PhysicalCritDamage, "percentage" },
            { WeaponStatType.SpellCritChance, "percentage" },
            { WeaponStatType.SpellCritDamage, "percentage" }
        };

        public static readonly Dictionary<WeaponStatType, UnitStatType> WeaponStatTypes = new()
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

        public static readonly Dictionary<WeaponStatType, float> WeaponStatValues = new()
        {
            {WeaponStatType.MaxHealth, ConfigService.MaxHealth},
            {WeaponStatType.MovementSpeed, ConfigService.MovementSpeed},
            {WeaponStatType.PrimaryAttackSpeed, ConfigService.PrimaryAttackSpeed},
            {WeaponStatType.PhysicalLifeLeech, ConfigService.PhysicalLifeLeech},
            {WeaponStatType.SpellLifeLeech, ConfigService.SpellLifeLeech},
            {WeaponStatType.PrimaryLifeLeech, ConfigService.PrimaryLifeLeech},
            {WeaponStatType.PhysicalPower, ConfigService.PhysicalPower},
            {WeaponStatType.SpellPower, ConfigService.SpellPower},
            {WeaponStatType.PhysicalCritChance, ConfigService.PhysicalCritChance},
            {WeaponStatType.PhysicalCritDamage, ConfigService.PhysicalCritDamage},
            {WeaponStatType.SpellCritChance, ConfigService.SpellCritChance},
            {WeaponStatType.SpellCritDamage, ConfigService.SpellCritDamage}
        };
    }
}
