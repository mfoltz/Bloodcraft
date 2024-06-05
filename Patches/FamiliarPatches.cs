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
using Unity.Transforms;

namespace Bloodcraft.Patches;

[HarmonyPatch]
class FamiliarPatches
{
    [HarmonyPatch(typeof(CreateGameplayEventOnBehaviourStateChangedSystem), nameof(CreateGameplayEventOnBehaviourStateChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnBehaviourStateChangedSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_221632411_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
                BehaviourTreeStateChangedEvent behaviourTreeStateChangedEvent = entity.Read<BehaviourTreeStateChangedEvent>();
                if (behaviourTreeStateChangedEvent.Entity.Has<Follower>() && behaviourTreeStateChangedEvent.Entity.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    Core.Log.LogInfo(behaviourTreeStateChangedEvent.Entity.Read<PrefabGUID>().LookupName());
                    Core.Log.LogInfo($"{behaviourTreeStateChangedEvent.PreviousState.ToString()}|{behaviourTreeStateChangedEvent.NewState.ToString()}");
                    BehaviourTreeState behaviourTreeState = behaviourTreeStateChangedEvent.Entity.Read<BehaviourTreeState>();
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {

                        
                        behaviourTreeState.Value = GenericEnemyState.Follow;
                        behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;
                        
                        entity.Write(behaviourTreeStateChangedEvent);
                        behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);
                    }
                    /*
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Combat))
                    {
                        // check for player in alertbuffer?
                        Core.Log.LogInfo(behaviourTreeStateChangedEvent.Entity.Read<PrefabGUID>().LookupName());
                        Core.Log.LogInfo("Preventing fam attack on player...");

                        AggroConsumer aggroConsumer = behaviourTreeStateChangedEvent.Entity.Read<AggroConsumer>();
                        if (aggroConsumer.AggroTarget._Entity.Has<PlayerCharacter>())
                        {
                            behaviourTreeState.Value = GenericEnemyState.Follow;
                            behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;

                            entity.Write(behaviourTreeStateChangedEvent);
                            behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);
                        }

                        
                    }
                    */
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
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
        NativeArray<Entity> entities = __instance.__query_565030732_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
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
                            ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, user, $"Familiar bound: <color=green>{famKey.LookupName()}</color>");
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
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(PlayerCombatBuffSystem_OnAggro), nameof(PlayerCombatBuffSystem_OnAggro.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(PlayerCombatBuffSystem_OnAggro __instance)
    {
        NativeArray<Entity> entities = __instance.__query_928948733_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
                InverseAggroEvents.Added added = entity.Read<InverseAggroEvents.Added>();
                Entity consumer = added.Consumer;
                Entity target = added.Producer;
                if (target.Has<PlayerCharacter>())
                {
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(target);
                    if (familiar != Entity.Null)
                    {
                        //AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                        //aggroConsumer.AggroTarget._Entity = consumer;
                        //entity.Write(aggroConsumer);
                        //Core.Log.LogInfo($"Familiar set to attack.");
                        float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, target.Read<LocalToWorld>().Position);
                        if (distance > 25f)
                        {
                            familiar.Write(new Translation { Value = target.Read<LocalToWorld>().Position });
                            Core.Log.LogInfo($"Familiar returned to owner.");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(LinkMinionToOwnerOnSpawnSystem), nameof(LinkMinionToOwnerOnSpawnSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(LinkMinionToOwnerOnSpawnSystem __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
                Core.Log.LogInfo($"Linking minion to owner | {entity.Read<PrefabGUID>().LookupName()}");
                entity.LogComponentTypes();
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

}
