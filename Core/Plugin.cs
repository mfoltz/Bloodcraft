using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using Cobalt.Hooks;
using HarmonyLib;
using ProjectM;
using System.Reflection;
using Unity.Entities;
using VampireCommandFramework;

namespace Cobalt.Core
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("gg.deca.Bloodstone")]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    public class Plugin : BasePlugin, IRunOnInitialized
    {
        private Harmony _harmony;
        internal static Plugin Instance { get; private set; }

        private static ManualLogSource Logger;
        public new static ManualLogSource Log => Logger;

        public static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
        public static readonly string PlayerExperienceJson = Path.Combine(Plugin.ConfigPath, "player_experience.json");
        public static readonly string PlayerBoolsJson = Path.Combine(Plugin.ConfigPath, "player_bools.json");
        public static readonly string PlayerWoodcuttingJson = Path.Combine(Plugin.ConfigPath, "player_woodcutting.json");
        public static readonly string PlayerMiningJson = Path.Combine(Plugin.ConfigPath, "player_mining.json");
        public static readonly string PlayerFishingJson = Path.Combine(Plugin.ConfigPath, "player_fishing.json");

        public override void Load()
        {
            Instance = this;
            Logger = base.Log;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            CommandRegistry.RegisterAll();
            InitConfig();
            ServerEventsPatches.OnGameDataInitialized += GameDataOnInitialize;
            LoadAllData();
            //UpdateStats();
            Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
        }

        private void GameDataOnInitialize(World world)
        {
        }

       

      

        private static void InitConfig()
        {
            // Initialize configuration settings
            if (!Directory.Exists(ConfigPath))
            {
                Directory.CreateDirectory(ConfigPath);
            }
        }

        public override bool Unload()
        {
            Config.Clear();
            _harmony.UnpatchSelf();
            SaveAllData();  // Call a method that saves all data types.
            return true;
        }

        private static void SaveAllData()
        {
            DataStructures.SavePlayerExperience();
            DataStructures.SavePlayerBools();
            DataStructures.SavePlayerWoodcutting();
            DataStructures.SavePlayerMining();
            DataStructures.SavePlayerFishing();
        }

        private static void LoadAllData()
        {
            DataStructures.LoadPlayerExperience();
            DataStructures.LoadPlayerBools();
            DataStructures.LoadPlayerWoodcutting();
            DataStructures.LoadPlayerMining();
            DataStructures.LoadPlayerFishing();
        }

        public void OnGameInitialized()
        {
        }
    }
}