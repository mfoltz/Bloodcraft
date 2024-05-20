using ProjectM;

namespace Cobalt.Systems.Expertise
{
    public class WeaponStats
    {
        public class PlayerWeaponUtilities
        {
            public static bool ChooseStat(ulong steamId, ExpertiseSystem.WeaponType weaponType, WeaponStatManager.WeaponStatType statType)
            {
                if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) || !weaponStats.TryGetValue(weaponType, out var Stats))
                {
                    Stats = [];
                    Core.DataStructures.PlayerWeaponStats[steamId][weaponType] = Stats;
                }

                if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= 2)
                {
                    return false; // Only allow 2 stats to be chosen
                }

                Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
                Core.DataStructures.SavePlayerWeaponStats();
                return true;
            }

            public static void ResetStats(ulong steamId, ExpertiseSystem.WeaponType weaponType)
            {
                if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStatStats) && weaponStatStats.TryGetValue(weaponType, out var Stats))
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
                PhysicalPower,
                SpellPower,
                PhysicalCritChance,
                PhysicalCritDamage,
                SpellCritChance,
                SpellCritDamage
            }

            public static readonly Dictionary<WeaponStatType, UnitStatType> WeaponStatMap = new()
                {
                    { WeaponStatType.PhysicalPower, UnitStatType.PhysicalPower },
                    { WeaponStatType.SpellPower, UnitStatType.SpellPower },
                    { WeaponStatType.PhysicalCritChance, UnitStatType.PhysicalCriticalStrikeChance },
                    { WeaponStatType.PhysicalCritDamage, UnitStatType.PhysicalCriticalStrikeDamage },
                    { WeaponStatType.SpellCritChance, UnitStatType.SpellCriticalStrikeChance },
                    { WeaponStatType.SpellCritDamage, UnitStatType.SpellCriticalStrikeDamage }
                };

            private static readonly Dictionary<WeaponStatType, float> baseCaps = new()
                {
                    {WeaponStatType.PhysicalPower, 15},
                    {WeaponStatType.SpellPower, 15},
                    {WeaponStatType.PhysicalCritChance, 0.15f},
                    {WeaponStatType.PhysicalCritDamage, 0.75f},
                    {WeaponStatType.SpellCritChance, 0.15f},
                    {WeaponStatType.SpellCritDamage, 0.75f}
                };

            public static Dictionary<WeaponStatType, float> BaseCaps
            {
                get => baseCaps;
            }
        }
    }
}