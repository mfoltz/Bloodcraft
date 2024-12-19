using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ItemPickupSystemPatch
{
    static readonly bool _quests = ConfigService.QuestSystem;

    [HarmonyPatch(typeof(ItemPickupSystem), nameof(ItemPickupSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ItemPickupSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_quests) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<InteractPickup>() || !entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out Entity player))
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
