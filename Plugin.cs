using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using VampireCommandFramework;
using static Bloodcraft.Services.ConfigService.ConfigInitialization;
using static Bloodcraft.Services.DataService.PlayerDataInitialization;

namespace Bloodcraft;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
internal class Plugin : BasePlugin
{
    Harmony _harmony;
    internal static Plugin Instance { get; set; }
    public static Harmony Harmony => Instance._harmony;
    internal static ManualLogSource LogInstance => Instance.Log;
    public override void Load()
    {
        Instance = this;

        if (Application.productName != "VRisingServer")
        {
            LogInstance.LogInfo("Bloodcraft is a server mod and will not continue loading on the client; this is not an error, and likely just means you're using ServerLaunchFix in which case you may disregard this");

            return;
        }

        // Console.OutputEncoding = System.Text.Encoding.UTF8;
        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

        InitializeConfig();
        LoadPlayerData();
        CommandRegistry.RegisterAll();

        Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded!");
    }
    public override bool Unload()
    {
        Config.Clear();
        _harmony.UnpatchSelf();

        return true;
    }
}

