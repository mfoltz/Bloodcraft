using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UnitSpawnerPatch
{
    [HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(UnitSpawnerReactSystem __instance)
    {
        if (!Core._initialized) return;

        // NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        using NativeAccessor<Entity> entities = __instance.EntityQueries[0].ToEntityArrayAccessor();

        try
        {
            foreach (Entity entity in entities)
            {
                entity.HasWith((ref IsMinion isMinion) =>
                {
                    isMinion.Value = true;
                });
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[UnitSpawnerPatch.OnUpdatePrefix] Exception: {e}");
        }
        finally
        {
            // entities.Dispose();
        }
    }
}
