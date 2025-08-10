using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class SpawnMoveSpeedBuffSystemPatch
{
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    static readonly PrefabGUID _solarusFinalBuff = new(2144624015);

    [HarmonyPatch(typeof(Spawn_MoveSpeedBuffSystem), nameof(Spawn_MoveSpeedBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(Spawn_MoveSpeedBuffSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGuid) || !entity.TryGetComponent(out EntityOwner entityOwner)) continue;
                else if (prefabGuid.Equals(_solarusFinalBuff) && entityOwner.Owner.IsFollowingPlayer()
                    && entity.TryGetBuffer<ApplyBuffOnGameplayEvent>(out var buffer) && !buffer.IsEmpty)
                {
                    ApplyBuffOnGameplayEvent applyBuffOnGameplayEvent = buffer[0];
                    applyBuffOnGameplayEvent.Buff0 = PrefabGUID.Empty;
                    buffer[0] = applyBuffOnGameplayEvent;
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
