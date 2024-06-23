using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.WarEvents;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class InitializationPatch
{
    /*
    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix()
    {
        Core.Log.LogInfo($"SpawnTeamSystem_OnPersistenceLoad.OnUpdate() Postfix...");
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
    */

    [HarmonyPatch(typeof(WarEventRegistrySystem), nameof(WarEventRegistrySystem.RegisterWarEventEntities))]
    [HarmonyPostfix]
    static void Postfix()
    {
        if (!Core.hasInitialized) Core.Initialize();
        if (Core.hasInitialized)
        {
            Core.Log.LogInfo($"|{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] initialized|");
        }
    }
}