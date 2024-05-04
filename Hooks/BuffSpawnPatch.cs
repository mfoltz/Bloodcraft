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
        [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void Prefix(EquipItemSystem __instance)
        {
            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
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
        [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void Prefix(UnEquipItemSystem __instance)
        {
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
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
