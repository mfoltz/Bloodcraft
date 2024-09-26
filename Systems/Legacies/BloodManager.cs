using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Systems.Legacies.BloodSystem;

namespace Bloodcraft.Systems.Legacies;
internal static class BloodManager
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystemSpawn => SystemService.ModifyUnitStatBuffSystem_Spawn;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    public static bool ChooseStat(ulong steamId, BloodType BloodType, BloodStatType statType)
    {
        if (steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(BloodType, out var Stats))
        {
            if (ConfigService.HardSynergies)
            {
                if (!ClassUtilities.HasClass(steamId))
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

                if (Stats.Count >= ConfigService.LegacyStatChoices || Stats.Contains(statType))
                {
                    return false; // Only allow configured amount of stats to be chosen and no duplicates
                }

                Stats.Add(statType);
                steamId.SetPlayerBloodStats(bloodStats);

                return true;
            }
            else
            {
                if (Stats.Count >= ConfigService.LegacyStatChoices || Stats.Contains(statType))
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
    public static void ApplyBloodStats(ulong steamId, BloodType bloodType, Entity bloodBuff)
    {
        IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
        if (handler != null && steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var bonuses))
        {
            if (!bloodBuff.Has<ModifyUnitStatBuff_DOTS>()) // add bonuses if doesn't have buffer
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

            ModifyUnitStatBuffSystemSpawn.OnUpdate();
        }
    }
    public static float CalculateScaledBloodBonus(IBloodHandler handler, ulong steamId, BloodType bloodType, BloodStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetLegacyData(steamId);
            float maxBonus = BloodStatValues[statType];

            if (Classes && steamId.TryGetPlayerClasses(out var classes) && classes.Count != 0)
            {
                var (_, classBloodStats) = classes.First().Value; // get class to check if stat allowed
                List<BloodStatType> bloodStatTypes = classBloodStats.Select(value => (BloodStatType)value).ToList();

                if (bloodStatTypes.Contains(statType))
                {
                    maxBonus *= ConfigService.StatSynergyMultiplier;
                }
            }

            if (ConfigService.PrestigeSystem && steamId.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(BloodTypeToPrestigeMap[bloodType], out var PrestigeData))
            {
                float gainFactor = 1 + (ConfigService.PrestigeStatMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }

            float scaledBonus = maxBonus * ((float)xpData.Key / ConfigService.MaxBloodLevel); // Scale bonus up to maxLevel then full effect
            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    public static void UpdateBloodStats(Entity player, User user, BloodType bloodType)
    {
        if (!BloodTypeToConsumeSourceMap.TryGetValue(bloodType, out var consumeSource)) return;
        else if (bloodType.Equals(BloodType.None)) return;

        Blood blood = player.Read<Blood>();
        float quality = blood.Quality;

        // applying same blood to player again and letting game handle the ModifyUnitStatDOTS is much easier than trying to handle it manually
        ConsumeBloodDebugEvent consumeBloodDebugEvent = new()
        {
            Amount = 0,
            Quality = quality,
            Source = consumeSource
        };

        Core.Log.LogInfo($"Consume Blood Event: {consumeBloodDebugEvent.Amount} {consumeBloodDebugEvent.Quality} {consumeBloodDebugEvent.Source.LookupName()}");
        DebugEventsSystem.ConsumeBloodEvent(user.Index, ref consumeBloodDebugEvent);
    }
    public static BloodType GetCurrentBloodType(Entity character)
    {
        Blood blood = character.Read<Blood>();
        return GetBloodTypeFromPrefab(blood.BloodType);
    }
    public static class BloodStats
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
