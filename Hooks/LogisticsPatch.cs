using Cobalt.Core;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks;

public class LogisticsPatches
{
    [HarmonyPatch(typeof(UpdateRefiningSystem), nameof(UpdateRefiningSystem.OnUpdate))]
    public static class UpdateRefiningSystemPatch
    {
        public static void Prefix(UpdateRefiningSystem __instance)
        {
            //Plugin.Log.LogInfo("Running UpdateRefiningSystem hook...");
            EntityManager entityManager = VWorld.Server.EntityManager;
            PrefabCollectionSystem prefabCollectionSystem = VWorld.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
            GameDataSystem gameDataSystem = VWorld.Server.GetExistingSystemManaged<GameDataSystem>();

            EntityQuery stationsQuery = entityManager.CreateEntityQuery(LogisticsUtilities.RefinementStationQuery);
            NativeArray<Entity> stations = stationsQuery.ToEntityArray(Allocator.TempJob);
            var needs = new Dictionary<PrefabGUID, List<(Entity station, int amount)>>();
            // First, assess the needs of each station
            try
            {
                foreach (var station in stations)
                {
                    if (!entityManager.Exists(station) || !station.Has<Refinementstation>() || !station.Read<NameableInteractable>().Name.ToString().ToLower().Contains("receiver")) continue;

                    var refinementStation = station.Read<Refinementstation>();

                    var recipesBuffer = station.ReadBuffer<RefinementstationRecipesBuffer>();
                    foreach (var recipe in recipesBuffer)
                    {
                        Entity recipeEntity = prefabCollectionSystem._PrefabGuidToEntityMap[recipe.RecipeGuid];
                        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
                        foreach (var requirement in requirements)
                        {
                            if (!needs.ContainsKey(requirement.Guid))
                                needs[requirement.Guid] = [];

                            needs[requirement.Guid].Add((station, requirement.Amount));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited UpdateRefiningSystem needsProcessing early: {e}");
            }
            finally
            {
                //Plugin.Log.LogInfo("Disposing station query...");
                stationsQuery.Dispose();
                stations.Dispose();
            }
            // Process each station's output to see if it can fulfill any other station's needs
            var providers = __instance._Query.ToEntityArray(Allocator.TempJob);
            try
            {
                foreach (var provider in providers)
                {
                    if (!entityManager.Exists(provider) || !provider.Has<Refinementstation>() || !provider.Read<NameableInteractable>().Name.ToString().ToLower().Contains("provider")) continue;
                    //provider.LogComponentTypes();
                    var refinementStation = provider.Read<Refinementstation>();

                    Entity outputInventory = refinementStation.OutputInventoryEntity._Entity;
                    foreach (var needKey in needs.Keys)
                    {
                        int availableAmount = serverGameManager.GetInventoryItemCount(outputInventory, needKey);
                        //int availableAmount = InventoryUtilities.GetItemAmount(entityManager, outputInventory, needKey);
                        //Plugin.Log.LogInfo($"{availableAmount.ToString()} | {needKey.LookupName()}");
                        if (availableAmount <= 0) continue;

                        foreach (var (station, amount) in needs[needKey])
                        {
                            if (!serverGameManager.IsAllies(provider, station) || !LogisticsUtilities.SameTerritory(provider, station)) continue;
                            Refinementstation receivingStation = station.Read<Refinementstation>();
                            Entity inputInventory = receivingStation.InputInventoryEntity._Entity;

                            var transferAmount = Math.Min(amount, availableAmount);
                          
                            // need to use try remove then try add instead of inventoryUtilitiesServer.TryMoveItem
                            if (serverGameManager.TryRemoveInventoryItem(outputInventory, needKey, transferAmount))
                            {
                                if (serverGameManager.TryAddInventoryItem(inputInventory, needKey, transferAmount))
                                {
                                    Plugin.Log.LogInfo($"Moved {transferAmount.ToString()} of {needKey.LookupName()} from {provider.Read<NameableInteractable>().Name} to {station.Read<NameableInteractable>().Name}");
                                }
                                else
                                {
                                    Plugin.Log.LogInfo($"Failed to add {needKey.LookupName()}x{transferAmount} from provider to receiver.");
                                }
                            }
                            else
                            {
                                Plugin.Log.LogInfo($"Failed to remove {needKey.LookupName()}x{transferAmount} from provider.");
                            }

                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited UpdateRefiningSystem providerProcessing early: {e}");
            }
            finally
            {
                //Plugin.Log.LogInfo("Disposing of providers array...");
                providers.Dispose();
            }
        }
    }

    [HarmonyPatch(typeof(ServantMissionUpdateSystem), nameof(ServantMissionUpdateSystem.OnUpdate))]
    public static class ServantMissionPatch
    {
        public static void Prefix(ServantMissionUpdateSystem __instance)
        {
            NativeList<ServantMissionUpdateSystem.MissionIdentifier> missions = __instance._TempFinishedMissions;
            if (missions.IsEmpty || !missions.IsCreated) return;
            try
            {
                foreach (var mission in missions)
                {
                    if (mission.MissionOwner.Equals(Entity.Null)) continue;
                    else
                    {
                        LogisticsUtilities.ProcessServantInventory(mission.MissionOwner);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited ServantMissionActionSystem hook early: {e}");
            }
        }
    }

    public static class LogisticsUtilities
    {
        public static readonly ComponentType[] StashQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<Team>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<InventoryBuffer>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ];

        public static readonly ComponentType[] RefinementStationQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<Team>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<Refinementstation>()),
                ComponentType.ReadOnly(Il2CppType.Of<NameableInteractable>()),
            ];

        public static void ProcessServantInventory(Entity servant)
        {
            var buffer = servant.ReadBuffer<InventoryBuffer>();
            // check for items in inventory before processing
            if (buffer.IsEmpty || !buffer.IsCreated) return;
            else
            {
                Plugin.Log.LogInfo($"Inventory Items: {buffer.Length.ToString()}");
                GameDataSystem gameDataSystem = VWorld.Server.GetExistingSystemManaged<GameDataSystem>();
                ServerGameManager serverGameManager = VWorld.Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
                EntityManager entityManager = VWorld.Server.EntityManager;
                if (InventoryUtilities.TryGetInventoryEntity(entityManager, servant, out Entity inventory))
                {
                    NativeArray<Entity> stashes = entityManager.CreateEntityQuery(StashQuery).ToEntityArray(Allocator.Temp);
                    try
                    {
                        foreach (var stash in stashes)
                        {
                            if (stash.Equals(Entity.Null)) continue;

                            if (!serverGameManager.IsAllies(stash, servant) || !SameTerritory(stash, servant) || !stash.Read<NameableInteractable>().Name.ToString().ToLower().Equals("missions"))
                                continue;

                            if (!InventoryUtilities.TryGetInventoryEntity(entityManager, stash, out Entity stashInventory))
                            {
                                Plugin.Log.LogInfo("No stash inventory entity found.");
                                continue;
                            }

                            // Attempt to move items
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                // Try to move the item at index i to the stash
                                PrefabGUID item = buffer[i].ItemType;
                                //var itemCount = InventoryUtilities.GetItemAmount(entityManager, inventory, item);
                                var itemCount = serverGameManager.GetInventoryItemCount(inventory, item);
                                if (serverGameManager.TryRemoveInventoryItem(inventory, item, itemCount))
                                {
                                    if (serverGameManager.TryAddInventoryItem(stashInventory, item, itemCount))
                                    {
                                        Plugin.Log.LogInfo($"Moved {item.LookupName()}x{itemCount} to {stash.Read<NameableInteractable>().Name} from servant.");
                                    }
                                    else
                                    {
                                        Plugin.Log.LogInfo($"Failed to add {item.LookupName()}x{itemCount} to {stash.Read<NameableInteractable>().Name} after removing from servant.");
                                    }
                                }

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogError($"Exited ProcessServantInventory early: {e}");
                    }
                    finally
                    {
                        stashes.Dispose();
                    }
                }
            }
        }

        public static bool SameTerritory(Entity input, Entity ouput)
        {
            if (input.Has<CastleHeartConnection>() && ouput.Has<CastleHeartConnection>())
            {
                Entity inputHeart = input.Read<CastleHeartConnection>().CastleHeartEntity._Entity;
                Entity outputHeart = ouput.Read<CastleHeartConnection>().CastleHeartEntity._Entity;
                if (inputHeart.Equals(outputHeart)) return true;
            }
            return false;
        }
    }
}