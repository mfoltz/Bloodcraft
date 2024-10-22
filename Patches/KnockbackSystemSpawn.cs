using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay;
using Unity.Collections;
using Unity.Entities;
using ProjectM.Scripting;
using ProjectM.Shared;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class KnockbackSystemSpawnPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManger => Core.ServerGameManager;

    [HarmonyPatch(typeof(KnockbackSystemSpawn), nameof(KnockbackSystemSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(KnockbackSystemSpawn __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_1729431709_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner)) continue;

                Entity buffTarget = entity.GetBuffTarget();

                if (buffTarget.IsPlayer() && ServerGameManger.IsAllies(buffTarget, entityOwner.Owner))
                {
                    Core.Log.LogInfo($"Checking knockback on ally...");

                    if (entity.TryGetComponent(out Buff buff) && buff.BuffEffectType.Equals(BuffEffectType.Debuff))
                    {
                        Core.Log.LogInfo($"Protecting ally from knockback debuff...");
                        DestroyUtility.Destroy(EntityManager, entity);
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
