using Bloodstone.API;
using ProjectM;

namespace Cobalt.Systems
{
    public class WeaponStatsSystem
    {
        public class PlayerStats
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

        public class StatManager
        {
            private static readonly ServerGameSettingsSystem serverGameSettingsSystem = VWorld.Server.GetExistingSystem<ServerGameSettingsSystem>();

            public class FocusSystem
            {
                public enum StatType
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

                public static readonly Dictionary<int, StatType> FocusStatMap = new()
                {
                { 0, StatType.MaxHealth },
                { 1, StatType.CastSpeed },
                { 2, StatType.AttackSpeed },
                { 3, StatType.PhysicalPower },
                { 4, StatType.SpellPower },
                { 5, StatType.PhysicalCritChance },
                { 6, StatType.PhysicalCritDamage },
                { 7, StatType.SpellCritChance },
                { 8, StatType.SpellCritDamage }
                };

                public class StatCaps
                {
                    private static Dictionary<StatType, float> baseCaps = new()
                {
                    {StatType.MaxHealth, 500},
                    {StatType.CastSpeed, 1f},
                    {StatType.AttackSpeed, 1f},
                    {StatType.PhysicalPower, 50},
                    {StatType.SpellPower, 50},
                    {StatType.PhysicalCritChance, 0.5f},
                    {StatType.PhysicalCritDamage, 2f},
                    {StatType.SpellCritChance, 0.5f},
                    {StatType.SpellCritDamage, 2f}
                };

                    public static Dictionary<StatType, float> BaseCaps { get => baseCaps; set => baseCaps = value; }
                }

                public class StatIncreases
                {
                    private static Dictionary<StatType, (float Increase, int MasteryCost)> baseIncreases = new()
                    {
                        {StatType.MaxHealth, (0.1f, 50)},
                        {StatType.CastSpeed, (0.01f, 30)},
                        {StatType.AttackSpeed, (0.01f, 30)},
                        {StatType.PhysicalPower, (5f, 40)},
                        {StatType.SpellPower, (5f, 40)},
                        {StatType.PhysicalCritChance, (0.01f, 20)},
                        {StatType.PhysicalCritDamage, (0.1f, 20)},
                        {StatType.SpellCritChance, (0.01f, 20)},
                        {StatType.SpellCritDamage, (0.1f, 20)}
                    };

                    public static Dictionary<StatType, (float Increase, int MasteryCost)> BaseIncreases
                    {
                        get => baseIncreases;
                        set => baseIncreases = value;
                    }
                }
            }
        }
    }
}