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
    public class PlayerProgressionCacheManager
    {
        public class PlayerProgressionData(int level, bool hasPrestiged)
        {
            public int Level { get; set; } = level;
            public bool HasPrestiged { get; set; } = hasPrestiged;
        }
        public static IReadOnlyList<ulong> IgnoreShared => DataService.PlayerDictionaries._ignoreSharedExperience;

        static readonly ConcurrentDictionary<ulong, PlayerProgressionData> _playerProgressionCache = [];
        public static IReadOnlyDictionary<ulong, PlayerProgressionData> PlayerProgressionCache => _playerProgressionCache;
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

        var userIndexToSteamId = ServerBootstrapSystem._PlatformIdToApprovedUserIndex.ReverseIl2CppDictionary();

        HashSet<Entity> players = [source];

        try
        {
            UserActivityGrid userActivityGrid = UserActivityGridSystem.GetUserActivityGrid();
            UserBitMask128 userBitMask = userActivityGrid.GetUsersInRadius(position, _shareDistance);
            UserBitMask128.Enumerable usersInRange = userBitMask.GetUsers();

            Core.Log.LogWarning($"Users in range of deathEvent - {usersInRange._Mask.Count}");

            foreach (int userIndex in usersInRange)
            {
                if (!userIndexToSteamId.TryGetValue(userIndex, out ulong steamId))
                {
                    // Core.Log.LogWarning($"UserIndexToSteamId invalid - {userIndex} | {steamId}");
                    continue;
                }
                else if (IgnoreShared.Contains(steamId))
                {
                    // Core.Log.LogWarning($"IgnoreShared - {userIndex} | {steamId}");
                }
                if (!steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
                {
                    continue;
                }
                if (!playerInfo.CharEntity.HasBuff(_pveCombatBuff))
                {
                    // Core.Log.LogWarning($"Not in combat - {userIndex} | {steamId}");
                    continue;
                }

                var targetProgression = GetProgressionCacheData(steamId);

                // Core.Log.LogWarning($"ProgressionCache - {userIndex} | {steamId} | {targetProgression.Level} | {targetProgression.HasPrestiged}");

                if (_isPvE)
                {
                    if (targetProgression.HasPrestiged || _shareLevelRange.Equals(0) || source.IsAllies(playerInfo.CharEntity))
                    {
                        // Core.Log.LogWarning($"[PvE] Adding {steamId} to participants (hasPrestiged, isAllies, or no shareLevelRange ({_shareLevelRange})");
                        players.Add(playerInfo.CharEntity);
                    }
                    else if (Math.Abs(sourceLevel - targetProgression.Level) <= _shareLevelRange)
                    {
                        // Core.Log.LogWarning($"[PvE] Adding {steamId} to participants (level difference <= {_shareLevelRange})");
                        players.Add(playerInfo.CharEntity);
                    }
                    else
                    {
                        // Core.Log.LogWarning($"[PvE] Ignoring {steamId} (level difference > {_shareLevelRange})");
                    }
                }
                else if (source.IsAllies(playerInfo.CharEntity))
                {
                    // Core.Log.LogWarning($"[PvP] Adding {steamId} to participants (isAllies)");
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
        var userIndexToSteamId = ServerBootstrapSystem._PlatformIdToApprovedUserIndex.ReverseIl2CppDictionary();
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
                    // Core.Log.LogWarning($"UserIndexToSteamId invalid - {userIndex} | {steamId}");
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
    public static void ApplyPlayerStats(Entity buffEntity, Entity playerCharacter)
    {
        if (playerCharacter.TryGetComponent(out VampireSpecificAttributes vampireAttributes)
            && playerCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared)
            && playerCharacter.TryGetComponent(out MinionMasterStats minionMaster)
            && playerCharacter.TryGetComponent(out LifeLeech lifeLeech)
            && playerCharacter.TryGetComponent(out UnitStats unitStats)
            && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
        {
            foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in buffer)
            {
                UnitStatType unitStatType = modifyUnitStatBuff.StatType;

                if (UnitStatAtributeCaps.ContainsKey(unitStatType))
                {
                    Core.Log.LogWarning($"[ApplyPlayerStats()] - {unitStatType} | {modifyUnitStatBuff.StatType} | {modifyUnitStatBuff.Value}");

                    switch (unitStatType)
                    {
                        // VampireSpecificAttributes
                        case UnitStatType.BonusMaxHealth:
                            vampireAttributes.BonusMaxHealth._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusPhysicalPower:
                            vampireAttributes.BonusPhysicalPower._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusSpellPower:
                            vampireAttributes.BonusSpellPower._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusMovementSpeed:
                            vampireAttributes.BonusMovementSpeed._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusShapeshiftMovementSpeed:
                            vampireAttributes.BonusShapeshiftMovementSpeed._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellFreeCast:
                            vampireAttributes.SpellFreeCast._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.WeaponFreeCast:
                            vampireAttributes.WeaponFreeCast._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.IncreasedShieldEfficiency:
                            vampireAttributes.IncreasedShieldEfficiency._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PhysicalCriticalStrikeChance:
                            vampireAttributes.PhysicalCriticalStrikeChance._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PhysicalCriticalStrikeDamage:
                            vampireAttributes.PhysicalCriticalStrikeDamage._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellCriticalStrikeChance:
                            vampireAttributes.SpellCriticalStrikeChance._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellCriticalStrikeDamage:
                            vampireAttributes.SpellCriticalStrikeDamage._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.ResourceYield:
                            vampireAttributes.ResourceYieldModifier._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.UltimateEfficiency:
                            vampireAttributes.UltimateEfficiency._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.WeaponSkillPower:
                            vampireAttributes.WeaponSkillPower._Value += modifyUnitStatBuff.Value;
                            break;

                        // LifeLeech
                        case UnitStatType.PhysicalLifeLeech:
                            lifeLeech.PhysicalLifeLeechFactor._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellLifeLeech:
                            lifeLeech.SpellLifeLeechFactor._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PrimaryLifeLeech:
                            lifeLeech.PrimaryLeechFactor._Value += modifyUnitStatBuff.Value;
                            break;

                        // Attack & Cast Speed
                        case UnitStatType.PrimaryAttackSpeed:
                            abilityBar_Shared.PrimaryAttackSpeed._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.AbilityAttackSpeed:
                            abilityBar_Shared.AbilityAttackSpeed._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellCooldownRecoveryRate:
                            abilityBar_Shared.SpellCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.WeaponCooldownRecoveryRate:
                            abilityBar_Shared.WeaponCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.UltimateCooldownRecoveryRate:
                            abilityBar_Shared.UltimateCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.FeedCooldownRecoveryRate:
                            abilityBar_Shared.FeedCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PrimaryCooldownModifier:
                            abilityBar_Shared.PrimaryCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.TravelCooldownRecoveryRate:
                            abilityBar_Shared.TravelCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.CooldownRecoveryRate:
                            abilityBar_Shared.CooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                            break;

                        // UnitStats
                        case UnitStatType.HealingReceived:
                            unitStats.HealingReceived._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BloodDrain:
                            unitStats.ReducedBloodDrain._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.CorruptionDamageReduction:
                            unitStats.CorruptionDamageReduction._Value += modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.DamageReduction:
                            unitStats.DamageReduction._Value += modifyUnitStatBuff.Value;
                            break;

                        // MinionMaster
                        case UnitStatType.MinionDamage:
                            minionMaster.MinionDamageModifier._Value += modifyUnitStatBuff.Value;
                            break;

                        default:
                            break;
                    }
                }
            }

            playerCharacter.Write(vampireAttributes);
            playerCharacter.Write(abilityBar_Shared);
            playerCharacter.Write(minionMaster);
            playerCharacter.Write(lifeLeech);
            playerCharacter.Write(unitStats);
        }
        else
        {
            Core.Log.LogWarning($"[ApplyPlayerStats] Couldn't get all required components!");
        }
    }
    public static void RemovePlayerStats(Entity buffEntity, Entity playerCharacter)
    {
        if (playerCharacter.TryGetComponent(out VampireSpecificAttributes vampireAttributes)
            && playerCharacter.TryGetComponent(out AbilityBar_Shared abilityBar_Shared)
            && playerCharacter.TryGetComponent(out MinionMasterStats minionMaster)
            && playerCharacter.TryGetComponent(out LifeLeech lifeLeech)
            && playerCharacter.TryGetComponent(out UnitStats unitStats)
            && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
        {
            foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in buffer)
            {
                UnitStatType unitStatType = modifyUnitStatBuff.StatType;

                if (UnitStatAtributeCaps.ContainsKey(unitStatType))
                {
                    Core.Log.LogWarning($"[RemovePlayerStats] - {unitStatType} | {modifyUnitStatBuff.StatType} | {modifyUnitStatBuff.Value}");

                    switch (unitStatType)
                    {
                        // VampireSpecificAttributes
                        case UnitStatType.BonusMaxHealth:
                            vampireAttributes.BonusMaxHealth._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusPhysicalPower:
                            vampireAttributes.BonusPhysicalPower._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusSpellPower:
                            vampireAttributes.BonusSpellPower._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusMovementSpeed:
                            vampireAttributes.BonusMovementSpeed._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BonusShapeshiftMovementSpeed:
                            vampireAttributes.BonusShapeshiftMovementSpeed._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellFreeCast:
                            vampireAttributes.SpellFreeCast._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.WeaponFreeCast:
                            vampireAttributes.WeaponFreeCast._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.IncreasedShieldEfficiency:
                            vampireAttributes.IncreasedShieldEfficiency._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PhysicalCriticalStrikeChance:
                            vampireAttributes.PhysicalCriticalStrikeChance._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PhysicalCriticalStrikeDamage:
                            vampireAttributes.PhysicalCriticalStrikeDamage._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellCriticalStrikeChance:
                            vampireAttributes.SpellCriticalStrikeChance._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellCriticalStrikeDamage:
                            vampireAttributes.SpellCriticalStrikeDamage._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.ResourceYield:
                            vampireAttributes.ResourceYieldModifier._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.UltimateEfficiency:
                            vampireAttributes.UltimateEfficiency._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.WeaponSkillPower:
                            vampireAttributes.WeaponSkillPower._Value -= modifyUnitStatBuff.Value;
                            break;

                        // LifeLeech
                        case UnitStatType.PhysicalLifeLeech:
                            lifeLeech.PhysicalLifeLeechFactor._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellLifeLeech:
                            lifeLeech.SpellLifeLeechFactor._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PrimaryLifeLeech:
                            lifeLeech.PrimaryLeechFactor._Value -= modifyUnitStatBuff.Value;
                            break;

                        // Attack & Cast Speed
                        case UnitStatType.PrimaryAttackSpeed:
                            abilityBar_Shared.PrimaryAttackSpeed._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.AbilityAttackSpeed:
                            abilityBar_Shared.AbilityAttackSpeed._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.SpellCooldownRecoveryRate:
                            abilityBar_Shared.SpellCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.WeaponCooldownRecoveryRate:
                            abilityBar_Shared.WeaponCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.UltimateCooldownRecoveryRate:
                            abilityBar_Shared.UltimateCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.FeedCooldownRecoveryRate:
                            abilityBar_Shared.FeedCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.PrimaryCooldownModifier:
                            abilityBar_Shared.PrimaryCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.TravelCooldownRecoveryRate:
                            abilityBar_Shared.TravelCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.CooldownRecoveryRate:
                            abilityBar_Shared.CooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                            break;

                        // UnitStats
                        case UnitStatType.HealingReceived:
                            unitStats.HealingReceived._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.BloodDrain:
                            unitStats.ReducedBloodDrain._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.CorruptionDamageReduction:
                            unitStats.CorruptionDamageReduction._Value -= modifyUnitStatBuff.Value;
                            break;
                        case UnitStatType.DamageReduction:
                            unitStats.DamageReduction._Value -= modifyUnitStatBuff.Value;
                            break;

                        // MinionMaster
                        case UnitStatType.MinionDamage:
                            minionMaster.MinionDamageModifier._Value -= modifyUnitStatBuff.Value;
                            break;

                        default:
                            break;
                    }
                }
            }

            playerCharacter.Write(vampireAttributes);
            playerCharacter.Write(abilityBar_Shared);
            playerCharacter.Write(minionMaster);
            playerCharacter.Write(lifeLeech);
            playerCharacter.Write(unitStats);
        }
        else
        {
            Core.Log.LogWarning($"[RemovePlayerStats] Couldn't get all required components!");
        }
    }
    public static void ApplyFamiliarStats(Entity buffEntity, Entity familiar)
    {
        if (familiar.TryGetComponent(out AbilityBar_Shared abilityBar_Shared)
        && familiar.TryGetComponent(out AiMoveSpeeds aiMoveSpeeds)
        && familiar.TryGetComponent(out UnitStats unitStats)
        && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
        {
            foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in buffer)
            {
                UnitStatType unitStatType = modifyUnitStatBuff.StatType;
                Core.Log.LogWarning($"[ApplyFamiliarStats] - {unitStatType} | {modifyUnitStatBuff.StatType} | {modifyUnitStatBuff.Value}");

                switch (unitStatType)
                {
                    // Attack & Cast Speed
                    case UnitStatType.PrimaryAttackSpeed:
                        abilityBar_Shared.PrimaryAttackSpeed._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.AbilityAttackSpeed:
                        abilityBar_Shared.AbilityAttackSpeed._Value += modifyUnitStatBuff.Value;
                        break;
                    /*
                    case UnitStatType.SpellCooldownRecoveryRate:
                        abilityBar_Shared.SpellCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.WeaponCooldownRecoveryRate:
                        abilityBar_Shared.WeaponCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.UltimateCooldownRecoveryRate:
                        abilityBar_Shared.UltimateCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.FeedCooldownRecoveryRate:
                        abilityBar_Shared.FeedCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.PrimaryCooldownModifier:
                        abilityBar_Shared.PrimaryCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.TravelCooldownRecoveryRate:
                        abilityBar_Shared.TravelCooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.CooldownRecoveryRate:
                        abilityBar_Shared.CooldownRecoveryRate._Value += modifyUnitStatBuff.Value;
                        break;
                    */

                    // UnitStats
                    case UnitStatType.PhysicalPower:
                        unitStats.PhysicalPower._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.SpellPower:
                        unitStats.SpellPower._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.PhysicalResistance:
                        unitStats.PhysicalResistance._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.SpellResistance:
                        unitStats.SpellResistance._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.HealingReceived:
                        unitStats.HealingReceived._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.BloodDrain:
                        unitStats.ReducedBloodDrain._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.CorruptionDamageReduction:
                        unitStats.CorruptionDamageReduction._Value += modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.DamageReduction:
                        unitStats.DamageReduction._Value += modifyUnitStatBuff.Value;
                        break;

                    // AiMoveSpeeds
                    case UnitStatType.MovementSpeed or UnitStatType.BonusMovementSpeed:
                        aiMoveSpeeds.Walk._Value += modifyUnitStatBuff.Value;
                        aiMoveSpeeds.Run._Value += modifyUnitStatBuff.Value;
                        break;

                    default:
                        break;
                }
            }

            familiar.Write(abilityBar_Shared);
            familiar.Write(unitStats);
            familiar.Write(aiMoveSpeeds);
        }
        else
        {
            Core.Log.LogWarning($"[ApplyFamiliarStats] Couldn't get all required components!");
        }
    }
    public static void RemoveFamiliarStats(Entity buffEntity, Entity familiar)
    {
        if (familiar.TryGetComponent(out AbilityBar_Shared abilityBar_Shared)
        && familiar.TryGetComponent(out AiMoveSpeeds aiMoveSpeeds)
        && familiar.TryGetComponent(out UnitStats unitStats)
        && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer))
        {
            foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in buffer)
            {
                UnitStatType unitStatType = modifyUnitStatBuff.StatType;
                Core.Log.LogWarning($"[RemoveFamiliarStats] - {unitStatType} | {modifyUnitStatBuff.StatType} | {modifyUnitStatBuff.Value}");

                switch (unitStatType)
                {
                    // Attack & Cast Speed
                    case UnitStatType.PrimaryAttackSpeed:
                        abilityBar_Shared.PrimaryAttackSpeed._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.AbilityAttackSpeed:
                        abilityBar_Shared.AbilityAttackSpeed._Value -= modifyUnitStatBuff.Value;
                        break;
                    /*
                    case UnitStatType.SpellCooldownRecoveryRate:
                        abilityBar_Shared.SpellCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.WeaponCooldownRecoveryRate:
                        abilityBar_Shared.WeaponCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.UltimateCooldownRecoveryRate:
                        abilityBar_Shared.UltimateCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.FeedCooldownRecoveryRate:
                        abilityBar_Shared.FeedCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.PrimaryCooldownModifier:
                        abilityBar_Shared.PrimaryCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.TravelCooldownRecoveryRate:
                        abilityBar_Shared.TravelCooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.CooldownRecoveryRate:
                        abilityBar_Shared.CooldownRecoveryRate._Value -= modifyUnitStatBuff.Value;
                        break;
                    */

                    // UnitStats
                    case UnitStatType.PhysicalPower:
                        unitStats.PhysicalPower._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.SpellPower:
                        unitStats.SpellPower._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.PhysicalResistance:
                        unitStats.PhysicalResistance._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.SpellResistance:
                        unitStats.SpellResistance._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.HealingReceived:
                        unitStats.HealingReceived._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.CorruptionDamageReduction:
                        unitStats.CorruptionDamageReduction._Value -= modifyUnitStatBuff.Value;
                        break;
                    case UnitStatType.DamageReduction:
                        unitStats.DamageReduction._Value -= modifyUnitStatBuff.Value;
                        break;

                    // AiMoveSpeeds
                    case UnitStatType.MovementSpeed or UnitStatType.BonusMovementSpeed:
                        aiMoveSpeeds.Walk._Value -= modifyUnitStatBuff.Value;
                        aiMoveSpeeds.Run._Value -= modifyUnitStatBuff.Value;
                        break;

                    default:
                        break;
                }
            }

            familiar.Write(abilityBar_Shared);
            familiar.Write(unitStats);
            familiar.Write(aiMoveSpeeds);
        }
        else
        {
            Core.Log.LogWarning($"[RemoveFamiliarStats] Couldn't get all required components!");
        }
    }
    public static unsafe void GetAttributeCaps(Entity playerCharacter = default)
    {
        PrefabGUID vampireMale = PrefabGUIDs.CHAR_VampireMale;

        if (playerCharacter.TryGetComponent(out VampireAttributeCaps vampireAttributeCaps))
        {
            BlobAssetReference<VampireAttributes_Unboxed<AttributeCap>> blobRef = vampireAttributeCaps.Caps;
            VampireAttributes_Unboxed<AttributeCap>* unboxedAttributeCaps = (VampireAttributes_Unboxed<AttributeCap>*)blobRef.GetUnsafePtr();

            foreach (UnitStatType unitStatType in _vampireAttributeUnitStatTypes)
            {
                AttributeCap attributeCap = unboxedAttributeCaps->GetCap(unitStatType);
                _unitStatAttributeCaps[unitStatType] = attributeCap;
                Core.Log.LogWarning($"{unitStatType} - {attributeCap.Start} | {attributeCap.SoftCap} | {attributeCap.HardCap}");
            }
        }
        else if (vampireMale.TryGetPrefabEntity(out Entity prefabEntity)
            && prefabEntity.TryGetComponent(out vampireAttributeCaps))
        {
            BlobAssetReference<VampireAttributes_Unboxed<AttributeCap>> blobRef = vampireAttributeCaps.Caps;
            VampireAttributes_Unboxed<AttributeCap>* unboxedAttributeCaps = (VampireAttributes_Unboxed<AttributeCap>*)blobRef.GetUnsafePtr();

            foreach (UnitStatType unitStatType in _vampireAttributeUnitStatTypes)
            {
                AttributeCap attributeCap = unboxedAttributeCaps->GetCap(unitStatType);
                _unitStatAttributeCaps[unitStatType] = attributeCap;

                // Core.Log.LogWarning($"{unitStatType} - {attributeCap.Start} | {attributeCap.SoftCap} | {attributeCap.HardCap}");
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
