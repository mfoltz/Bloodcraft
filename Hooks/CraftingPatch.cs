using Cobalt.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Systems.Professions.ProfessionUtilities;
using User = ProjectM.Network.User;

namespace Cobalt.Hooks;

public class CraftingPatch
{
    [HarmonyPatch(typeof(UpdateCraftingSystem), nameof(UpdateCraftingSystem.OnUpdate))]
    public static class UpdateCraftingSystemPatch
    {
        private static readonly float BaseCraftingXP = 50;
        private static readonly float craftRate = Core.ServerGameSettingsSystem._Settings.CraftRateModifier;

        public static void Postfix(UpdateCraftingSystem __instance)
        {
            if (!Plugin.ProfessionSystem.Value) return;
            PrefabCollectionSystem prefabCollectionSystem = Core.PrefabCollectionSystem;
            NativeArray<Entity> entities = __instance.__query_1831452865_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null) || !entity.Has<CastleAreaRequirement>() || !entity.Has<QueuedWorkstationCraftAction>()) continue;

                    var actions = entity.ReadBuffer<QueuedWorkstationCraftAction>();
                    if (actions.IsEmpty) continue;
                    foreach (var action in actions)
                    {
                        User user = action.InitiateUser.Read<User>();
                        ulong steamId = user.PlatformId;
                        if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(entity.Read<NetworkId>(), out var jobs) && jobs.TryGetValue(steamId, out var playerJobs))
                        {
                            RecipeData recipeData = prefabCollectionSystem._PrefabGuidToEntityMap[action.RecipeGuid].Read<RecipeData>();
                            float delta = (recipeData.CraftDuration / craftRate) - action.ProgressTime;
                            if (delta < 0.1)
                            {
                                Entity recipe = prefabCollectionSystem._PrefabGuidToEntityMap[action.RecipeGuid];

                                var recipeOutput = recipe.ReadBuffer<RecipeOutputBuffer>();
                                PrefabGUID itemPrefab = recipeOutput[0].Guid;

                                bool jobExists = false;
                                for (int i = 0; i < playerJobs.Count; i++)
                                {
                                    if (playerJobs[i].Item1 == itemPrefab)
                                    {
                                        playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 - 1);
                                        jobExists = true;
                                        break;
                                    }
                                }

                                if (!jobExists) continue;

                                Entity item = prefabCollectionSystem._PrefabGuidToEntityMap[itemPrefab];
                                float ProfessionValue = BaseCraftingXP;
                                // t01 etc multiplier
                                ProfessionValue *= GetTierMultiplier(action.RecipeGuid);
                                IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(action.RecipeGuid, "");
                                if (handler != null)
                                {
                                    ProfessionSystem.SetProfession(itemPrefab, user, steamId, ProfessionValue, handler);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited UpdateCraftingSystem hook early: {e}");
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
            Core.Log.LogInfo("StartCraftingSystemPrefix called...");
            NativeArray<Entity> entities = __instance._StartCraftItemEventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (!entity.Has<StartCraftItemEvent>() || !entity.Has<FromCharacter>())
                    {
                        Core.Log.LogInfo("Entity does not have StartCraftItemEvent or FromCharacter...");
                        continue;
                    }
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    StartCraftItemEvent startCraftItemEvent = entity.Read<StartCraftItemEvent>();
                    PrefabGUID prefabGUID = startCraftItemEvent.RecipeId;
                    if (!Core.DataStructures.PlayerCraftingJobs.ContainsKey(startCraftItemEvent.Workstation))
                    {
                        Core.DataStructures.PlayerCraftingJobs[startCraftItemEvent.Workstation] = [];
                    }
                    var workstationJobs = Core.DataStructures.PlayerCraftingJobs[startCraftItemEvent.Workstation];

                    // Ensure the player’s job list exists
                    if (!workstationJobs.TryGetValue(steamId, out var jobs))
                    {
                        jobs = [];
                        workstationJobs[steamId] = jobs;
                    }

                    // Check if the job exists and update or add
                    var jobExists = false;
                    for (int i = 0; i < jobs.Count; i++)
                    {
                        if (jobs[i].Item1.Equals(prefabGUID))
                        {
                            Core.Log.LogInfo($"Adding Craft: {prefabGUID.LookupName()}");
                            jobs[i] = (jobs[i].Item1, jobs[i].Item2 + 1);
                            jobExists = true;
                            break;
                        }
                    }
                    if (!jobExists)
                    {
                        jobs.Add((prefabGUID, 1));
                    }
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited UpdateCraftingSystem hook early: {e}");
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
            Core.Log.LogInfo("StopCraftingSystemPrefix called...");
            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);// double check this
            try
            {
                foreach (Entity entity in entities)
                {
                    if (!entity.Has<StopCraftItemEvent>() || !entity.Has<FromCharacter>())
                    {
                        Core.Log.LogInfo("Entity does not have StopCraftItemEvent or FromCharacter...");
                        continue;
                    }
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    StopCraftItemEvent stopCraftItemEvent = entity.Read<StopCraftItemEvent>();
                    PrefabGUID prefabGUID = stopCraftItemEvent.RecipeGuid;
                    if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(stopCraftItemEvent.Workstation, out var jobs) && jobs.TryGetValue(steamId, out var playerJobs))
                    {
                        // if crafting job is active, remove
                        Core.Log.LogInfo($"Removing Craft: {prefabGUID.LookupName()}");
                        for (int i = 0; i < jobs.Count; i++)
                        {
                            if (playerJobs[i].Item1 == prefabGUID)
                            {
                                playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 - 1);
                                break;
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