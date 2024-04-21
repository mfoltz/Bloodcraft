using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks
{

    [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
    public static class EquipItemSystemPatch
    {
        public static void Prefix(EquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("EquipItemSystem Prefix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    Utilities.LogComponentTypes(entity);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogInfo($"Exited EquipItemSystem hook early {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }

}
