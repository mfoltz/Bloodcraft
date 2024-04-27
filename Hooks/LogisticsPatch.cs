using Bloodstone.API;
using Cobalt.Core;
using Cobalt.Systems;
using HarmonyLib;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using System.Reflection.Metadata.Ecma335;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks;
public class LogisticsPatches
{
    [HarmonyPatch(typeof(ActiveRefinementSequenceSystem), nameof(ActiveRefinementSequenceSystem.OnUpdate))]
    public static class RefinementStationPatch
    {
        public static void Prefix(ActiveRefinementSequenceSystem __instance)
        {
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null)) continue;
                    Plugin.Log.LogInfo("ActiveRefinementSequenceSystem Prefix...");
                    entity.LogComponentTypes();

                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited ActiveRefinementSequenceSystem hook early: {e}");
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
                    mission.MissionOwner.LogComponentTypes();
                    Plugin.Log.LogInfo("ServantMissionActionSystem Prefix...");

                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited ServantMissionActionSystem hook early: {e}");
            }
        }
    }
}