using Bloodstone.API;
using ProjectM;

namespace Cobalt.Systems.Bloodline
{
    public class BloodMasteryStatsSystem
    {
        public class BloodMasteryStats
        {
            // Stat properties
            public float ResourceYield { get; set; }
            public float PhysicalResistance { get; set; }
            public float SpellResistance { get; set; }
            public float SunResistance { get; set; }
            public float FireResistance { get; set; }
            public float HolyResistance { get; set; }
            public float SilverResistance { get; set; }
            public float PassiveHealthRegene { get; set; }

            // Tracking which stats have been chosen (not the actual values)
            public HashSet<BloodMasteryStatManager.BloodFocusSystem.BloodStatType> ChosenStats { get; private set; } = [];

            public void ChooseStat(BloodMasteryStatManager.BloodFocusSystem.BloodStatType statType)
            {
                if (ChosenStats.Count >= 2)
                {
                    throw new InvalidOperationException("Cannot choose more than two stats without resetting.");
                }

                ChosenStats.Add(statType);
            }

            public void ResetChosenStats()
            {
                ChosenStats.Clear();
            }

            public int StatsChosen => ChosenStats.Count;
        }

        public class BloodMasteryStatManager
        {
            public class BloodFocusSystem
            {
                public enum BloodStatType
                {
                    ResourceYield,
                    PhysicalResistance,
                    SpellResistance,
                    SunResistance,
                    FireResistance,
                    HolyResistance,
                    SilverResistance,
                    PassiveHealthRegen
                }

                public static readonly Dictionary<int, BloodStatType> BloodStatMap = new()
                {
                    { 0, BloodStatType.ResourceYield },
                    { 1, BloodStatType.PhysicalResistance },
                    { 2, BloodStatType.SpellResistance },
                    { 3, BloodStatType.SunResistance },
                    { 4, BloodStatType.FireResistance },
                    { 5, BloodStatType.HolyResistance },
                    { 6, BloodStatType.SilverResistance },
                    { 7, BloodStatType.PassiveHealthRegen }
                };
            }
        }
    }
}