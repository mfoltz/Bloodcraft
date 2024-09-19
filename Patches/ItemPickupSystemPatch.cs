using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ItemPickupSystemPatch
{
    [HarmonyPatch(typeof(ItemPickupSystem), nameof(ItemPickupSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ItemPickupSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<InteractPickup>()) continue;
                else if (entity.GetOwner().TryGetPlayer(out Entity player))
                {
                    ulong steamId = player.GetSteamId();
                    Core.Log.LogInfo(entity.Read<PrefabGUID>().LookupName());

                    if (DealDamageSystemPatch.LastDamageTime.ContainsKey(steamId)) DealDamageSystemPatch.LastDamageTime.Remove(steamId);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
