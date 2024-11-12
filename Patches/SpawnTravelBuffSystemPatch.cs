using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class SpawnTravelBuffSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly PrefabGUID BatLandingTravel = new(-371745443);
    static readonly PrefabGUID DraculaFlyToCenter = new(-1961466676);
    static readonly PrefabGUID PvPProtectedBuff = new(1111481396);

    [HarmonyPatch(typeof(Spawn_TravelBuffSystem), nameof(Spawn_TravelBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(Spawn_TravelBuffSystem __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (prefabGUID.Equals(BatLandingTravel) && entity.GetOwner().TryGetPlayer(out Entity player))
                {         
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    if (FamiliarUtilities.AutoCallMap.TryGetValue(player, out Entity familiar) && familiar.Exists() && steamId.TryGetFamiliarActives(out var data))
                    {
                        FamiliarUtilities.CallFamiliar(player, familiar, user, steamId, data);
                        FamiliarUtilities.AutoCallMap.Remove(player);
                    }
                }
                /*
                else if (ConfigService.FamiliarSystem && entity.TryGetComponent(out Buff buff) && buff.Target.IsPlayer())
                {
                    Entity buffTarget = buff.Target;
                    Entity owner = entityOwner.Owner;

                    if (GameMode.Equals(GameModeType.PvE))
                    {
                        if (owner.IsPlayer() && !owner.Equals(buffTarget))
                        {
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (ConfigService.FamiliarSystem)
                        {
                            if (owner.IsFollowingPlayer() || owner.GetOwner().IsFollowingPlayer())
                            {
                                if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                            }
                        }
                    }
                    else if (GameMode.Equals(GameModeType.PvP) && buffTarget.HasBuff(PvPProtectedBuff))
                    {
                        if (owner.IsPlayer() && !owner.Equals(buffTarget))
                        {
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (ConfigService.FamiliarSystem)
                        {
                            if (owner.IsFollowingPlayer() || owner.GetOwner().IsFollowingPlayer())
                            {
                                if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                            }
                        }
                    }
                }
                */

                /*
                else if (prefabGUID.Equals(DraculaFlyToCenter) && entity.TryGetComponent(out Buff buff) && buff.Target.TryGetComponent(out UnitStats unitStats))
                {
                    Core.Log.LogInfo("DraculaFlyToCenter detected, applying damage reduction...");

                    if (unitStats.DamageReduction._Value == 0f)
                    {
                        unitStats.DamageReduction._Value = 0.25f;
                        entity.Write(unitStats);
                    }
                }
                */
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
