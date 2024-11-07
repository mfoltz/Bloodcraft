using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Leveling.LevelingSystem;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DealDamageSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly Random Random = new();

    public static readonly Dictionary<ulong, DateTime> LastDamageTime = [];

    static readonly Dictionary<PlayerClass, PrefabGUID> ClassOnHitDebuffMap = new() // tier 1
    {
        { PlayerClass.BloodKnight, new(-1246704569) }, //leech
        { PlayerClass.DemonHunter, new(-1576512627) }, //static
        { PlayerClass.VampireLord, new(27300215) }, // chill
        { PlayerClass.ShadowBlade, new(348724578) }, // ignite
        { PlayerClass.ArcaneSorcerer, new(1723455773) }, // weaken
        { PlayerClass.DeathMage, new(-325758519) } // condemn
    };

    static readonly Dictionary<PlayerClass, PrefabGUID> ClassOnHitEffectMap = new() // tier 2
    {
        { PlayerClass.BloodKnight, new(2085766220) }, // lesser bloodrage
        { PlayerClass.DemonHunter, new(-737425100) }, // lesser stormshield
        { PlayerClass.VampireLord, new(620130895) }, // lesser frozenweapon
        { PlayerClass.ShadowBlade, new(763939566) }, // lesser powersurge
        { PlayerClass.ArcaneSorcerer, new(1433921398) }, // lesser aegis
        { PlayerClass.DeathMage, new(-2071441247) } // guardian block :p
    };

    static readonly PrefabGUID stormShield03 = new(1095865904);
    static readonly PrefabGUID stormShield02 = new(-1192885497);
    static readonly PrefabGUID stormShield01 = new(1044565673);
    static readonly PrefabGUID garlicDebuff = new(-1701323826);
    static readonly PrefabGUID silverDebuff = new(853298599);

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out DealDamageEvent dealDamageEvent)) continue;
                else if (!dealDamageEvent.Target.Exists() || !dealDamageEvent.SpellSource.Exists()) continue; // checks are kind of excessive here but null entities in this system can reeeeally mess things up for a save so leaving them for safety >_>
                //else if (dealDamageEvent.MainType != MainDamageType.Physical && dealDamageEvent.MainType != MainDamageType.Spell) continue; // skip if source isn't phys/spell
                else if (dealDamageEvent.SpellSource.TryGetComponent(out PrefabGUID sourcePrefabGUID) && (sourcePrefabGUID.Equals(silverDebuff) || sourcePrefabGUID.Equals(garlicDebuff))) continue; // skip if source is silver or garlic
                else if (!dealDamageEvent.SpellSource.Has<EntityOwner>()) continue; // not really sure why this would be the case but seems to be popping up in console so okay I guess
                
                //Core.Log.LogInfo(dealDamageEvent.SpellSource.GetPrefabGUID().LookupName());
                //Core.Log.LogInfo($"{dealDamageEvent.SpellSource.GetOwner().GetPrefabGUID().LookupName()} | {dealDamageEvent.Target.GetPrefabGUID().LookupName()}");
                if (dealDamageEvent.SpellSource.GetOwner().IsFollowingPlayer() && dealDamageEvent.Target.IsPlayer() && ServerGameManager.IsAllies(dealDamageEvent.SpellSource.GetOwner(), dealDamageEvent.Target))
                {
                    //Core.Log.LogInfo("Follower attacking ally, preventing...");
                    //DestroyUtility.Destroy(EntityManager, entity);
                    EntityManager.DestroyEntity(entity);
                }

                if (dealDamageEvent.MainType != MainDamageType.Physical && dealDamageEvent.MainType != MainDamageType.Spell) continue; // skip if source isn't phys/spell at this point

                if (ConfigService.QuestSystem && dealDamageEvent.Target.Has<YieldResourcesOnDamageTaken>() && dealDamageEvent.SpellSource.GetOwner().TryGetPlayer(out Entity player))
                {
                    ulong steamId = player.GetSteamId();
                    LastDamageTime[steamId] = DateTime.UtcNow;
                }
                else if (ConfigService.ClassSpellSchoolOnHitEffects && dealDamageEvent.SpellSource.GetOwner().TryGetPlayer(out player) && !dealDamageEvent.Target.IsPlayer())
                {
                    Entity userEntity = player.Read<PlayerCharacter>().UserEntity;
                    ulong steamId = userEntity.Read<User>().PlatformId;
                    if (!ClassUtilities.HasClass(steamId)) continue;

                    PlayerClass playerClass = ClassUtilities.GetPlayerClass(steamId);
                    if (Random.NextDouble() <= ConfigService.OnHitProcChance)
                    {
                        PrefabGUID prefabGUID = ClassOnHitDebuffMap[playerClass];

                        FromCharacter fromCharacter = new()
                        {
                            Character = dealDamageEvent.Target,
                            User = userEntity
                        };

                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = prefabGUID,
                        };

                        if (ServerGameManager.HasBuff(dealDamageEvent.Target, prefabGUID.ToIdentifier()))
                        {
                            applyBuffDebugEvent.BuffPrefabGUID = ClassOnHitEffectMap[playerClass];
                            fromCharacter.Character = player;

                            if (playerClass.Equals(PlayerClass.DemonHunter))
                            {
                                if (ServerGameManager.TryGetBuff(player, stormShield01.ToIdentifier(), out Entity firstBuff))
                                {
                                    firstBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else if (ServerGameManager.TryGetBuff(player, stormShield02.ToIdentifier(), out Entity secondBuff))
                                {
                                    secondBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else if (ServerGameManager.TryGetBuff(player, stormShield03.ToIdentifier(), out Entity thirdBuff))
                                {
                                    thirdBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else
                                {
                                    DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                                }
                            }
                            else
                            {
                                DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                            }
                        }
                        else
                        {
                            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                            if (ServerGameManager.TryGetBuff(dealDamageEvent.Target, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
                            {
                                buff.Write(new EntityOwner { Owner = player });
                            }
                        }
                    }
                }
                else if (ConfigService.FamiliarSystem && GameMode.Equals(GameModeType.PvP) && dealDamageEvent.SpellSource.GetOwner().TryGetPlayer(out player) && dealDamageEvent.Target.IsPlayer())
                {
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                    if (familiar.Exists() && !familiar.IsDisabled())
                    {
                        FamiliarUtilities.AddToFamiliarAggroBuffer(familiar, dealDamageEvent.Target);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
