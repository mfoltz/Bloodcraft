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
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    static readonly PrefabGUID _psychicFormGroup = new(-1908054166);
    static readonly PrefabGUID _batFormGroup = new(-104327922);

    [HarmonyPatch(typeof(ShapeshiftSystem), nameof(ShapeshiftSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ShapeshiftSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out FromCharacter fromCharacter)) continue;
                EnterShapeshiftEvent enterShapeshiftEvent = entity.Read<EnterShapeshiftEvent>();

                Entity playerCharacter = fromCharacter.Character;
                User user = playerCharacter.GetUser();
                ulong steamId = user.PlatformId;

                if (enterShapeshiftEvent.Shapeshift.Equals(_psychicFormGroup))
                {
                    Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                    if (steamId.TryGetFamiliarActives(out var data) && familiar.Exists() && !familiar.IsDisabled())
                    {
                        Familiars.DismissFamiliar(playerCharacter, familiar, user, steamId, data);
                    }
                }
                else if (enterShapeshiftEvent.Shapeshift.Equals(_batFormGroup))
                {
                    Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                    if (steamId.TryGetFamiliarActives(out var data) && familiar.Exists() && !familiar.IsDisabled())
                    {
                        Familiars.AutoCallMap[fromCharacter.Character] = familiar;
                        Familiars.DismissFamiliar(playerCharacter, familiar, user, steamId, data);
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
