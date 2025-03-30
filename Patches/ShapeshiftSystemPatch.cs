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

    static readonly PrefabGUID _vanishBuff = new(1595547018);           // AB_Bandit_Thief_Rush_Buff

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
                    bool hasActive = steamId.HasActiveFamiliar();
                    bool isDismissed = steamId.HasDismissedFamiliar();

                    if (hasActive && !isDismissed)
                    {
                        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                        if (familiar.HasBuff(_vanishBuff))
                        {
                            continue;
                        }

                        Familiars.DismissFamiliar(playerCharacter, familiar, user, steamId);
                    }
                }
                else if (enterShapeshiftEvent.Shapeshift.Equals(_batFormGroup))
                {
                    bool hasActive = steamId.HasActiveFamiliar();
                    bool isDismissed = steamId.HasDismissedFamiliar();

                    if (hasActive && !isDismissed)
                    {
                        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                        if (familiar.HasBuff(_vanishBuff))
                        {
                            continue;
                        }

                        Familiars.AutoCallMap[fromCharacter.Character] = familiar;
                        Familiars.DismissFamiliar(playerCharacter, familiar, user, steamId);
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
