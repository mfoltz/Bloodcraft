using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Unity.Entities;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Expertise.WeaponSystem;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Utilities.Progression.ModifyUnitStatBuffSettings;
using WeaponType = Bloodcraft.Interfaces.WeaponType;

namespace Bloodcraft.Systems.Expertise;
internal static class WeaponManager
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;

    static readonly float _synergyMultiplier = ConfigService.SynergyMultiplier;
    static readonly float _prestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
    static readonly int _maxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    static readonly int _expertiseStatChoices = ConfigService.ExpertiseStatChoices;
    public static class WeaponStats
    {
        public enum WeaponStatType : int
        {
            MaxHealth = 0,
            MovementSpeed = 1,
            PrimaryAttackSpeed = 2,
            PhysicalLifeLeech = 3,
            SpellLifeLeech = 4,
            PrimaryLifeLeech = 5,
            PhysicalPower = 6,
            SpellPower = 7,
            PhysicalCritChance = 8,
            PhysicalCritDamage = 9,
            SpellCritChance = 10,
            SpellCritDamage = 11
        }
        public static IReadOnlyDictionary<WeaponStatType, string> WeaponStatFormats => _weaponStatFormats;
        static readonly Dictionary<WeaponStatType, string> _weaponStatFormats = new()
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
        public static IReadOnlyDictionary<WeaponStatType, UnitStatType> WeaponStatTypes => _weaponStatTypes;
        static readonly Dictionary<WeaponStatType, UnitStatType> _weaponStatTypes = new()
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
        public static IReadOnlyDictionary<WeaponStatType, float> WeaponStatBaseCaps => _weaponStatBaseCaps;
        static readonly Dictionary<WeaponStatType, float> _weaponStatBaseCaps = new()
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
    public static bool ChooseStat(ulong steamId, WeaponType weaponType, WeaponStatType weaponStatType)
    {
        if (steamId.TryGetPlayerWeaponStats(out var weaponTypeStats) && weaponTypeStats.TryGetValue(weaponType, out var weaponStats))
        {
            if (weaponStats.Count >= _expertiseStatChoices || weaponStats.Contains(weaponStatType))
            {
                return false;
            }

            weaponStats.Add(weaponStatType);
            steamId.SetPlayerWeaponStats(weaponTypeStats);

            return true;
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
        ApplyWeaponStats(buffEntity, playerCharacter, steamId, weaponType);
    }
    public static void ApplyWeaponStats(Entity buffEntity, Entity playerCharacter, ulong steamId, WeaponType weaponType)
    {
        IWeaponExpertise handler = WeaponExpertiseFactory.GetExpertise(weaponType);

        if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
        {
            if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
            {
                buffer = EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
            }

            foreach (WeaponStatType weaponStatType in bonuses)
            {
                if (!TryGetScaledModifyUnitExpertiseStat(handler, playerCharacter, steamId, weaponType,
                    weaponStatType, out float statValue, out ModifyUnitStatBuff modifyUnitStatBuff)) continue;

                ModifyUnitStatBuff_DOTS newStatBuff = new()
                {
                    StatType = modifyUnitStatBuff.TargetUnitStat,
                    ModificationType = modifyUnitStatBuff.ModificationType,
                    AttributeCapType = modifyUnitStatBuff.AttributeCapType,
                    SoftCapValue = 0f,
                    Value = statValue,
                    Modifier = 1,
                    IncreaseByStacks = false,
                    ValueByStacks = 0,
                    Priority = 0,
                    Id = ModificationIDs.Create().NewModificationId()
                };

                // Core.Log.LogWarning($"[WeaponManager] {newStatBuff.StatType} | {newStatBuff.Value} | {newStatBuff.AttributeCapType} | {newStatBuff.Id.Id}");
                buffer.Add(newStatBuff);
            }
        }
    }
    public static bool TryGetScaledModifyUnitExpertiseStat(IWeaponExpertise handler, Entity playerCharacter, ulong steamId,
        WeaponType weaponType, WeaponStatType weaponStatType, out float statValue, out ModifyUnitStatBuff modifyUnitStatBuff)
    {
        modifyUnitStatBuff = default;
        statValue = 0f;

        if (handler != null)
        {
            if (!ModifyUnitExpertiseStatBuffs.TryGetValue(weaponStatType, out modifyUnitStatBuff))
            {
                return false;
            }

            var xpData = handler.GetExpertiseData(steamId);
            float maxBonus = modifyUnitStatBuff.BaseCap;

            if (_classes && steamId.HasClass(out PlayerClass? playerClass)
                && playerClass.HasValue && ClassWeaponStatSynergies[playerClass.Value].Contains(weaponStatType))
            {
                maxBonus *= _synergyMultiplier;
            }

            if (_prestige && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(WeaponPrestigeTypes[weaponType], out var expertisePrestiges))
            {
                float gainFactor = 1 + (_prestigeStatMultiplier * expertisePrestiges);
                maxBonus *= gainFactor;
            }

            /*
            try
            {
                if (playerCharacter.TryGetComponent(out VampireAttributeCapModificationsSource capModificationsSource)
                    && capModificationsSource.ModificationsEntity.TryGetComponent(out VampireAttributeCapModifications capModifications)
                    && WeaponStatTypes.TryGetValue(weaponStatType, out UnitStatType unitStatType))
                {
                    AttributeCapModIds capModIds = capModifications.CapModIds.GetCap(unitStatType);
                    capModId = modifyUnitStatBuff.AttributeCapType.Equals(AttributeCapType.SoftCapped) ?
                        capModIds.SoftCapModId : capModIds.HardCapModId;
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"[WeaponManager] Error getting cap modifications: {ex}");
            }
            */

            statValue = maxBonus * ((float)xpData.Key / _maxExpertiseLevel);
            return true;
        }

        return false;
    }
    public static WeaponType GetCurrentWeaponType(Entity character)
    {
        Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
        return GetWeaponTypeFromWeaponEntity(weapon);
    }
}
