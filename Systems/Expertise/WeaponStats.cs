using ProjectM;

namespace Bloodcraft.Systems.Expertise
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

                if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= Plugin.ExpertiseStatChoices.Value)
                {
                    return false; // Only allow configured amount of stats to be chosen per weapon
                }

                Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
                Core.DataStructures.SavePlayerWeaponStats();
                return true;
            }

            public static void ResetStats(ulong steamId, ExpertiseSystem.WeaponType weaponType)
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
                MaxHealth,
                MovementSpeed,
                PrimaryAttackSpeed,
                PhysicalLifeLeech,
                SpellLifeLeech,
                PrimaryLifeLeech,
                PhysicalPower,
                SpellPower,
                PhysicalCritChance,
                PhysicalCritDamage,
                SpellCritChance,
                SpellCritDamage
            }

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
}