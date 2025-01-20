using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Classes;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DealDamageSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
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

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _onHitEffects = ConfigService.ClassSpellSchoolOnHitEffects;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly float _onHitProcChance = ConfigService.OnHitProcChance;

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out DealDamageEvent dealDamageEvent) || !dealDamageEvent.SpellSource.TryGetComponent(out PrefabGUID sourcePrefabGUID) || !dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner)) continue;
                else if (sourcePrefabGUID.Equals(_silverDebuff) || sourcePrefabGUID.Equals(_garlicDebuff)) continue; // these both count for physical damage so need to check damage source
                else if (!dealDamageEvent.Target.Exists() || !dealDamageEvent.SpellSource.Exists()) continue; // checks are kind of excessive here but null entities here do weird things I don't want to have to figure out, like, ever >_>

                if (_familiars && entityOwner.Owner.TryGetFollowedPlayer(out Entity playerCharacter))
                {
                    if (dealDamageEvent.Target.IsPlayer() && playerCharacter.Equals(dealDamageEvent.Target))
                    {
                        EntityManager.DestroyEntity(entity); // need to destroy with main entityManager, destroy event too late/ineffective here for Raziel's holy damage against player owner
                    }
                    else if (_random.NextDouble() <= _onHitProcChance && IsValidDamageType(dealDamageEvent) && IsValidTarget(dealDamageEvent.Target))
                    {
                        PrefabGUID shinyDebuff = _shinyOnHitDebuffs.FirstOrDefault(buff => entityOwner.Owner.HasBuff(buff));

                        if (shinyDebuff.HasValue())
                        {
                            dealDamageEvent.Target.TryApplyBuffWithOwner(entityOwner.Owner, shinyDebuff);
                        }
                    }
                }
                else if (IsValidDamageType(dealDamageEvent))
                {
                    if (_quests && dealDamageEvent.Target.Has<YieldResourcesOnDamageTaken>() && entityOwner.Owner.TryGetPlayer(out playerCharacter))
                    {
                        ulong steamId = playerCharacter.GetSteamId();
                        LastDamageTime[steamId] = DateTime.UtcNow;
                    }
                    else if (_onHitEffects && IsValidTarget(dealDamageEvent.Target) && entityOwner.Owner.TryGetPlayer(out playerCharacter))
                    {
                        ulong steamId = playerCharacter.GetSteamId();

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
                                                playerCharacter.TryApplyBuff(_stormShield01);
                                            }
                                            else if (playerCharacter.TryGetBuff(_stormShield01, out Entity stormShieldFirstBuff))
                                            {
                                                stormShieldFirstBuff.With((ref Age age) =>
                                                {
                                                    age.Value = 0f;
                                                });

                                                playerCharacter.TryApplyBuff(_stormShield02);
                                            }
                                        }
                                        else if (playerCharacter.TryGetBuff(_stormShield02, out Entity stormShieldSecondBuff))
                                        {
                                            stormShieldSecondBuff.With((ref Age age) =>
                                            {
                                                age.Value = 0f;
                                            });

                                            playerCharacter.TryApplyBuff(_stormShield03);
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
                                    playerCharacter.TryApplyBuff(prefabGuid);
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
                            Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                            if (_familiarPvP && familiar.IsEligibleForCombat())
                            {
                                Familiars.AddToFamiliarAggroBuffer(familiar, dealDamageEvent.Target);
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
        Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

        if (familiar.TryGetComponent(out BehaviourTreeState behaviourTreeState) && familiar.IsEligibleForCombat())
        {
            if (!behaviourTreeState.Value.Equals(GenericEnemyState.Combat))
            {
                Familiars.AddToFamiliarAggroBuffer(familiar, entityOwner.Owner);
            }
        }
    }
}
