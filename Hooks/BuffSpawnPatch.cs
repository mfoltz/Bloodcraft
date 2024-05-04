using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Lapis.Hooks
{
    [HarmonyPatch]
    public class BuffPatch
    {
        private static readonly PrefabGUID unarmedBuff = new(-2075546002);
        [HarmonyPatch(typeof(EquipmentSyncSystem), nameof(EquipmentSyncSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void Prefix(EquipmentSyncSystem __instance)
        {
            NativeArray<Entity> entities = __instance.__query_710171128_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    entity.LogComponentTypes();
                    
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}
