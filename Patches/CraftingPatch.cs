using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Professions.ProfessionUtilities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

public class CraftingPatch
{
    [HarmonyPatch(typeof(StartCraftingSystem), nameof(StartCraftingSystem.OnUpdate))]
    public static class StartCraftingSystemPatch
    {
        public static void Prefix(StartCraftingSystem __instance)
        {
            //Core.Log.LogInfo("StartCraftingSystemPrefix called...");
            NativeArray<Entity> entities = __instance._StartCraftItemEventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (Plugin.ProfessionSystem.Value)
                    {
                        if (entity.Has<StartCraftItemEvent>() && entity.Has<FromCharacter>())
                        {
                            FromCharacter fromCharacter = entity.Read<FromCharacter>();
                            ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                            StartCraftItemEvent startCraftItemEvent = entity.Read<StartCraftItemEvent>();
                            PrefabGUID prefabGUID = startCraftItemEvent.RecipeId;
                            Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
                            var buffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                            PrefabGUID itemPrefab = buffer[0].Guid;
                            if (!Core.DataStructures.PlayerCraftingJobs.ContainsKey(startCraftItemEvent.Workstation))
                            {
                                Core.DataStructures.PlayerCraftingJobs[startCraftItemEvent.Workstation] = [];
                            }
                            var workstationJobs = Core.DataStructures.PlayerCraftingJobs[startCraftItemEvent.Workstation];

                            // Ensure the player’s job list exists
                            if (!workstationJobs.TryGetValue(steamId, out var playerJobs))
                            {
                                playerJobs = [];
                                workstationJobs[steamId] = playerJobs;
                            }

                            // Check if the job exists and update or add
                            var jobExists = false;
                            for (int i = 0; i < playerJobs.Count; i++)
                            {
                                if (playerJobs[i].Item1.Equals(itemPrefab))
                                {
                                    Core.Log.LogInfo($"Adding Craft: {itemPrefab.LookupName()}");
                                    playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 + 1);
                                    jobExists = true;
                                    break;
                                }
                            }
                            if (!jobExists)
                            {
                                Core.Log.LogInfo($"Adding Craft: {itemPrefab.LookupName()}");
                                playerJobs.Add((itemPrefab, 1));
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited StartCraftingSystem hook early: {e}");
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
            //Core.Log.LogInfo("StopCraftingSystemPrefix called...");

            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);// double check this
            try
            {
                foreach (Entity entity in entities)
                {
                    if (Plugin.ProfessionSystem.Value)
                    {
                        if (entity.Has<StopCraftItemEvent>() && entity.Has<FromCharacter>())
                        {
                            FromCharacter fromCharacter = entity.Read<FromCharacter>();
                            ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                            StopCraftItemEvent stopCraftItemEvent = entity.Read<StopCraftItemEvent>();
                            PrefabGUID prefabGUID = stopCraftItemEvent.RecipeGuid;
                            Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
                            var buffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
                            PrefabGUID itemPrefab = buffer[0].Guid;
                            if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(stopCraftItemEvent.Workstation, out var jobs) && jobs.TryGetValue(steamId, out var playerJobs))
                            {
                                // if crafting job is active, remove
                                for (int i = 0; i < playerJobs.Count; i++)
                                {
                                    if (playerJobs[i].Item1 == itemPrefab && playerJobs[i].Item2 > 0)
                                    {
                                        Core.Log.LogInfo($"Removing Craft: {itemPrefab.LookupName()}");
                                        playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 - 1);
                                        if (playerJobs[i].Item2 == 0) playerJobs.RemoveAt(i);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited StopCraftingSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}