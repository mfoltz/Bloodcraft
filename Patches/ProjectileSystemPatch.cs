using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class ProjectileSystemPatch
{
    static readonly PrefabGUID VampiricCurseProjectile = new(270228277);
    static readonly PrefabGUID CaptureBuff = new(1280015305);
    static readonly PrefabGUID ImmaterialBuff = new(-259674366);
    static readonly PrefabGUID BreakBuff = new(-1466712470);

    static readonly PrefabGUID CaptureT01 = new(-1763296393);
    static readonly PrefabGUID CaptureT02 = new(1093914645);
    static readonly PrefabGUID CaptureT03 = new(1504445802);

    [HarmonyPatch(typeof(ProjectileSystem), nameof(ProjectileSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ProjectileSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<EntityOwner>()) continue;
                else if (!entity.GetOwner().IsPlayer()) continue;
                else if (entity.TryGetComponent(out PrefabGUID projectilePrefab) && projectilePrefab.Equals(VampiricCurseProjectile))
                {
                    var applyBuffBuffer = entity.ReadBuffer<ApplyBuffOnGameplayEvent>();

                    ApplyBuffOnGameplayEvent applyBuffOnGameplayEvent = applyBuffBuffer[0];
                    if (!applyBuffOnGameplayEvent.Buff0.Equals(CaptureBuff))
                    {
                        applyBuffOnGameplayEvent.Buff0 = CaptureBuff;
                        applyBuffBuffer[0] = applyBuffOnGameplayEvent;

                        applyBuffOnGameplayEvent = applyBuffBuffer[1];
                        applyBuffOnGameplayEvent.Buff0 = ImmaterialBuff;
                        applyBuffBuffer[1] = applyBuffOnGameplayEvent;

                        if (entity.Has<ApplyKnockbackOnGameplayEvent>()) entity.Remove<ApplyKnockbackOnGameplayEvent>();
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
*/