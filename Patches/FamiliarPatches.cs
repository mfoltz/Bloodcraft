using Bloodcraft.Systems.Familiars;
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
                    BehaviourTreeState behaviourTreeState = behaviourTreeStateChangedEvent.Entity.Read<BehaviourTreeState>();
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {
                        behaviourTreeState.Value = GenericEnemyState.Follow;
                        behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;

                        entity.Write(behaviourTreeStateChangedEvent);
                        behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);
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

    [HarmonyPatch(typeof(FollowerSystem), nameof(FollowerSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ref FollowerSystem __instance)
    {
        Core.Log.LogInfo("FollowerSystem.OnUpdate");
        NativeArray<Entity> entities = __instance.__query_652683813_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
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
