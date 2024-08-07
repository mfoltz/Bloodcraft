﻿using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class FamiliarPatches
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static EntityManager EntityManager => Core.EntityManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;

    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    static readonly PrefabGUID abilityGroupSlot = new(-633717863);
    static readonly PrefabGUID dominateBuff = new(-1447419822);
    static readonly PrefabGUID pvpProtBuff = new(1111481396);
    static readonly PrefabGUID manticore = new(-393555055);
    static readonly PrefabGUID dracula = new(-327335305);
    static readonly PrefabGUID monster = new(1233988687);
    static readonly PrefabGUID solarus = new(-740796338);
    static readonly PrefabGUID manticoreVisual = new(1670636401);
    static readonly PrefabGUID draculaVisual = new(-1923843097);
    static readonly PrefabGUID monsterVisual = new(-2067402784);
    static readonly PrefabGUID solarusVisual = new(178225731);
    static readonly PrefabGUID holyBeamHard = new(583436571);
    static readonly PrefabGUID holySpinners = new(-1852467646);
    static readonly PrefabGUID divineAngel = new(-1737346940);
    static readonly PrefabGUID fallenAngel = new(-76116724);

    public static readonly List<PrefabGUID> shardBearers = [manticore, dracula, monster, solarus];
    static readonly bool EliteShardBearers = Plugin.EliteShardBearers.Value;
    static GameModeType GameMode => Core.ServerGameSettings.GameModeType;

    public static Dictionary<Entity, HashSet<Entity>> FamiliarMinions = [];

    [HarmonyPatch(typeof(CreateGameplayEventOnBehaviourStateChangedSystem), nameof(CreateGameplayEventOnBehaviourStateChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnBehaviourStateChangedSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_221632411_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                BehaviourTreeStateChangedEvent behaviourTreeStateChangedEvent = entity.Read<BehaviourTreeStateChangedEvent>();
                if (behaviourTreeStateChangedEvent.Entity.Has<Follower>() && behaviourTreeStateChangedEvent.Entity.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    //Core.Log.LogInfo($"{behaviourTreeStateChangedEvent.PreviousState.ToString()}|{behaviourTreeStateChangedEvent.NewState.ToString()}");
                    BehaviourTreeState behaviourTreeState = behaviourTreeStateChangedEvent.Entity.Read<BehaviourTreeState>();
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {
                        //Core.Log.LogInfo($"{behaviourTreeStateChangedEvent.Entity.Read<PrefabGUID>().LookupName()}: {behaviourTreeState.Value}");
                        Entity familiar = behaviourTreeStateChangedEvent.Entity;
                        behaviourTreeState.Value = GenericEnemyState.Follow;
                        behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;
                        entity.Write(behaviourTreeStateChangedEvent);
                        behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);
                        if (FamiliarMinions.ContainsKey(familiar))
                        {
                            Core.FamiliarService.HandleFamiliarMinions(familiar);
                        }
                    }
                    /*
                    else if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Combat))
                    {
                        Entity character = behaviourTreeStateChangedEvent.Entity.Read<Follower>().Followed.Value;
                        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);
                        if (EntityManager.Exists(familiar))
                        {
                            FamiliarService.StartFamiliarHandler(familiar);
                        }
                    }
                    */
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(SpawnTransformSystem_OnSpawn), nameof(SpawnTransformSystem_OnSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SpawnTransformSystem_OnSpawn __instance)
    {
        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                //Core.Log.LogInfo(entity.Read<PrefabGUID>().LookupName());
                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                TeamReference teamReference = entity.Read<TeamReference>();
                bool summon = false;
                NativeList<Entity> alliedUsers = new(Allocator.Temp);
                try
                {
                    TeamUtility.GetAlliedUsers(EntityManager, teamReference, alliedUsers);
                    foreach (Entity userEntity in alliedUsers)
                    {
                        User user = userEntity.Read<User>();
                        ulong steamID = user.PlatformId;
                        if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["Binding"] && Core.DataStructures.FamiliarActives.TryGetValue(steamID, out var data) && data.FamKey.Equals(prefabGUID.GuidHash))
                        {
                            FamiliarSummonUtilities.HandleFamiliar(user.LocalCharacter._Entity, entity);
                            bools["Binding"] = false;
                            Core.DataStructures.SavePlayerBools();
                            LocalizationService.HandleServerReply(Core.EntityManager, user, $"Familiar bound: <color=green>{prefabGUID.GetPrefabName()}</color>");
                            summon = true;
                            break;
                        }
                    }
                }
                finally
                {
                    alliedUsers.Dispose();
                }

                if (summon) continue;

                if (EliteShardBearers && shardBearers.Contains(prefabGUID))
                {
                    if (prefabGUID.Equals(manticore))
                    {
                        HandleManticore(entity);
                    }
                    else if (prefabGUID.Equals(dracula))
                    {
                        HandleDracula(entity);
                    }
                    else if (prefabGUID.Equals(monster))
                    {
                        HandleMonster(entity);
                    }
                    else if (prefabGUID.Equals(solarus))
                    {
                        HandleSolarus(entity);
                    }
                }
                if (EliteShardBearers && prefabGUID.Equals(divineAngel))
                {
                    HandleAngel(entity);
                }
                if (EliteShardBearers && prefabGUID.Equals(fallenAngel))
                {
                    HandleFallenAngel(entity);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }   
    
    [HarmonyPatch(typeof(InteractValidateAndStopSystemServer), nameof(InteractValidateAndStopSystemServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(InteractValidateAndStopSystemServer __instance)
    {
        NativeArray<Entity> entities = __instance.__query_195794971_3.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                if (entity.TryGetComponent(out EntityOwner entityOwner))
                {
                    if (entityOwner.Owner.Has<PlayerCharacter>() && entity.Read<PrefabGUID>().GuidHash.Equals(-986064531) || entity.Read<PrefabGUID>().GuidHash.Equals(985937733)) // player using waygate
                    {
                        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(entityOwner.Owner);
                        Entity userEntity = entityOwner.Owner.Read<PlayerCharacter>().UserEntity;
                        ulong steamID = userEntity.Read<User>().PlatformId;

                        if (ServerGameManager.TryGetBuff(entityOwner.Owner, dominateBuff.ToIdentifier(), out Entity _))
                        {
                            continue;
                        }

                        if (EntityManager.Exists(familiar) && !familiar.Has<Disabled>()) 
                        {
                            EmoteSystemPatch.CallDismiss(userEntity, entityOwner.Owner, steamID); // auto dismiss familiar 
                        }
                        else if (EntityManager.Exists(familiar) && familiar.Has<Disabled>())
                        {
                            EmoteSystemPatch.CallDismiss(userEntity, entityOwner.Owner, steamID); // auto dismiss familiar 
                        }
                    }
                }         
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
    
    [HarmonyPatch(typeof(LinkMinionToOwnerOnSpawnSystem), nameof(LinkMinionToOwnerOnSpawnSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(LinkMinionToOwnerOnSpawnSystem __instance) // get EntityOwner (familiar), apply ModifyTeamBuff
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                if (entity.TryGetComponent(out EntityOwner entityOwner) && (entityOwner.Owner.TryGetComponent(out Follower follower) && follower.Followed._Value.Has<PlayerCharacter>()))
                {
                    //Core.Log.LogInfo(entity.Read<PrefabGUID>().LookupName());
                    Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                    if (familiar != Entity.Null)
                    {
                        
                        if (!FamiliarMinions.ContainsKey(familiar))
                        {
                            FamiliarMinions.Add(familiar, [entity]);
                            //Core.Log.LogInfo("Added new minion entry...");
                        }
                        else
                        {
                            FamiliarMinions[familiar].Add(entity);
                            //Core.Log.LogInfo("Added minion to existing entry...");
                        }

                        if (entity.Read<PrefabGUID>().LookupName().ToLower().Contains("vblood")) continue;

                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = new(1273155981),
                        };

                        FromCharacter fromCharacter = new()
                        {
                            Character = entity,
                            User = familiar
                        };

                        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        if (ServerGameManager.TryGetBuff(entity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
                        {
                            if (buff.Has<LifeTime>())
                            {
                                buff.Write(new LifeTime { Duration = 30, EndAction = LifeTimeEndAction.Destroy });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(ImprisonedBuffSystem), nameof(ImprisonedBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ImprisonedBuffSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1231815368_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (entity.TryGetComponent(out Buff buff) && !buff.Target.Has<CharmSource>()) // if no charm source, destroy
                {
                    DestroyUtility.CreateDestroyEvent(EntityManager, buff.Target, DestroyReason.Default, DestroyDebugReason.None);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void HandleManticore(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 5f;
        aiMoveSpeeds.Run._Value = 6.5f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, manticoreVisual);
    }
    static void HandleMonster(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 2.5f;
        aiMoveSpeeds.Run._Value = 5.5f;
        entity.Write(aiMoveSpeeds);
        
        HandleVisual(entity, monsterVisual);
    }
    static void HandleSolarus(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 4f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, solarusVisual);
    }
    static void HandleAngel(Entity entity)
    {
        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);

        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 5f;
        aiMoveSpeeds.Run._Value = 7.5f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, solarusVisual);
    }
    static void HandleFallenAngel(Entity entity)
    {
        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 2;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);
    }
    static void HandleDracula(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();
        if (entity.Has<MaxMinionsPerPlayerElement>()) entity.Remove<MaxMinionsPerPlayerElement>();

        Health health = entity.Read<Health>();
        health.MaxHealth._Value *= 5;
        health.Value = health.MaxHealth._Value;
        entity.Write(health);
        
        UnitStats unitStats = entity.Read<UnitStats>();
        unitStats.PhysicalPower._Value *= 1.5f;
        unitStats.SpellPower._Value *= 1.5f;
        entity.Write(unitStats);

        AbilityBar_Shared abilityBarShared = entity.Read<AbilityBar_Shared>();
        abilityBarShared.AttackSpeed._Value = 2f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 2.5f;
        aiMoveSpeeds.Run._Value = 3.5f;
        aiMoveSpeeds.Circle._Value = 3.5f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, draculaVisual);
    }
    public static void HandleVisual(Entity entity, PrefabGUID visual)
    {
        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = visual,
        };

        FromCharacter fromCharacter = new()
        {
            Character = entity,
            User = entity
        };

        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (ServerGameManager.TryGetBuff(entity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
        {
            if (buff.Has<Buff>())
            {
                BuffCategory component = buff.Read<BuffCategory>();
                component.Groups = BuffCategoryFlag.None;
                buff.Write(component);
            }
            if (buff.Has<CreateGameplayEventsOnSpawn>())
            {
                buff.Remove<CreateGameplayEventsOnSpawn>();
            }
            if (buff.Has<GameplayEventListeners>())
            {
                buff.Remove<GameplayEventListeners>();
            }
            if (buff.Has<LifeTime>())
            {
                LifeTime lifetime = buff.Read<LifeTime>();
                lifetime.Duration = -1;
                lifetime.EndAction = LifeTimeEndAction.None;
                buff.Write(lifetime);
            }
            if (buff.Has<RemoveBuffOnGameplayEvent>())
            {
                buff.Remove<RemoveBuffOnGameplayEvent>();
            }
            if (buff.Has<RemoveBuffOnGameplayEventEntry>())
            {
                buff.Remove<RemoveBuffOnGameplayEventEntry>();
            }
            if (buff.Has<DealDamageOnGameplayEvent>())
            {
                buff.Remove<DealDamageOnGameplayEvent>();
            }
            if (buff.Has<HealOnGameplayEvent>())
            {
                buff.Remove<HealOnGameplayEvent>();
            }
            if (buff.Has<BloodBuffScript_ChanceToResetCooldown>())
            {
                buff.Remove<BloodBuffScript_ChanceToResetCooldown>();
            }
            if (buff.Has<ModifyMovementSpeedBuff>())
            {
                buff.Remove<ModifyMovementSpeedBuff>();
            }
            if (buff.Has<ApplyBuffOnGameplayEvent>())
            {
                buff.Remove<ApplyBuffOnGameplayEvent>();
            }
            if (buff.Has<DestroyOnGameplayEvent>())
            {
                buff.Remove<DestroyOnGameplayEvent>();
            }
            if (buff.Has<WeakenBuff>())
            {
                buff.Remove<WeakenBuff>();
            }
            if (buff.Has<ReplaceAbilityOnSlotBuff>())
            {
                buff.Remove<ReplaceAbilityOnSlotBuff>();
            }
            if (buff.Has<AmplifyBuff>())
            {
                buff.Remove<AmplifyBuff>();
            }
        }
    }
}
