using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class SpawnTravelBuffSystemPatch
{
    static readonly PrefabGUID BatLandingTravel = new(-371745443);
    static readonly PrefabGUID DraculaFlyToCenter = new(-1961466676);

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
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
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
