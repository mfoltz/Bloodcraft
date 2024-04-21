using Bloodstone.API;
using ProjectM;

namespace Cobalt.Systems.Bloodline
{
    public class BloodlineStatsSystem
    {
        public class PlayerBloodlineStats
        {
            public float ResourceYield { get; set; }
            public float DurabilityLoss { get; set; }
            public float PhysicalResistance { get; set; }
            public float SpellResistance { get; set; }
            public float SunResistance { get; set; }
            public float FireResistance { get; set; }
            public float HolyResistance { get; set; }
            public float SilverResistance { get; set; }
            public float PassiveHealthRegene { get; set; }
        }

        public class BloodlineStatManager
        {
            public class BloodFocusSystem
            {
                public enum BloodStatType
                {
                    ResourceYield,
                    DurabilityLoss,
                    PhysicalResistance,
                    SpellResistance,
                    SunResistance,
                    FireResistance,
                    HolyResistance,
                    SilverResistance,
                    PassiveHealthRegene
                }

                public static readonly Dictionary<int, BloodStatType> BloodStatMap = new()
                {
                { 0, BloodStatType.ResourceYield },
                { 1, BloodStatType.DurabilityLoss },
                { 2, BloodStatType.PhysicalResistance },
                { 3, BloodStatType.SpellResistance },
                { 4, BloodStatType.SunResistance },
                { 5, BloodStatType.FireResistance },
                { 6, BloodStatType.HolyResistance },
                { 7, BloodStatType.SilverResistance },
                { 8, BloodStatType.PassiveHealthRegene }
                };

                public class BloodStatCaps
                {
                    private static Dictionary<BloodStatType, float> baseCaps = new()
                    {
                        {BloodStatType.ResourceYield, 1f},
                        {BloodStatType.DurabilityLoss, 1f},
                        {BloodStatType.PhysicalResistance, 0.25f},
                        {BloodStatType.SpellResistance, 0.25f},
                        {BloodStatType.SunResistance, 100f},
                        {BloodStatType.FireResistance, 100f},
                        {BloodStatType.HolyResistance, 100f},
                        {BloodStatType.SilverResistance, 100f},
                        {BloodStatType.PassiveHealthRegene, 2.5f}
                    };

                    public static Dictionary<BloodStatType, float> BaseCaps 
                    { 
                        get => baseCaps; 
                        set => baseCaps = value; 
                    }
                }

                public class BloodStatIncreases
                {
                    private static Dictionary<BloodStatType, (float Increase, int BloodCost)> baseIncreases = new()
                    {
                        {BloodStatType.ResourceYield, (0.01f, 100)},
                        {BloodStatType.DurabilityLoss, (0.01f, 50)},
                        {BloodStatType.PhysicalResistance, (0.01f, 150)},
                        {BloodStatType.SpellResistance, (0.01f, 150)},
                        {BloodStatType.SunResistance, (1f, 50)},
                        {BloodStatType.FireResistance, (1f, 50)},
                        {BloodStatType.HolyResistance, (1f, 50)},
                        {BloodStatType.SilverResistance, (1f, 50)},
                        {BloodStatType.PassiveHealthRegene, (0.05f, 100)}
                    };

                    public static Dictionary<BloodStatType, (float Increase, int BloodCost)> BaseIncreases
                    {
                        get => baseIncreases;
                        set => baseIncreases = value;
                    }
                }
            }
        }
    }
}