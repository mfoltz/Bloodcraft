using Bloodcraft.Services;
using ProjectM;
using Unity.Entities;
using static Bloodcraft.Systems.Legacies.BloodHandler.BloodStats;

namespace Bloodcraft.Systems.Legacies;
internal static class BloodHandler
{
    static EntityManager EntityManager => Core.EntityManager;
    
    public static bool ChooseStat(ulong steamId, BloodSystem.BloodType BloodType, BloodStats.BloodStatType statType)
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
    public static void ResetStats(ulong steamId, BloodSystem.BloodType BloodType)
    {
        if (Core.DataStructures.PlayerBloodStats.TryGetValue(steamId, out var bloodStats) && bloodStats.TryGetValue(BloodType, out var Stats))
        {
            Stats.Clear();
            Core.DataStructures.SavePlayerBloodStats();
        }
    }
    public static void ApplyBloodBonuses(ulong steamId, BloodSystem.BloodType bloodType, Entity bloodBuff)
    {
        IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
        if (Core.DataStructures.PlayerBloodStats.TryGetValue(steamId, out var bloodStats) && bloodStats.TryGetValue(bloodType, out var bonuses))
        {
            if (!bloodBuff.Has<ModifyUnitStatBuff_DOTS>())
            {
                EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(bloodBuff);
            }

            var buffer = bloodBuff.ReadBuffer<ModifyUnitStatBuff_DOTS>();
            foreach (var bloodStatType in bonuses)
            {
                float scaledBonus = CalculateScaledBloodBonus(handler, steamId, bloodType, bloodStatType);

                bool found = false;
                for (int i = 0; i < buffer.Length; i++)
                {
                    ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                    if (statBuff.StatType.Equals(BloodStatTypes[bloodStatType]))
                    {
                        statBuff.Value += scaledBonus; // Modify the value accordingly
                        buffer[i] = statBuff; // Assign the modified struct back to the buffer
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // If not found, create a new stat modifier
                    UnitStatType statType = BloodStatTypes[bloodStatType];
                    ModifyUnitStatBuff_DOTS newStatBuff = new()
                    {
                        StatType = statType,
                        ModificationType = ModificationType.AddToBase,
                        Value = scaledBonus,
                        Modifier = 1,
                        IncreaseByStacks = false,
                        ValueByStacks = 0,
                        Priority = 0,
                        Id = ModificationId.Empty
                    };
                    buffer.Add(newStatBuff);
                }
            }
        }
    }
    public static float CalculateScaledBloodBonus(IBloodHandler handler, ulong steamId, BloodSystem.BloodType bloodType, BloodHandler.BloodStats.BloodStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetLegacyData(steamId);
            float maxBonus = BloodHandler.BloodStats.BloodStatValues[statType];

            if (Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classes) && classes.Count > 0)
            {
                List<int> playerClassStats = classes.First().Value.Item2;
                List<BloodHandler.BloodStats.BloodStatType> bloodStatTypes = playerClassStats.Select(value => (BloodHandler.BloodStats.BloodStatType)value).ToList();
                if (bloodStatTypes.Contains(statType))
                {
                    maxBonus *= ConfigService.StatSynergyMultiplier;
                }
            }

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(BloodSystem.BloodPrestigeMap[bloodType], out var PrestigeData) && PrestigeData > 0)
            {
                float gainFactor = 1 + (ConfigService.PrestigeStatMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }

            float scaledBonus = maxBonus * ((float)xpData.Key / ConfigService.MaxBloodLevel); // Scale bonus up to maxLevel then full effect
            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    public static BloodSystem.BloodType GetCurrentBloodType(Entity character)
    {
        Blood blood = character.Read<Blood>();
        return BloodSystem.GetBloodTypeFromPrefab(blood.BloodType);
    }
    
    public class BloodStats
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
