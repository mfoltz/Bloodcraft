using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class PlayerTeleportSystemPatch
{
    [HarmonyPatch(typeof(PlayerTeleportSystem), nameof(PlayerTeleportSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePrefix(PlayerTeleportSystem __instance)
    {
        if (!Core.hasInitialized) return;
        else if (ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.EntityQueries[1].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.TryGetComponent(out PlayerTeleportDebugEvent playerTeleportDebugEvent) && entity.TryGetComponent(out FromCharacter fromCharacter))
                {
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(fromCharacter.Character);

                    if (familiar.Exists())
                    {
                        FamiliarUtilities.TryReturnFamiliar(familiar, fromCharacter.Character);
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
