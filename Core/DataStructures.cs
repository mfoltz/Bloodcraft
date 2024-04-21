using System.Text.Json;
using static Cobalt.Systems.WeaponStatsSystem;

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

        private static Dictionary<ulong, KeyValuePair<int, float>> playerExperience = [];
        private static Dictionary<ulong, KeyValuePair<int, DateTime>> playerMastery = [];
        private static Dictionary<ulong, Dictionary<string, bool>> playerBools = [];
        private static Dictionary<ulong, PlayerStats> playerStats = [];

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

        public static Dictionary<ulong, KeyValuePair<int, DateTime>> PlayerMastery
        {
            get => playerMastery;
            set => playerMastery = value;
        }

        public static Dictionary<ulong, PlayerStats> PlayerStats
        {
            get => playerStats;
            set => playerStats = value;
        }

        private static readonly Dictionary<string, string> filePaths = new()
        {
            {"Experience", Plugin.PlayerExperienceJson},
            {"Mastery", Plugin.PlayerMasteryJson},
            {"Bools", Plugin.PlayerBoolsJson},
            {"Stats", Plugin.PlayerStatsJson}
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

        public static void LoadPlayerMastery() => LoadData(ref playerMastery, "Mastery");

        public static void LoadPlayerBools() => LoadData(ref playerBools, "Bools");

        public static void LoadPlayerStats() => LoadData(ref playerStats, "Stats");

        public static void SaveData<T>(Dictionary<ulong, T> data, string key)
        {
            string path = filePaths[key];
            try
            {
                string json = JsonSerializer.Serialize(data, prettyJsonOptions);
                File.WriteAllText(path, json);
                Plugin.Log.LogInfo($"{key} data saved successfully.");
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

        public static void SavePlayerMastery() => SaveData(PlayerMastery, "Mastery");

        public static void SavePlayerBools() => SaveData(PlayerBools, "Bools");

        public static void SavePlayerStats() => SaveData(PlayerStats, "Stats");
    }
}