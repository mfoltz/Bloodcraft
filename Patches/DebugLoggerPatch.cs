using Bloodcraft.Services;
using HarmonyLib;
using UnityEngine;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DebugLoggerPatch
{
    static bool Familiars { get; } = ConfigService.FamiliarSystem;
    const string MAP_ICON_ERROR = "PlayerMapIcon requires the creator to have the PlayerCharacter component.";

    [HarmonyPatch(typeof(Debug), nameof(Debug.LogError), new Type[] { typeof(Il2CppSystem.Object) })]   // don't use preview features here (collection initialization) or GitHub build workflow gets mad
    [HarmonyPrefix]                                                                                     // this patch is to prevent log spam from player map icons on familiars
    static bool LogErrorPrefix(Il2CppSystem.Object message)
    {
        if (!Core.IsReady) return true;
        if (!Familiars) return true;

        string stringMessage = message.ToString();
        return !stringMessage.Contains(MAP_ICON_ERROR);
    }
}
