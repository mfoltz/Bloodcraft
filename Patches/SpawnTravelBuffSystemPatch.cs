using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class SpawnTravelBuffSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID BatLandingTravel = new(-371745443);

    [HarmonyPatch(typeof(Spawn_TravelBuffSystem), nameof(Spawn_TravelBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(Spawn_TravelBuffSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (prefabGUID.Equals(BatLandingTravel) && entity.GetOwner().TryGetPlayer(out Entity player))
                {         
                    if (FamiliarUtilities.AutoCallMap.TryGetValue(player, out Entity familiar) && familiar.Exists())
                    {
                        FamiliarUtilities.AutoCall(player, familiar);
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
