using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static UnityEngine.UI.GridLayoutGroup;
using EnterShapeshiftEvent = ProjectM.Network.EnterShapeshiftEvent;

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

                //Core.Log.LogInfo($"EnterShapeShiftEvent: {enterShapeshiftEvent.Shapeshift.LookupName()}");

                if (enterShapeshiftEvent.Shapeshift.Equals(PsychicFormGroup))
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
                else if (enterShapeshiftEvent.Shapeshift.Equals(BatFormGroup))
                {
                    Core.Log.LogInfo("Bat form imminent, dismissing familiar...");
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(fromCharacter.Character);

                    if (familiar.Exists() && !familiar.IsDisabled())
                    {
                        FamiliarUtilities.AutoDismiss(fromCharacter.Character, familiar);
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
