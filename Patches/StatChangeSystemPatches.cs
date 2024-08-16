using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Familiars;
using Bloodcraft.SystemUtilities.Legacies;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.SystemUtilities.Experience.PlayerLevelingUtilities;
using Random = System.Random;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class StatChangeSystemPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ConfigService ConfigService => Core.ConfigService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem.Settings.GameModeType;

    static readonly Random Random = new();

    static readonly PrefabGUID pvpProtBuff = new(1111481396);
    static readonly PrefabGUID stormShield03 = new(1095865904);
    static readonly PrefabGUID stormShield02 = new(-1192885497);
    static readonly PrefabGUID stormShield01 = new(1044565673);
    static readonly PrefabGUID garlicDebuff = new(-1701323826);
    static readonly PrefabGUID silverDebuff = new(853298599);

    [HarmonyPatch(typeof(StatChangeMutationSystem), nameof(StatChangeMutationSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StatChangeMutationSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StatChangeEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                if (ConfigService.BloodSystem && ConfigService.BloodQualityBonus && entity.Has<StatChangeEvent>() && entity.Has<BloodQualityChange>())
                {
                    if (!EntityManager.Exists(entity)) continue;
                    StatChangeEvent statChangeEvent = entity.Read<StatChangeEvent>();
                    BloodQualityChange bloodQualityChange = entity.Read<BloodQualityChange>();
                    if (!statChangeEvent.Entity.Has<Blood>()) continue;
                    Blood blood = statChangeEvent.Entity.Read<Blood>();
                    LegacyUtilities.BloodType bloodType = LegacyUtilities.GetBloodTypeFromPrefab(bloodQualityChange.BloodType);
                    ulong steamID = statChangeEvent.Entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                    var bloodQualityBuff = statChangeEvent.Entity.ReadBuffer<BloodQualityBuff>();

                    if (bloodHandler == null)
                    {
                        continue;
                    }

                    float legacyKey = bloodHandler.GetLegacyData(steamID).Value;

                    if (ConfigService.PrestigeSystem && Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var prestiges) && prestiges.TryGetValue(LegacyUtilities.BloodPrestigeMap[bloodType], out var bloodPrestige) && bloodPrestige > 0)
                    {
                        legacyKey = (float)bloodPrestige * ConfigService.PrestigeBloodQuality;
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
                    }
                    else if (!ConfigService.PrestigeSystem)
                    {
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
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

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Exists() || !entity.Has<DealDamageEvent>()) continue;

                DealDamageEvent dealDamageEvent = entity.Read<DealDamageEvent>();

                if (dealDamageEvent.MainType != MainDamageType.Physical && dealDamageEvent.MainType != MainDamageType.Spell && dealDamageEvent.MainType != MainDamageType.Holy) continue;

                if (!dealDamageEvent.Target.Exists() || !dealDamageEvent.SpellSource.Exists()) continue; // this caused plants to disappear after harvesting...? or something broke hard after that error

                //if (!dealDamageEvent.SpellSource.Has<PrefabGUID>()) continue; // hopefully this is safer, even though that makes zero sense
                PrefabGUID sourcePrefab = dealDamageEvent.SpellSource.Read<PrefabGUID>();
                if (sourcePrefab.Equals(silverDebuff) || sourcePrefab.Equals(garlicDebuff)) continue;

                //Core.Log.LogInfo($"DealDamageEvent: {dealDamageEvent.MainType} | Source: {dealDamageEvent.SpellSource.Read<PrefabGUID>().LookupName()} | Target: {dealDamageEvent.Target.Read<PrefabGUID>().LookupName()}");

                if (dealDamageEvent.Target.TryGetComponent(out PlayerCharacter target))
                {
                    if (!dealDamageEvent.MainType.Equals(MainDamageType.Holy) && ConfigService.Parties && ConfigService.PreventFriendlyFire && !GameMode.Equals(GameModeType.PvE) && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
                    {
                        Dictionary<ulong, HashSet<string>> playerAlliances = new(Core.DataStructures.PlayerParties);
                        string targetName = target.Name.Value;
                        string sourceName = source.Name.Value;
                        ulong steamId = source.UserEntity.Read<User>().PlatformId;

                        if (playerAlliances.Values.Any(set => set.Contains(targetName) && set.Contains(sourceName)))
                        {
                            EntityManager.DestroyEntity(entity);
                        }
                    }
                    else if (ConfigService.FamiliarSystem && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner familiarEntityOwner) && familiarEntityOwner.Owner.TryGetComponent(out Follower follower) && follower.Followed._Value.Has<PlayerCharacter>())
                    {
                        if (dealDamageEvent.Target.Has<Follower>() && dealDamageEvent.Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                        {
                            EntityManager.DestroyEntity(entity);
                            continue;
                        }

                        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                        if (GameMode.Equals(GameModeType.PvE)) // always stop in PvE
                        {
                            if (familiar != Entity.Null)
                            {
                                EntityManager.DestroyEntity(entity);
                            }
                        }
                        else if (ServerGameManager.HasBuff(dealDamageEvent.Target, pvpProtBuff.ToIdentifier())) // account for KindredArenas~
                        {
                            if (familiar != Entity.Null)
                            {
                                EntityManager.DestroyEntity(entity);
                            }
                        }
                        else if (ConfigService.Parties && ConfigService.PreventFriendlyFire) // check for parties in PvP 
                        {
                            Dictionary<ulong, HashSet<string>> playerParties = new(Core.DataStructures.PlayerParties);
                            string targetName = target.Name.Value;
                            string ownerName = follower.Followed._Value.Read<PlayerCharacter>().Name.Value;

                            if (familiar != Entity.Null && playerParties.Values.Any(set => set.Contains(targetName) && set.Contains(ownerName)))
                            {
                                EntityManager.DestroyEntity(entity);
                            }
                        }
                    }
                }
                else if (!dealDamageEvent.MainType.Equals(MainDamageType.Holy) && dealDamageEvent.SpellSource.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetComponent(out PlayerCharacter source))
                {
                    Entity userEntity = source.UserEntity;
                    if (ConfigService.FamiliarSystem)
                    {
                        if (dealDamageEvent.Target.Has<Follower>() && dealDamageEvent.Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // protect familiars from player damage
                        {
                            EntityManager.DestroyEntity(entity);
                            continue;
                        }
                        else if (dealDamageEvent.Target.Has<EntityOwner>() && dealDamageEvent.Target.Read<EntityOwner>().Owner.Has<Follower>()) // protect familiar summons from player damage
                        {
                            Follower follower = dealDamageEvent.Target.Read<EntityOwner>().Owner.Read<Follower>();
                            if (follower.Followed._Value.Has<PlayerCharacter>())
                            {
                                EntityManager.DestroyEntity(entity);
                                continue;
                            }
                        }
                        else if (dealDamageEvent.Target.Has<Minion>() && dealDamageEvent.Target.Has<EntityOwner>() && dealDamageEvent.Target.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) // protect minion player summons from player damage
                        {
                            EntityManager.DestroyEntity(entity);
                            continue;
                        }
                    }

                    if (!ConfigService.ClassSpellSchoolOnHitEffects || !ConfigService.Classes) continue;
                    
                    ulong steamId = userEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.PlayerClass.TryGetValue(steamId, out var classData) && classData.Keys.Count == 0) continue;

                    PlayerClasses playerClass = GetPlayerClass(steamId);
                    if (Random.NextDouble() <= ConfigService.OnHitProcChance)
                    {
                        PrefabGUID prefabGUID = ClassOnHitDebuffMap[playerClass];
                        FromCharacter fromCharacter = new()
                        {
                            Character = dealDamageEvent.Target,
                            User = source.UserEntity
                        };

                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = prefabGUID,
                        };

                        if (ServerGameManager.HasBuff(dealDamageEvent.Target, prefabGUID.ToIdentifier()))
                        {
                            applyBuffDebugEvent.BuffPrefabGUID = ClassOnHitEffectMap[playerClass];
                            fromCharacter.Character = entityOwner.Owner;
                            if (playerClass.Equals(PlayerClasses.DemonHunter))
                            {
                                if (ServerGameManager.TryGetBuff(entityOwner.Owner, stormShield01.ToIdentifier(), out Entity firstBuff))
                                {
                                    firstBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else if (ServerGameManager.TryGetBuff(entityOwner.Owner, stormShield02.ToIdentifier(), out Entity secondBuff))
                                {
                                    secondBuff.Write(new LifeTime { Duration = 5f, EndAction = LifeTimeEndAction.Destroy });
                                }
                                else if (ServerGameManager.TryGetBuff(entityOwner.Owner, stormShield03.ToIdentifier(), out Entity thirdBuff))
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
                                buff.Write(new EntityOwner { Owner = entityOwner.Owner });
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
