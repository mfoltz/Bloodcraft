using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Unity.Entities;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Systems.Legacies.BloodSystem;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Utilities.Progression.ModifyUnitStatBuffSettings;

namespace Bloodcraft.Systems.Legacies;
internal static class BloodManager
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;

    static readonly float _synergyMultiplier = ConfigService.SynergyMultiplier;
    static readonly float _prestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
    static readonly int _maxLegacyLevel = ConfigService.MaxBloodLevel;
    static readonly int _legacyStatChoices = ConfigService.LegacyStatChoices;
    public static class BloodStats
    {
        public enum BloodStatType : int
        {
            HealingReceived = 0,
            DamageReduction = 1,
            PhysicalResistance = 2,
            SpellResistance = 3,
            ResourceYield = 4,
            ReducedBloodDrain = 5,
            SpellCooldownRecoveryRate = 6,
            WeaponCooldownRecoveryRate = 7,
            UltimateCooldownRecoveryRate = 8,
            MinionDamage = 9,
            AbilityAttackSpeed = 10,
            CorruptionDamageReduction = 11
        }
        public static IReadOnlyDictionary<BloodStatType, UnitStatType> BloodStatTypes => _bloodStatTypes;
        static readonly Dictionary<BloodStatType, UnitStatType> _bloodStatTypes = new()
        {
            { BloodStatType.HealingReceived, UnitStatType.HealingReceived },
            { BloodStatType.DamageReduction, UnitStatType.DamageReduction },
            { BloodStatType.PhysicalResistance, UnitStatType.PhysicalResistance },
            { BloodStatType.SpellResistance, UnitStatType.SpellResistance },
            { BloodStatType.ResourceYield, UnitStatType.ResourceYield },
            { BloodStatType.ReducedBloodDrain, UnitStatType.ReducedBloodDrain },
            { BloodStatType.SpellCooldownRecoveryRate, UnitStatType.SpellCooldownRecoveryRate },
            { BloodStatType.WeaponCooldownRecoveryRate, UnitStatType.WeaponCooldownRecoveryRate },
            { BloodStatType.UltimateCooldownRecoveryRate, UnitStatType.UltimateCooldownRecoveryRate },
            { BloodStatType.MinionDamage, UnitStatType.MinionDamage },
            { BloodStatType.AbilityAttackSpeed, UnitStatType.AbilityAttackSpeed },
            { BloodStatType.CorruptionDamageReduction, UnitStatType.CorruptionDamageReduction }
        };
        public static IReadOnlyDictionary<BloodStatType, float> BloodStatBaseCaps => _bloodStatBaseCaps;
        static readonly Dictionary<BloodStatType, float> _bloodStatBaseCaps = new()
        {
            {BloodStatType.HealingReceived, ConfigService.HealingReceived},
            {BloodStatType.DamageReduction, ConfigService.DamageReduction},
            {BloodStatType.PhysicalResistance, ConfigService.PhysicalResistance},
            {BloodStatType.SpellResistance, ConfigService.SpellResistance},
            {BloodStatType.ResourceYield, ConfigService.ResourceYield},
            {BloodStatType.ReducedBloodDrain, ConfigService.ReducedBloodDrain},
            {BloodStatType.SpellCooldownRecoveryRate, ConfigService.SpellCooldownRecoveryRate},
            {BloodStatType.WeaponCooldownRecoveryRate, ConfigService.WeaponCooldownRecoveryRate},
            {BloodStatType.UltimateCooldownRecoveryRate, ConfigService.UltimateCooldownRecoveryRate},
            {BloodStatType.MinionDamage, ConfigService.MinionDamage},
            {BloodStatType.AbilityAttackSpeed, ConfigService.AbilityAttackSpeed},
            {BloodStatType.CorruptionDamageReduction, ConfigService.CorruptionDamageReduction}
        };
    }
    public static bool ChooseStat(ulong steamId, BloodType bloodType, BloodStatType bloodStatType)
    {
        if (steamId.TryGetPlayerBloodStats(out var bloodTypeStats) && bloodTypeStats.TryGetValue(bloodType, out var bloodStats))
        {
            if (bloodStats.Count >= _legacyStatChoices || bloodStats.Contains(bloodStatType))
            {
                return false; // Only allow configured amount of stats to be chosen and no duplicates
            }

            bloodStats.Add(bloodStatType);
            steamId.SetPlayerBloodStats(bloodTypeStats);

            return true;
        }

        return false;
    }
    public static void ResetStats(ulong steamId, BloodType BloodType)
    {
        if (steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(BloodType, out var Stats))
        {
            Stats.Clear();
            steamId.SetPlayerBloodStats(bloodStats);
        }
    }
    public static void UpdateBloodStats(Entity buffEntity, Entity playerCharacter, ulong steamId)
    {
        BloodType bloodType = GetCurrentBloodType(playerCharacter.GetBlood());
        ApplyBloodStats(buffEntity, playerCharacter, bloodType, steamId);
    }
    public static void ApplyBloodStats(Entity buffEntity, Entity playerCharacter, BloodType bloodType, ulong steamId)
    {
        IBloodLegacy handler = BloodLegacyFactory.GetBloodHandler(bloodType);

        if (handler != null && steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var bloodStatTypes))
        {
            if (!buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
            {
                buffer = EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
            }

            foreach (BloodStatType bloodStatType in bloodStatTypes)
            {
                if (!TryGetScaledModifyUnitLegacyStat(handler, playerCharacter, steamId, bloodType,
                    bloodStatType, out float statValue, out ModifyUnitStatBuff modifyUnitStatBuff)) continue;

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

                // Core.Log.LogWarning($"[BloodManager] {newStatBuff.StatType} | {newStatBuff.Value} | {newStatBuff.AttributeCapType} | {newStatBuff.Id.Id}");
                buffer.Add(newStatBuff);
            }
        }
    }
    public static bool TryGetScaledModifyUnitLegacyStat(IBloodLegacy handler, Entity playerCharacter, ulong steamId,
        BloodType bloodType, BloodStatType bloodStatType, out float statValue, out ModifyUnitStatBuff modifyUnitStatBuff)
    {
        modifyUnitStatBuff = default;
        statValue = 0f;

        if (handler != null)
        {
            if (!ModifyUnitLegacyStatBuffs.TryGetValue(bloodStatType, out modifyUnitStatBuff))
            {
                return false;
            }

            var xpData = handler.GetLegacyData(steamId);
            float maxBonus = modifyUnitStatBuff.BaseCap;

            if (_classes && steamId.HasClass(out PlayerClass? playerClass)
                && playerClass.HasValue && ClassBloodStatSynergies[playerClass.Value].Contains(bloodStatType))
            {
                maxBonus *= _synergyMultiplier;
            }

            if (_prestige && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(BloodPrestigeTypes[bloodType], out var legacyPrestiges))
            {
                float gainFactor = 1 + (_prestigeStatMultiplier * legacyPrestiges);
                maxBonus *= gainFactor;
            }

            statValue = maxBonus * ((float)xpData.Key / _maxLegacyLevel);
            return true;
        }

        return false;
    }
    public static BloodType GetCurrentBloodType(Blood blood)
    {
        return GetBloodTypeFromPrefab(blood.BloodType);
    }
}
