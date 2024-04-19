using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using Steamworks;
using System.Reflection;
using System.Text.Json;
using Unity.Entities;
using VampireCommandFramework;
using VCreate.Core.Toolbox;
using VCreate.Hooks;
using VCreate.Systems;
using VRising.GameData;

namespace VCreate.Core
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
        public static readonly string PlayerSettingsJson = Path.Combine(Plugin.ConfigPath, "player_settings.json");
        public static readonly string PetDataJson = Path.Combine(Plugin.ConfigPath, "pet_data.json");
        public static readonly string UnlockedPetsJson = Path.Combine(Plugin.ConfigPath, "unlocked_pets.json");
        public static readonly string PetBuffMapJson = Path.Combine(Plugin.ConfigPath, "pet_buff_map.json");

        public override void Load()
        {
            Instance = this;
            Logger = base.Log;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            CommandRegistry.RegisterAll();
            InitConfig();
            ServerEvents.OnGameDataInitialized += GameDataOnInitialize;
            GameData.OnInitialize += GameDataOnInitialize;
            LoadData();
            Plugin.Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
            var keys = DataStructures.PlayerPetsMap.Keys;
        }

        private void GameDataOnInitialize(World world)
        {
            // modify faction targets here
            //ModifyGameData();
        }
        private static void ModifyGameData()
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
            VCreate.Core.DataStructures.SavePlayerSettings();
            return true;
        }

        public void OnGameInitialized()
        {
            CastleTerritoryCache.Initialize();
            Plugin.Logger.LogInfo("TerritoryCache loaded");
        }

        public static void LoadData()
        {
            LoadPlayerSettings();
            LoadPetData();
            LoadUnlockedPets();
            LoadPetBuffMap();
        }

        public static void LoadPlayerSettings()
        {
            if (!File.Exists(Plugin.PlayerSettingsJson))
            {
                var stream = File.Create(Plugin.PlayerSettingsJson);
                stream.Dispose();
            }

            string json = File.ReadAllText(Plugin.PlayerSettingsJson);
            //Plugin.Logger.LogWarning($"PlayerSettings found: {json}");
            try
            {
                var settings = JsonSerializer.Deserialize<Dictionary<ulong, Omnitool>>(json);
                VCreate.Core.DataStructures.PlayerSettings = settings ?? [];
                Plugin.Logger.LogWarning("PlayerSettings Populated");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogInfo($"No data to deserialize yet: {ex}");
                VCreate.Core.DataStructures.PlayerSettings = [];
                Plugin.Logger.LogWarning("PlayerSettings Created");
            }
        }

        public static void LoadPetData()
        {
            if (!File.Exists(Plugin.PetDataJson))
            {
                var stream = File.Create(Plugin.PetDataJson);
                stream.Dispose();
            }

            string json = File.ReadAllText(Plugin.PetDataJson);
            //Plugin.Logger.LogWarning($"PetData found: {json}");
            try
            {
                var settings = JsonSerializer.Deserialize<Dictionary<ulong, Dictionary<string, PetExperienceProfile>>>(json);
                VCreate.Core.DataStructures.PlayerPetsMap = settings ?? [];
                Plugin.Logger.LogWarning("PetData Populated");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogInfo($"No data to deserialize yet: {ex}");
                VCreate.Core.DataStructures.PlayerPetsMap = [];
                Plugin.Logger.LogWarning("PetData Created");
            }
        }

        public static void LoadUnlockedPets()
        {
            if (!File.Exists(Plugin.UnlockedPetsJson))
            {
                var stream = File.Create(Plugin.UnlockedPetsJson);
                stream.Dispose();
            }

            string json = File.ReadAllText(Plugin.UnlockedPetsJson);
            //Plugin.Logger.LogWarning($"UnlockedPets found: {json}");
            try
            {
                var settings = JsonSerializer.Deserialize<Dictionary<ulong, List<int>>>(json);
                VCreate.Core.DataStructures.UnlockedPets = settings ?? [];
                Plugin.Logger.LogWarning("UnlockedPets Populated");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogInfo($"No data to deserialize yet: {ex}");
                VCreate.Core.DataStructures.UnlockedPets = [];
                Plugin.Logger.LogWarning("UnlockedPets Created");
            }
        }

        public static void LoadPetBuffMap()
        {
            if (!File.Exists(Plugin.PetBuffMapJson))
            {
                var stream = File.Create(Plugin.PetBuffMapJson);
                stream.Dispose();
            }

            string json = File.ReadAllText(Plugin.PetBuffMapJson);
            //Plugin.Logger.LogWarning($"PetBuffMap found: {json}");
            try
            {
                var settings = JsonSerializer.Deserialize<Dictionary<ulong, Dictionary<int, Dictionary<string, HashSet<int>>>>>(json);
                VCreate.Core.DataStructures.PetBuffMap = settings ?? [];
                Plugin.Logger.LogWarning("PetBuffMap Populated");
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogInfo($"No data to deserialize yet: {ex}");
                VCreate.Core.DataStructures.PetBuffMap = [];
                Plugin.Logger.LogWarning("PetBuffMap Created");
            }
        }
    }
}
