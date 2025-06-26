using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class SpawnTravelBuffSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    static readonly PrefabGUID _batLandingTravel = new(-371745443);
    static readonly PrefabGUID _draculaFlyToCenter = new(-1961466676);
    static readonly PrefabGUID _pvpProtectedBuff = new(1111481396);

    [HarmonyPatch(typeof(Spawn_TravelBuffSystem), nameof(Spawn_TravelBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(Spawn_TravelBuffSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                Core.Log.LogWarning($"[Spawn_TravelBuffSystem]: {entity.GetPrefabGuid().GetPrefabName()}");

                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (prefabGUID.Equals(_batLandingTravel) && entityOwner.Owner.TryGetPlayer(out Entity player))
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    if (Familiars.AutoCallMap.TryRemove(player, out Entity familiar) && familiar.Exists())
                    {
                        Familiars.CallFamiliar(player, familiar, user, steamId);
                    }

                    Core.Log.LogWarning($"[Spawn_TravelBuffSystem]: {prefabGUID.GetPrefabName()}");
                }
                else
                {
                    Core.Log.LogWarning($"[Spawn_TravelBuffSystem]: {prefabGUID.GetPrefabName()}");
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
