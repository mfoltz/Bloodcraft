using Bloodcraft.Services;
using ProjectM;
using Unity.Entities;
using static Bloodcraft.SystemUtilities.Expertise.WeaponHandler.WeaponStats;

namespace Bloodcraft.SystemUtilities.Expertise;
internal static class WeaponHandler
{
    static EntityManager EntityManager => Core.EntityManager;
    static ConfigService ConfigService => Core.ConfigService;

    public static bool ChooseStat(ulong steamId, WeaponSystem.WeaponType weaponType, WeaponStatType statType)
    {
        if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) || !weaponStats.TryGetValue(weaponType, out var Stats))
        {
            Stats = [];
            Core.DataStructures.PlayerWeaponStats[steamId][weaponType] = Stats;
        }

        if (ConfigService.HardSynergies)
        {
            if (!Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classes) || classes.Keys.Count == 0)
            {
                return false;
            }

            List<int> playerClassStats = classes.First().Value.Item1;
            List<WeaponStatType> weaponStatTypes = playerClassStats.Select(value => (WeaponStatType)value).ToList();

            if (!weaponStatTypes.Contains(statType))
            {
                return false;
            }

            if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= ConfigService.ExpertiseStatChoices || Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Contains(statType))
            {
                return false; // Only allow configured amount of stats to be chosen per weapon, one stat type per weapon
            }

            Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
            Core.DataStructures.SavePlayerWeaponStats();
            return true;
        }

        if (Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Count >= ConfigService.ExpertiseStatChoices || Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Contains(statType)) 
        {
            return false; // Only allow configured amount of stats to be chosen per weapon
        }

        Core.DataStructures.PlayerWeaponStats[steamId][weaponType].Add(statType);
        Core.DataStructures.SavePlayerWeaponStats();
        return true;
    }
    public static void ResetStats(ulong steamId, WeaponSystem.WeaponType weaponType)
    {
        if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
        {
            Stats.Clear();
            Core.DataStructures.SavePlayerWeaponStats();
        }
    }
    public static void ApplyWeaponBonuses(ulong steamId, WeaponSystem.WeaponType weaponType, Entity weaponEntity)
    {
        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
        {
            if (!weaponEntity.Has<ModifyUnitStatBuff_DOTS>())
            {
                EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(weaponEntity);
            }

            var buffer = weaponEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
            foreach (WeaponStatType weaponStatType in bonuses)
            {
                float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponType, weaponStatType);
                bool found = false;

                for (int i = 0; i < buffer.Length; i++)
                {
                    ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                    if (statBuff.StatType.Equals(WeaponStatTypes[weaponStatType])) // Assuming WeaponStatType can be cast to UnitStatType
                    {
                        if (weaponStatType.Equals(WeaponStatType.MovementSpeed))
                        {
                            break;
                        }
                        statBuff.Value += scaledBonus; // Modify the value accordingly
                        buffer[i] = statBuff; // Assign the modified struct back to the buffer
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    UnitStatType statType = WeaponStatTypes[weaponStatType];
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
    public static float CalculateScaledWeaponBonus(IExpertiseHandler handler, ulong steamId, WeaponSystem.WeaponType weaponType, WeaponStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetExpertiseData(steamId);
            float maxBonus = WeaponStatValues[statType];

            if (Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classes) && classes.Count > 0)
            {
                List<int> playerClassStats = classes.First().Value.Item1;
                List<WeaponStatType> weaponStatTypes = playerClassStats.Select(value => (WeaponStatType)value).ToList();

                if (weaponStatTypes.Contains(statType))
                {
                    maxBonus *= ConfigService.StatSynergyMultiplier;
                }
            }

            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(WeaponSystem.WeaponPrestigeMap[weaponType], out var PrestigeData) && PrestigeData > 0)
            {
                float gainFactor = 1 + (ConfigService.PrestigeStatMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }

            float scaledBonus = maxBonus * ((float)xpData.Key / ConfigService.MaxExpertiseLevel); // Scale bonus up to 99%
            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    public static WeaponSystem.WeaponType GetCurrentWeaponType(Entity character)
    {
        Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
        //if (weapon.Equals(Entity.Null)) return ExpertiseUtilities.WeaponType.Unarmed;
        return WeaponSystem.GetWeaponTypeFromSlotEntity(weapon);
    }
    
    public class WeaponStats
    {
        public enum WeaponStatType
        {
            MaxHealth, // 0
            MovementSpeed, // 1
            PrimaryAttackSpeed, // 2
            PhysicalLifeLeech, // 3
            SpellLifeLeech, // 4
            PrimaryLifeLeech, // 5
            PhysicalPower, // 6
            SpellPower, // 7
            PhysicalCritChance, // 8
            PhysicalCritDamage, // 9
            SpellCritChance, // 10
            SpellCritDamage // 11
        }

        public static readonly Dictionary<WeaponStatType, string> WeaponStatFormats = new()
        { 
            { WeaponStatType.MaxHealth, "integer" },
            { WeaponStatType.MovementSpeed, "decimal" },
            { WeaponStatType.PrimaryAttackSpeed, "percentage" },
            { WeaponStatType.PhysicalLifeLeech, "percentage" },
            { WeaponStatType.SpellLifeLeech, "percentage" },
            { WeaponStatType.PrimaryLifeLeech, "percentage" },
            { WeaponStatType.PhysicalPower, "integer" },
            { WeaponStatType.SpellPower, "integer" },
            { WeaponStatType.PhysicalCritChance, "percentage" },
            { WeaponStatType.PhysicalCritDamage, "percentage" },
            { WeaponStatType.SpellCritChance, "percentage" },
            { WeaponStatType.SpellCritDamage, "percentage" }
        };

        public static readonly Dictionary<WeaponStatType, UnitStatType> WeaponStatTypes = new()
        {
            { WeaponStatType.MaxHealth, UnitStatType.MaxHealth },
            { WeaponStatType.MovementSpeed, UnitStatType.MovementSpeed },
            { WeaponStatType.PrimaryAttackSpeed, UnitStatType.PrimaryAttackSpeed },
            { WeaponStatType.PhysicalLifeLeech, UnitStatType.PhysicalLifeLeech },
            { WeaponStatType.SpellLifeLeech, UnitStatType.SpellLifeLeech },
            { WeaponStatType.PrimaryLifeLeech, UnitStatType.PrimaryLifeLeech },
            { WeaponStatType.PhysicalPower, UnitStatType.PhysicalPower },
            { WeaponStatType.SpellPower, UnitStatType.SpellPower },
            { WeaponStatType.PhysicalCritChance, UnitStatType.PhysicalCriticalStrikeChance },
            { WeaponStatType.PhysicalCritDamage, UnitStatType.PhysicalCriticalStrikeDamage },
            { WeaponStatType.SpellCritChance, UnitStatType.SpellCriticalStrikeChance },
            { WeaponStatType.SpellCritDamage, UnitStatType.SpellCriticalStrikeDamage },
        };

        public static readonly Dictionary<WeaponStatType, float> WeaponStatValues = new()
        {
            {WeaponStatType.MaxHealth, ConfigService.MaxHealth},
            {WeaponStatType.MovementSpeed, ConfigService.MovementSpeed},
            {WeaponStatType.PrimaryAttackSpeed, ConfigService.PrimaryAttackSpeed},
            {WeaponStatType.PhysicalLifeLeech, ConfigService.PhysicalLifeLeech},
            {WeaponStatType.SpellLifeLeech, ConfigService.SpellLifeLeech},
            {WeaponStatType.PrimaryLifeLeech, ConfigService.PrimaryLifeLeech},
            {WeaponStatType.PhysicalPower, ConfigService.PhysicalPower},
            {WeaponStatType.SpellPower, ConfigService.SpellPower},
            {WeaponStatType.PhysicalCritChance, ConfigService.PhysicalCritChance},
            {WeaponStatType.PhysicalCritDamage, ConfigService.PhysicalCritDamage},
            {WeaponStatType.SpellCritChance, ConfigService.SpellCritChance},
            {WeaponStatType.SpellCritDamage, ConfigService.SpellCritDamage}
        };
    }
}
