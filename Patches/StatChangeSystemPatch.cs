using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Systems.Leveling.ClassManager.ClassOnHitSettings;
using static Bloodcraft.Utilities.Classes;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class StatChangeSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly Random _random = new();
    public static IReadOnlyDictionary<ulong, DateTime> LastDamageTime => _lastDamageTime;
    static readonly ConcurrentDictionary<ulong, DateTime> _lastDamageTime = [];

    static readonly HashSet<PrefabGUID> _shinyOnHitDebuffs = new()
    {
        { Buffs.VampireIgniteDebuff},
        { Buffs.VampireStaticDebuff},
        { Buffs.VampireLeechDebuff},
        { Buffs.VampireWeakenDebuff},
        { Buffs.VampireChillDebuff},
        { Buffs.VampireCondemnDebuff}
    };

    static readonly HashSet<PrefabGUID> _ignoredSources = new()
    {
        { Buffs.GarlicDebuff },
        { Buffs.SilverDebuff },
        { Buffs.HolyDebuff },
        { Buffs.DivineDebuff }
    };

    static readonly PrefabGUID _slashersMeleeHit03 = PrefabGUIDs.AB_Vampire_Slashers_Primary_MeleeAttack_Hit03;
    static readonly PrefabGUID _twinBladesSweepingStrikeHit = PrefabGUIDs.AB_Vampire_TwinBlades_SweepingStrike_Hit;
    static readonly PrefabGUID _vargulfBleedBuff = Buffs.VargulfBleedBuff;
    static readonly PrefabGUID _iceShieldBuff = PrefabGUIDs.Frost_Vampire_Buff_IceShield_SpellMod;

    static readonly bool _slashers = Core.BleedingEdge.Contains(Interfaces.WeaponType.Slashers);
    static readonly bool _twinBlades = Core.BleedingEdge.Contains(Interfaces.WeaponType.TwinBlades);
    static readonly bool _onHitEffects = ConfigService.ClassOnHitEffects;
    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly float _onHitProcChance = ConfigService.OnHitProcChance;

    const float SHINY_DEBUFF_CHANCE = 0.1f;

    static readonly PrefabGUID _activeCharmedHumanBuff = Buffs.ActiveCharmedHumanBuff;

    [HarmonyPatch(typeof(StatChangeSystem), nameof(StatChangeSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StatChangeSystem __instance)
    {
        if (!Core.IsReady) return;

        // NativeArray<Entity> entities = __instance._DamageTakenEventQuery.ToEntityArray(Allocator.Temp);
        // NativeArray<DamageTakenEvent> damageTakenEvents = __instance._DamageTakenEventQuery.ToComponentDataArray<DamageTakenEvent>(Allocator.Temp);

        using NativeAccessor<Entity> entities = __instance._DamageTakenEventQuery.ToEntityArrayAccessor(Allocator.Temp);
        using NativeAccessor<DamageTakenEvent> damageTakenEvents = __instance._DamageTakenEventQuery.ToComponentDataArrayAccessor<DamageTakenEvent>(Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                DamageTakenEvent damageTakenEvent = damageTakenEvents[i];

                PrefabGUID sourcePrefabGuid = damageTakenEvent.Source.GetPrefabGuid();
                Entity sourceOwner = damageTakenEvent.Source.GetOwner();

                // Core.Log.LogWarning($"[StatChangeSystem] Source: {sourcePrefabGuid.GetPrefabName()} | Target: {damageTakenEvent.Entity.GetPrefabGuid().GetPrefabName()}");

                if (_ignoredSources.Contains(sourcePrefabGuid)) continue;
                else if (_familiars && sourceOwner.TryGetFollowedPlayer(out Entity playerCharacter))
                {
                    if (damageTakenEvent.Entity.IsPlayer() && playerCharacter.Equals(damageTakenEvent.Entity))
                    {
                        entity.Destroy(true);
                    }
                    else if (IsValidTarget(damageTakenEvent.Entity))
                    {
                        PrefabGUID shinyDebuff = _shinyOnHitDebuffs.FirstOrDefault(buff => sourceOwner.HasBuff(buff));

                        if (shinyDebuff.HasValue() && sourceOwner.TryGetBuffStacks(shinyDebuff, out Entity _, out int stacks))
                        {
                            float debuffChance = SHINY_DEBUFF_CHANCE * stacks;
                            double nextDouble = _random.NextDouble();

                            // Core.Log.LogWarning($"Familiar rolling for shiny debuff - {sourceOwner.GetPrefabGuid().GetPrefabName()}");

                            if (nextDouble <= debuffChance)
                            {
                                // Core.Log.LogWarning($"Familiar applying shiny debuff {shinyDebuff.GetPrefabName()}! ({stacks}|{nextDouble}|{debuffChance * 100}%)");
                                damageTakenEvent.Entity.TryApplyBuffWithOwner(sourceOwner, shinyDebuff);
                            }
                            else
                            {
                                // Core.Log.LogWarning($"Familiar failed to apply shiny debuff {shinyDebuff.GetPrefabName()}! ({stacks}|{nextDouble}|{debuffChance * 100}%)");
                            }
                        }
                    }
                }
                else
                {
                    if (_gameMode.Equals(GameModeType.PvE) && damageTakenEvent.Entity.HasBuff(_activeCharmedHumanBuff))
                    {
                        entity.Destroy(true);
                    }
                    else if (_quests && damageTakenEvent.Entity.Has<YieldResourcesOnDamageTaken>() && sourceOwner.TryGetPlayer(out playerCharacter))
                    {
                        ulong steamId = playerCharacter.GetSteamId();
                        _lastDamageTime[steamId] = DateTime.UtcNow;
                    }
                    else if (IsValidTarget(damageTakenEvent.Entity) && sourceOwner.TryGetPlayer(out playerCharacter))
                    {
                        // Core.Log.LogInfo($"Player: {steamId} | Source: {dealDamageEvent.SpellSource.GetPrefabGuid().GetPrefabName()}");
                        ulong steamId = playerCharacter.GetSteamId();

                        if (sourcePrefabGuid.Equals(_slashersMeleeHit03))
                        {
                            damageTakenEvent.Entity.TryApplyBuff(_vargulfBleedBuff);
                        }

                        if (sourcePrefabGuid.Equals(_twinBladesSweepingStrikeHit) && damageTakenEvent.Entity.IsVBloodOrGateBoss())
                        {
                            playerCharacter.TryApplyBuff(_iceShieldBuff);
                        }

                        if (!_onHitEffects || !_classes) continue;

                        if (!steamId.HasClass(out PlayerClass? playerClass)
                            || !playerClass.HasValue) continue;

                        if (_random.NextDouble() <= _onHitProcChance && ClassOnDamageEffects.TryGetValue(playerClass.Value, out OnHitEffects onDamageEffects))
                        {
                            onDamageEffects.ApplyEffect(playerCharacter, damageTakenEvent.Entity);
                        }
                    }
                    else if (_familiars)
                    {
                        if (_gameMode.Equals(GameModeType.PvP) && sourceOwner.TryGetPlayer(out playerCharacter) && damageTakenEvent.Entity.IsPlayer())
                        {
                            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (_familiarPvP && familiar.EligibleForCombat())
                            {
                                Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, [damageTakenEvent.Entity]);
                            }
                        }
                        else if (damageTakenEvent.Entity.TryGetFollowedPlayer(out playerCharacter))
                        {
                            ReactToUnitDamage(playerCharacter, sourceOwner);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[StatChangeSystem] Exception: {e}");
        }
        finally
        {
            // entities.Dispose();
            // damageTakenEvents.Dispose();
        }
    }
    public static void RemoveOnItemPickup(ulong steamId)
    {
        _lastDamageTime.TryRemove(steamId, out DateTime _);
    }
    static bool IsValidTarget(Entity entity)
    {
        return entity.Has<Movement>() && entity.Has<Health>() && !entity.IsPlayer();
    }
    static void ReactToUnitDamage(Entity playerCharacter, Entity spellOwner)
    {
        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

        if (familiar.TryGetComponent(out BehaviourTreeState behaviourTreeState) && familiar.EligibleForCombat())
        {
            if (!behaviourTreeState.Value.Equals(GenericEnemyState.Combat))
            {
                familiar.With((ref Follower follower) => follower.ModeModifiable._Value = 1);

                Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, [spellOwner]);
            }
        }
    }
}

/*
#nullable enable
internal static class DealDamageSystemDetour
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly Random _random = new();

    public static readonly Dictionary<ulong, DateTime> LastDamageTime = [];

    static readonly Dictionary<PlayerClass, PrefabGUID> _classOnHitDebuffs = new() // tier 1
    {
        { PlayerClass.BloodKnight, Buffs.VampireLeechDebuff },
        { PlayerClass.DemonHunter, Buffs.VampireStaticDebuff },   
        { PlayerClass.VampireLord, Buffs.VampireChillDebuff },  
        { PlayerClass.ShadowBlade, Buffs.VampireIgniteDebuff },   
        { PlayerClass.ArcaneSorcerer, Buffs.VampireWeakenDebuff }, 
        { PlayerClass.DeathMage, Buffs.VampireCondemnDebuff }
    };

    static readonly Dictionary<PlayerClass, PrefabGUID> _classOnHitEffects = new() // tier 2
    {
        { PlayerClass.BloodKnight, new(2085766220) },    // lesser bloodrage
        { PlayerClass.DemonHunter, new(-737425100) },    // lesser stormshield
        { PlayerClass.VampireLord, new(620130895) },     // lesser frozenweapon
        { PlayerClass.ShadowBlade, new(763939566) },     // lesser powersurge
        { PlayerClass.ArcaneSorcerer, new(1433921398) }, // lesser aegis
        { PlayerClass.DeathMage, new(-2071441247) }      // guardian block :p
    };

    static readonly HashSet<PrefabGUID> _shinyOnHitDebuffs = new()
    {
        { Buffs.VampireIgniteDebuff},
        { Buffs.VampireStaticDebuff}, 
        { Buffs.VampireLeechDebuff}, 
        { Buffs.VampireWeakenDebuff}, 
        { Buffs.VampireChillDebuff},  
        { Buffs.VampireCondemnDebuff}  
    };

    static readonly PrefabGUID _stormShield03 = Buffs.StormShieldTertiaryBuff;
    static readonly PrefabGUID _stormShield02 = Buffs.StormShieldSecondaryBuff;
    static readonly PrefabGUID _stormShield01 = Buffs.StormShieldPrimaryBuff;
    static readonly PrefabGUID _garlicDebuff = Buffs.GarlicDebuff;
    static readonly PrefabGUID _silverDebuff = Buffs.SilverDebuff;

    static readonly PrefabGUID _slashersMeleeHit03 = Prefabs.AB_Vampire_Slashers_Primary_MeleeAttack_Hit03;
    static readonly PrefabGUID _vargulfBleedBuff = Buffs.VargulfBleedBuff;
    static readonly PrefabGUID _twinBladesSweepingStrikeHit = Prefabs.AB_Vampire_TwinBlades_SweepingStrike_Hit;
    static readonly PrefabGUID _twinBladesIceShield = Prefabs.Frost_Vampire_Buff_IceShield_SpellMod;

    static readonly bool _slashersBleed = ConfigService.BleedingEdge;
    static readonly bool _onHitEffects = ConfigService.ClassSpellSchoolOnHitEffects;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly float _onHitProcChance = ConfigService.OnHitProcChance;

    const float SHINY_DEBUFF_BASE = 0.1f;

    static readonly PrefabGUID _activeCharmedHumanBuff = Buffs.ActiveCharmedHumanBuff;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void DealDamageSystemHandler(IntPtr _this, ref SystemState state);
    static DealDamageSystemHandler? _dealDamageSystem;
    static INativeDetour? _dealDamageSystemDetour;
    public static unsafe void Initialize()
    {
        try
        {
            _dealDamageSystemDetour = NativeDetour.Create(
            typeof(DealDamageSystem),
            "OnUpdate",
            HandleDealDamageSystem,
            out _dealDamageSystem);
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Failed to route {nameof(DealDamageSystemDetour)}: {e}");
        }
    }
    public static unsafe void HandleDealDamageSystem(IntPtr _this, ref SystemState state)
    {
        Core.Log.LogWarning($"[DealDamageSystem Detour]");

        // ref SystemState systemState = ref *(SystemState*)state;
        NativeArray<Entity> entities = systemState.q.get_Item(0).ToEntityArray(Allocator.Temp);
        NativeArray<DealDamageEvent> dealDamageEvents = state.EntityQueries.get_Item(0).ToComponentDataArray<DealDamageEvent>(Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities.get_Item(i);
                DealDamageEvent dealDamageEvent = dealDamageEvents.get_Item(i);

                PrefabGUID spellPrefabGuid = dealDamageEvent.SpellSource.GetPrefabGuid();
                Entity spellOwner = dealDamageEvent.SpellSource.GetOwner();

                if (spellPrefabGuid.Equals(_silverDebuff) || spellPrefabGuid.Equals(_garlicDebuff)) continue;
                else if (_familiars && spellOwner.TryGetFollowedPlayer(out Entity playerCharacter))
                {
                    if (dealDamageEvent.Target.IsPlayer() && playerCharacter.Equals(dealDamageEvent.Target))
                    {
                        EntityManager.DestroyEntity(entity); // need to destroy with main entityManager, destroy event too late/ineffective here for Raziel's holy damage against player owner
                    }
                    else if (IsValidDamageType(dealDamageEvent) && IsValidTarget(dealDamageEvent.Target))
                    {
                        PrefabGUID shinyDebuff = _shinyOnHitDebuffs.FirstOrDefault(buff => spellOwner.HasBuff(buff));

                        if (shinyDebuff.HasValue() && spellOwner.TryGetBuffStacks(shinyDebuff, out int stacks))
                        {
                            float debuffChance = SHINY_DEBUFF_BASE * stacks;
                            double nextDouble = _random.NextDouble();

                            if (nextDouble <= debuffChance)
                            {
                                Core.Log.LogWarning($"Familiar applying shiny debuff {shinyDebuff.GetPrefabName()}! ({stacks}|{nextDouble}|{debuffChance * 100}%)");
                                dealDamageEvent.Target.TryApplyBuffWithOwner(spellOwner, shinyDebuff);
                            }
                            else
                            {
                                Core.Log.LogWarning($"Familiar failed to apply shiny debuff {shinyDebuff.GetPrefabName()}! ({stacks}|{nextDouble}|{debuffChance * 100}%)");
                            }
                        }
                    }
                }
                else if (IsValidDamageType(dealDamageEvent))
                {
                    if (_gameMode.Equals(GameModeType.PvE) && dealDamageEvent.Target.HasBuff(_activeCharmedHumanBuff))
                    {
                        EntityManager.DestroyEntity(entity);
                        continue;
                    }
                    else if (_quests && dealDamageEvent.Target.Has<YieldResourcesOnDamageTaken>() && spellOwner.TryGetPlayer(out playerCharacter))
                    {
                        ulong steamId = playerCharacter.GetSteamId();
                        LastDamageTime[steamId] = DateTime.UtcNow;
                    }
                    else if (_onHitEffects && IsValidTarget(dealDamageEvent.Target) && spellOwner.TryGetPlayer(out playerCharacter))
                    {
                        // Core.Log.LogInfo($"Player: {steamId} | Source: {dealDamageEvent.SpellSource.GetPrefabGuid().GetPrefabName()}");
                        ulong steamId = playerCharacter.GetSteamId();

                        if (_slashersBleed && spellPrefabGuid.Equals(_slashersMeleeHit03))
                        {
                            if (!dealDamageEvent.Target.HasBuff(_vargulfBleedBuff))
                            {
                                // Core.Log.LogInfo($"Applying Vargulf Bleed to {dealDamageEvent.Target.GetPrefabGuid().GetPrefabName()}");
                                ServerGameManager.InstantiateBuffEntityImmediate(playerCharacter, dealDamageEvent.Target, _vargulfBleedBuff);
                            }
                            else if (dealDamageEvent.Target.TryGetBuff(_vargulfBleedBuff, out Entity buffEntity) && buffEntity.TryGetComponent(out Buff buff))
                            {
                                int stacks = buff.Stacks + 1;
                                int newStacks = stacks > buff.MaxStacks ? buff.MaxStacks : stacks;

                                ServerGameManager.InstantiateBuffEntityImmediate(playerCharacter, dealDamageEvent.Target, _vargulfBleedBuff, null, newStacks);
                                // Core.Log.LogInfo($"Adding Vargulf Bleed stack to {dealDamageEvent.Target.GetPrefabGuid().GetPrefabName()}");
                            }
                        }

                        if (spellPrefabGuid.Equals(_twinBladesSweepingStrikeHit) && dealDamageEvent.Target.IsVBloodOrGateBoss())
                        {
                            spellOwner.TryApplyBuff(_twinBladesIceShield);
                        }

                        if (!HasClass(steamId)) continue;
                        PlayerClass playerClass = GetPlayerClass(steamId);

                        if (_random.NextDouble() <= _onHitProcChance && _classOnHitDebuffs.TryGetValue(playerClass, out PrefabGUID prefabGuid))
                        {
                            if (dealDamageEvent.Target.HasBuff(prefabGuid) && _classOnHitEffects.TryGetValue(playerClass, out prefabGuid))
                            {
                                if (playerClass.Equals(PlayerClass.DemonHunter))
                                {
                                    if (!playerCharacter.HasBuff(_stormShield03))
                                    {
                                        if (!playerCharacter.HasBuff(_stormShield02))
                                        {
                                            if (!playerCharacter.HasBuff(_stormShield01))
                                            {
                                                Buffs.TryApplyBuff(playerCharacter, _stormShield01);
                                            }
                                            else if (playerCharacter.TryGetBuff(_stormShield01, out Entity stormShieldFirstBuff))
                                            {
                                                stormShieldFirstBuff.With((ref Age age) =>
                                                {
                                                    age.Value = 0f;
                                                });
                                                Buffs.TryApplyBuff(playerCharacter, _stormShield02);
                                            }
                                        }
                                        else if (playerCharacter.TryGetBuff(_stormShield02, out Entity stormShieldSecondBuff))
                                        {
                                            stormShieldSecondBuff.With((ref Age age) =>
                                            {
                                                age.Value = 0f;
                                            });
                                            Buffs.TryApplyBuff(playerCharacter, _stormShield03);
                                        }
                                    }
                                    else if (playerCharacter.TryGetBuff(_stormShield03, out Entity stormShieldThirdBuff))
                                    {
                                        stormShieldThirdBuff.With((ref Age age) =>
                                        {
                                            age.Value = 0f;
                                        });
                                    }
                                }
                                else
                                {
                                    Buffs.TryApplyBuff(playerCharacter, prefabGuid);
                                }
                            }
                            else
                            {
                                if (Buffs.TryApplyBuff(dealDamageEvent.Target, prefabGuid) && dealDamageEvent.Target.TryGetBuff(prefabGuid, out Entity buffEntity))
                                {
                                    buffEntity.With((ref EntityOwner entityOwner) =>
                                    {
                                        entityOwner.Owner = playerCharacter;
                                    });
                                }
                            }
                        }
                    }
                    else if (_familiars)
                    {
                        if (_gameMode.Equals(GameModeType.PvP) && spellOwner.TryGetPlayer(out playerCharacter) && dealDamageEvent.Target.IsPlayer())
                        {
                            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (_familiarPvP && familiar.EligibleForCombat())
                            {
                                Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, [dealDamageEvent.Target]);
                            }
                        }
                        else if (dealDamageEvent.Target.TryGetFollowedPlayer(out playerCharacter))
                        {
                            ReactToUnitDamage(playerCharacter, spellOwner);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            dealDamageEvents.Dispose();
        }
        _dealDamageSystem!(_this, ref state);
    }
    static bool IsValidDamageType(DealDamageEvent dealDamageEvent)
    {
        return dealDamageEvent.MainType.Equals(MainDamageType.Physical) || dealDamageEvent.MainType.Equals(MainDamageType.Spell);
    }
    static bool IsValidTarget(Entity entity)
    {
        return entity.Has<Movement>() && entity.Has<Health>() && !entity.IsPlayer();
    }
    static void ReactToUnitDamage(Entity playerCharacter, Entity spellOwner)
    {
        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

        if (familiar.TryGetComponent(out BehaviourTreeState behaviourTreeState) && familiar.EligibleForCombat())
        {
            if (!behaviourTreeState.Value.Equals(GenericEnemyState.Combat))
            {
                familiar.With((ref Follower follower) =>
                {
                    follower.ModeModifiable._Value = 1;
                });

                Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, [spellOwner]);
            }
        }
    }
}
*/

/*
[HarmonyPatch]
internal static class DealDamageSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;
    static readonly Random _random = new();

    public static readonly Dictionary<ulong, DateTime> LastDamageTime = [];

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _onHitEffects = ConfigService.ClassSpellSchoolOnHitEffects;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly float _onHitProcChance = ConfigService.OnHitProcChance;

    static readonly HashSet<PrefabGUID> _shinyOnHitDebuffs = new()
    {
        new(348724578),  // Ignite
        new(-1576512627), // Static
        new(-1246704569), // Leech
        new(1723455773),  // Weaken
        new(27300215),    // Chill
        new(-325758519)   // Condemn
    };

    static readonly PrefabGUID _stormShield03 = new(1095865904);
    static readonly PrefabGUID _stormShield02 = new(-1192885497);
    static readonly PrefabGUID _stormShield01 = new(1044565673);
    static readonly PrefabGUID _garlicDebuff = new(-1701323826);
    static readonly PrefabGUID _silverDebuff = new(853298599);

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        NativeArray<DealDamageEvent> dealDamageEvents = __instance._Query.ToComponentDataArray<DealDamageEvent>(Allocator.Temp);

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                DealDamageEvent dealDamageEvent = dealDamageEvents[i];

                if (!dealDamageEvent.SpellSource.Exists() || !dealDamageEvent.Target.Exists()) continue;
                if (dealDamageEvent.SpellSource.HasBuff(_silverDebuff) || dealDamageEvent.SpellSource.HasBuff(_garlicDebuff)) continue;

                if (!dealDamageEvent.SpellSource.TryGetComponent(out PrefabGUID sourcePrefabGUID) || 
                    !dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner)) continue;

                Entity owner = entityOwner.Owner;
                Entity target = dealDamageEvent.Target;

                int damageType = GetBuffType(sourcePrefabGUID, owner, target, ref playerCharacterLookup, ref blockFeedBuffLookup);

                switch (damageType)
                {
                    case 1 when _familiars:
                        if (target.IsPlayer() && owner.Equals(target))
                        {
                            EntityManager.DestroyEntity(entity);
                        }
                        else if (_random.NextDouble() <= _onHitProcChance && IsValidDamageType(dealDamageEvent) && IsValidTarget(target))
                        {
                            PrefabGUID shinyDebuff = _shinyOnHitDebuffs.FirstOrDefault(buff => owner.HasBuff(buff));
                            if (shinyDebuff.HasValue()) target.TryApplyBuffWithOwner(owner, shinyDebuff);
                        }
                        break;

                    case 2 when _quests:
                        ulong steamId = owner.GetSteamId();
                        LastDamageTime[steamId] = DateTime.UtcNow;
                        break;

                    case 3 when _onHitEffects:
                        HandleOnHitEffect(owner, target, dealDamageEvent);
                        break;

                    case 4 when _familiars:
                        if (_gameMode.Equals(GameModeType.PvP) && owner.TryGetPlayer(out Entity playerCharacter) && target.IsPlayer())
                        {
                            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
                            if (_familiarPvP && familiar.EligibleForCombat())
                            {
                                Familiars.AddToFamiliarAggroBuffer(familiar, target);
                            }
                        }
                        else if (blockFeedBuffLookup.HasComponent(target))
                        {
                            ReactToUnitDamage(target, owner);
                        }
                        break;
                }
            }
        }
        finally
        {
            entities.Dispose();
            dealDamageEvents.Dispose();
        }
    }

    static int GetBuffType(
        PrefabGUID sourcePrefabGUID,
        Entity owner,
        Entity target,
        ref ComponentLookup<PlayerCharacter> playerCharacterLookup,
        ref ComponentLookup<BlockFeedBuff> blockFeedBuffLookup)
    {
        if (playerCharacterLookup.HasComponent(owner))
        {
            return _familiars ? 1 :
                   _quests && target.Has<YieldResourcesOnDamageTaken>() ? 2 :
                   _onHitEffects && IsValidTarget(target) ? 3 : 0;
        }
        else if (blockFeedBuffLookup.HasComponent(target))
        {
            return 4; // Familiar PvP behavior or aggro response
        }
        return 0;
    }

    static void HandleOnHitEffect(Entity owner, Entity target, DealDamageEvent dealDamageEvent)
    {
        ulong steamId = owner.GetSteamId();
        if (!HasClass(steamId)) return;

        PlayerClass playerClass = GetPlayerClass(steamId);
        if (_random.NextDouble() > _onHitProcChance) return;

        if (_classOnHitDebuffs.TryGetValue(playerClass, out PrefabGUID debuff))
        {
            if (target.HasBuff(debuff) && _classOnHitEffects.TryGetValue(playerClass, out PrefabGUID effect))
            {
                Buffs.TryApplyBuff(owner, effect);
            }
            else if (Buffs.TryApplyBuff(target, debuff) && target.TryGetBuff(debuff, out Entity buffEntity))
            {
                buffEntity.With((ref EntityOwner buffOwner) => buffOwner.Owner = owner);
            }
        }
    }

    static bool IsValidDamageType(DealDamageEvent dealDamageEvent)
    {
        return dealDamageEvent.MainType.Equals(MainDamageType.Physical) || dealDamageEvent.MainType.Equals(MainDamageType.Spell);
    }

    static bool IsValidTarget(Entity entity)
    {
        return entity.Has<Movement>() && entity.Has<Health>() && !entity.IsPlayer();
    }

    static void ReactToUnitDamage(Entity playerCharacter, Entity owner)
    {
        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

        if (familiar.TryGetComponent(out BehaviourTreeState behaviourTreeState) && familiar.EligibleForCombat())
        {
            if (!behaviourTreeState.Value.Equals(GenericEnemyState.Combat))
            {
                familiar.With((ref Follower follower) => follower.ModeModifiable._Value = 1);
                Familiars.AddToFamiliarAggroBuffer(familiar, owner);
            }
        }
    }
}
*/