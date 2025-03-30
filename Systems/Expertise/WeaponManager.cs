using Bloodcraft.Services;
using ProjectM;
using Unity.Entities;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;

namespace Bloodcraft.Systems.Expertise;
internal static class WeaponManager
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _hardSynergies = ConfigService.HardSynergies;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _prestige = ConfigService.PrestigeSystem;

    static readonly float _statSynergyMultiplier = ConfigService.StatSynergyMultiplier;
    static readonly float _prestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
    static readonly int _maxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    static readonly int _expertiseStatChoices = ConfigService.ExpertiseStatChoices;
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
    public static bool ChooseStat(ulong steamId, WeaponType weaponType, WeaponStatType statType)
    {
        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
        {
            if (_hardSynergies)
            {
                if (!Utilities.Classes.HasClass(steamId))
                {
                    return false;
                }

                var classes = steamId.TryGetPlayerClasses(out var classData) ? classData : [];
                var (WeaponStats, _) = classes.First().Value; // get class to check if stat allowed
                List<WeaponStatType> weaponStatTypes = [..WeaponStats.Select(value => (WeaponStatType)value)];

                if (!weaponStatTypes.Contains(statType)) // hard synergy stat check
                {
                    return false;
                }

                if (Stats.Count >= _expertiseStatChoices || Stats.Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen and no duplicates
                }

                Stats.Add(statType);
                steamId.SetPlayerWeaponStats(weaponStats);

                return true;
            }
            else
            {
                if (Stats.Count >= _expertiseStatChoices || Stats.Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen and no duplicates
                }

                Stats.Add(statType);
                steamId.SetPlayerWeaponStats(weaponStats);

                return true;
            }
        }
        return false;
    }
    public static void ResetStats(ulong steamId, WeaponType weaponType)
    {
        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var Stats))
        {
            Stats.Clear();
            steamId.SetPlayerWeaponStats(weaponStats);
        }
    }
    public static void UpdateWeaponStats(Entity buffEntity, Entity playerCharacter, ulong steamId)
    {
        WeaponType weaponType = GetCurrentWeaponType(playerCharacter);
        ApplyWeaponStats(buffEntity, weaponType, steamId);
    }
    public static void ApplyWeaponStats(Entity buffEntity, WeaponType weaponType, ulong steamId)
    {
        IWeaponHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
        {
            if (!buffEntity.Has<ModifyUnitStatBuff_DOTS>())
            {
                EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
            }

            var buffer = buffEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();

            foreach (WeaponStatType weaponStatType in bonuses)
            {
                float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponType, weaponStatType);
                UnitStatType statType = WeaponStatTypes[weaponStatType];

                ModifyUnitStatBuff_DOTS newStatBuff = new()
                {
                    StatType = statType,
                    ModificationType = !statType.Equals(UnitStatType.MovementSpeed) ? ModificationType.AddToBase : ModificationType.MultiplyBaseAdd,
                    Value = scaledBonus,
                    Modifier = 1,
                    IncreaseByStacks = false,
                    ValueByStacks = 0,
                    Priority = 0,
                    Id = ModificationIDs.Create().NewModificationId()
                };

                buffer.Add(newStatBuff);
            }
        }
    }
    public static float CalculateScaledWeaponBonus(IWeaponHandler handler, ulong steamId, WeaponType weaponType, WeaponStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetExpertiseData(steamId);
            float maxBonus = WeaponStatValues[statType];

            if (_classes && steamId.TryGetPlayerClasses(out var classes) && classes.Count != 0)
            {
                var (classWeaponStats, _) = classes.First().Value; // get class to check if stat allowed
                List<WeaponStatType> weaponStatTypes = [..classWeaponStats.Select(value => (WeaponStatType)value)];

                if (weaponStatTypes.Contains(statType))
                {
                    maxBonus *= _statSynergyMultiplier;
                }
            }

            if (_prestige && steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(WeaponSystem.WeaponPrestigeMap[weaponType], out var PrestigeData))
            {
                float gainFactor = 1 + (_prestigeStatMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }

            float scaledBonus = maxBonus * ((float)xpData.Key / _maxExpertiseLevel); // Scale bonus up to 99%
            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    public static WeaponType GetCurrentWeaponType(Entity character)
    {
        Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
        return WeaponSystem.GetWeaponTypeFromWeaponEntity(weapon);
    }
}
