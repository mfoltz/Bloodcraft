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

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    [HarmonyPatch(typeof(ImprisonedBuffSystem), nameof(ImprisonedBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ImprisonedBuffSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.__query_1231815368_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<Buff>()) continue;

                Entity buffTarget = entity.GetBuffTarget();
                if (!buffTarget.Has<CharmSource>() && buffTarget.Has<BlockFeedBuff>()) // if no charm source, found familiar being imprisoned, destroy it
                {
                    if (buffTarget.Has<Disabled>()) buffTarget.Remove<Disabled>();
                    if (buffTarget.Has<Minion>()) buffTarget.Remove<Minion>();
                    if (buffTarget.Has<BlockFeedBuff>()) buffTarget.Remove<BlockFeedBuff>();

                    DestroyUtility.Destroy(EntityManager, buffTarget);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
