using HarmonyLib;
using UnityEngine;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class TestPatches
{
    const string LOG_FILTER = "Synced Float";

    [HarmonyPatch(typeof(Logger))]
    [HarmonyPatch("Log")]     
    [HarmonyPatch([typeof(LogType), typeof(Il2CppSystem.Object)])]
    public static class LogPatchParameters
    {
        [HarmonyPrefix]
        static bool LogPrefix(LogType logType, Il2CppSystem.Object message)
        {
            //if (!Core.hasInitialized) return true;

            string logMessage = message.ToString();
            
            if (logMessage.Contains(LOG_FILTER))
            {
                return false;
            }

            return true;
        }
    }
}
