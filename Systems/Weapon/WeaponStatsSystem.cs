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

                public class WeaponStatCaps
                {
                    private static Dictionary<WeaponStatType, float> baseCaps = new()
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

                    public static Dictionary<WeaponStatType, float> BaseCaps { get => baseCaps; set => baseCaps = value; }
                }

                public class WeaponStatIncreases
                {
                    private static Dictionary<WeaponStatType, (float Increase, int MasteryCost)> baseIncreases = new()
                    {
                        {WeaponStatType.MaxHealth, (10f, 100)},
                        {WeaponStatType.CastSpeed, (0.01f, 100)},
                        {WeaponStatType.AttackSpeed, (0.01f, 100)},
                        {WeaponStatType.PhysicalPower, (0.5f, 25)},
                        {WeaponStatType.SpellPower, (0.5f, 25)},
                        {WeaponStatType.PhysicalCritChance, (0.01f, 50)},
                        {WeaponStatType.PhysicalCritDamage, (0.05f, 50)},
                        {WeaponStatType.SpellCritChance, (0.01f, 50)},
                        {WeaponStatType.SpellCritDamage, (0.05f, 50)}
                    };

                    public static Dictionary<WeaponStatType, (float Increase, int MasteryCost)> BaseIncreases
                    {
                        get => baseIncreases;
                        set => baseIncreases = value;
                    }
                }
            }
        }
    }
}