using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ProjectileSystemPatch
{
    static readonly PrefabGUID vampiricCurseProjectile = new(270228277);
    static readonly PrefabGUID captureBuff = new(1280015305);
    static readonly PrefabGUID immaterialBuff = new(-259674366);

    [HarmonyPatch(typeof(ProjectileSystem), nameof(ProjectileSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ProjectileSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities) // probably want to add capture mode or w/e to toggle for player to acivate this
            {
                if (!entity.GetOwner().IsPlayer()) continue;
                else if (entity.TryGetComponent(out PrefabGUID projectilePrefab) && projectilePrefab.Equals(vampiricCurseProjectile))
                {
                    var buffer = entity.ReadBuffer<ApplyBuffOnGameplayEvent>();
                    
                    ApplyBuffOnGameplayEvent applyBuffOnGameplayEvent = buffer[0];
                    if (!applyBuffOnGameplayEvent.Buff0.Equals(captureBuff))
                    {
                        applyBuffOnGameplayEvent.Buff0 = captureBuff;
                        buffer[0] = applyBuffOnGameplayEvent;

                        applyBuffOnGameplayEvent = buffer[1];
                        applyBuffOnGameplayEvent.Buff0 = immaterialBuff;
                        buffer[1] = applyBuffOnGameplayEvent;
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
