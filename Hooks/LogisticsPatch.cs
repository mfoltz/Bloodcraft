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
using ProjectM.Sequencer;
using ProjectM.Shared.Systems;
using System.Reflection.Metadata.Ecma335;
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
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null)) continue;
                    Refinementstation refinementstation = entity.Read<Refinementstation>();
                    if (!refinementstation.IsWorking) continue;
                    Plugin.Log.LogInfo($"{refinementstation.CurrentRecipeGuid.LookupName()} | {refinementstation.Status}");
                    //entity.LogComponentTypes();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited UpdateRefiningSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
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
        private static readonly ComponentType[] StashQuery =
            [
                ComponentType.ReadOnly(Il2CppType.Of<Team>()),
                ComponentType.ReadOnly(Il2CppType.Of<CastleHeartConnection>()),
                ComponentType.ReadOnly(Il2CppType.Of<InventoryBuffer>()),
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
                GameDataSystem gameDataSystem = VWorld.Server.GetExistingSystem<GameDataSystem>();
                ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
                EntityManager entityManager = VWorld.Server.EntityManager;
                if (InventoryUtilities.TryGetInventoryEntity(entityManager, servant, out Entity inventory))
                {
                    NativeArray<Entity> stashes = entityManager.CreateEntityQuery(StashQuery).ToEntityArray(Allocator.Temp);
                    try
                    {
                        foreach (var stash in stashes)
                        {
                            if (stash.Equals(Entity.Null)) continue;
                            else
                            {
                                if (serverGameManager.IsAllies(stash, servant) && SameTerritory(stash, servant) && stash.Read<NameableInteractable>().Name.ToString().ToLower().Equals("missions"))
                                {
                                    // if allies and in the same territory and named missions, try to move items
                                    if (InventoryUtilities.TryGetInventoryEntity(entityManager, stash, out Entity stashInventory))
                                    {
                                        for (int i = 0; i < buffer.Length; i++)
                                        {
                                            InventoryUtilitiesServer.TryMoveItem(entityManager, gameDataSystem.ItemHashLookupMap, inventory, i, stashInventory);
                                        }
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

        public static bool SameTerritory(Entity stash, Entity servant)
        {
            if (stash.Has<CastleHeartConnection>() && servant.Has<CastleHeartConnection>())
            {
                Entity stashHeart = stash.Read<CastleHeartConnection>().CastleHeartEntity._Entity;
                Entity servantHeart = servant.Read<CastleHeartConnection>().CastleHeartEntity._Entity;
                if (stashHeart.Equals(servantHeart)) return true;
            }
            return false;
        }
    }
}