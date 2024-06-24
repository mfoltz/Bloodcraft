using Bloodcraft.Systems.Familiars;
using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class FamiliarPatches
{
    static readonly PrefabGUID dominateBuff = new(-1447419822);

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
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Combat)) // simulate player target?
                    {
                        if (behaviourTreeStateChangedEvent.Entity.Has<AggroBuffer>())
                        {
                            var buffer = behaviourTreeStateChangedEvent.Entity.ReadBuffer<AggroBuffer>();
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                AggroBuffer item = buffer[i];
                                item.IsPlayer = true;
                                buffer[i] = item;
                                //Core.Log.LogInfo("Set IsPlayer to true...");
                                break;
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

                TeamReference teamReference = entity.Read<TeamReference>();
                NativeList<Entity> alliedUsers = new NativeList<Entity>(Allocator.Temp);
                try
                {
                    PrefabGUID famKey = entity.Read<PrefabGUID>();
                    TeamUtility.GetAlliedUsers(Core.EntityManager, teamReference, alliedUsers);
                    foreach (Entity userEntity in alliedUsers)
                    {
                        User user = userEntity.Read<User>();
                        ulong steamID = user.PlatformId;
                        if (Core.DataStructures.PlayerBools.TryGetValue(steamID, out var bools) && bools["Binding"] && Core.DataStructures.FamiliarActives.TryGetValue(steamID, out var data) && data.Item2.Equals(famKey.GuidHash))
                        {
                            FamiliarSummonSystem.HandleFamiliar(user.LocalCharacter._Entity, entity);
                            bools["Binding"] = false;
                            Core.DataStructures.SavePlayerBools();
                            LocalizationService.HandleServerReply(Core.EntityManager, user, $"Familiar bound: <color=green>{famKey.GetPrefabName()}</color>");
                        }
                    }
                }
                finally
                {
                    alliedUsers.Dispose();
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
                        Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(entityOwner.Owner);
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
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
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
}
