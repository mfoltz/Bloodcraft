using Bloodcraft.Systems.Legacy;
using ProjectM;
using static Bloodcraft.Systems.Legacies.BloodStats.BloodStatManager;
using static Bloodcraft.Systems.Legacy.BloodSystem;

namespace Bloodcraft.Systems.Legacies;
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

            if (Plugin.HardSynergies.Value)
            {
                if (!Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) || classes.Count == 0)
                {
                    return false;
                }
                
                List<int> playerClassStats = classes.First().Value.Item2;

                List<BloodStatType> weaponStatTypes = playerClassStats.Select(value => (BloodStatType)value).ToList();

                if (!weaponStatTypes.Contains(statType))
                {
                    return false;
                }
                if (Core.DataStructures.PlayerBloodStats[steamId][BloodType].Count >= Plugin.LegacyStatChoices.Value || Core.DataStructures.PlayerBloodStats[steamId][BloodType].Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen per blood, only allow one stat type per blood
                }
                Core.DataStructures.PlayerBloodStats[steamId][BloodType].Add(statType);
                Core.DataStructures.SavePlayerWeaponStats();
                return true;
            }

            if (Core.DataStructures.PlayerBloodStats[steamId][BloodType].Count >= Plugin.LegacyStatChoices.Value || Core.DataStructures.PlayerBloodStats[steamId][BloodType].Contains(statType))
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
            ResourceYield,
            CCReduction,
            SpellCooldownRecoveryRate,
            WeaponCooldownRecoveryRate,
            UltimateCooldownRecoveryRate,
            MinionDamage,
            ShieldAbsorb,
            BloodEfficiency
        }

        public static readonly Dictionary<BloodType, List<BloodStatType>> BloodSynergyMap = new()
            {
                { BloodType.Worker, new List<BloodStatType> { BloodStatType.ResourceYield, BloodStatType.CCReduction } },
                { BloodType.Warrior, new List<BloodStatType> { BloodStatType.DamageReduction, BloodStatType.PhysicalResistance } },
                { BloodType.Scholar, new List<BloodStatType> { BloodStatType.SpellResistance, BloodStatType.SpellCooldownRecoveryRate } },
                { BloodType.Rogue, new List<BloodStatType> { BloodStatType.CCReduction, BloodStatType.MinionDamage } },
                { BloodType.Mutant, new List<BloodStatType> { BloodStatType.SpellCooldownRecoveryRate} },
                { BloodType.Draculin, new List<BloodStatType> { BloodStatType.SpellResistance, BloodStatType.UltimateCooldownRecoveryRate } },
                { BloodType.Immortal, new List<BloodStatType> { BloodStatType.WeaponCooldownRecoveryRate, BloodStatType.ResourceYield } },
                { BloodType.Creature, new List<BloodStatType> { BloodStatType.HealingReceived, BloodStatType.MinionDamage } },
                { BloodType.Brute, new List<BloodStatType> { BloodStatType.ShieldAbsorb, BloodStatType.SpellCooldownRecoveryRate} }
            };

        public static readonly Dictionary<BloodStatType, UnitStatType> BloodStatMap = new()
            {
                { BloodStatType.HealingReceived, UnitStatType.HealingReceived },
                { BloodStatType.DamageReduction, UnitStatType.DamageReduction },
                { BloodStatType.PhysicalResistance, UnitStatType.PhysicalResistance },
                { BloodStatType.SpellResistance, UnitStatType.SpellResistance },
                { BloodStatType.ResourceYield, UnitStatType.ResourceYield },
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
                {BloodStatType.ResourceYield, Plugin.ResourceYield.Value},
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