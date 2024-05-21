using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Reflection;
using VampireCommandFramework;

namespace Bloodcraft
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        private Harmony _harmony;
        internal static Plugin Instance { get; private set; }
        public static ManualLogSource LogInstance => Instance.Log;

        public static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
        public static readonly string PlayerExperiencePath = Path.Combine(ConfigPath, "ExperienceLeveling");
        public static readonly string PlayerExpertisePath = Path.Combine(ConfigPath, "WeaponExpertise");
        public static readonly string PlayerBloodPath = Path.Combine(ConfigPath, "BloodLegacies");
        public static readonly string PlayerProfessionPath = Path.Combine(ConfigPath, "Professions");

        public static ConfigEntry<bool> LevelingSystem;
        public static ConfigEntry<int> MaxPlayerLevel;
        public static ConfigEntry<int> UnitLevelingMultiplier;
        public static ConfigEntry<int> VBloodLevelingMultiplier;
        public static ConfigEntry<int> GroupLevelingMultiplier;

        public static ConfigEntry<bool> ExpertiseSystem;
        public static ConfigEntry<bool> Sanguimancy;
        public static ConfigEntry<int> MaxExpertiseLevel;
        public static ConfigEntry<int> UnitExpertiseMultiplier;
        public static ConfigEntry<int> VBloodExpertiseMultiplier;

        public static ConfigEntry<bool> BloodSystem;
        public static ConfigEntry<int> MaxBloodLevel;
        public static ConfigEntry<int> UnitLegacyMultiplier;
        public static ConfigEntry<int> VBloodLegacyMultipler;

        public static ConfigEntry<bool> ProfessionSystem;
        public static ConfigEntry<int> MaxProfessionLevel;
        public static ConfigEntry<int> ProfessionMultiplier;

        public override void Load()
        {
            Instance = this;

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
            InitConfig();
            CommandRegistry.RegisterAll();
            LoadAllData();
            Core.Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_NAME} is loaded!");
        }

        private static void InitConfig()
        {
            CreateDirectories(ConfigPath);
            CreateDirectories(PlayerExperiencePath);
            CreateDirectories(PlayerExpertisePath);
            CreateDirectories(PlayerBloodPath);
            CreateDirectories(PlayerProfessionPath);

            LevelingSystem = Instance.Config.Bind("Config", "LevelingSystem", false, "Enable or disable the leveling system.");
            MaxPlayerLevel = Instance.Config.Bind("Config", "MaxLevel", 90, "The maximum level a player can reach.");
            UnitLevelingMultiplier = Instance.Config.Bind("Config", "UnitLevelingMultiplier", 5, "The multiplier for experience gained from units.");
            VBloodLevelingMultiplier = Instance.Config.Bind("Config", "VBloodLevelingMultiplier", 15, "The multiplier for experience gained from VBlood.");
            GroupLevelingMultiplier = Instance.Config.Bind("Config", "GroupLevelingMultiplier", 1, "The multiplier for experience gained from group kills.");

            ExpertiseSystem = Instance.Config.Bind("Config", "ExpertiseSystem", false, "Enable or disable the expertise system.");
            Sanguimancy = Instance.Config.Bind("Config", "Sanguimancy", false, "Enable or disable sanguimancy.");
            MaxExpertiseLevel = Instance.Config.Bind("Config", "MaxExpertiseLevel", 99, "The maximum level a player can reach in expertise.");
            UnitExpertiseMultiplier = Instance.Config.Bind("Config", "UnitExpertiseMultiplier", 5, "The multiplier for expertise gained from units.");
            VBloodExpertiseMultiplier = Instance.Config.Bind("Config", "VBloodExpertiseMultiplier", 15, "The multiplier for expertise gained from VBlood.");

            BloodSystem = Instance.Config.Bind("Config", "BloodSystem", false, "Enable or disable the blood system.");
            MaxBloodLevel = Instance.Config.Bind("Config", "MaxBloodLevel", 99, "The maximum level a player can reach in sanguimancy.");
            UnitLegacyMultiplier = Instance.Config.Bind("Config", "UnitLegacyMultiplier", 5, "The multiplier for blood stats gained from units.");
            VBloodLegacyMultipler = Instance.Config.Bind("Config", "VBloodLegacyMultipler", 15, "The multiplier for blood stats gained from VBlood.");

            ProfessionSystem = Instance.Config.Bind("Config", "ProfessionSystem", false, "Enable or disable the profession system.");
            MaxProfessionLevel = Instance.Config.Bind("Config", "MaxProfessionLevel", 99, "The maximum level a player can reach in professions.");
            ProfessionMultiplier = Instance.Config.Bind("Config", "ProfessionMultiplier", 10, "The multiplier for profession experience gained.");

            // Initialize configuration settings
        }

        private static void CreateDirectories(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public override bool Unload()
        {
            Config.Clear();
            _harmony.UnpatchSelf();
            return true;
        }

        private static void LoadAllData()
        {
            foreach (var loadFunction in loadFunctions)
            {
                loadFunction();
            }
        }

        private static readonly Action[] loadFunctions =
        [
            Core.DataStructures.LoadPlayerExperience,
            Core.DataStructures.LoadPlayerPrestige,
            Core.DataStructures.LoadPlayerBools,
            Core.DataStructures.LoadPlayerWoodcutting,
            Core.DataStructures.LoadPlayerMining,
            Core.DataStructures.LoadPlayerFishing,
            Core.DataStructures.LoadPlayerBlacksmithing,
            Core.DataStructures.LoadPlayerTailoring,
            Core.DataStructures.LoadPlayerJewelcrafting,
            Core.DataStructures.LoadPlayerAlchemy,
            Core.DataStructures.LoadPlayerHarvesting,
            Core.DataStructures.LoadPlayerSanguimancy,
            Core.DataStructures.LoadPlayerSwordExpertise,
            Core.DataStructures.LoadPlayerAxeExpertise,
            Core.DataStructures.LoadPlayerMaceExpertise,
            Core.DataStructures.LoadPlayerSpearExpertise,
            Core.DataStructures.LoadPlayerCrossbowExpertise,
            Core.DataStructures.LoadPlayerGreatSwordExpertise,
            Core.DataStructures.LoadPlayerSlashersExpertise,
            Core.DataStructures.LoadPlayerPistolsExpertise,
            Core.DataStructures.LoadPlayerReaperExpertise,
            Core.DataStructures.LoadPlayerLongbowExpertise,
            Core.DataStructures.LoadPlayerWhipExpertise,
            Core.DataStructures.LoadPlayerWeaponStats,
            Core.DataStructures.LoadPlayerSanguimancySpells
        ];
    }
}