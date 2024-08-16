using Bloodcraft.Services;
using ProjectM;
using static Bloodcraft.SystemUtilities.Legacies.LegacyStats.BloodStatManager;

namespace Bloodcraft.SystemUtilities.Legacies;
internal static class LegacyStats
{
    static ConfigService ConfigService => Core.ConfigService;
    public class PlayerBloodUtilities
    {
        public static bool ChooseStat(ulong steamId, LegacyUtilities.BloodType BloodType, BloodStatManager.BloodStatType statType)
        {
            if (!Core.DataStructures.PlayerBloodStats.TryGetValue(steamId, out var bloodStats) || !bloodStats.TryGetValue(BloodType, out var Stats))
            {
                Stats = [];
                Core.DataStructures.PlayerBloodStats[steamId][BloodType] = Stats;
            }

            if (ConfigService.HardSynergies)
            {
                if (!Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classes) || classes.Count == 0)
                {
                    return false;
                }
                
                List<int> playerClassStats = classes.First().Value.Item2;

                List<BloodStatType> weaponStatTypes = playerClassStats.Select(value => (BloodStatType)value).ToList();

                if (!weaponStatTypes.Contains(statType))
                {
                    return false;
                }

                if (Core.DataStructures.PlayerBloodStats[steamId][BloodType].Count >= ConfigService.LegacyStatChoices || Core.DataStructures.PlayerBloodStats[steamId][BloodType].Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen per blood, only allow one stat type per blood
                }

                Core.DataStructures.PlayerBloodStats[steamId][BloodType].Add(statType);
                Core.DataStructures.SavePlayerWeaponStats();
                return true;
            }

            if (Core.DataStructures.PlayerBloodStats[steamId][BloodType].Count >= ConfigService.LegacyStatChoices || Core.DataStructures.PlayerBloodStats[steamId][BloodType].Contains(statType))
            {
                return false; // Only allow configured amount of stats to be chosen per weapon
            }
            
            Core.DataStructures.PlayerBloodStats[steamId][BloodType].Add(statType);
            Core.DataStructures.SavePlayerBloodStats();
            return true;
        }
        public static void ResetStats(ulong steamId, LegacyUtilities.BloodType BloodType)
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
            HealingReceived, // 0
            DamageReduction, // 1
            PhysicalResistance, // 2
            SpellResistance, // 3
            ResourceYield, // 4
            CCReduction, // 5
            SpellCooldownRecoveryRate, // 6
            WeaponCooldownRecoveryRate, // 7
            UltimateCooldownRecoveryRate, // 8
            MinionDamage, // 9
            ShieldAbsorb, // 10
            BloodEfficiency // 11
        }

        public static readonly Dictionary<BloodStatType, UnitStatType> BloodStatTypes = new()
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

        public static readonly Dictionary<BloodStatType, float> BloodStatValues = new()
        {
            {BloodStatType.HealingReceived, ConfigService.HealingReceived},
            {BloodStatType.DamageReduction, ConfigService.DamageReduction},
            {BloodStatType.PhysicalResistance, ConfigService.PhysicalResistance},
            {BloodStatType.SpellResistance, ConfigService.SpellResistance},
            {BloodStatType.ResourceYield, ConfigService.ResourceYield},
            {BloodStatType.CCReduction, ConfigService.CCReduction},
            {BloodStatType.SpellCooldownRecoveryRate, ConfigService.SpellCooldownRecoveryRate},
            {BloodStatType.WeaponCooldownRecoveryRate, ConfigService.WeaponCooldownRecoveryRate},
            {BloodStatType.UltimateCooldownRecoveryRate, ConfigService.UltimateCooldownRecoveryRate},
            {BloodStatType.MinionDamage, ConfigService.MinionDamage},
            {BloodStatType.ShieldAbsorb, ConfigService.ShieldAbsorb},
            {BloodStatType.BloodEfficiency, ConfigService.BloodEfficiency}
        };
    }
}
