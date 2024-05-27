using Bloodcraft.Systems.Legacy;
using ProjectM;

namespace Bloodcraft.Systems.Legacies
{
    public class BloodStats
    {
        public class PlayerBloodUtilities
        {
            public static bool ChooseStat(ulong steamId, BloodSystem.BloodType BloodType, BloodStatManager.BloodStatType statType)
            {
                if (!Core.DataStructures.PlayerBloodStats.TryGetValue(steamId, out var bloodStats) || !bloodStats.TryGetValue(BloodType, out var Stats))
                {
                    Stats = [];
                    Core.DataStructures.PlayerBloodStats[steamId][BloodType] = Stats;
                }

                if (Core.DataStructures.PlayerBloodStats[steamId][BloodType].Count >= Plugin.LegacyStatChoices.Value)
                {
                    return false; // Only allow configured amount of stats to be chosen per weapon
                }

                Core.DataStructures.PlayerBloodStats[steamId][BloodType].Add(statType);
                Core.DataStructures.SavePlayerBloodStats();
                return true;
            }

            public static void ResetStats(ulong steamId, BloodSystem.BloodType BloodType)
            {
                if (Core.DataStructures.PlayerBloodStats.TryGetValue(steamId, out var bloodStats) && bloodStats.TryGetValue(BloodType, out var Stats))
                {
                    Stats.Clear();
                    Core.DataStructures.SavePlayerBloodStats();
                }
            }
        }

        public class BloodStatManager
        {
            public enum BloodStatType
            {
                HealingReceived,
                DamageReduction,
                PhysicalResistance,
                SpellResistance,
                BloodDrain,
                CCReduction,
                SpellCooldownRecoveryRate,
                WeaponCooldownRecoveryRate,
                UltimateCooldownRecoveryRate,
                MinionDamage,
                ShieldAbsorb,
                BloodEfficiency
            }

            public static readonly Dictionary<BloodStatType, UnitStatType> BloodStatMap = new()
                {
                    { BloodStatType.HealingReceived, UnitStatType.HealingReceived },
                    { BloodStatType.DamageReduction, UnitStatType.DamageReduction },
                    { BloodStatType.PhysicalResistance, UnitStatType.PhysicalResistance },
                    { BloodStatType.SpellResistance, UnitStatType.SpellResistance },
                    { BloodStatType.BloodDrain, UnitStatType.BloodDrain },
                    { BloodStatType.CCReduction, UnitStatType.CCReduction },
                    { BloodStatType.SpellCooldownRecoveryRate, UnitStatType.SpellCooldownRecoveryRate },
                    { BloodStatType.WeaponCooldownRecoveryRate, UnitStatType.WeaponCooldownRecoveryRate },
                    { BloodStatType.UltimateCooldownRecoveryRate, UnitStatType.UltimateCooldownRecoveryRate },
                    { BloodStatType.MinionDamage, UnitStatType.MinionDamage },
                    { BloodStatType.ShieldAbsorb, UnitStatType.ShieldAbsorb },
                    { BloodStatType.BloodEfficiency, UnitStatType.BloodEfficiency }
                };

            static readonly Dictionary<BloodStatType, float> baseCaps = new()
                {
                    {BloodStatType.HealingReceived, Plugin.HealingReceived.Value},
                    {BloodStatType.DamageReduction, Plugin.DamageReduction.Value},
                    {BloodStatType.PhysicalResistance, Plugin.PhysicalResistance.Value},
                    {BloodStatType.SpellResistance, Plugin.SpellResistance.Value},
                    {BloodStatType.BloodDrain, Plugin.BloodDrain.Value},
                    {BloodStatType.CCReduction, Plugin.CCReduction.Value},
                    {BloodStatType.SpellCooldownRecoveryRate, Plugin.SpellCooldownRecoveryRate.Value},
                    {BloodStatType.WeaponCooldownRecoveryRate, Plugin.WeaponCooldownRecoveryRate.Value},
                    {BloodStatType.UltimateCooldownRecoveryRate, Plugin.UltimateCooldownRecoveryRate.Value},
                    {BloodStatType.MinionDamage, Plugin.MinionDamage.Value},
                    {BloodStatType.ShieldAbsorb, Plugin.ShieldAbsorb.Value},
                    {BloodStatType.BloodEfficiency, Plugin.BloodEfficiency.Value}
                };

            public static Dictionary<BloodStatType, float> BaseCaps
            {
                get => baseCaps;
            }
        }
    }
}