using System.Text.Json;
using VCreate.Systems;
namespace VCreate.Core
{
    public class DataStructures
    {
        // Encapsulated fields with properties
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = false,
            IncludeFields = true
        };

        private static readonly JsonSerializerOptions prettyJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        private static Dictionary<ulong, Omnitool> playerSettings = [];

        private static Dictionary<ulong, Dictionary<string, PetExperienceProfile>> playerPetsMap = [];

        private static Dictionary<ulong, List<int>> unlockedPets = [];

        private static Dictionary<ulong, Dictionary<int, Dictionary<string, HashSet<int>>>> petBuffMap = [];

        // Property for playerSettings if external access or modification is required
        public static Dictionary<ulong, Omnitool> PlayerSettings
        {
            get => playerSettings;
            set => playerSettings = value;
        }
        public static Dictionary<ulong, Dictionary<string, PetExperienceProfile>> PlayerPetsMap
        {
            get => playerPetsMap;
            set => playerPetsMap = value;
        }
        public static Dictionary<ulong, List<int>> UnlockedPets
        {
            get => unlockedPets;
            set => unlockedPets = value;
        }
        public static Dictionary<ulong, Dictionary<int, Dictionary<string, HashSet<int>>>> PetBuffMap
        {
            get => petBuffMap;
            set => petBuffMap = value;
        }
        public static void SavePlayerSettings()
        {
            try
            {
                //string json = JsonSerializer.Serialize(playerSettings, prettyJsonOptions); // Consider using prettyJsonOptions if you want the output to be indented.
                File.WriteAllText(Plugin.PlayerSettingsJson, JsonSerializer.Serialize(DataStructures.PlayerSettings));
            }
            catch (IOException ex)
            {
                // Handle file write exceptions
                Plugin.Log.LogInfo($"An error occurred saving settings: {ex.Message}");

            }
            catch (JsonException ex)
            {
                // Handle JSON serialization exceptions
                Plugin.Log.LogInfo($"An error occurred during JSON serialization: {ex.Message}");
            }
        }
        public static void SavePetExperience()
        {
            try
            {
                //string json = JsonSerializer.Serialize(playerSettings, prettyJsonOptions); // Consider using prettyJsonOptions if you want the output to be indented.
                File.WriteAllText(Plugin.PetDataJson, JsonSerializer.Serialize(DataStructures.PlayerPetsMap));
            }
            catch (IOException ex)
            {
                // Handle file write exceptions
                Plugin.Log.LogInfo($"An error occurred saving settings: {ex.Message}");

            }
            catch (JsonException ex)
            {
                // Handle JSON serialization exceptions
                Plugin.Log.LogInfo($"An error occurred during JSON serialization: {ex.Message}");
            }
        }
        public static void SaveUnlockedPets()
        {
            try
            {
                //string json = JsonSerializer.Serialize(playerSettings, prettyJsonOptions); // Consider using prettyJsonOptions if you want the output to be indented.
                File.WriteAllText(Plugin.UnlockedPetsJson, JsonSerializer.Serialize(DataStructures.UnlockedPets));
            }
            catch (IOException ex)
            {
                // Handle file write exceptions
                Plugin.Log.LogInfo($"An error occurred saving settings: {ex.Message}");

            }
            catch (JsonException ex)
            {
                // Handle JSON serialization exceptions
                Plugin.Log.LogInfo($"An error occurred during JSON serialization: {ex.Message}");
            }
        }
        public static void SavePetBuffMap()
        {
            try
            {
                //string json = JsonSerializer.Serialize(playerSettings, prettyJsonOptions); // Consider using prettyJsonOptions if you want the output to be indented.
                File.WriteAllText(Plugin.PetBuffMapJson, JsonSerializer.Serialize(DataStructures.PetBuffMap));
            }
            catch (IOException ex)
            {
                // Handle file write exceptions
                Plugin.Log.LogInfo($"An error occurred saving settings: {ex.Message}");

            }
            catch (JsonException ex)
            {
                // Handle JSON serialization exceptions
                Plugin.Log.LogInfo($"An error occurred during JSON serialization: {ex.Message}");
            }
        }

        
    }
}