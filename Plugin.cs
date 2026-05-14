using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodcraft.Services;
using HarmonyLib;
using ScarletRCON.Shared;
using UnityEngine;
using VampireCommandFramework;
using static Bloodcraft.Services.ConfigService.ConfigInitialization;
using static Bloodcraft.Services.DataService.PlayerDataInitialization;

namespace Bloodcraft;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("gg.deca.VampireCommandFramework")]
[BepInDependency("markvaaz.ScarletRCON", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("io.zfolmt.Emberglass", BepInDependency.DependencyFlags.SoftDependency)]
internal class Plugin : BasePlugin
{
    internal static Harmony Harmony { get; set; }
    internal static Harmony BootstrapHarmony { get; set; }

    internal static class MiniBehaviour
    {
        internal static Plugin Instance { get; set; }

        internal static ManualLogSource LogSource { get; set; }

        internal static void OnLoad()
        {
            StartupStateService.Reset();

            if (!IsVRisingServer())
                return;

            Bootstrap.Initialize(BootstrapHarmony, LogSource); // init
            StartupStateService.Mark(StartupState.BootstrapPatched);

            Harmony?.PatchAll();                                // other
            StartupStateService.Mark(StartupState.MainHarmonyPatched);

            OnLoadInternal();                                   // configs, command registration, mod-specific player data, etc.

            if (StartupStateService.IsReady())
                LogSource.LogInfo($"Startup checks passed. {StartupStateService.BuildSummary()}");
            else
                LogSource.LogWarning($"Startup checks failed. {StartupStateService.BuildSummary()}");
        }

        static void OnLoadInternal()
        {
            InitializeConfig();
            StartupStateService.Mark(StartupState.ConfigLoaded);

            LoadPlayerData();
            StartupStateService.Mark(StartupState.PlayerDataLoaded);

            CommandRegistry.RegisterAll();
            StartupStateService.Mark(StartupState.CommandsRegistered);

            RconCommandRegistrar.RegisterAll();
            StartupStateService.Mark(StartupState.RconRegistered);

            LogSource.LogInfo($"Loaded [{MyPluginInfo.PLUGIN_VERSION}]");
        }

        internal static bool OnUnload()
        {
            Harmony?.UnpatchSelf();
            BootstrapHarmony?.UnpatchSelf();

            CommandRegistry.UnregisterAssembly();
            RconCommandRegistrar.UnregisterAssembly();
            StartupStateService.Reset();

            return true;
        }
    }

    static bool IsVRisingServer()
        => Application.productName == "VRisingServer";

    public override void Load()
        => MiniBehaviour.OnLoad();

    public override bool Unload()
        => MiniBehaviour.OnUnload();

    public Plugin()
    {
        Harmony = new(MyPluginInfo.PLUGIN_GUID);
        BootstrapHarmony = new($"{MyPluginInfo.PLUGIN_GUID}.bootstrap");
        MiniBehaviour.Instance = this;
        MiniBehaviour.LogSource = Log;
    }
}

