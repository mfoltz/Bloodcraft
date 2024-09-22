using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

#if DEV
[HarmonyPatch]
internal static class EquipItemPatches
{
    [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(EquipItemSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                Core.Log.LogInfo("EquipItemSystem");
                entity.LogComponentTypes();
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(UnEquipItemSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                Core.Log.LogInfo("UnequipItemSystem");
                entity.LogComponentTypes();
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
#endif