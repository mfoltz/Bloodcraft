using BepInEx.Logging;
using Bloodcraft.Services;
using HarmonyLib;
using ProjectM.Gameplay.WarEvents;

namespace Bloodcraft;

[HarmonyPatch(typeof(WarEventRegistrySystem), nameof(WarEventRegistrySystem.RegisterWarEventEntities))]
internal static class Bootstrap
{
    static ManualLogSource _logSource;
    static bool _initialized;

    internal static void Initialize(Harmony harmony, ManualLogSource logSource)
    {
        _logSource = logSource;
        harmony.CreateClassProcessor(typeof(Bootstrap)).Patch();
    }

    [HarmonyPostfix]
    static void Postfix()
    {
        if (_initialized)
            return;

        _initialized = true;

        try
        {
            Core.OnInitialize();
            StartupStateService.Mark(StartupState.BootstrapFired);
            _logSource.LogInfo($"Initialized [{MyPluginInfo.PLUGIN_VERSION}]");
        }
        catch (Exception ex)
        {
            _logSource.LogError($"{ex}");
        }
    }
}
