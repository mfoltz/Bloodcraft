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

        private static ConfigEntry<bool> _levelingSystem;
        private static ConfigEntry<int> _maxPlayerLevel;
        private static ConfigEntry<int> _unitLevelingMultiplier;
        private static ConfigEntry<int> _vBloodLevelingMultiplier;
        private static ConfigEntry<int> _groupLevelingMultiplier;

        private static ConfigEntry<bool> _expertiseSystem;
        private static ConfigEntry<bool> _sanguimancy;
        private static ConfigEntry<int> _maxExpertiseLevel;
        private static ConfigEntry<int> _unitExpertiseMultiplier;
        private static ConfigEntry<int> _vBloodExpertiseMultiplier;

        private static ConfigEntry<bool> _bloodSystem;
        private static ConfigEntry<int> _maxBloodLevel;
        private static ConfigEntry<int> _unitLegacyMultiplier;
        private static ConfigEntry<int> _vBloodLegacyMultipler;

        private static ConfigEntry<bool> _professionSystem;
        private static ConfigEntry<int> _maxProfessionLevel;
        private static ConfigEntry<int> _professionMultiplier;

        public static ConfigEntry<bool> LevelingSystem => _levelingSystem;
        public static ConfigEntry<int> MaxPlayerLevel => _maxPlayerLevel;
        public static ConfigEntry<int> UnitLevelingMultiplier => _unitLevelingMultiplier;
        public static ConfigEntry<int> VBloodLevelingMultiplier => _vBloodLevelingMultiplier;
        public static ConfigEntry<int> GroupLevelingMultiplier => _groupLevelingMultiplier;

        public static ConfigEntry<bool> ExpertiseSystem => _expertiseSystem;
        public static ConfigEntry<bool> Sanguimancy => _sanguimancy;
        public static ConfigEntry<int> MaxExpertiseLevel => _maxExpertiseLevel;
        public static ConfigEntry<int> UnitExpertiseMultiplier => _unitExpertiseMultiplier;
        public static ConfigEntry<int> VBloodExpertiseMultiplier => _vBloodExpertiseMultiplier;

        public static ConfigEntry<bool> BloodSystem => _bloodSystem;
        public static ConfigEntry<int> MaxBloodLevel => _maxBloodLevel;
        public static ConfigEntry<int> UnitLegacyMultiplier => _unitLegacyMultiplier;
        public static ConfigEntry<int> VBloodLegacyMultipler => _vBloodLegacyMultipler;

        public static ConfigEntry<bool> ProfessionSystem => _professionSystem;
        public static ConfigEntry<int> MaxProfessionLevel => _maxProfessionLevel;
        public static ConfigEntry<int> ProfessionMultiplier => _professionMultiplier;

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

            _levelingSystem = Instance.Config.Bind("Config", "LevelingSystem", false, "Enable or disable the leveling system.");
            _maxPlayerLevel = Instance.Config.Bind("Config", "MaxLevel", 90, "The maximum level a player can reach.");
            _unitLevelingMultiplier = Instance.Config.Bind("Config", "UnitLevelingMultiplier", 5, "The multiplier for experience gained from units.");
            _vBloodLevelingMultiplier = Instance.Config.Bind("Config", "VBloodLevelingMultiplier", 15, "The multiplier for experience gained from VBloods.");
            _groupLevelingMultiplier = Instance.Config.Bind("Config", "GroupLevelingMultiplier", 1, "The multiplier for experience gained from group kills.");

            _expertiseSystem = Instance.Config.Bind("Config", "ExpertiseSystem", false, "Enable or disable the expertise system.");
            _sanguimancy = Instance.Config.Bind("Config", "Sanguimancy", false, "Enable or disable sanguimancy.");
            _maxExpertiseLevel = Instance.Config.Bind("Config", "MaxExpertiseLevel", 99, "The maximum level a player can reach in weapon expertise.");
            _unitExpertiseMultiplier = Instance.Config.Bind("Config", "UnitExpertiseMultiplier", 5, "The multiplier for expertise gained from units.");
            _vBloodExpertiseMultiplier = Instance.Config.Bind("Config", "VBloodExpertiseMultiplier", 15, "The multiplier for expertise gained from VBloods.");

            _bloodSystem = Instance.Config.Bind("Config", "BloodSystem", false, "Enable or disable the blood legacy system.");
            _maxBloodLevel = Instance.Config.Bind("Config", "MaxBloodLevel", 99, "The maximum level a player can reach in blood legacies.");
            _unitLegacyMultiplier = Instance.Config.Bind("Config", "UnitLegacyMultiplier", 5, "The multiplier for lineage gained from units.");
            _vBloodLegacyMultipler = Instance.Config.Bind("Config", "VBloodLegacyMultipler", 15, "The multiplier for lineage gained from VBloods.");

            _professionSystem = Instance.Config.Bind("Config", "ProfessionSystem", false, "Enable or disable the profession system.");
            _maxProfessionLevel = Instance.Config.Bind("Config", "MaxProfessionLevel", 99, "The maximum level a player can reach in professions.");
            _professionMultiplier = Instance.Config.Bind("Config", "ProfessionMultiplier", 10, "The multiplier for profession experience gained.");

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
            Core.DataStructures.LoadPlayerSanguimancySpells,
            Core.DataStructures.LoadPlayerWorkerLegacy,
            Core.DataStructures.LoadPlayerWarriorLegacy,
            Core.DataStructures.LoadPlayerScholarLegacy,
            Core.DataStructures.LoadPlayerRogueLegacy,
            Core.DataStructures.LoadPlayerMutantLegacy,
            Core.DataStructures.LoadPlayerVBloodLegacy,
            Core.DataStructures.LoadPlayerDraculinLegacy,
            Core.DataStructures.LoadPlayerImmortalLegacy,
            Core.DataStructures.LoadPlayerCreatureLegacy,
            Core.DataStructures.LoadPlayerBruteLegacy
        ];
    }
}