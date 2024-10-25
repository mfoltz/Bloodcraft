using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay;
using Unity.Collections;
using Unity.Entities;
using ProjectM.Scripting;
using ProjectM.Shared;
using Bloodcraft.Services;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class KnockbackSystemSpawnPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManger => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    [HarmonyPatch(typeof(KnockbackSystemSpawn), nameof(KnockbackSystemSpawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(KnockbackSystemSpawn __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_1729431709_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner)) continue;

                Entity buffTarget = entity.GetBuffTarget();
                Entity owner = entityOwner.Owner;

                if (owner.IsFollowingPlayer() && buffTarget.IsPlayer())
                {
                    if (ServerGameManger.IsAllies(buffTarget, owner))
                    {
                        PreventKnockback(entity);
                    }
                    else if (GameMode.Equals(GameModeType.PvE))
                    {
                        PreventKnockback(entity);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void PreventKnockback(Entity knockbackBuff)
    {
        if (knockbackBuff.TryGetComponent(out Buff buff) && buff.BuffEffectType.Equals(BuffEffectType.Debuff))
        {
            //Core.Log.LogInfo($"Protecting ally from knockback debuff...");
            DestroyUtility.Destroy(EntityManager, knockbackBuff);
        }
    }
}
