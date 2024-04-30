using Bloodstone.API;
using ProjectM;

namespace Cobalt.Systems.Weapon
{
    public class WeaponStatsSystem
    {
        public class PlayerWeaponStats
        {
            public float MaxHealth { get; set; }
            public float CastSpeed { get; set; }
            public float AttackSpeed { get; set; }
            public float PhysicalPower { get; set; }
            public float SpellPower { get; set; }
            public float PhysicalCritChance { get; set; }
            public float PhysicalCritDamage { get; set; }
            public float SpellCritChance { get; set; }
            public float SpellCritDamage { get; set; }

            // Instance variable to track chosen stats for each weapon stats object
            public HashSet<WeaponStatManager.WeaponFocusSystem.WeaponStatType> ChosenStats = [];

            public void ChooseStat(WeaponStatManager.WeaponFocusSystem.WeaponStatType statType)
            {
                if (ChosenStats.Count >= 2)
                {
                    throw new InvalidOperationException("Cannot choose more than two stats for this weapon.");
                }

                ChosenStats.Add(statType);
            }

            public void ResetChosenStats()
            {
                ChosenStats.Clear();
            }

            public int StatsChosen => ChosenStats.Count;

            public float GetStatValue(WeaponStatManager.WeaponFocusSystem.WeaponStatType statType)
            {
                return statType switch
                {
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.MaxHealth => MaxHealth,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.CastSpeed => CastSpeed,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.AttackSpeed => AttackSpeed,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalPower => PhysicalPower,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellPower => SpellPower,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalCritChance => PhysicalCritChance,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.PhysicalCritDamage => PhysicalCritDamage,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellCritChance => SpellCritChance,
                    WeaponStatManager.WeaponFocusSystem.WeaponStatType.SpellCritDamage => SpellCritDamage,
                    _ => throw new ArgumentException("Unknown weapon stat type"),
                };
            }
        }

        public class WeaponStatManager
        {
            public class WeaponFocusSystem
            {
                public enum WeaponStatType
                {
                    MaxHealth,
                    CastSpeed,
                    AttackSpeed,
                    PhysicalPower,
                    SpellPower,
                    PhysicalCritChance,
                    PhysicalCritDamage,
                    SpellCritChance,
                    SpellCritDamage
                }

                public static readonly Dictionary<int, WeaponStatType> WeaponStatMap = new()
                {
                    { 0, WeaponStatType.MaxHealth },
                    { 1, WeaponStatType.CastSpeed },
                    { 2, WeaponStatType.AttackSpeed },
                    { 3, WeaponStatType.PhysicalPower },
                    { 4, WeaponStatType.SpellPower },
                    { 5, WeaponStatType.PhysicalCritChance },
                    { 6, WeaponStatType.PhysicalCritDamage },
                    { 7, WeaponStatType.SpellCritChance },
                    { 8, WeaponStatType.SpellCritDamage }
                };

                private static readonly Dictionary<WeaponStatType, float> baseCaps = new()
                {
                    {WeaponStatType.MaxHealth, 1000f},
                    {WeaponStatType.CastSpeed, 1f},
                    {WeaponStatType.AttackSpeed, 1f},
                    {WeaponStatType.PhysicalPower, 50},
                    {WeaponStatType.SpellPower, 50},
                    {WeaponStatType.PhysicalCritChance, 0.5f},
                    {WeaponStatType.PhysicalCritDamage, 2f},
                    {WeaponStatType.SpellCritChance, 0.5f},
                    {WeaponStatType.SpellCritDamage, 2f}
                };

                public static Dictionary<WeaponStatType, float> BaseCaps
                {
                    get => baseCaps;
                }
            }
        }
    }
}