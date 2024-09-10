using HarmonyLib;
using Unity.Scenes;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class InitializationPatch
{
    [HarmonyPatch(typeof(SceneSystem), nameof(SceneSystem.ShutdownStreamingSupport))]
    [HarmonyPostfix]
    static void ShutdownStreamingSupportPostfix()
    {
        try
        {
            Core.Initialize();
            if (Core.hasInitialized)
            {
                Core.Log.LogInfo($"|{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized|");
                Plugin.Harmony.Unpatch(typeof(SceneSystem).GetMethod("ShutdownStreamingSupport"), typeof(InitializationPatch).GetMethod("ShutdownStreamingSupportPostfix"));
            }
        }
        catch
        {
            Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to initialize, exiting on try-catch...");
        }
    }
}