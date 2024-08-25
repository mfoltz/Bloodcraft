using Bloodcraft.Services;
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
    private static void OnUpdatePrefix(UnitSpawnerReactSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_2099432189_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;

                if (ConfigService.UnitSpawnerMultiplier < 1f && entity.Has<IsMinion>())
                {
                    entity.Write(new IsMinion { Value = true });
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
