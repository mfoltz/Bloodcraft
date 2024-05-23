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
        private static ConfigEntry<int> _startingLevel;
        private static ConfigEntry<int> _unitLevelingMultiplier;
        private static ConfigEntry<int> _vBloodLevelingMultiplier;
        private static ConfigEntry<int> _groupLevelingMultiplier;

        private static ConfigEntry<bool> _expertiseSystem;
        private static ConfigEntry<bool> _sanguimancy;
        private static ConfigEntry<int> _firstSlot;
        private static ConfigEntry<int> _secondSlot;
        private static ConfigEntry<int> _maxExpertiseLevel;
        private static ConfigEntry<int> _unitExpertiseMultiplier;
        private static ConfigEntry<int> _vBloodExpertiseMultiplier;
        private static ConfigEntry<int> _MaxStatChoices;
        private static ConfigEntry<int> _ResetStatsItem;
        private static ConfigEntry<int> _ResetStatsItemQuantity;

        private static ConfigEntry<float> _physicalPower;
        private static ConfigEntry<float> _spellPower;
        private static ConfigEntry<float> _physicalCritChance;
        private static ConfigEntry<float> _physicalCritDamage;
        private static ConfigEntry<float> _spellCritChance;
        private static ConfigEntry<float> _spellCritDamage;

        private static ConfigEntry<bool> _bloodSystem;
        private static ConfigEntry<int> _maxBloodLevel;
        private static ConfigEntry<int> _unitLegacyMultiplier;
        private static ConfigEntry<int> _vBloodLegacyMultipler;

        private static ConfigEntry<bool> _professionSystem;
        private static ConfigEntry<int> _maxProfessionLevel;
        private static ConfigEntry<int> _professionMultiplier;

        public static ConfigEntry<bool> LevelingSystem => _levelingSystem;
        public static ConfigEntry<int> MaxPlayerLevel => _maxPlayerLevel;
        public static ConfigEntry<int> StartingLevel => _startingLevel;
        public static ConfigEntry<int> UnitLevelingMultiplier => _unitLevelingMultiplier;
        public static ConfigEntry<int> VBloodLevelingMultiplier => _vBloodLevelingMultiplier;
        public static ConfigEntry<int> GroupLevelingMultiplier => _groupLevelingMultiplier;

        public static ConfigEntry<bool> ExpertiseSystem => _expertiseSystem;
        public static ConfigEntry<bool> Sanguimancy => _sanguimancy;
        public static ConfigEntry<int> FirstSlot => _firstSlot;
        public static ConfigEntry<int> SecondSlot => _secondSlot;
        public static ConfigEntry<int> MaxExpertiseLevel => _maxExpertiseLevel;
        public static ConfigEntry<int> UnitExpertiseMultiplier => _unitExpertiseMultiplier;
        public static ConfigEntry<int> VBloodExpertiseMultiplier => _vBloodExpertiseMultiplier;
        public static ConfigEntry<int> MaxStatChoices => _MaxStatChoices;
        public static ConfigEntry<int> ResetStatsItem => _ResetStatsItem;
        public static ConfigEntry<int> ResetStatsItemQuantity => _ResetStatsItemQuantity;

        public static ConfigEntry<float> PhysicalPower => _physicalPower;
        public static ConfigEntry<float> SpellPower => _spellPower;
        public static ConfigEntry<float> PhysicalCritChance => _physicalCritChance;
        public static ConfigEntry<float> PhysicalCritDamage => _physicalCritDamage;
        public static ConfigEntry<float> SpellCritChance => _spellCritChance;
        public static ConfigEntry<float> SpellCritDamage => _spellCritDamage;

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
            Core.Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME}[{MyPluginInfo.PLUGIN_VERSION}] loaded!");
        }

        static void InitConfig()
        {
            CreateDirectories(ConfigPath);
            CreateDirectories(PlayerExperiencePath);
            CreateDirectories(PlayerExpertisePath);
            CreateDirectories(PlayerBloodPath);
            CreateDirectories(PlayerProfessionPath);

            _levelingSystem = Instance.Config.Bind("Config", "LevelingSystem", false, "Enable or disable the leveling system.");
            _maxPlayerLevel = Instance.Config.Bind("Config", "MaxLevel", 90, "The maximum level a player can reach.");
            _startingLevel = Instance.Config.Bind("Config", "StartingLevel", 0, "Starting level for players if no data is found.");
            _unitLevelingMultiplier = Instance.Config.Bind("Config", "UnitLevelingMultiplier", 5, "The multiplier for experience gained from units.");
            _vBloodLevelingMultiplier = Instance.Config.Bind("Config", "VBloodLevelingMultiplier", 15, "The multiplier for experience gained from VBloods.");
            _groupLevelingMultiplier = Instance.Config.Bind("Config", "GroupLevelingMultiplier", 1, "The multiplier for experience gained from group kills.");

            _expertiseSystem = Instance.Config.Bind("Config", "ExpertiseSystem", false, "Enable or disable the expertise system.");
            _sanguimancy = Instance.Config.Bind("Config", "Sanguimancy", false, "Enable or disable sanguimancy (extra spells for unarmed expertise).");
            _firstSlot = Instance.Config.Bind("Config", "FirstSlot", 25, "Level of sanguimancy required for first slot unlock.");
            _secondSlot = Instance.Config.Bind("Config", "SecondSlot", 50, "Level of sanguimancy required for second slot unlock.");
            _maxExpertiseLevel = Instance.Config.Bind("Config", "MaxExpertiseLevel", 99, "The maximum level a player can reach in weapon expertise.");
            _unitExpertiseMultiplier = Instance.Config.Bind("Config", "UnitExpertiseMultiplier", 5, "The multiplier for expertise gained from units.");
            _vBloodExpertiseMultiplier = Instance.Config.Bind("Config", "VBloodExpertiseMultiplier", 15, "The multiplier for expertise gained from VBloods.");
            _MaxStatChoices = Instance.Config.Bind("Config", "MaxStatChoices", 2, "The maximum number of stat choices a player can pick for a weapon expertise.");
            _ResetStatsItem = Instance.Config.Bind("Config", "ResetStatsItem", 0, "Item PrefabGUID cost for resetting weapon stats.");
            _ResetStatsItemQuantity = Instance.Config.Bind("Config", "ResetStatsItemQuantity", 0, "Quantity of item required for resetting stats.");

            _physicalPower = Instance.Config.Bind("Config", "PhysicalPower", 15f, "The base cap for physical power.");
            _spellPower = Instance.Config.Bind("Config", "SpellPower", 15f, "The base cap for spell power.");
            _physicalCritChance = Instance.Config.Bind("Config", "PhysicalCritChance", 0.15f, "The base cap for physical critical strike chance.");
            _physicalCritDamage = Instance.Config.Bind("Config", "PhysicalCritDamage", 0.75f, "The base cap for physical critical strike damage.");
            _spellCritChance = Instance.Config.Bind("Config", "SpellCritChance", 0.15f, "The base cap for spell critical strike chance.");
            _spellCritDamage = Instance.Config.Bind("Config", "SpellCritDamage", 0.75f, "The base cap for spell critical strike damage.");

            _bloodSystem = Instance.Config.Bind("Config", "BloodSystem", false, "Enable or disable the blood legacy system.");
            _maxBloodLevel = Instance.Config.Bind("Config", "MaxBloodLevel", 99, "The maximum level a player can reach in blood legacies.");
            _unitLegacyMultiplier = Instance.Config.Bind("Config", "UnitLegacyMultiplier", 5, "The multiplier for lineage gained from units.");
            _vBloodLegacyMultipler = Instance.Config.Bind("Config", "VBloodLegacyMultipler", 15, "The multiplier for lineage gained from VBloods.");

            _professionSystem = Instance.Config.Bind("Config", "ProfessionSystem", false, "Enable or disable the profession system.");
            _maxProfessionLevel = Instance.Config.Bind("Config", "MaxProfessionLevel", 99, "The maximum level a player can reach in professions.");
            _professionMultiplier = Instance.Config.Bind("Config", "ProfessionMultiplier", 10, "The multiplier for profession experience gained.");

            // Initialize configuration settings
        }
        static void CreateDirectories(string path)
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
        static void LoadAllData()
        {
            Core.DataStructures.LoadPlayerBools();
            if (LevelingSystem.Value)
            {
                foreach (var loadFunction in loadLeveling)
                {
                    loadFunction();
                }
            }
            if (ExpertiseSystem.Value)
            {
                foreach (var loadFunction in loadExpertises)
                {
                    loadFunction();
                }
                if (Sanguimancy.Value)
                {
                    foreach (var loadFunction in loadSanguimancy)
                    {
                        loadFunction();
                    }
                }
            }
            if (BloodSystem.Value)
            {
                foreach (var loadFunction in loadLegacies)
                {
                    loadFunction();
                }
            }
            if (ProfessionSystem.Value)
            {
                foreach (var loadFunction in loadProfessions)
                {
                    loadFunction();
                }
            }
        }

        static readonly Action[] loadLeveling =
        [
            Core.DataStructures.LoadPlayerExperience,
        ];

        static readonly Action[] loadExpertises =
        [
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
        ];

        static readonly Action[] loadSanguimancy =
        [
            Core.DataStructures.LoadPlayerSanguimancy,
            Core.DataStructures.LoadPlayerSanguimancySpells
        ];

        static readonly Action[] loadLegacies =
        [
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

        static readonly Action[] loadProfessions =
        [
            Core.DataStructures.LoadPlayerWoodcutting,
            Core.DataStructures.LoadPlayerMining,
            Core.DataStructures.LoadPlayerFishing,
            Core.DataStructures.LoadPlayerBlacksmithing,
            Core.DataStructures.LoadPlayerTailoring,
            Core.DataStructures.LoadPlayerJewelcrafting,
            Core.DataStructures.LoadPlayerAlchemy,
            Core.DataStructures.LoadPlayerHarvesting,
        ];
    }
}