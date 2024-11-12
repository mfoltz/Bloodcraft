using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay;
using Unity.Collections;
using Unity.Entities;
using ProjectM.Scripting;
using ProjectM.Shared;
using Bloodcraft.Services;
using Stunlock.Core;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class KnockbackSystemSpawnPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManger => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly PrefabGUID PvPProtectionBuff = new(1111481396);
    static readonly PrefabGUID AllyKnockbackBuff = new(-2099203048);

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
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (prefabGUID == AllyKnockbackBuff) continue;

                Entity buffTarget = entity.GetBuffTarget();
                Entity owner = entityOwner.Owner;

                if (owner.IsFollowingPlayer() && buffTarget.TryGetPlayer(out Entity player))
                {
                    if (ServerGameManger.IsAllies(buffTarget, owner))
                    {
                        PreventKnockback(entity);
                    }
                    else if (GameMode.Equals(GameModeType.PvE))
                    {
                        PreventKnockback(entity);
                    }
                    else if (player.HasBuff(PvPProtectionBuff))
                    {
                        PreventKnockback(entity);
                    }
                }
                else if (owner.IsPlayer() && !owner.Equals(buffTarget) && buffTarget.TryGetPlayer(out player))
                {
                    if (GameMode.Equals(GameModeType.PvP) && player.HasBuff(PvPProtectionBuff))
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
            DestroyUtility.Destroy(EntityManager, knockbackBuff);
        }
    }
}
