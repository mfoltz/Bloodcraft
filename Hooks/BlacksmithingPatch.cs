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
using System.Reflection.Metadata.Ecma335;
using Unity.Collections;
using Unity.Entities;


namespace Cobalt.Hooks;

public class BlacksmithingPatch
{
    [HarmonyPatch(typeof(UpdateCraftingSystem), nameof(UpdateCraftingSystem.OnUpdate))]
    public static class UpdateCraftingSystemPatch
    {
        public static void Prefix(UpdateCraftingSystem __instance)
        {
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null) || !entity.Has<CastleAreaRequirement>() || !entity.Has<QueuedWorkstationCraftAction>()) continue;
                    var actions = entity.ReadBuffer<QueuedWorkstationCraftAction>();
                    if (actions.IsEmpty || !actions.IsCreated) continue;
                    
                    Plugin.Log.LogInfo("UpdateCraftingSystem Prefix...");
                    entity.LogComponentTypes();
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
                    if (entity.Equals(Entity.Null)) continue;
                    Plugin.Log.LogInfo("StopCraftingSystem Prefix...");
                    entity.LogComponentTypes();
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
                    if (entity.Equals(Entity.Null)) continue;
                    Plugin.Log.LogInfo("StartCraftingSystem Prefix...");
                    entity.LogComponentTypes();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited StartCraftingSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }

}