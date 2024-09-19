using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class ScriptDestroyServerPatch
{
    static readonly PrefabGUID captureBuff = new(1280015305);

    [HarmonyPatch(typeof(ScriptDestroyServer), nameof(ScriptDestroyServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptDestroyServer __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_1231292250_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<EntityOwner>()) continue;
                else if (entity.GetOwner().TryGetPlayer(out Entity player) && entity.TryGetComponent(out PrefabGUID prefab) && prefab.Equals(captureBuff))
                {
                    //Core.Log.LogInfo($"ScriptDestroyServer: {captureBuff.LookupName()}");
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
