using Bloodcraft.Services;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class DebugLoggerPatch
{
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    const string MAP_ICON_ERROR = "PlayerMapIcon requires the creator to have the PlayerCharacter component.";

    [HarmonyPatch(typeof(Debug), nameof(Debug.LogError), new Type[] { typeof(Il2CppSystem.Object) })]
    [HarmonyPrefix]
    static bool LogErrorPrefix(Il2CppSystem.Object message)
    {
        if (!Core._initialized) return true;
        else if (!_familiars) return true;

        string stringMessage = message.ToString();
        if (stringMessage.Contains(MAP_ICON_ERROR))
        {
            return false;
        }

        return true;
    }
}
