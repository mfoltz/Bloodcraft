using HarmonyLib;
using ProjectM;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class InitializationPatch
{
    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix()
    {
        try
        {
            Core.Initialize();

            if (Core._initialized)
            {
                Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized!");
                Plugin.Harmony.Unpatch(typeof(SpawnTeamSystem_OnPersistenceLoad).GetMethod("OnUpdate"), typeof(InitializationPatch).GetMethod("OnUpdatePostfix"));
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to initialize, exiting on try-catch: {ex}");
        }
    }
}