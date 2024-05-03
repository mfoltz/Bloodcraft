using Cobalt.Core;

namespace Cobalt.Systems.Bloodline
{
    public class BloodStatsSystem
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
        }

        public class PlayerBloodUtilities
        {
            public static bool ChooseStat(ulong steamId, string statType)
            {
                if (!DataStructures.PlayerBloodChoices.ContainsKey(steamId))
                    DataStructures.PlayerBloodChoices[steamId] = [];

                if (DataStructures.PlayerBloodChoices[steamId].Count >= 3)
                {
                    return false; // Only allow 3 stats to be chosen
                }

                DataStructures.PlayerBloodChoices[steamId].Add(statType);
                DataStructures.SavePlayerBloodChoices();
                return true;
            }

            public static void ResetChosenStats(ulong steamId)
            {
                if (DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var bloodChoices))
                {
                    bloodChoices.Clear();
                    DataStructures.SavePlayerBloodChoices();
                }
            }
        }

        public class BloodStatManager
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