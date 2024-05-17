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
            //Core.Log.LogInfo("UpdateCraftingSystemPrefix called...");
            PrefabCollectionSystem prefabCollectionSystem = Core.PrefabCollectionSystem;
            NativeArray<Entity> entities = __instance.__query_1831452865_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null) || !entity.Has<CastleAreaRequirement>() || !entity.Has<QueuedWorkstationCraftAction>()) continue;

                    var actions = entity.ReadBuffer<QueuedWorkstationCraftAction>();
                    if (actions.IsEmpty || !actions.IsCreated) continue;
                    foreach (var action in actions)
                    {
                        User user = action.InitiateUser.Read<User>();
                        ulong steamId = user.PlatformId;
                        if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var jobs) && jobs.ContainsKey(action.RecipeGuid) && jobs[action.RecipeGuid])
                        {
                            //Core.Log.LogInfo(action.ProgressTime);
                            RecipeData recipeData = prefabCollectionSystem._PrefabGuidToEntityMap[action.RecipeGuid].Read<RecipeData>();
                            float delta = (recipeData.CraftDuration / craftRate) - action.ProgressTime;
                            //Core.Log.LogInfo($"{recipeData.CraftDuration} | {action.ProgressTime} | {delta}");
                            if (delta < 0.1)
                            {
                                //Core.Log.LogInfo("CraftingXP");

                                Entity recipe = prefabCollectionSystem._PrefabGuidToEntityMap[action.RecipeGuid];
                                //CastleWorkstation workstation = entity.Read<CastleWorkstation>();
                                //ServantType servantType = workstation.BonusServantType;
                                //Core.Log.LogInfo(servantType.ToString());
                                var recipeOutput = recipe.ReadBuffer<RecipeOutputBuffer>();
                                PrefabGUID itemPrefab = recipeOutput[0].Guid;
                                Entity item = prefabCollectionSystem._PrefabGuidToEntityMap[itemPrefab];
                                float ProfessionValue = BaseCraftingXP;
                                // t01 etc multiplier
                                ProfessionValue *= GetTierMultiplier(action.RecipeGuid);
                                IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(action.RecipeGuid, "");
                                if (handler != null)
                                {
                                    ProfessionSystem.SetProfession(itemPrefab, user, steamId, ProfessionValue, handler);
                                }
                                jobs.Remove(action.RecipeGuid);
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
                    if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var jobs) && !jobs.ContainsKey(prefabGUID))
                    {
                        // if crafting job not already present, add to cache
                        Core.Log.LogInfo($"Active Craft: {prefabGUID.LookupName()}");
                        jobs.Add(prefabGUID, true);
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
                    if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var jobs) && jobs.ContainsKey(prefabGUID))
                    {
                        // if crafting job is active, remove
                        Core.Log.LogInfo($"Inactive Craft: {prefabGUID.LookupName()}");
                        jobs.Remove(prefabGUID);
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