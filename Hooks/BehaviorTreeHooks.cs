using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VCreate.Core;
using VCreate.Core.Toolbox;

[HarmonyPatch(typeof(RepairDoubleVBloodSpawnedSystem), nameof(RepairDoubleVBloodSpawnedSystem.OnUpdate))]
public static class RepairDoubleVBloodSpawnedSystemPatch
{
    public static bool Prefix(RepairDoubleVBloodSpawnedSystem __instance)
    {
        Plugin.Log.LogInfo("RepairDoubleVBloodSpawnedSystem Prefix called...");
        //NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        //foreach (var entity in entities)
        //{
            //entity.LogComponentTypes();
        //}
        return false;
    }
}

/*
[HarmonyPatch(typeof(TeleportBuffSystem_Server), nameof(TeleportBuffSystem_Server.OnUpdate))]
public static class TeleportBuffSystem_ServerPatch
{
    public static void Postfix(TeleportBuffSystem_Server __instance)
    {
        Plugin.Log.LogInfo("TeleportBuffSystem_Server Postfix called..."); // so for the duration of the teleport the entity has the teleportbuff
        NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                //entity.LogComponentTypes();
                Entity target = entity.Read<Buff>().Target;
                target.LogComponentTypes();
                bool check = Utilities.HasComponent<PlayerCharacter>(target);
                if (check)
                {
                    if (DataStructures.PlayerSettings.TryGetValue(target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId, out var settings) && !settings.NotNew)
                    {
                        settings.NotNew = true;
                        DataStructures.PlayerSettings[entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId] = settings;
                        DataStructures.SavePlayerSettings();
                        Vision vision = entity.Read<Vision>();
                        vision.Range._Value = 1000f;
                        entity.Write(vision);
                        Helper.UnlockWaypoints(entity.Read<PlayerCharacter>().UserEntity);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogInfo($"Exited TeleportToWaypointEventSystem hook early {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
}
*/

[HarmonyPatch(typeof(BehaviourTreeStateChangedEventSystem), nameof(BehaviourTreeStateChangedEventSystem.OnUpdate))]
public static class BehaviourTreeStateChangedEventSystemPatch
{
    public static void Prefix(BehaviourTreeStateChangedEventSystem __instance)
    {
        //ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
        NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        EntityManager entityManager = VWorld.Server.EntityManager;
        
        try
        {
            foreach (var entity in entities)
            {
                // entity.LogComponentTypes();
                if (!entity.Has<Follower>()) continue;
                if (!entity.Read<Follower>().Followed._Value.Has<PlayerCharacter>() || !entity.Has<BehaviourTreeState>()) continue;
                ulong platformId = entity.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                if (Utilities.HasComponent<BehaviourTreeState>(entity) && entity.Read<BehaviourTreeState>().Value == GenericEnemyState.Return)
                {
                    BehaviourTreeState behaviourTreeStateChangedEvent = entity.Read<BehaviourTreeState>();
                    behaviourTreeStateChangedEvent.Value = GenericEnemyState.Follow;
                    entity.Write(behaviourTreeStateChangedEvent);
                }
                else if (Utilities.HasComponent<BehaviourTreeState>(entity) && entity.Read<BehaviourTreeState>().Value == GenericEnemyState.Combat)
                {
                    if (!entity.Has<AggroBuffer>() || entity.ReadBuffer<AggroBuffer>().IsEmpty) continue;
                    var aggroBuffer = entityManager.GetBuffer<AggroBuffer>(entity);
                    AggroConsumer aggroConsumer = entity.Read<AggroConsumer>();
                    Aggroable aggroable = entity.Read<Aggroable>();

                    for (int i = 0; i < aggroBuffer.Length; i++)
                    {
                        Entity player = aggroBuffer[i].Entity;
                        if (aggroBuffer[i].IsPlayer && Utilities.HasComponent<Mounter>(player) && !player.Read<Mounter>().MountEntity.Equals(Entity.Null))
                        {
                            bool check = BuffUtility.TryGetBuff(player, VCreate.Data.Buffs.AB_Militia_HoundMaster_QuickShot_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
                            if (!check)
                            {
                                aggroConsumer.Active = ModifiableBool.CreateFixed(false);
                                entity.Write(aggroConsumer);
                                aggroable.Value = ModifiableBool.CreateFixed(false);
                                aggroable.AggroFactor = ModifiableFloat.CreateFixed(0f);
                                aggroable.DistanceFactor = ModifiableFloat.CreateFixed(0f);

                                entity.Write(aggroable);
                                //Plugin.Log.LogInfo("Normal mounter, ignore...");
                            }
                            else
                            {
                                //Plugin.Log.LogInfo("Event mounter, allowing...");
                                DamageCategoryStats damageCategoryStats = entity.Read<DamageCategoryStats>();
                                damageCategoryStats.DamageVsPlayerVampires = ModifiableFloat.CreateFixed(0.25f);
                                entity.Write(damageCategoryStats);
                                continue;
                                
                            }

                        }
                        else if (aggroBuffer[i].IsPlayer && BuffUtility.TryGetBuff(player, VCreate.Data.Buffs.Buff_General_PvPProtected, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _))
                        {
                            aggroConsumer.Active = ModifiableBool.CreateFixed(false);
                            entity.Write(aggroConsumer);
                            aggroable.Value = ModifiableBool.CreateFixed(false);
                            aggroable.AggroFactor = ModifiableFloat.CreateFixed(0f);
                            aggroable.DistanceFactor = ModifiableFloat.CreateFixed(0f);
                            entity.Write(aggroable);
                            //Plugin.Log.LogInfo("PvP protected player, don't target...");
                        }
                        
                    }
                }
                else if (Utilities.HasComponent<BehaviourTreeState>(entity) && (entity.Read<BehaviourTreeState>().Value == GenericEnemyState.Follow))
                {
                    var targetPosition = entity.Read<Follower>().Followed._Value.Read<Translation>().Value;
                    var currentPosition = entity.Read<Translation>().Value;
                    var distanceMagnitude = math.length(currentPosition - targetPosition);

                    if (distanceMagnitude < 2f)
                    {
                        BehaviourTreeState behaviourTreeStateChangedEvent = entity.Read<BehaviourTreeState>();
                        behaviourTreeStateChangedEvent.Value = GenericEnemyState.Idle;
                        entity.Write(behaviourTreeStateChangedEvent);
                        AggroConsumer aggroConsumer = entity.Read<AggroConsumer>();
                        Aggroable aggroable = entity.Read<Aggroable>();

                        if (!aggroConsumer.Active)
                        {
                            aggroConsumer.Active = ModifiableBool.CreateFixed(true);
                            entity.Write(aggroConsumer);
                            aggroable.Value = ModifiableBool.CreateFixed(true);
                            aggroable.AggroFactor = ModifiableFloat.CreateFixed(1f);
                            aggroable.DistanceFactor = ModifiableFloat.CreateFixed(1f);
                            entity.Write(aggroable);
                            //Plugin.Log.LogInfo("Resetting aggro for familiar...");
                            DamageCategoryStats damageCategoryStats = entity.Read<DamageCategoryStats>();
                            damageCategoryStats.DamageVsPlayerVampires = ModifiableFloat.CreateFixed(0f);
                            entity.Write(damageCategoryStats);
                        }

                        
                        
                    }
                }
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogInfo($"Exited BehaviorTreeState hook early {e}");
        }
        finally
        {
            // Dispose of the NativeArray properly in the finally block to ensure it's always executed.
            entities.Dispose();
        }
    }
}

/*
[HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
public static class EquipItemSystemPatch
{
    public static void Prefix(EquipItemSystem __instance)
    {
        Plugin.Log.LogInfo("EquipItemSystem Prefix called...");
        NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                entity.LogComponentTypes();
            }
        }
        catch (Exception e)
        {
            Plugin.Log.LogInfo($"Exited EquipItemSystem hook early {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
}
*/