using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using EnterShapeshiftEvent = ProjectM.Network.EnterShapeshiftEvent;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ShapeshiftSystemPatch
{
    static readonly PrefabGUID PsychicFormGroup = new(-1908054166);
    static readonly PrefabGUID BatFormGroup = new(-104327922);

    [HarmonyPatch(typeof(ShapeshiftSystem), nameof(ShapeshiftSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ShapeshiftSystem __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out FromCharacter fromCharacter)) continue;
                EnterShapeshiftEvent enterShapeshiftEvent = entity.Read<EnterShapeshiftEvent>();

                if (enterShapeshiftEvent.Shapeshift.Equals(PsychicFormGroup))
                {
                    Entity character = fromCharacter.Character;
                    User user = fromCharacter.User.Read<User>();
                    ulong steamId = user.PlatformId;
                    
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);
                    if (steamId.TryGetFamiliarActives(out var data) && familiar.Exists() && !familiar.IsDisabled())
                    {
                        FamiliarUtilities.DismissFamiliar(character, familiar, user, steamId, data);
                    }
                }
                else if (enterShapeshiftEvent.Shapeshift.Equals(BatFormGroup))
                {
                    Entity character = fromCharacter.Character;
                    User user = fromCharacter.User.Read<User>();
                    ulong steamId = user.PlatformId;

                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);
                    if (steamId.TryGetFamiliarActives(out var data) && familiar.Exists() && !familiar.IsDisabled())
                    {
                        //FamiliarUtilities.AutoCallMap.TryAdd(fromCharacter.Character, familiar);

                        FamiliarUtilities.AutoCallMap[fromCharacter.Character] = familiar;
                        FamiliarUtilities.DismissFamiliar(character, familiar, user, steamId, data);
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
