using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using EnterShapeshiftEvent = ProjectM.Network.EnterShapeshiftEvent;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ShapeshiftSystemPatch
{
    static readonly PrefabGUID dominateAbility = new(-1908054166);

    [HarmonyPatch(typeof(ShapeshiftSystem), nameof(ShapeshiftSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ShapeshiftSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {

                EnterShapeshiftEvent enterShapeshiftEvent = entity.Read<EnterShapeshiftEvent>();
                FromCharacter fromCharacter = entity.Read<FromCharacter>();

                if (enterShapeshiftEvent.Shapeshift.Equals(dominateAbility))
                {
                    Entity character = fromCharacter.Character;
                    Entity userEntity = fromCharacter.User;
                    ulong steamId = userEntity.Read<User>().PlatformId;

                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);
                    if (familiar.Exists() && !familiar.Disabled())
                    {
                        EmoteSystemPatch.CallDismiss(userEntity, character, steamId);
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
