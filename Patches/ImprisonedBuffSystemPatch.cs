using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ImprisonedBuffSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;

    [HarmonyPatch(typeof(ImprisonedBuffSystem), nameof(ImprisonedBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ImprisonedBuffSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_1231815368_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<Buff>()) continue;

                Entity buffTarget = entity.GetBuffTarget();
                if (!buffTarget.Has<CharmSource>()) // if no charm source, found familiar being imprisoned, destroy it
                {
                    if (buffTarget.Has<Disabled>()) buffTarget.Remove<Disabled>();
                    DestroyUtility.Destroy(EntityManager, buffTarget, DestroyDebugReason.None);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
