using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class PlayerTeleportSystemPatch
{
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    [HarmonyPatch(typeof(PlayerTeleportSystem), nameof(PlayerTeleportSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePrefix(PlayerTeleportSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[1].ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<PlayerTeleportDebugEvent>() && entity.TryGetComponent(out FromCharacter fromCharacter))
                {
                    Entity familiar = Familiars.GetActiveFamiliar(fromCharacter.Character);

                    if (familiar.Exists())
                    {
                        Familiars.TryReturnFamiliar(familiar, fromCharacter.Character);
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
