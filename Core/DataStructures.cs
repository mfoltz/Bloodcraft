using ProjectM;
using System.Text.Json;
using static Cobalt.Systems.Bloodline.BloodStatsSystem;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;

namespace Cobalt.Core
{
    public class DataStructures
    {
        // Encapsulated fields with properties

        private static readonly JsonSerializerOptions prettyJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        // structures to write to json for permanence

        private static Dictionary<ulong, KeyValuePair<int, float>> playerExperience = [];
        private static Dictionary<ulong, Dictionary<string, bool>> playerBools = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerWoodcutting = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerMining = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerFishing = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerBlacksmithing = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerTailoring = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerMastery = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerBloodline = [];
        private static Dictionary<ulong, PlayerWeaponStats> playerWeaponStats = [];
        private static Dictionary<ulong, PlayerBloodlineStats> playerBloodlineStats = [];

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerExperience
        {
            get => playerExperience;
            set => playerExperience = value;
        }

        public static Dictionary<ulong, Dictionary<string, bool>> PlayerBools
        {
            get => playerBools;
            set => playerBools = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerWoodcutting
        {
            get => playerWoodcutting;
            set => playerWoodcutting = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerMining
        {
            get => playerMining;
            set => playerMining = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerFishing
        {
            get => playerFishing;
            set => playerFishing = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerBlacksmithing
        {
            get => playerBlacksmithing;
            set => playerBlacksmithing = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerTailoring
        {
            get => playerTailoring;
            set => playerTailoring = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerCombatMastery
        {
            get => playerMastery;
            set => playerMastery = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerBloodMastery
        {
            get => playerBloodline;
            set => playerBloodline = value;
        }

        public static Dictionary<ulong, PlayerWeaponStats> PlayerWeaponStats
        {
            get => playerWeaponStats;
            set => playerWeaponStats = value;
        }

        public static Dictionary<ulong, PlayerBloodlineStats> PlayerBloodStats
        {
            get => playerBloodlineStats;
            set => playerBloodlineStats = value;
        }

        // cache-only
        private static Dictionary<ulong, Dictionary<PrefabGUID, bool>> playerCraftingJobs = [];

        public static Dictionary<ulong, Dictionary<PrefabGUID, bool>> PlayerCraftingJobs
        {
            get => playerCraftingJobs;
            set => playerCraftingJobs = value;
        }

        // file paths dictionary
        private static readonly Dictionary<string, string> filePaths = new()
        {
            {"Experience", Plugin.PlayerExperienceJson},
            {"PlayerBools", Plugin.PlayerBoolsJson},
            {"Woodcutting", Plugin.PlayerWoodcuttingJson},
            {"Mining", Plugin.PlayerMiningJson},
            {"Fishing", Plugin.PlayerFishingJson},
            {"Blacksmithing", Plugin.PlayerBlacksmithingJson},
            {"Tailoring", Plugin.PlayerTailoringJson},
            {"CombatMastery", Plugin.PlayerCombatMasteryJson},
            {"BloodMastery", Plugin.PlayerBloodMasteryJson},
            {"WeaponStats", Plugin.PlayerWeaponStatsJson},
            {"BloodStats", Plugin.PlayerBloodStatsJson}
        };

        // Generic method to save any type of dictionary.

        public static void LoadData<T>(ref Dictionary<ulong, T> dataStructure, string key)
        {
            string path = filePaths[key];
            if (!File.Exists(path))
            {
                // If the file does not exist, create a new empty file to avoid errors on initial load.
                File.Create(path).Dispose();
                dataStructure = []; // Initialize as empty if file does not exist.
                Plugin.Log.LogInfo($"{key} file created as it did not exist.");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<Dictionary<ulong, T>>(json, prettyJsonOptions);
                dataStructure = data ?? []; // Ensure non-null assignment.
                Plugin.Log.LogInfo($"{key} data loaded successfully.");
            }
            catch (IOException ex)
            {
                Plugin.Log.LogError($"Error reading {key} data from file: {ex.Message}");
                dataStructure = []; // Provide default empty dictionary on error.
            }
            catch (JsonException ex)
            {
                Plugin.Log.LogError($"JSON deserialization error when loading {key} data: {ex.Message}");
                dataStructure = []; // Provide default empty dictionary on error.
            }
        }

        public static void LoadPlayerExperience() => LoadData(ref playerExperience, "Experience");

        public static void LoadPlayerBools() => LoadData(ref playerBools, "PlayerBools");

        public static void LoadPlayerWoodcutting() => LoadData(ref playerWoodcutting, "Woodcutting");

        public static void LoadPlayerMining() => LoadData(ref playerMining, "Mining");

        public static void LoadPlayerFishing() => LoadData(ref playerFishing, "Fishing");

        public static void LoadPlayerBlacksmithing() => LoadData(ref playerBlacksmithing, "Blacksmithing");

        public static void LoadPlayerTailoring() => LoadData(ref playerTailoring, "Tailoring");

        public static void LoadPlayerCombatMastery() => LoadData(ref playerMastery, "CombatMastery");

        public static void LoadPlayerBloodMastery() => LoadData(ref playerBloodline, "BloodMastery");

        public static void LoadPlayerWeaponStats() => LoadData(ref playerWeaponStats, "WeaponStats");

        public static void LoadPlayerBloodStats() => LoadData(ref playerBloodlineStats, "BloodStats");

        public static void SaveData<T>(Dictionary<ulong, T> data, string key)
        {
            string path = filePaths[key];
            try
            {
                string json = JsonSerializer.Serialize(data, prettyJsonOptions);
                File.WriteAllText(path, json);
                //Plugin.Log.LogInfo($"{key} data saved successfully.");
            }
            catch (IOException ex)
            {
                Plugin.Log.LogError($"Failed to write {key} data to file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Plugin.Log.LogError($"JSON serialization error when saving {key} data: {ex.Message}");
            }
        }

        public static void SavePlayerExperience() => SaveData(PlayerExperience, "Experience");

        public static void SavePlayerBools() => SaveData(PlayerBools, "PlayerBools");

        public static void SavePlayerWoodcutting() => SaveData(PlayerWoodcutting, "Woodcutting");

        public static void SavePlayerMining() => SaveData(PlayerMining, "Mining");

        public static void SavePlayerFishing() => SaveData(PlayerFishing, "Fishing");

        public static void SavePlayerBlacksmithing() => SaveData(PlayerBlacksmithing, "Blacksmithing");

        public static void SavePlayerTailoring() => SaveData(PlayerTailoring, "Tailoring");

        public static void SavePlayerMastery() => SaveData(PlayerCombatMastery, "CombatMastery");

        public static void SavePlayerBloodline() => SaveData(PlayerBloodMastery, "BloodMastery");

        public static void SavePlayerWeaponStats() => SaveData(PlayerWeaponStats, "WeaponStats");

        public static void SavePlayerBloodlineStats() => SaveData(PlayerBloodStats, "BloodStats");
    }
}