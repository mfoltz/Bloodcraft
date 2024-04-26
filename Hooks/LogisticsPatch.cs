using Bloodstone.API;
using Cobalt.Core;
using Cobalt.Systems;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks;
public class LogisticsPatches
{
    [HarmonyPatch(typeof(ItemPickupSystem), nameof(ItemPickupSystem.OnUpdate))]
    public static class ItemPickupSystemPatch
    {
        public static void Prefix(ItemPickupSystem __instance)
        {
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null)) continue;
                    //Plugin.Log.LogInfo("_DestroyedDropTableQuery");
                    entity.LogComponentTypes();

                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited DropInventorySystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}