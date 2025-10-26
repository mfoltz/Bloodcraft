using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Utilities.Progression.PlayerProgressionCacheManager;
using User = ProjectM.Network.User;

namespace Bloodcraft.Utilities;
internal static class Progression
{
    static SystemService SystemService => Core.SystemService;
    static UserActivityGridSystem UserActivityGridSystem => SystemService.UserActivityGridSystem;
    static ServerBootstrapSystem ServerBootstrapSystem => SystemService.ServerBootstrapSystem;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _isPvE = _gameMode.Equals(GameModeType.PvE);
    static readonly bool _expShare = ConfigService.ExpShare;
    static readonly int _shareLevelRange = ConfigService.ExpShareLevelRange;
    static readonly float _shareDistance = ConfigService.ExpShareDistance;

    static readonly PrefabGUID _pveCombatBuff = Buffs.PvECombatBuff;
    static readonly PrefabGUID _draculaVBlood = PrefabGUIDs.CHAR_Vampire_Dracula_VBlood;
    static readonly PrefabGUID _megaraVBlood = PrefabGUIDs.CHAR_Blackfang_Morgana_VBlood;

    const float EXP_CONSTANT = 0.1f;
    const float EXP_POWER = 2f;
    const int SIMULATED_LEVEL_FACTOR = 2;
    public static class ModifyUnitStatBuffSettings
    {
        public class ModifyUnitStatBuff(
            Enum sourceStatType,
            UnitStatType targetUnitStat,
            ModificationType modificationType = ModificationType.Add,
            AttributeCapType capType = AttributeCapType.Uncapped,
            bool invert = false)
        {
            public Enum SourceStatType { get; } = sourceStatType;
            public UnitStatType TargetUnitStat { get; } = targetUnitStat;
            public ModificationType ModificationType { get; } = modificationType;
            public AttributeCapType AttributeCapType { get; } = capType;
            public float BaseCap { get; } = GetBaseCapValue(sourceStatType);
            public bool Invert { get; } = invert;
            static float GetBaseCapValue(object statType)
            {
                return statType switch
                {
                    BloodStatType blood when BloodStatBaseCaps.TryGetValue(blood, out var val) => val,
                    WeaponStatType weapon when WeaponStatBaseCaps.TryGetValue(weapon, out var val) => val,
                    ClassStatType cls when ClassStatBaseCaps.TryGetValue(cls, out var val) => val,
                    _ => 0f
                };
            }
        }
        public static IReadOnlyDictionary<WeaponStatType, ModifyUnitStatBuff> ModifyUnitExpertiseStatBuffs => _modifyUnitExpertiseStatBuffs;
        static readonly Dictionary<WeaponStatType, ModifyUnitStatBuff> _modifyUnitExpertiseStatBuffs = new()
        {
            { WeaponStatType.MaxHealth, new ModifyUnitStatBuff(WeaponStatType.MaxHealth, UnitStatType.MaxHealth) },
            { WeaponStatType.MovementSpeed, new ModifyUnitStatBuff(WeaponStatType.MovementSpeed, UnitStatType.MovementSpeed) },
            { WeaponStatType.PrimaryAttackSpeed, new ModifyUnitStatBuff(WeaponStatType.PrimaryAttackSpeed, UnitStatType.PrimaryAttackSpeed) },
            { WeaponStatType.PhysicalLifeLeech, new ModifyUnitStatBuff(WeaponStatType.PhysicalLifeLeech, UnitStatType.PhysicalLifeLeech) },
            { WeaponStatType.SpellLifeLeech, new ModifyUnitStatBuff(WeaponStatType.SpellLifeLeech, UnitStatType.SpellLifeLeech) },
            { WeaponStatType.PrimaryLifeLeech, new ModifyUnitStatBuff(WeaponStatType.PrimaryLifeLeech, UnitStatType.PrimaryLifeLeech) },
            { WeaponStatType.PhysicalPower, new ModifyUnitStatBuff(WeaponStatType.PhysicalPower, UnitStatType.PhysicalPower) },
            { WeaponStatType.SpellPower, new ModifyUnitStatBuff(WeaponStatType.SpellPower, UnitStatType.SpellPower) },
            { WeaponStatType.PhysicalCritChance, new ModifyUnitStatBuff(WeaponStatType.PhysicalCritChance, UnitStatType.PhysicalCriticalStrikeChance) },
            { WeaponStatType.PhysicalCritDamage, new ModifyUnitStatBuff(WeaponStatType.PhysicalCritDamage, UnitStatType.PhysicalCriticalStrikeDamage) },
            { WeaponStatType.SpellCritChance, new ModifyUnitStatBuff(WeaponStatType.SpellCritChance, UnitStatType.SpellCriticalStrikeChance) },
            { WeaponStatType.SpellCritDamage, new ModifyUnitStatBuff(WeaponStatType.SpellCritDamage, UnitStatType.SpellCriticalStrikeDamage) }
        };
        public static IReadOnlyDictionary<BloodStatType, ModifyUnitStatBuff> ModifyUnitLegacyStatBuffs => _modifyUnitLegacyStatBuffs;
        static readonly Dictionary<BloodStatType, ModifyUnitStatBuff> _modifyUnitLegacyStatBuffs = new()
        {
            { BloodStatType.HealingReceived, new ModifyUnitStatBuff(BloodStatType.HealingReceived, UnitStatType.HealingReceived) },
            { BloodStatType.DamageReduction, new ModifyUnitStatBuff(BloodStatType.DamageReduction, UnitStatType.DamageReduction) },
            { BloodStatType.PhysicalResistance, new ModifyUnitStatBuff(BloodStatType.PhysicalResistance, UnitStatType.PhysicalResistance) },
            { BloodStatType.SpellResistance, new ModifyUnitStatBuff(BloodStatType.SpellResistance, UnitStatType.SpellResistance) },
            { BloodStatType.ResourceYield, new ModifyUnitStatBuff(BloodStatType.ResourceYield, UnitStatType.ResourceYield) },
            { BloodStatType.ReducedBloodDrain, new ModifyUnitStatBuff(BloodStatType.ReducedBloodDrain, UnitStatType.ReducedBloodDrain) },
            { BloodStatType.SpellCooldownRecoveryRate, new ModifyUnitStatBuff(BloodStatType.SpellCooldownRecoveryRate, UnitStatType.SpellCooldownRecoveryRate) },
            { BloodStatType.WeaponCooldownRecoveryRate, new ModifyUnitStatBuff(BloodStatType.WeaponCooldownRecoveryRate, UnitStatType.WeaponCooldownRecoveryRate) },
            { BloodStatType.UltimateCooldownRecoveryRate, new ModifyUnitStatBuff(BloodStatType.UltimateCooldownRecoveryRate, UnitStatType.UltimateCooldownRecoveryRate) },
            { BloodStatType.MinionDamage, new ModifyUnitStatBuff(BloodStatType.MinionDamage, UnitStatType.MinionDamage) },
            { BloodStatType.AbilityAttackSpeed, new ModifyUnitStatBuff(BloodStatType.AbilityAttackSpeed, UnitStatType.AbilityAttackSpeed) },
            { BloodStatType.CorruptionDamageReduction, new ModifyUnitStatBuff(BloodStatType.CorruptionDamageReduction, UnitStatType.CorruptionDamageReduction) },
        };
    }
    public static IReadOnlyDictionary<UnitStatType, AttributeCap> UnitStatAtributeCaps => _unitStatAttributeCaps;
    static readonly Dictionary<UnitStatType, AttributeCap> _unitStatAttributeCaps = [];

    static readonly HashSet<UnitStatType> _vampireAttributeUnitStatTypes =
    [
        UnitStatType.BonusMaxHealth,
        UnitStatType.MovementSpeed,
        UnitStatType.BonusMovementSpeed,
        UnitStatType.BonusShapeshiftMovementSpeed,
        UnitStatType.BonusMountMovementSpeed,
        UnitStatType.BonusPhysicalPower,
        UnitStatType.BonusSpellPower,
        UnitStatType.AbilityAttackSpeed,
        UnitStatType.PrimaryAttackSpeed,
        UnitStatType.WeaponCooldownRecoveryRate,
        UnitStatType.SpellCooldownRecoveryRate,
        UnitStatType.UltimateCooldownRecoveryRate,
        UnitStatType.FeedCooldownRecoveryRate,
        UnitStatType.PrimaryLifeLeech,
        UnitStatType.PhysicalLifeLeech,
        UnitStatType.SpellLifeLeech,
        UnitStatType.MinionDamage,
        UnitStatType.DamageVsUndeads,
        UnitStatType.DamageVsHumans,
        UnitStatType.DamageVsDemons,
        UnitStatType.DamageVsMechanical,
        UnitStatType.DamageVsBeasts,
        UnitStatType.DamageVsCastleObjects,
        UnitStatType.DamageVsVampires,
        UnitStatType.DamageVsWood,
        UnitStatType.DamageVsMineral,
        UnitStatType.DamageVsVegetation,
        UnitStatType.DamageVsLightArmor,
        UnitStatType.DamageVsVBloods,
        UnitStatType.DamageVsMagic,
        UnitStatType.ResistVsBeasts,
        UnitStatType.ResistVsVampires,
        UnitStatType.ResistVsMechanical,
        UnitStatType.ResistVsDemons,
        UnitStatType.ResistVsUndeads,
        UnitStatType.ResistVsHumans,
        UnitStatType.PhysicalCriticalStrikeChance,
        UnitStatType.PhysicalCriticalStrikeDamage,
        UnitStatType.SpellCriticalStrikeChance,
        UnitStatType.SpellCriticalStrikeDamage,
        UnitStatType.CorruptionDamageReduction,
        UnitStatType.PassiveHealthRegen,
        UnitStatType.DamageReduction,
        UnitStatType.HealingReceived,
        UnitStatType.BloodEfficiency,
        UnitStatType.ReducedBloodDrain,
        UnitStatType.BloodDrainMultiplier,
        UnitStatType.DemountProtection,
        UnitStatType.PvPResilience,
        UnitStatType.ResourceYield,
        UnitStatType.ReducedResourceDurabilityLoss,
        UnitStatType.IncreasedShieldEfficiency,
        UnitStatType.UltimateEfficiency,
        UnitStatType.WeaponSkillPower,
        UnitStatType.SpellFreeCast,
        UnitStatType.WeaponFreeCast
    ];
    public static class PlayerProgressionCacheManager
    {
        public static IReadOnlyList<ulong> IgnoreShared => DataService.PlayerDictionaries._ignoreSharedExperience;
        static readonly ConcurrentDictionary<ulong, PlayerProgressionData> _playerProgressionCache = [];
        public class PlayerProgressionData(int level, bool hasPrestiged)
        {
            public int Level { get; set; } = level;
            public bool HasPrestiged { get; set; } = hasPrestiged;
        }
        public static void UpdatePlayerProgression(ulong steamId, int level, bool hasPrestiged)
        {
            if (_playerProgressionCache.ContainsKey(steamId))
            {
                _playerProgressionCache[steamId] = new PlayerProgressionData(level, hasPrestiged);
            }
            else
            {
                _playerProgressionCache.TryAdd(steamId, new PlayerProgressionData(level, hasPrestiged));
            }
        }
        public static void UpdatePlayerProgressionLevel(ulong steamId, int level)
        {
            if (_playerProgressionCache.TryGetValue(steamId, out PlayerProgressionData playerProgressionData))
            {
                playerProgressionData.Level = level;
            }
            else
            {
                UpdatePlayerProgression(steamId, level, false);
            }
        }
        public static void UpdatePlayerProgressionPrestige(ulong steamId, bool hasPrestiged)
        {
            if (_playerProgressionCache.TryGetValue(steamId, out PlayerProgressionData playerProgressionData))
            {
                playerProgressionData.HasPrestiged = hasPrestiged;
            }
            else
            {
                UpdatePlayerProgression(steamId, 1, hasPrestiged);
            }
        }
        public static PlayerProgressionData GetProgressionCacheData(ulong steamId)
        {
            if (_playerProgressionCache.TryGetValue(steamId, out var data))
                return data;

            int level = LevelingSystem.GetLevel(steamId);
            bool hasPrestiged = steamId.TryGetPlayerPrestiges(out var prestiges)
                                && prestiges.TryGetValue(PrestigeType.Experience, out int prestigeCount)
                                && prestigeCount >= 1;

            data = new PlayerProgressionData(level, hasPrestiged);
            _playerProgressionCache[steamId] = data;
            return data;
        }
    }
    /// <summary>
    /// Determines whether a nearby player qualifies to receive experience from a kill.
    /// </summary>
    /// <param name="experienceSharingEnabled">Indicates whether the server configuration allows experience sharing.</param>
    /// <param name="isPvE">True when the server is operating in PvE mode.</param>
    /// <param name="targetHasPrestiged">Whether the prospective recipient has prestiges applied.</param>
    /// <param name="levelDifference">The difference between the killer's level and the candidate's level.</param>
    /// <param name="shareLevelRange">The configured level difference threshold for sharing experience.</param>
    /// <param name="areAllied">True if the killer and candidate belong to the same clan or alliance.</param>
    /// <param name="isIgnored">True when the candidate opted out of experience sharing.</param>
    /// <returns><c>true</c> when experience should be shared with the candidate; otherwise, <c>false</c>.</returns>
    internal static bool ShouldShareExperience(
        bool experienceSharingEnabled,
        bool isPvE,
        bool targetHasPrestiged,
        int levelDifference,
        int shareLevelRange,
        bool areAllied,
        bool isIgnored)
    {
        if (!experienceSharingEnabled || isIgnored)
        {
            return false;
        }

        if (isPvE)
        {
            if (targetHasPrestiged || shareLevelRange.Equals(0) || areAllied)
            {
                return true;
            }

            return Math.Abs(levelDifference) <= shareLevelRange;
        }

        return areAllied;
    }

    public static HashSet<Entity> GetDeathParticipants(Entity source)
    {
        float3 position = source.GetPosition();
        User sourceUser = source.GetUser();

        var sourceProgression = GetProgressionCacheData(sourceUser.PlatformId);
        int sourceLevel = sourceProgression.Level;

        if (!_expShare)
        {
            return [source];
        }

        var userIndexToSteamId = ServerBootstrapSystem?._PlatformIdToApprovedUserIndex?.ReverseIl2CppDictionary();

        HashSet<Entity> players = [source];

        try
        {
            UserActivityGrid userActivityGrid = UserActivityGridSystem.GetUserActivityGrid();
            UserBitMask128 userBitMask = userActivityGrid.GetUsersInRadius(position, _shareDistance);
            UserBitMask128.Enumerable usersInRange = userBitMask.GetUsers();

            foreach (int userIndex in usersInRange)
            {
                if (!userIndexToSteamId.TryGetValue(userIndex, out ulong steamId))
                    continue;

                bool isIgnored = Enumerable.Contains(IgnoreShared, steamId);
                if (isIgnored)
                    continue;
                if (!steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                    continue;
                if (!playerInfo.CharEntity.HasBuff(_pveCombatBuff))
                    continue;

                var targetProgression = GetProgressionCacheData(steamId);

                bool areAllied = source.IsAllied(playerInfo.CharEntity);
                int levelDifference = sourceLevel - targetProgression.Level;

                if (ShouldShareExperience(
                        _expShare,
                        _isPvE,
                        targetProgression.HasPrestiged,
                        levelDifference,
                        _shareLevelRange,
                        areAllied,
                        isIgnored))
                {
                    players.Add(playerInfo.CharEntity);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"Error getting users in range from activity grid: {e}");
        }

        return players;
    }
    public static List<PlayerInfo> GetUsersNearPosition(float3 position, float radius)
    {
        var userIndexToSteamId = ServerBootstrapSystem?._PlatformIdToApprovedUserIndex?.ReverseIl2CppDictionary();
        List<PlayerInfo> playerInfos = [];

        try
        {
            UserActivityGrid userActivityGrid = UserActivityGridSystem.GetUserActivityGrid();
            UserBitMask128 userBitMask = userActivityGrid.GetUsersInRadius(position, radius);
            UserBitMask128.Enumerable usersInRange = userBitMask.GetUsers();

            foreach (int userIndex in usersInRange)
            {
                if (!userIndexToSteamId.TryGetValue(userIndex, out ulong steamId))
                {
                    continue;
                }

                if (!steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                {
                    continue;
                }

                playerInfos.Add(playerInfo);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"Error getting users in range from activity grid: {e}");
        }

        return playerInfos;
    }
    public static bool ConsumedDracula(Entity userEntity)
    {
        if (userEntity.TryGetComponent(out ProgressionMapper progressionMapper))
        {
            Entity progressionEntity = progressionMapper.ProgressionEntity.GetEntityOnServer();

            if (progressionEntity.TryGetBuffer<UnlockedVBlood>(out var buffer))
            {
                foreach (UnlockedVBlood unlockedVBlood in buffer)
                {
                    if (unlockedVBlood.VBlood.Equals(_draculaVBlood))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    public static bool ConsumedMegara(Entity userEntity)
    {
        if (userEntity.TryGetComponent(out ProgressionMapper progressionMapper))
        {
            Entity progressionEntity = progressionMapper.ProgressionEntity.GetEntityOnServer();

            if (progressionEntity.TryGetBuffer<UnlockedVBlood>(out var buffer))
            {
                foreach (UnlockedVBlood unlockedVBlood in buffer)
                {
                    if (unlockedVBlood.VBlood.Equals(_megaraVBlood))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    public static int GetSimulatedLevel(Entity userEntity)
    {
        if (userEntity.TryGetComponent(out ProgressionMapper progressionMapper))
        {
            Entity progressionEntity = progressionMapper.ProgressionEntity.GetEntityOnServer();

            if (progressionEntity.TryGetBuffer<UnlockedVBlood>(out var buffer))
            {
                return buffer.Length * SIMULATED_LEVEL_FACTOR;
            }
        }

        return 0;
    }
    public static unsafe void GetAttributeCaps()
    {
        PrefabGUID vampireMale = PrefabGUIDs.CHAR_VampireMale;
        Entity prefab = vampireMale.GetPrefabEntity();

        if (prefab.TryGetComponent(out VampireAttributeCaps vampireAttributeCaps))
        {
            BlobAssetReference<VampireAttributes_Unboxed<AttributeCap>> blobRef = vampireAttributeCaps.Caps;
            VampireAttributes_Unboxed<AttributeCap>* unboxedAttributeCaps = (VampireAttributes_Unboxed<AttributeCap>*)blobRef.GetUnsafePtr();

            foreach (UnitStatType unitStatType in _vampireAttributeUnitStatTypes)
            {
                AttributeCap attributeCap = unboxedAttributeCaps->GetCap(unitStatType);
                _unitStatAttributeCaps[unitStatType] = attributeCap;
            }
        }
        else
        {
            Core.Log.LogWarning($"Failed to get {nameof(VampireAttributeCaps)} from CHAR_VampireMale prefabEntity!");
        }
    }
    public static int ConvertXpToLevel(float xp)
    {
        return (int)(EXP_CONSTANT * Math.Sqrt(xp));
    }
    public static int ConvertLevelToXp(int level)
    {
        return (int)Math.Pow(level / EXP_CONSTANT, EXP_POWER);
    }
}
