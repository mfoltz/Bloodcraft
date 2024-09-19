using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Leveling.LevelingSystem;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DealDamageSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly Random Random = new();

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    public static readonly Dictionary<ulong, DateTime> LastDamageTime = [];

    static readonly PrefabGUID stormShield03 = new(1095865904);
    static readonly PrefabGUID stormShield02 = new(-1192885497);
    static readonly PrefabGUID stormShield01 = new(1044565673);
    static readonly PrefabGUID garlicDebuff = new(-1701323826);
    static readonly PrefabGUID silverDebuff = new(853298599);

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.ClassSpellSchoolOnHitEffects || !Classes) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out DealDamageEvent dealDamageEvent)) continue;
                else if (!dealDamageEvent.Target.Exists() || !dealDamageEvent.SpellSource.Exists()) continue;
                else if (dealDamageEvent.MainType != MainDamageType.Physical && dealDamageEvent.MainType != MainDamageType.Spell) continue; // skip if source isn't phys/spell
                else if (dealDamageEvent.SpellSource.TryGetComponent(out PrefabGUID sourcePrefabGUID) && (sourcePrefabGUID.Equals(silverDebuff) || sourcePrefabGUID.Equals(garlicDebuff))) continue; // skip if source is silver or garlic

                Entity player;
                if (dealDamageEvent.Target.Has<YieldResourcesOnDamageTaken>() && dealDamageEvent.SpellSource.GetOwner().TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();
                    LastDamageTime[steamId] = DateTime.UtcNow;
                }
                else if (dealDamageEvent.SpellSource.GetOwner().TryGetPlayer(out player) && !dealDamageEvent.Target.IsPlayer())
                {
                    Entity userEntity = player.Read<PlayerCharacter>().UserEntity;
                    ulong steamId = userEntity.Read<User>().PlatformId;
                    if (!ClassUtilities.HasClass(steamId)) continue;

                    PlayerClasses playerClass = ClassUtilities.GetPlayerClass(steamId);
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

                            if (playerClass.Equals(PlayerClasses.DemonHunter))
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
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
