using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Classes;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DealDamageSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly Random _random = new();

    public static readonly Dictionary<ulong, DateTime> LastDamageTime = [];

    static readonly Dictionary<PlayerClass, PrefabGUID> _classOnHitDebuffs = new() // tier 1
    {
        { PlayerClass.BloodKnight, new(-1246704569) },   // leech
        { PlayerClass.DemonHunter, new(-1576512627) },   // static
        { PlayerClass.VampireLord, new(27300215) },      // chill
        { PlayerClass.ShadowBlade, new(348724578) },     // ignite
        { PlayerClass.ArcaneSorcerer, new(1723455773) }, // weaken
        { PlayerClass.DeathMage, new(-325758519) }       // condemn
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
        { new(348724578)},   // ignite
        { new(-1576512627)}, // static
        { new(-1246704569)}, // leech
        { new(1723455773)},  // weaken
        { new(27300215)},    // chill
        { new(-325758519)}   // condemn
    };

    static readonly PrefabGUID _stormShield03 = new(1095865904);
    static readonly PrefabGUID _stormShield02 = new(-1192885497);
    static readonly PrefabGUID _stormShield01 = new(1044565673);
    static readonly PrefabGUID _garlicDebuff = new(-1701323826);
    static readonly PrefabGUID _silverDebuff = new(853298599);

    static readonly PrefabGUID _slashersHit03 = new(-130408903);
    static readonly PrefabGUID _vargulfBleedBuff = new(1581496399);

    static readonly bool _slashersBleed = ConfigService.BleedingEdge;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _onHitEffects = ConfigService.ClassSpellSchoolOnHitEffects;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly float _onHitProcChance = ConfigService.OnHitProcChance;

    const float SHINY_DEBUFF_CHANCE = 0.1f;

    static readonly PrefabGUID _activeCharmedHumanBuff = new(1303169868);

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        // NativeArray<DealDamageEvent> dealDamageEvents = __instance._Query.ToComponentDataArray<DealDamageEvent>(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out DealDamageEvent dealDamageEvent) || !dealDamageEvent.SpellSource.TryGetComponent(out PrefabGUID sourcePrefabGuid) || !dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner)) continue;
                else if (sourcePrefabGuid.Equals(_silverDebuff) || sourcePrefabGuid.Equals(_garlicDebuff)) continue; // these both count for physical damage so need to check damage source
                else if (!dealDamageEvent.Target.Exists() || !dealDamageEvent.SpellSource.Exists()) continue; // checks are kind of excessive here but null entities here do weird things I don't want to have to figure out, like, ever >_>

                if (_familiars && entityOwner.Owner.TryGetFollowedPlayer(out Entity playerCharacter))
                {
                    if (dealDamageEvent.Target.IsPlayer() && playerCharacter.Equals(dealDamageEvent.Target))
                    {
                        EntityManager.DestroyEntity(entity); // need to destroy with main entityManager, destroy event too late/ineffective here for Raziel's holy damage against player owner
                    }
                    else if (IsValidDamageType(dealDamageEvent) && IsValidTarget(dealDamageEvent.Target))
                    {
                        Entity familiar = entityOwner.Owner;
                        PrefabGUID shinyDebuff = _shinyOnHitDebuffs.FirstOrDefault(buff => familiar.HasBuff(buff));

                        if (shinyDebuff.HasValue() && familiar.TryGetBuffStacks(shinyDebuff, out int stacks))
                        {
                            float debuffChance = SHINY_DEBUFF_CHANCE * stacks;
                            double nextDouble = _random.NextDouble();

                            if (nextDouble <= debuffChance)
                            {
                                Core.Log.LogWarning($"Familiar applying shiny debuff {shinyDebuff.GetPrefabName()}! ({stacks}|{nextDouble}|{debuffChance * 100}%)");
                                dealDamageEvent.Target.TryApplyBuffWithOwner(familiar, shinyDebuff);
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
                    else if (_quests && dealDamageEvent.Target.Has<YieldResourcesOnDamageTaken>() && entityOwner.Owner.TryGetPlayer(out playerCharacter))
                    {
                        ulong steamId = playerCharacter.GetSteamId();
                        LastDamageTime[steamId] = DateTime.UtcNow;
                    }
                    else if (_onHitEffects && IsValidTarget(dealDamageEvent.Target) && entityOwner.Owner.TryGetPlayer(out playerCharacter))
                    {
                        // Core.Log.LogInfo($"Player: {steamId} | Source: {dealDamageEvent.SpellSource.GetPrefabGuid().GetPrefabName()}");
                        ulong steamId = playerCharacter.GetSteamId();

                        if (sourcePrefabGuid.Equals(_slashersHit03))
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
                        if (_gameMode.Equals(GameModeType.PvP) && entityOwner.Owner.TryGetPlayer(out playerCharacter) && dealDamageEvent.Target.IsPlayer())
                        {
                            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (_familiarPvP && familiar.EligibleForCombat())
                            {
                                Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, dealDamageEvent.Target);
                            }
                        }
                        else if (dealDamageEvent.Target.TryGetFollowedPlayer(out playerCharacter))
                        {
                            ReactToUnitDamage(playerCharacter, entityOwner);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
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
    static void ReactToUnitDamage(Entity playerCharacter, EntityOwner entityOwner)
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

                Familiars.AddToFamiliarAggroBuffer(playerCharacter, familiar, entityOwner.Owner);
            }
        }
    }
}

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