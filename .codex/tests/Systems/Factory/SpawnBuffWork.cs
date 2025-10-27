using System;
using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test double that mirrors the script-spawn and unit-spawn patches.
/// </summary>
public sealed class SpawnBuffWork : ISystemWork
{
    /// <summary>
    /// Describes the categories returned when classifying a spawn buff.
    /// </summary>
    public enum BuffCategory
    {
        /// <summary>
        /// No action is taken for the buff.
        /// </summary>
        None = 0,

        /// <summary>
        /// Shapeshift modifications should be applied.
        /// </summary>
        Shapeshift = 1,

        /// <summary>
        /// Blood bolt trigger buffs should be removed.
        /// </summary>
        RemoveBloodBoltTriggers = 2,

        /// <summary>
        /// The Dracula blood bolt ability group cooldown should be set.
        /// </summary>
        ApplyBloodBoltCooldown = 3,

        /// <summary>
        /// Player or familiar bonus stats should be applied.
        /// </summary>
        ApplyBonusStats = 4,

        /// <summary>
        /// Spell level should be reset for the buff target.
        /// </summary>
        ResetSpellLevel = 6,

        /// <summary>
        /// Blood legacy stats should be refreshed.
        /// </summary>
        RefreshBloodStats = 7,

        /// <summary>
        /// Debuff familiars allied with the owner should be removed.
        /// </summary>
        RemoveFamiliarDebuff = 8,

        /// <summary>
        /// Familiar castle man routines should be invoked.
        /// </summary>
        FamiliarCastleMan = 9,

        /// <summary>
        /// Familiar shapeshift routines should be started.
        /// </summary>
        FamiliarShapeshift = 10,
    }

    /// <summary>
    /// Delegate used to set ability cooldowns during processing.
    /// </summary>
    /// <param name="source">Entity originating the cooldown request.</param>
    /// <param name="abilityGroupHash">Ability group hash receiving the cooldown.</param>
    /// <param name="cooldownSeconds">Cooldown value in seconds.</param>
    public delegate void AbilityCooldownSetter(EntityHandle source, int abilityGroupHash, float cooldownSeconds);

    /// <summary>
    /// Delegate used to refresh stats for the buff target.
    /// </summary>
    /// <param name="target">Entity whose stats should be refreshed.</param>
    public delegate void StatRefreshDelegate(EntityHandle target);

    /// <summary>
    /// Describes the script-spawn data cached for processing.
    /// </summary>
    /// <param name="BuffEntity">Entity representing the buff instance.</param>
    /// <param name="Target">Entity receiving the buff.</param>
    /// <param name="Owner">Entity owning the buff.</param>
    /// <param name="Prefab">Prefab applied by the buff.</param>
    /// <param name="TargetIsPlayer">Indicates whether the buff target is a player.</param>
    /// <param name="TargetIsFamiliar">Indicates whether the buff target is a familiar.</param>
    /// <param name="OwnerIsFamiliar">Indicates whether the buff owner is a familiar.</param>
    /// <param name="IsBloodBuff">Indicates whether the buff entity has a <see cref="BloodBuff"/> component.</param>
    /// <param name="IsDebuff">Indicates whether the buff entity represents a debuff.</param>
    public readonly record struct ScriptSpawnEntry(
        EntityHandle BuffEntity,
        EntityHandle Target,
        EntityHandle Owner,
        int PrefabHash,
        bool TargetIsPlayer,
        bool TargetIsFamiliar,
        bool OwnerIsFamiliar,
        bool IsBloodBuff,
        bool IsDebuff);

    const int BloodBoltChannelBuffHash = 136816739;
    const int BonusStatsBuffHash = 1774716596;
    const int FamiliarCastleManBuffHash = 731266864;
    const int DefaultBloodBoltAbilityGroupHash = 797450963;

    static readonly HashSet<int> shapeshiftBuffs = new()
    {
        -31099041,
        -1859425781,
        174249800
    };

    static readonly HashSet<int> bloodBoltSwarmTriggers = new()
    {
        1615225381,
        832491730,
        -622814018
    };

    static readonly HashSet<int> werewolfBuffs = new()
    {
        -1598161201,
        -622259665
    };

    static QueryDescription CreateScriptSpawnQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<PrefabGUID>();
        builder.AddAllReadOnly<Buff>();
        builder.AddAllReadOnly<EntityOwner>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateMinionSpawnQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadWrite<IsMinion>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription scriptSpawnQuery = CreateScriptSpawnQuery();
    static readonly QueryDescription minionSpawnQuery = CreateMinionSpawnQuery();

    readonly float bloodBoltCooldownSeconds;
    readonly AbilityCooldownSetter? abilityCooldownSetter;
    readonly StatRefreshDelegate? statRefreshDelegate;
    readonly int? configuredBloodBoltGroupHash;

    Dictionary<EntityHandle, ScriptSpawnEntry>? scriptEntries;
    List<EntityHandle>? scriptOrder;
    Dictionary<EntityHandle, bool>? minionStates;
    List<EntityHandle>? minionOrder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpawnBuffWork"/> class.
    /// </summary>
    public SpawnBuffWork()
        : this(0f, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpawnBuffWork"/> class.
    /// </summary>
    /// <param name="bloodBoltCooldownSeconds">Cooldown applied to the Dracula blood bolt ability group.</param>
    /// <param name="abilityCooldownSetter">Optional delegate invoked when the cooldown should be set.</param>
    /// <param name="statRefreshDelegate">Optional delegate invoked when stats should be refreshed.</param>
    /// <param name="bloodBoltAbilityGroupHash">Optional override for the blood bolt ability group.</param>
    public SpawnBuffWork(
        float bloodBoltCooldownSeconds = 0f,
        AbilityCooldownSetter? abilityCooldownSetter = null,
        StatRefreshDelegate? statRefreshDelegate = null,
        int? bloodBoltAbilityGroupHash = null)
    {
        this.bloodBoltCooldownSeconds = bloodBoltCooldownSeconds;
        this.abilityCooldownSetter = abilityCooldownSetter;
        this.statRefreshDelegate = statRefreshDelegate;
        configuredBloodBoltGroupHash = bloodBoltAbilityGroupHash;
        scriptEntries = null;
        scriptOrder = null;
        minionStates = null;
        minionOrder = null;
    }

    Dictionary<EntityHandle, ScriptSpawnEntry> ScriptEntries => scriptEntries ??= new();
    List<EntityHandle> ScriptOrder => scriptOrder ??= new();
    Dictionary<EntityHandle, bool> MinionStates => minionStates ??= new();
    List<EntityHandle> MinionOrder => minionOrder ??= new();

    /// <summary>
    /// Gets the primary script spawn query description.
    /// </summary>
    public QueryDescription ScriptSpawnQuery => scriptSpawnQuery;

    /// <summary>
    /// Gets the supplemental minion spawn query description.
    /// </summary>
    public QueryDescription MinionSpawnQuery => minionSpawnQuery;

    /// <summary>
    /// Gets the cooldown applied when a blood bolt trigger is processed.
    /// </summary>
    public float BloodBoltCooldown => bloodBoltCooldownSeconds;

    /// <summary>
    /// Gets the ability group that receives the blood bolt cooldown.
    /// </summary>
    public int BloodBoltAbilityGroupHash => configuredBloodBoltGroupHash ?? DefaultBloodBoltAbilityGroupHash;

    /// <summary>
    /// Gets the recorded script-spawn entities in insertion order.
    /// </summary>
    public IReadOnlyList<EntityHandle> ScriptSpawnEntities => ScriptOrder;

    /// <summary>
    /// Gets the recorded minion entities in insertion order.
    /// </summary>
    public IReadOnlyList<EntityHandle> MinionEntities => MinionOrder;

    /// <summary>
    /// Provides a read-only view of the shapeshift buff hashes.
    /// </summary>
    public static IReadOnlyCollection<int> ShapeshiftBuffHashes => shapeshiftBuffs;

    /// <summary>
    /// Provides a read-only view of the blood bolt trigger hashes.
    /// </summary>
    public static IReadOnlyCollection<int> BloodBoltTriggerHashes => bloodBoltSwarmTriggers;

    /// <summary>
    /// Provides a read-only view of the werewolf buff hashes.
    /// </summary>
    public static IReadOnlyCollection<int> WerewolfBuffHashes => werewolfBuffs;

    /// <inheritdoc />
    public bool RequireForUpdate => true;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<PrefabGUID>();
        builder.AddAllReadOnly<Buff>();
        builder.AddAllReadOnly<EntityOwner>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetComponentLookup<PlayerCharacter>(isReadOnly: true);
            _ = facade.GetComponentLookup<BlockFeedBuff>(isReadOnly: true);
            _ = facade.GetComponentLookup<BloodBuff>(isReadOnly: true);
            _ = facade.GetComponentLookup<IsMinion>(isReadOnly: false);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        context.ForEachEntity(scriptSpawnQuery, ProcessScriptSpawn);
        context.ForEachEntity(minionSpawnQuery, ProcessMinionSpawn);
    }

    void ProcessScriptSpawn(EntityHandle entity)
    {
        if (!ScriptEntries.TryGetValue(entity, out var entry))
        {
            return;
        }

        BuffCategory category = ClassifyBuff(
            entry.PrefabHash,
            entry.IsDebuff,
            entry.TargetIsPlayer,
            entry.TargetIsFamiliar,
            entry.IsBloodBuff);

        if (category == BuffCategory.ApplyBloodBoltCooldown && abilityCooldownSetter != null && bloodBoltCooldownSeconds > 0f)
        {
            abilityCooldownSetter(entry.Owner, BloodBoltAbilityGroupHash, bloodBoltCooldownSeconds);
        }
        else if (category == BuffCategory.RefreshBloodStats && statRefreshDelegate != null)
        {
            statRefreshDelegate(entry.Target);
        }
    }

    void ProcessMinionSpawn(EntityHandle entity)
    {
        if (!MinionStates.ContainsKey(entity))
        {
            MinionOrder.Add(entity);
        }

        MinionStates[entity] = true;
    }

    /// <summary>
    /// Adds a script-spawn entry to be processed during the next tick.
    /// </summary>
    /// <param name="entry">Entry to add.</param>
    public void AddScriptSpawnEntry(ScriptSpawnEntry entry)
    {
        if (!ScriptEntries.ContainsKey(entry.BuffEntity))
        {
            ScriptOrder.Add(entry.BuffEntity);
        }

        ScriptEntries[entry.BuffEntity] = entry;
    }

    /// <summary>
    /// Adds a minion entity to be processed during the next tick.
    /// </summary>
    /// <param name="entity">Entity handle representing the minion.</param>
    /// <param name="isMinion">Initial value recorded for the entity.</param>
    public void AddMinionEntity(EntityHandle entity, bool isMinion = false)
    {
        if (!MinionStates.ContainsKey(entity))
        {
            MinionOrder.Add(entity);
        }

        MinionStates[entity] = isMinion;
    }

    /// <summary>
    /// Attempts to retrieve a script-spawn entry by entity handle.
    /// </summary>
    /// <param name="entity">Entity handle associated with the entry.</param>
    /// <param name="entry">Retrieved entry.</param>
    public bool TryGetScriptSpawnEntry(EntityHandle entity, out ScriptSpawnEntry entry)
    {
        return ScriptEntries.TryGetValue(entity, out entry);
    }

    /// <summary>
    /// Attempts to retrieve the minion state for the specified entity.
    /// </summary>
    /// <param name="entity">Entity handle representing the minion.</param>
    /// <param name="isMinion">Retrieved state.</param>
    public bool TryGetMinionState(EntityHandle entity, out bool isMinion)
    {
        return MinionStates.TryGetValue(entity, out isMinion);
    }

    /// <summary>
    /// Determines whether the specified prefab hash is part of the shapeshift set.
    /// </summary>
    /// <param name="prefab">Prefab to evaluate.</param>
    public static bool IsShapeshiftBuff(int prefabHash) => shapeshiftBuffs.Contains(prefabHash);

    /// <summary>
    /// Determines whether the specified prefab hash is part of the blood bolt trigger set.
    /// </summary>
    /// <param name="prefab">Prefab to evaluate.</param>
    public static bool IsBloodBoltTrigger(int prefabHash) => bloodBoltSwarmTriggers.Contains(prefabHash);

    /// <summary>
    /// Determines whether the specified prefab hash is part of the werewolf buff set.
    /// </summary>
    /// <param name="prefab">Prefab to evaluate.</param>
    public static bool IsWerewolfBuff(int prefabHash) => werewolfBuffs.Contains(prefabHash);

    /// <summary>
    /// Classifies the buff using the same logic as <see cref="Patches.ScriptSpawnServerPatch"/>.
    /// </summary>
    /// <param name="prefab">Prefab applied by the buff.</param>
    /// <param name="isDebuff">Indicates whether the buff is a debuff.</param>
    /// <param name="targetIsPlayer">Indicates whether the target is a player.</param>
    /// <param name="targetIsFamiliar">Indicates whether the target is a familiar.</param>
    /// <param name="isBloodBuff">Indicates whether the buff entity has a <see cref="BloodBuff"/> component.</param>
    public static BuffCategory ClassifyBuff(
        int prefabHash,
        bool isDebuff,
        bool targetIsPlayer,
        bool targetIsFamiliar,
        bool isBloodBuff)
    {
        if (targetIsPlayer)
        {
            if (bloodBoltSwarmTriggers.Contains(prefabHash))
            {
                return BuffCategory.RemoveBloodBoltTriggers;
            }

            if (prefabHash == BonusStatsBuffHash)
            {
                return BuffCategory.ApplyBonusStats;
            }

            if (shapeshiftBuffs.Contains(prefabHash))
            {
                return BuffCategory.Shapeshift;
            }

            return prefabHash switch
            {
                BloodBoltChannelBuffHash => BuffCategory.ApplyBloodBoltCooldown,
                _ when isBloodBuff => BuffCategory.RefreshBloodStats,
                _ when isDebuff => BuffCategory.RemoveFamiliarDebuff,
                _ => BuffCategory.None
            };
        }

        if (targetIsFamiliar)
        {
            if (prefabHash == BonusStatsBuffHash)
            {
                return BuffCategory.ApplyBonusStats;
            }

            if (werewolfBuffs.Contains(prefabHash))
            {
                return BuffCategory.FamiliarShapeshift;
            }

            return prefabHash switch
            {
                FamiliarCastleManBuffHash => BuffCategory.FamiliarCastleMan,
                _ => BuffCategory.None
            };
        }

        return BuffCategory.None;
    }
}
