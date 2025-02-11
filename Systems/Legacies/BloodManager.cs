using Bloodcraft.Services;
using ProjectM;
using Unity.Entities;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Systems.Legacies.BloodSystem;

namespace Bloodcraft.Systems.Legacies;
internal static class BloodManager
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _hardSynergies = ConfigService.HardSynergies;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _prestige = ConfigService.PrestigeSystem;

    static readonly float _statSynergyMultiplier = ConfigService.StatSynergyMultiplier;
    static readonly float _prestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
    static readonly int _maxLegacyLevel = ConfigService.MaxBloodLevel;
    static readonly int _legacyStatChoices = ConfigService.LegacyStatChoices;
    public static class BloodStats
    {
        public enum BloodStatType
        {
            HealingReceived, // 0
            DamageReduction, // 1
            PhysicalResistance, // 2
            SpellResistance, // 3
            ResourceYield, // 4
            BloodDrain, // 5
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
            { BloodStatType.BloodDrain, UnitStatType.BloodDrain },
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
            {BloodStatType.BloodDrain, ConfigService.BloodDrain},
            {BloodStatType.SpellCooldownRecoveryRate, ConfigService.SpellCooldownRecoveryRate},
            {BloodStatType.WeaponCooldownRecoveryRate, ConfigService.WeaponCooldownRecoveryRate},
            {BloodStatType.UltimateCooldownRecoveryRate, ConfigService.UltimateCooldownRecoveryRate},
            {BloodStatType.MinionDamage, ConfigService.MinionDamage},
            {BloodStatType.ShieldAbsorb, ConfigService.ShieldAbsorb},
            {BloodStatType.BloodEfficiency, ConfigService.BloodEfficiency}
        };
    }
    public static bool ChooseStat(ulong steamId, BloodType BloodType, BloodStatType statType)
    {
        if (steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(BloodType, out var Stats))
        {
            if (_hardSynergies)
            {
                if (!Utilities.Classes.HasClass(steamId))
                {
                    return false;
                }

                var classes = steamId.TryGetPlayerClasses(out var classData) ? classData : [];
                var (_, BloodStats) = classes.First().Value; // get class to check if stat allowed
                List<BloodStatType> bloodStatTypes = BloodStats.Select(value => (BloodStatType)value).ToList();

                if (!bloodStatTypes.Contains(statType)) // hard synergy stat check
                {
                    return false;
                }

                if (Stats.Count >= _legacyStatChoices || Stats.Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen and no duplicates
                }

                Stats.Add(statType);
                steamId.SetPlayerBloodStats(bloodStats);

                return true;
            }
            else
            {
                if (Stats.Count >= _legacyStatChoices || Stats.Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen and no duplicates
                }

                Stats.Add(statType);
                steamId.SetPlayerBloodStats(bloodStats);

                return true;
            }
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
        BloodType bloodType = GetCurrentBloodType(playerCharacter);
        ApplyBloodStats(buffEntity, bloodType, steamId);
    }
    public static void ApplyBloodStats(Entity bloodBuff, BloodType bloodType, ulong steamId)
    {
        IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);

        if (handler != null && steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var bonuses))
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
                        Id = ModificationIDs.Create().NewModificationId()
                    };

                    buffer.Add(newStatBuff);
                }
            }
        }
    }
    public static float CalculateScaledBloodBonus(IBloodHandler handler, ulong steamId, BloodType bloodType, BloodStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetLegacyData(steamId);
            float maxBonus = BloodStatValues[statType];

            if (_classes && steamId.TryGetPlayerClasses(out var classes) && classes.Count != 0)
            {
                var (_, classBloodStats) = classes.First().Value;
                List<BloodStatType> bloodStatTypes = classBloodStats.Select(value => (BloodStatType)value).ToList();

                if (bloodStatTypes.Contains(statType))
                {
                    maxBonus *= _statSynergyMultiplier;
                }
            }

            if (_prestige && steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(BloodTypeToPrestigeMap[bloodType], out var PrestigeData))
            {
                float gainFactor = 1 + (_prestigeStatMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }

            float scaledBonus = maxBonus * ((float)xpData.Key / _maxLegacyLevel);

            return scaledBonus;
        }

        return 0;
    }
    public static BloodType GetCurrentBloodType(Entity character)
    {
        Blood blood = character.Read<Blood>();
        return GetBloodTypeFromPrefab(blood.BloodType);
    }
}
