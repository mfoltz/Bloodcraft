using Bloodstone.API;
using Cobalt.Core;
using Cobalt.Systems;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using ProjectM.UI;
using System.Reflection.Metadata.Ecma335;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks;

public class CraftingPatch
{
    [HarmonyPatch(typeof(UpdateCraftingSystem), nameof(UpdateCraftingSystem.OnUpdate))]
    public static class UpdateCraftingSystemPatch
    {
        public static void Prefix(UpdateCraftingSystem __instance)
        {
            PrefabCollectionSystem prefabCollectionSystem = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>();
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null) || !entity.Has<CastleAreaRequirement>() || !entity.Has<QueuedWorkstationCraftAction>()) continue;
                    var listeners = entity.ReadBuffer<GameplayEventListeners>();
                    if (!listeners.IsEmpty || listeners.IsCreated)
                    {
                        foreach(var listener in listeners)
                        {
                            Plugin.Log.LogInfo(listener.GameplayEventType.ToString());
                        }
                    }

                    var actions = entity.ReadBuffer<QueuedWorkstationCraftAction>();
                    if (actions.IsEmpty || !actions.IsCreated) continue;
                    foreach (var action in actions)
                    {
                        ulong steamId = action.InitiateUser.Read<User>().PlatformId;
                        if (DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var jobs) && jobs.ContainsKey(action.RecipeGuid) && jobs[action.RecipeGuid])
                        {
                            Plugin.Log.LogInfo(action.ProgressTime);
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited UpdateCraftingSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
    [HarmonyPatch(typeof(StartCraftingSystem), nameof(StartCraftingSystem.OnUpdate))]
    public static class StartCraftingSystemPatch
    {
        public static void Prefix(StartCraftingSystem __instance)
        {
            NativeArray<Entity> entities = __instance.__StartCraftingJob_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (!entity.Has<StartCraftItemEvent>() || !entity.Has<FromCharacter>())
                    {
                        Plugin.Log.LogInfo("Entity does not have StartCraftItemEvent or FromCharacter...");
                        continue;
                    }
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    StartCraftItemEvent startCraftItemEvent = entity.Read<StartCraftItemEvent>();
                    PrefabGUID prefabGUID = startCraftItemEvent.RecipeId;
                    if (DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var jobs) && !jobs.ContainsKey(prefabGUID))
                    {
                        // if crafting job not already present, add to cache
                        jobs.Add(prefabGUID, true);
                    }
                    else
                    {
                        // if crafting job already present, set to active
                        jobs[prefabGUID] = true;

                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited UpdateCraftingSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
    [HarmonyPatch(typeof(StopCraftingSystem), nameof(StopCraftingSystem.OnUpdate))]
    public static class StopCraftingSystemPatch
    {
        public static void Prefix(StopCraftingSystem __instance)
        {
            NativeArray<Entity> entities = __instance.__StopCraftingJob_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (!entity.Has<StopCraftItemEvent>() || !entity.Has<FromCharacter>())
                    {
                        Plugin.Log.LogInfo("Entity does not have StopCraftItemEvent or FromCharacter...");
                        continue;
                    }
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    StopCraftItemEvent stopCraftItemEvent = entity.Read<StopCraftItemEvent>();
                    PrefabGUID prefabGUID = stopCraftItemEvent.RecipeGuid;
                    if (DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var jobs) && jobs.ContainsKey(prefabGUID))
                    {
                        // if crafting job is active, set to inactive
                        jobs[prefabGUID] = false;
                    }
                    

                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited StopCraftingSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}