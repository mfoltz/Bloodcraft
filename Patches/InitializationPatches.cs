using HarmonyLib;
using ProjectM;

namespace Bloodcraft.Patches;

[HarmonyPatch]
[HarmonyPriority(1001)]
internal static class InitializationPatches
{
    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
    [HarmonyPostfix]
    static void SpawnTeamSystemOnUpdatePostfix()
    {
        try
        {
            Core.Initialize();
            if (Core.hasInitialized) Core.Log.LogInfo($"|{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized|");
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] failed to initialize with {e} on try-catch (other mod initializations in SpawnTeamSystem_OnPersistenceLoad.OnUpdate() shouldn't be affected by this)");
        }
    }
}