using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
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
        else if (!ConfigService.QuestSystem) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<InteractPickup>() || !entity.Has<EntityOwner>()) continue;
                else if (entity.GetOwner().TryGetPlayer(out Entity player))
                {
                    ulong steamId = player.GetSteamId();

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
