using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class FamiliarPatches
{
    static readonly PrefabGUID dominateBuff = new(-1447419822);
    static readonly PrefabGUID pvpProtBuff = new(1111481396);
    static readonly PrefabGUID manticore = new(-393555055);
    static readonly PrefabGUID dracula = new(-327335305);
    static readonly PrefabGUID monster = new(1233988687);
    static readonly PrefabGUID solarus = new(-740796338);
    static readonly PrefabGUID manticoreVisual = new(1670636401);
    static readonly PrefabGUID draculaVisual = new(-1923843097);
    static readonly PrefabGUID monsterVisual = new(-2067402784);
    static readonly PrefabGUID solarusVisual = new(-1466712470);
    static readonly List<PrefabGUID> shardBearers = [manticore, dracula, monster, solarus];
    static readonly bool EliteShardBearers = Plugin.EliteShardBearers.Value;
    static GameModeType GameMode => Core.ServerGameSettings.GameModeType;

    public static Dictionary<Entity, HashSet<Entity>> familiarMinions = [];

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
                    //Core.Log.LogInfo(behaviourTreeStateChangedEvent.Entity.Read<PrefabGUID>().GetPrefabName());
                    //Core.Log.LogInfo($"{behaviourTreeStateChangedEvent.PreviousState.ToString()}|{behaviourTreeStateChangedEvent.NewState.ToString()}");
                    BehaviourTreeState behaviourTreeState = behaviourTreeStateChangedEvent.Entity.Read<BehaviourTreeState>();
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {
                        Entity familiar = behaviourTreeStateChangedEvent.Entity;

                        behaviourTreeState.Value = GenericEnemyState.Follow;
                        behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;
                        
                        entity.Write(behaviourTreeStateChangedEvent);
                        behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);
                        if (familiarMinions.ContainsKey(familiar))
                        {
                            Core.FamiliarService.HandleFamiliarMinions(familiar);
                        }
                    }
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Combat))
                    {
                        if (behaviourTreeStateChangedEvent.Entity.Has<AggroConsumer>())
                        {
                            AggroConsumer aggroConsumer = behaviourTreeStateChangedEvent.Entity.Read<AggroConsumer>();
                            if ((GameMode.Equals(GameModeType.PvE) || Core.ServerGameManager.HasBuff(aggroConsumer.AggroTarget._Entity, pvpProtBuff.ToIdentifier())))
                            {
                                Follower follower = behaviourTreeStateChangedEvent.Entity.Read<Follower>();
                                follower.ModeModifiable._Value = 0;
                                behaviourTreeStateChangedEvent.Entity.Write(follower);
                            }
                            /*
                            else if ((GameMode.Equals(GameModeType.PvE) || Core.ServerGameManager.HasBuff(aggroConsumer.AggroTarget._Entity, pvpProtBuff.ToIdentifier()) && aggroConsumer.AggroTarget._Entity.Has<Follower>()))
                            {
                                Follower following = aggroConsumer.AggroTarget._Entity.Read<Follower>();
                                if (following.Followed._Value != Entity.Null && following.Followed._Value.Has<PlayerCharacter>()) // if player familiar and pvp protected, don't target
                                {
                                    Follower follower = behaviourTreeStateChangedEvent.Entity.Read<Follower>();
                                    follower.ModeModifiable._Value = 0;
                                    behaviourTreeStateChangedEvent.Entity.Write(following);
                                }
                            }
                            */
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
                    TeamUtility.GetAlliedUsers(Core.EntityManager, teamReference, alliedUsers);
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
                        ProcessManticore(entity);
                    }
                    else if (prefabGUID.Equals(dracula))
                    {
                        ProcessDracula(entity);
                    }
                    else if (prefabGUID.Equals(monster))
                    {
                        ProcessMonster(entity);
                    }
                    else if (prefabGUID.Equals(solarus))
                    {
                        ProcessSolarus(entity);
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

                        if (Core.ServerGameManager.TryGetBuff(entityOwner.Owner, dominateBuff.ToIdentifier(), out Entity _))
                        {
                            continue;
                        }

                        if (Core.EntityManager.Exists(familiar) && !familiar.Has<Disabled>()) 
                        {
                            EmoteSystemPatch.CallDismiss(userEntity, entityOwner.Owner, steamID); // auto dismiss familiar 
                        }
                        else if (Core.EntityManager.Exists(familiar) && familiar.Has<Disabled>())
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
                        
                        if (!familiarMinions.ContainsKey(familiar))
                        {
                            familiarMinions.Add(familiar, [entity]);
                            //Core.Log.LogInfo("Added new minion entry...");
                        }
                        else
                        {
                            familiarMinions[familiar].Add(entity);
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

                        Core.DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        if (Core.ServerGameManager.TryGetBuff(entity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
                        {
                            if (buff.Has<LifeTime>())
                            {
                                buff.Write(new LifeTime { Duration = 30, EndAction = LifeTimeEndAction.Destroy });
                                //Core.Log.LogInfo("Set lifetime to 30 seconds...");
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
                    //Core.Log.LogInfo("Destroying imprisoned familiar...");
                    DestroyUtility.CreateDestroyEvent(Core.EntityManager, buff.Target, DestroyReason.Default, DestroyDebugReason.None);
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
    static void ProcessManticore(Entity entity)
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
        abilityBarShared.AttackSpeed._Value = 1.5f;
        abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 5f;
        aiMoveSpeeds.Run._Value = 6.5f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, manticoreVisual);
        //Core.Log.LogInfo("Manticore processed...");
    }
    static void ProcessMonster(Entity entity)
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
        abilityBarShared.AttackSpeed._Value = 1.25f;
        abilityBarShared.PrimaryAttackSpeed._Value = 1.5f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 2.5f;
        aiMoveSpeeds.Run._Value = 5.5f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, monsterVisual);
        //Core.Log.LogInfo("Manticore processed...");
    }
    static void ProcessSolarus(Entity entity)
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
        abilityBarShared.AttackSpeed._Value = 1.25f;
        abilityBarShared.PrimaryAttackSpeed._Value = 1.5f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 3f;
        aiMoveSpeeds.Run._Value = 4f;
        aiMoveSpeeds.Circle._Value = 3f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, solarusVisual);
        //Core.Log.LogInfo("Manticore processed...");
    }
    static void ProcessDracula(Entity entity)
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
        abilityBarShared.AttackSpeed._Value = 1.25f;
        abilityBarShared.PrimaryAttackSpeed._Value = 1.5f;
        entity.Write(abilityBarShared);

        AiMoveSpeeds aiMoveSpeeds = entity.Read<AiMoveSpeeds>();
        aiMoveSpeeds.Walk._Value = 2.5f;
        aiMoveSpeeds.Run._Value = 3.5f;
        aiMoveSpeeds.Circle._Value = 3.5f;
        entity.Write(aiMoveSpeeds);

        HandleVisual(entity, draculaVisual);
        //Core.Log.LogInfo("Manticore processed...");
    }

    static void HandleVisual(Entity entity, PrefabGUID visual)
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

        Core.DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (Core.ServerGameManager.TryGetBuff(entity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
        {
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
        }
    }
}
