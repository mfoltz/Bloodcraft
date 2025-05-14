using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.WeaponCoating;
using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Patches;

/* they last forever with duration over base so not touching for now
[HarmonyPatch]
internal static class CoatingEquipSystemPatch
{
    static readonly bool _professions = ConfigService.ProfessionSystem;

    static readonly WaitForSeconds _delay = new(0.1f);
    const int MAX_PROFESSION_LEVEL = ProfessionSystem.MAX_PROFESSION_LEVEL;

    [HarmonyPatch(typeof(CoatingEquipSystem), nameof(CoatingEquipSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CoatingEquipSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_professions) return;

        using NativeAccessor<ApplyCoatingEvent_Internal> applyCoatingEvents = __instance._InternalApplyCoatingEvents.ToComponentDataArrayAccessor<ApplyCoatingEvent_Internal>();

        try
        {
            foreach (ApplyCoatingEvent_Internal applyCoatingEvent in applyCoatingEvents)
            {
                // Core.Log.LogWarning($"[CoatingEquipSystem.OnUpdatePrefix] ApplyCoatingEvent_Internal");
                ModifyCoatingDuration(applyCoatingEvent.ItemTarget, applyCoatingEvent.Character).Start();
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[CoatingEquipSystem.OnUpdatePrefix] Exception: {e}");
        }
    }
    static IEnumerator ModifyCoatingDuration(Entity itemEntity, Entity playerCharacter)
    {
        yield return _delay;

        IProfession alchemyHandler = ProfessionFactory.GetProfession(ProfessionType.Alchemy);
        ulong steamId = playerCharacter.GetSteamId();

        int level = alchemyHandler.GetProfessionLevel(steamId);
        float duration = 1 + level / (float)MAX_PROFESSION_LEVEL;

        itemEntity.HasWith((ref Coatable coatable) =>
        {
            // Core.Log.LogWarning($"[CoatingEquipSystem.OnUpdatePrefix] Coating duration: {coatable.CoatingExpireTime} -> {coatable.CoatingExpireTime * duration}");
            coatable.CoatingExpireTime *= duration;
        });
    }
}
*/
