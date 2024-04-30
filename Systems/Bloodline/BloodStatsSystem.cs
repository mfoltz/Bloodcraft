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
            public float PassiveHealthRegen { get; set; }

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

            public float GetStatValue(BloodMasteryStatManager.BloodFocusSystem.BloodStatType statType)
            {
                return statType switch
                {
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.ResourceYield => ResourceYield,
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.PhysicalResistance => PhysicalResistance,
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SpellResistance => SpellResistance,
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SunResistance => SunResistance,
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.FireResistance => FireResistance,
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.HolyResistance => HolyResistance,
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.SilverResistance => SilverResistance,
                    BloodMasteryStatManager.BloodFocusSystem.BloodStatType.PassiveHealthRegen => PassiveHealthRegen,
                    _ => throw new InvalidOperationException("Unknown stat type"),
                };
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

                private static readonly Dictionary<BloodStatType, float> baseCaps = new()
                {
                    {BloodStatType.ResourceYield, 1f},
                    {BloodStatType.PhysicalResistance, 0.25f},
                    {BloodStatType.SpellResistance, 0.25f},
                    {BloodStatType.SunResistance, 100f},
                    {BloodStatType.FireResistance, 100f},
                    {BloodStatType.HolyResistance, 100f},
                    {BloodStatType.SilverResistance, 100f},
                };

                public static Dictionary<BloodStatType, float> BaseCaps
                {
                    get => baseCaps;
                }
            }
        }
    }
}