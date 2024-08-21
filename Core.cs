using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Experience;
using Bloodcraft.SystemUtilities.Expertise;
using Bloodcraft.SystemUtilities.Familiars;
using Bloodcraft.SystemUtilities.Legacies;
using Bloodcraft.SystemUtilities.Leveling;
using Bloodcraft.SystemUtilities.Professions;
using Bloodcraft.SystemUtilities.Quests;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Text.Json;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft;
internal static class Core
{
    static World Server { get; } = GetServerWorld() ?? throw new Exception("There is no Server world (yet)...");
    public static EntityManager EntityManager => Server.EntityManager;
    public static ServerGameManager ServerGameManager => SystemService.ServerScriptMapper.GetServerGameManager();
    public static SystemService SystemService { get; } = new(Server);
    public static ConfigService ConfigService { get; } = new();
    public static PlayerService PlayerService { get; } = new();
    public static LocalizationService LocalizationService { get; } = new();
    public static QuestService QuestService { get; } = ConfigService.QuestSystem ? new() : null;
    public static double ServerTime => ServerGameManager.ServerTime;
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour monoBehaviour;
    public static bool hasInitialized;
    public static void Initialize()
    {
        if (hasInitialized) return;
        
        if (ConfigService.FamiliarSystem) Utilities.FamiliarBans();
        if (ConfigService.ExtraRecipes) RecipeUtilities.ExtraRecipes();
        if (ConfigService.QuestSystem) Utilities.QuestRewards();
        if (ConfigService.StarterKit) Utilities.StarterKit();
        
        hasInitialized = true;
    }
    static World GetServerWorld()
    {
        return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (monoBehaviour == null)
        {
            GameObject gameObject = new("Bloodcraft");
            monoBehaviour = gameObject.AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }
        monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
    public static class DataStructures
    {
        // Encapsulated fields with properties

        static readonly JsonSerializerOptions prettyJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        // structures to write to json for permanence

        private static Dictionary<ulong, KeyValuePair<int, float>> playerExperience = [];
        private static Dictionary<ulong, KeyValuePair<DateTime, float>> playerRestedXP = [];
        private static Dictionary<ulong, Dictionary<string, bool>> playerBools = [];
        private static Dictionary<ulong, Dictionary<LevelingSystem.PlayerClasses, (List<int>, List<int>)>> playerClass = [];
        private static Dictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> playerPrestiges = [];

        // professions

        private static Dictionary<ulong, KeyValuePair<int, float>> playerWoodcutting = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerMining = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerFishing = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerBlacksmithing = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerTailoring = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerEnchanting = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerAlchemy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerHarvesting = [];

        // weapon expertise

        private static Dictionary<ulong, KeyValuePair<int, float>> playerSwordExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerAxeExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerMaceExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerSpearExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerCrossbowExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerGreatSwordExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerSlashersExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerPistolsExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerReaperExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerLongbowExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerWhipExpertise = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerFishingPoleExpertise = [];
        private static Dictionary<ulong, Dictionary<WeaponSystem.WeaponType, List<WeaponHandler.WeaponStats.WeaponStatType>>> playerWeaponStats = [];

        private static Dictionary<ulong, KeyValuePair<int, float>> playerUnarmedExpertise = []; // this is unarmed and needs to be renamed to match the rest
        private static Dictionary<ulong, (int FirstUnarmed, int SecondUnarmed, int ClassSpell)> playerSpells = [];

        // blood legacies

        private static Dictionary<ulong, KeyValuePair<int, float>> playerWorkerLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerWarriorLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerScholarLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerRogueLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerMutantLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerVBloodLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerDraculinLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerImmortalLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerCreatureLegacy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerBruteLegacy = [];
        private static Dictionary<ulong, Dictionary<BloodSystem.BloodType, List<BloodHandler.BloodStats.BloodStatType>>> playerBloodStats = [];

        // familiar data

        private static Dictionary<ulong, UnlockedFamiliarData> unlockedFamiliars = [];
        private static Dictionary<ulong, (Entity Familiar, int FamKey)> familiarActives = [];
        private static Dictionary<ulong, string> familiarSet = [];
        private static Dictionary<ulong, int> familiarChoice = [];
        private static Dictionary<ulong, FamiliarExperienceData> familiarExperience = [];
        private static Dictionary<ulong, FamiliarPrestigeData> familiarPrestiges = [];
        private static Dictionary<ulong, FamiliarBuffsData> familiarBuffs = [];

        // quest data
        private static Dictionary<ulong, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)>> playerQuests = [];

        [Serializable]
        public class FamiliarExperienceData
        {
            public Dictionary<int, KeyValuePair<int, float>> FamiliarExperience { get; set; } = [];
        }

        [Serializable]
        public class FamiliarPrestigeData
        {
            public Dictionary<int, KeyValuePair<int, List<FamiliarSummonSystem.FamiliarStatType>>> FamiliarPrestige { get; set; } = [];
        }

        [Serializable]
        public class UnlockedFamiliarData
        {
            public Dictionary<string, List<int>> UnlockedFamiliars { get; set; } = [];
        }

        [Serializable]
        public class FamiliarBuffsData
        {
            public Dictionary<int, List<int>> FamiliarBuffs { get; set; } = [];
        }

        public static Dictionary<ulong, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)>> PlayerQuests
        {
            get => playerQuests;
            set => playerQuests = value;
        }

        public static Dictionary<ulong, (Entity Familiar, int FamKey)> FamiliarActives
        {
            get => familiarActives;
            set => familiarActives = value;
        }
        public static Dictionary<ulong, string> FamiliarSet
        {
            get => familiarSet;
            set => familiarSet = value;
        }
        public static Dictionary<ulong, int> FamiliarChoice
        {
            get => familiarChoice;
            set => familiarChoice = value;
        }
        public static Dictionary<ulong, FamiliarBuffsData> FamiliarBuffs
        {
            get => familiarBuffs;
            set => familiarBuffs = value;
        }
        public static Dictionary<ulong, UnlockedFamiliarData> UnlockedFamiliars
        {
            get => unlockedFamiliars;
            set => unlockedFamiliars = value;
        }
        public static Dictionary<ulong, FamiliarExperienceData> FamiliarExperience
        {
            get => familiarExperience;
            set => familiarExperience = value;
        }
        public static Dictionary<ulong, FamiliarPrestigeData> FamiliarPrestiges
        {
            get => familiarPrestiges;
            set => familiarPrestiges = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerExperience
        {
            get => playerExperience;
            set => playerExperience = value;
        }
        public static Dictionary<ulong, KeyValuePair<DateTime, float>> PlayerRestedXP
        {
            get => playerRestedXP;
            set => playerRestedXP = value;
        }
        public static Dictionary<ulong, Dictionary<LevelingSystem.PlayerClasses, (List<int>, List<int>)>> PlayerClass
        {
            get => playerClass;
            set => playerClass = value;
        }
        public static Dictionary<ulong, Dictionary<PrestigeSystem.PrestigeType, int>> PlayerPrestiges
        {
            get => playerPrestiges;
            set => playerPrestiges = value;
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
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerEnchanting
        {
            get => playerEnchanting;
            set => playerEnchanting = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerAlchemy
        {
            get => playerAlchemy;
            set => playerAlchemy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerHarvesting
        {
            get => playerHarvesting;
            set => playerHarvesting = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerSwordExpertise
        {
            get => playerSwordExpertise;
            set => playerSwordExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerAxeExpertise
        {
            get => playerAxeExpertise;
            set => playerAxeExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerMaceExpertise
        {
            get => playerMaceExpertise;
            set => playerMaceExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerSpearExpertise
        {
            get => playerSpearExpertise;
            set => playerSpearExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerCrossbowExpertise
        {
            get => playerCrossbowExpertise;
            set => playerCrossbowExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerGreatSwordExpertise
        {
            get => playerGreatSwordExpertise;
            set => playerGreatSwordExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerSlashersExpertise
        {
            get => playerSlashersExpertise;
            set => playerSlashersExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerPistolsExpertise
        {
            get => playerPistolsExpertise;
            set => playerPistolsExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerReaperExpertise
        {
            get => playerReaperExpertise;
            set => playerReaperExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerLongbowExpertise
        {
            get => playerLongbowExpertise;
            set => playerLongbowExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerWhipExpertise
        {
            get => playerWhipExpertise;
            set => playerWhipExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerFishingPoleExpertise
        {
            get => playerFishingPoleExpertise;
            set => playerFishingPoleExpertise = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerUnarmedExpertise
        {
            get => playerUnarmedExpertise;
            set => playerUnarmedExpertise = value;
        }
        public static Dictionary<ulong, (int FirstUnarmed, int SecondUnarmed, int ClassSpell)> PlayerSpells
        {
            get => playerSpells;
            set => playerSpells = value;
        }
        public static Dictionary<ulong, Dictionary<WeaponSystem.WeaponType, List<WeaponHandler.WeaponStats.WeaponStatType>>> PlayerWeaponStats
        {
            get => playerWeaponStats;
            set => playerWeaponStats = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerWorkerLegacy
        {
            get => playerWorkerLegacy;
            set => playerWorkerLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerWarriorLegacy
        {
            get => playerWarriorLegacy;
            set => playerWarriorLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerScholarLegacy
        {
            get => playerScholarLegacy;
            set => playerScholarLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerRogueLegacy
        {
            get => playerRogueLegacy;
            set => playerRogueLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerMutantLegacy
        {
            get => playerMutantLegacy;
            set => playerMutantLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerVBloodLegacy
        {
            get => playerVBloodLegacy;
            set => playerVBloodLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerDraculinLegacy
        {
            get => playerDraculinLegacy;
            set => playerDraculinLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerImmortalLegacy
        {
            get => playerImmortalLegacy;
            set => playerImmortalLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerCreatureLegacy
        {
            get => playerCreatureLegacy;
            set => playerCreatureLegacy = value;
        }
        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerBruteLegacy
        {
            get => playerBruteLegacy;
            set => playerBruteLegacy = value;
        }
        public static Dictionary<ulong, Dictionary<BloodSystem.BloodType, List<BloodHandler.BloodStats.BloodStatType>>> PlayerBloodStats
        {
            get => playerBloodStats;
            set => playerBloodStats = value;
        }

        static Dictionary<ulong, HashSet<string>> playerParties = []; // userEntities of players in the same alliance
        public static Dictionary<ulong, HashSet<string>> PlayerParties
        {
            get => playerParties;
            set => playerParties = value;
        }

        // cache-only
        static Dictionary<Entity, Dictionary<PrefabGUID, int>> playerCraftingJobs = [];

        public static Dictionary<Entity, Dictionary<PrefabGUID, int>> PlayerCraftingJobs
        {
            get => playerCraftingJobs;
            set => playerCraftingJobs = value;
        }

        static Dictionary<ulong, int> playerMaxWeaponLevels = [];

        public static Dictionary<ulong, int> PlayerMaxWeaponLevels
        {
            get => playerMaxWeaponLevels;
            set => playerMaxWeaponLevels = value;
        }

        // file paths dictionary
        private static readonly Dictionary<string, string> filePaths = new()
        {
            {"Experience", JsonFiles.PlayerExperienceJson},
            {"RestedXP", JsonFiles.PlayerRestedXPJson },
            {"Quests", JsonFiles.PlayerQuestsJson },
            {"Classes", JsonFiles.PlayerClassesJson },
            {"Prestiges", JsonFiles.PlayerPrestigesJson },
            {"PlayerBools", JsonFiles.PlayerBoolsJson},
            {"PlayerParties", JsonFiles.PlayerPartiesJson},
            {"Woodcutting", JsonFiles.PlayerWoodcuttingJson},
            {"Mining", JsonFiles.PlayerMiningJson},
            {"Fishing", JsonFiles.PlayerFishingJson},
            {"Blacksmithing", JsonFiles.PlayerBlacksmithingJson},
            {"Tailoring", JsonFiles.PlayerTailoringJson},
            {"Enchanting", JsonFiles.PlayerEnchantingJson},
            {"Alchemy", JsonFiles.PlayerAlchemyJson},
            {"Harvesting", JsonFiles.PlayerHarvestingJson},
            {"SwordExpertise", JsonFiles.PlayerSwordExpertiseJson },
            {"AxeExpertise", JsonFiles.PlayerAxeExpertiseJson},
            {"MaceExpertise", JsonFiles.PlayerMaceExpertiseJson},
            {"SpearExpertise", JsonFiles.PlayerSpearExpertiseJson},
            {"CrossbowExpertise", JsonFiles.PlayerCrossbowExpertiseJson},
            {"GreatSwordExpertise", JsonFiles.PlayerGreatSwordExpertise},
            {"SlashersExpertise", JsonFiles.PlayerSlashersExpertiseJson},
            {"PistolsExpertise", JsonFiles.PlayerPistolsExpertiseJson},
            {"ReaperExpertise", JsonFiles.PlayerReaperExpertise},
            {"LongbowExpertise", JsonFiles.PlayerLongbowExpertiseJson},
            {"WhipExpertise", JsonFiles.PlayerWhipExpertiseJson},
            {"FishingPoleExpertise", JsonFiles.PlayerFishingPoleExpertiseJson},
            {"UnarmedExpertise", JsonFiles.PlayerUnarmedExpertiseJson},
            {"PlayerSpells", JsonFiles.PlayerSpellsJson},
            {"WeaponStats", JsonFiles.PlayerWeaponStatsJson},
            {"WorkerLegacy", JsonFiles.PlayerWorkerLegacyJson},
            {"WarriorLegacy", JsonFiles.PlayerWarriorLegacyJson},
            {"ScholarLegacy", JsonFiles.PlayerScholarLegacyJson},
            {"RogueLegacy", JsonFiles.PlayerRogueLegacyJson},
            {"MutantLegacy", JsonFiles.PlayerMutantLegacyJson},
            {"VBloodLegacy", JsonFiles.PlayerVBloodLegacyJson},
            {"DraculinLegacy", JsonFiles.PlayerDraculinLegacyJson},
            {"ImmortalLegacy", JsonFiles.PlayerImmortalLegacyJson},
            {"CreatureLegacy", JsonFiles.PlayerCreatureLegacyJson},
            {"BruteLegacy", JsonFiles.PlayerBruteLegacyJson},
            {"BloodStats", JsonFiles.PlayerBloodStatsJson},
            {"FamiliarActives", JsonFiles.PlayerFamiliarActivesJson},
            {"FamiliarSets", JsonFiles.PlayerFamiliarSetsJson }
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
                Log.LogInfo($"{key} file created as it did not exist.");
                return;
            }
            try
            {
                string json = File.ReadAllText(path);

                json = json.Replace("Sanguimancy", "UnarmedExpertise");

                if (string.IsNullOrWhiteSpace(json))
                {
                    // Handle the empty file case
                    //Log.LogWarning($"{key} data file is empty or contains only whitespace.");
                    dataStructure = []; // Provide default empty dictionary
                }
                else
                {
                    var data = JsonSerializer.Deserialize<Dictionary<ulong, T>>(json, prettyJsonOptions);
                    dataStructure = data ?? []; // Ensure non-null assignment
                }
            }
            catch (IOException ex)
            {
                Log.LogError($"Error reading {key} data from file: {ex.Message}");
                dataStructure = []; // Provide default empty dictionary on error.
            }
            catch (JsonException ex)
            {
                Log.LogError($"JSON deserialization error when loading {key} data: {ex.Message}");
                dataStructure = []; // Provide default empty dictionary on error.
            }
        }

        public static void LoadPlayerExperience() => LoadData(ref playerExperience, "Experience");

        public static void LoadPlayerRestedXP() => LoadData(ref playerRestedXP, "RestedXP");

        public static void LoadPlayerQuests() => LoadData(ref playerQuests, "Quests");

        public static void LoadPlayerClasses() => LoadData(ref playerClass, "Classes");

        public static void LoadPlayerPrestiges() => LoadData(ref playerPrestiges, "Prestiges");

        public static void LoadPlayerBools() => LoadData(ref playerBools, "PlayerBools");

        public static void LoadPlayerParties() => LoadData(ref playerParties, "PlayerParties");

        public static void LoadPlayerWoodcutting() => LoadData(ref playerWoodcutting, "Woodcutting");

        public static void LoadPlayerMining() => LoadData(ref playerMining, "Mining");

        public static void LoadPlayerFishing() => LoadData(ref playerFishing, "Fishing");

        public static void LoadPlayerBlacksmithing() => LoadData(ref playerBlacksmithing, "Blacksmithing");

        public static void LoadPlayerTailoring() => LoadData(ref playerTailoring, "Tailoring");

        public static void LoadPlayerEnchanting() => LoadData(ref playerEnchanting, "Enchanting");

        public static void LoadPlayerAlchemy() => LoadData(ref playerAlchemy, "Alchemy");

        public static void LoadPlayerHarvesting() => LoadData(ref playerHarvesting, "Harvesting");

        public static void LoadPlayerSwordExpertise() => LoadData(ref playerSwordExpertise, "SwordExpertise");

        public static void LoadPlayerAxeExpertise() => LoadData(ref playerAxeExpertise, "AxeExpertise");

        public static void LoadPlayerMaceExpertise() => LoadData(ref playerMaceExpertise, "MaceExpertise");

        public static void LoadPlayerSpearExpertise() => LoadData(ref playerSpearExpertise, "SpearExpertise");

        public static void LoadPlayerCrossbowExpertise() => LoadData(ref playerCrossbowExpertise, "CrossbowExpertise");

        public static void LoadPlayerGreatSwordExpertise() => LoadData(ref playerGreatSwordExpertise, "GreatSwordExpertise");

        public static void LoadPlayerSlashersExpertise() => LoadData(ref playerSlashersExpertise, "SlashersExpertise");

        public static void LoadPlayerPistolsExpertise() => LoadData(ref playerPistolsExpertise, "PistolsExpertise");

        public static void LoadPlayerReaperExpertise() => LoadData(ref playerReaperExpertise, "ReaperExpertise");

        public static void LoadPlayerLongbowExpertise() => LoadData(ref playerLongbowExpertise, "LongbowExpertise");

        public static void LoadPlayerWhipExpertise() => LoadData(ref playerWhipExpertise, "WhipExpertise");

        public static void LoadPlayerFishingPoleExpertise() => LoadData(ref playerFishingPoleExpertise, "FishingPoleExpertise");

        public static void LoadPlayerUnarmedExpertise() => LoadData(ref playerUnarmedExpertise, "UnarmedExpertise");

        public static void LoadPlayerSpells() => LoadData(ref playerSpells, "PlayerSpells");

        public static void LoadPlayerWeaponStats() => LoadData(ref playerWeaponStats, "WeaponStats");

        public static void LoadPlayerWorkerLegacy() => LoadData(ref playerWorkerLegacy, "WorkerLegacy");

        public static void LoadPlayerWarriorLegacy() => LoadData(ref playerWarriorLegacy, "WarriorLegacy");

        public static void LoadPlayerScholarLegacy() => LoadData(ref playerScholarLegacy, "ScholarLegacy");

        public static void LoadPlayerRogueLegacy() => LoadData(ref playerRogueLegacy, "RogueLegacy");

        public static void LoadPlayerMutantLegacy() => LoadData(ref playerMutantLegacy, "MutantLegacy");

        public static void LoadPlayerVBloodLegacy() => LoadData(ref playerVBloodLegacy, "VBloodLegacy");

        public static void LoadPlayerDraculinLegacy() => LoadData(ref playerDraculinLegacy, "DraculinLegacy");

        public static void LoadPlayerImmortalLegacy() => LoadData(ref playerImmortalLegacy, "ImmortalLegacy");

        public static void LoadPlayerCreatureLegacy() => LoadData(ref playerCreatureLegacy, "CreatureLegacy");

        public static void LoadPlayerBruteLegacy() => LoadData(ref playerBruteLegacy, "BruteLegacy");

        public static void LoadPlayerBloodStats() => LoadData(ref playerBloodStats, "BloodStats");

        public static void LoadPlayerFamiliarActives() => LoadData(ref familiarActives, "FamiliarActives");

        public static void LoadPlayerFamiliarSets() => LoadData(ref familiarSet, "FamiliarSets");

        public static void SaveData<T>(Dictionary<ulong, T> data, string key)
        {
            string path = filePaths[key];
            try
            {
                string json = JsonSerializer.Serialize(data, prettyJsonOptions);
                File.WriteAllText(path, json);
                //Core.Log.LogInfo($"{key} data saved successfully.");
            }
            catch (IOException ex)
            {
                Log.LogInfo($"Failed to write {key} data to file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Log.LogInfo($"JSON serialization error when saving {key} data: {ex.Message}");
            }
        }
        public static void SavePlayerExperience() => SaveData(PlayerExperience, "Experience");

        public static void SavePlayerRestedXP() => SaveData(PlayerRestedXP, "RestedXP");

        public static void SavePlayerQuests() => SaveData(PlayerQuests, "Quests");

        public static void SavePlayerClasses() => SaveData(PlayerClass, "Classes");

        public static void SavePlayerPrestiges() => SaveData(PlayerPrestiges, "Prestiges");

        public static void SavePlayerBools() => SaveData(PlayerBools, "PlayerBools");

        public static void SavePlayerParties() => SaveData(PlayerParties, "PlayerParties");

        public static void SavePlayerWoodcutting() => SaveData(PlayerWoodcutting, "Woodcutting");

        public static void SavePlayerMining() => SaveData(PlayerMining, "Mining");

        public static void SavePlayerFishing() => SaveData(PlayerFishing, "Fishing");

        public static void SavePlayerBlacksmithing() => SaveData(PlayerBlacksmithing, "Blacksmithing");

        public static void SavePlayerTailoring() => SaveData(PlayerTailoring, "Tailoring");

        public static void SavePlayerEnchanting() => SaveData(PlayerEnchanting, "Enchanting");

        public static void SavePlayerAlchemy() => SaveData(PlayerAlchemy, "Alchemy");

        public static void SavePlayerHarvesting() => SaveData(PlayerHarvesting, "Harvesting");

        public static void SavePlayerSwordExpertise() => SaveData(PlayerSwordExpertise, "SwordExpertise");

        public static void SavePlayerAxeExpertise() => SaveData(PlayerAxeExpertise, "AxeExpertise");

        public static void SavePlayerMaceExpertise() => SaveData(PlayerMaceExpertise, "MaceExpertise");

        public static void SavePlayerSpearExpertise() => SaveData(PlayerSpearExpertise, "SpearExpertise");

        public static void SavePlayerCrossbowExpertise() => SaveData(PlayerCrossbowExpertise, "CrossbowExpertise");

        public static void SavePlayerGreatSwordExpertise() => SaveData(PlayerGreatSwordExpertise, "GreatSwordExpertise");

        public static void SavePlayerSlashersExpertise() => SaveData(PlayerSlashersExpertise, "SlashersExpertise");

        public static void SavePlayerPistolsExpertise() => SaveData(PlayerPistolsExpertise, "PistolsExpertise");

        public static void SavePlayerReaperExpertise() => SaveData(PlayerReaperExpertise, "ReaperExpertise");

        public static void SavePlayerLongbowExpertise() => SaveData(PlayerLongbowExpertise, "LongbowExpertise");

        public static void SavePlayerWhipExpertise() => SaveData(PlayerWhipExpertise, "WhipExpertise");

        public static void SavePlayerFishingPoleExpertise() => SaveData(PlayerFishingPoleExpertise, "FishingPoleExpertise");

        public static void SavePlayerUnarmedExpertise() => SaveData(PlayerUnarmedExpertise, "UnarmedExpertise");

        public static void SavePlayerSpells() => SaveData(PlayerSpells, "PlayerSpells");

        public static void SavePlayerWeaponStats() => SaveData(PlayerWeaponStats, "WeaponStats");

        public static void SavePlayerWorkerLegacy() => SaveData(PlayerWorkerLegacy, "WorkerLegacy");

        public static void SavePlayerWarriorLegacy() => SaveData(PlayerWarriorLegacy, "WarriorLegacy");

        public static void SavePlayerScholarLegacy() => SaveData(PlayerScholarLegacy, "ScholarLegacy");

        public static void SavePlayerRogueLegacy() => SaveData(PlayerRogueLegacy, "RogueLegacy");

        public static void SavePlayerMutantLegacy() => SaveData(PlayerMutantLegacy, "MutantLegacy");

        public static void SavePlayerVBloodLegacy() => SaveData(PlayerVBloodLegacy, "VBloodLegacy");

        public static void SavePlayerDraculinLegacy() => SaveData(PlayerDraculinLegacy, "DraculinLegacy");

        public static void SavePlayerImmortalLegacy() => SaveData(PlayerImmortalLegacy, "ImmortalLegacy");

        public static void SavePlayerCreatureLegacy() => SaveData(PlayerCreatureLegacy, "CreatureLegacy");

        public static void SavePlayerBruteLegacy() => SaveData(PlayerBruteLegacy, "BruteLegacy");

        public static void SavePlayerBloodStats() => SaveData(PlayerBloodStats, "BloodStats");

        public static void SavePlayerFamiliarActives() => SaveData(FamiliarActives, "FamiliarActives");

        public static void SavePlayerFamiliarSets() => SaveData(FamiliarSet, "FamiliarSets");
    }
    public static class FamiliarExperienceManager
    {
        static string GetFilePath(ulong playerId) => Path.Combine(Plugin.FamiliarExperiencePath, $"{playerId}_familiar_experience.json");

        public static void SaveFamiliarExperience(ulong playerId, FamiliarExperienceData data)
        {
            string filePath = GetFilePath(playerId);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, jsonString);
        }

        public static FamiliarExperienceData LoadFamiliarExperience(ulong playerId)
        {
            string filePath = GetFilePath(playerId);
            if (!File.Exists(filePath))
                return new FamiliarExperienceData();

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FamiliarExperienceData>(jsonString);
        }
    }
    public static class FamiliarPrestigeManager
    {
        static string GetFilePath(ulong playerId) => Path.Combine(Plugin.FamiliarExperiencePath, $"{playerId}_familiar_prestige.json");

        public static void SaveFamiliarPrestige(ulong playerId, FamiliarPrestigeData data)
        {
            string filePath = GetFilePath(playerId);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, jsonString);
        }

        public static FamiliarPrestigeData LoadFamiliarPrestige(ulong playerId)
        {
            string filePath = GetFilePath(playerId);
            if (!File.Exists(filePath))
                return new FamiliarPrestigeData();

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FamiliarPrestigeData>(jsonString);
        }
    }
    public static class FamiliarBuffsManager
    {
        static string GetFilePath(ulong playerId) => Path.Combine(Plugin.FamiliarUnlocksPath, $"{playerId}_familiar_buffs.json");

        public static void SaveFamiliarBuffs(ulong playerId, FamiliarBuffsData data)
        {
            string filePath = GetFilePath(playerId);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, jsonString);
        }

        public static FamiliarBuffsData LoadFamiliarBuffs(ulong playerId)
        {
            string filePath = GetFilePath(playerId);
            if (!File.Exists(filePath))
                return new FamiliarBuffsData();

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FamiliarBuffsData>(jsonString);
        }
    }
    public static class FamiliarUnlocksManager
    {
        static string GetFilePath(ulong playerId) => Path.Combine(Plugin.FamiliarUnlocksPath, $"{playerId}_familiar_unlocks.json");

        public static void SaveUnlockedFamiliars(ulong playerId, UnlockedFamiliarData data)
        {
            string filePath = GetFilePath(playerId);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, jsonString);
        }

        public static UnlockedFamiliarData LoadUnlockedFamiliars(ulong playerId)
        {
            string filePath = GetFilePath(playerId);
            if (!File.Exists(filePath))
                return new UnlockedFamiliarData();

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<UnlockedFamiliarData>(jsonString);
        }
    }
    public static class JsonFiles
    {
        public static readonly string PlayerExperienceJson = Path.Combine(Plugin.PlayerLevelingPath, "player_experience.json");
        public static readonly string PlayerRestedXPJson = Path.Combine(Plugin.PlayerLevelingPath, "player_rested_xp.json");
        public static readonly string PlayerQuestsJson = Path.Combine(Plugin.PlayerQuestsPath, "player_quests.json");
        public static readonly string PlayerPrestigesJson = Path.Combine(Plugin.PlayerLevelingPath, "player_prestiges.json");
        public static readonly string PlayerClassesJson = Path.Combine(Plugin.ConfigFiles, "player_classes.json");
        public static readonly string PlayerBoolsJson = Path.Combine(Plugin.ConfigFiles, "player_bools.json");
        public static readonly string PlayerPartiesJson = Path.Combine(Plugin.ConfigFiles, "player_parties.json");
        public static readonly string PlayerWoodcuttingJson = Path.Combine(Plugin.PlayerProfessionPath, "player_woodcutting.json");
        public static readonly string PlayerMiningJson = Path.Combine(Plugin.PlayerProfessionPath, "player_mining.json");
        public static readonly string PlayerFishingJson = Path.Combine(Plugin.PlayerProfessionPath, "player_fishing.json");
        public static readonly string PlayerBlacksmithingJson = Path.Combine(Plugin.PlayerProfessionPath, "player_blacksmithing.json");
        public static readonly string PlayerTailoringJson = Path.Combine(Plugin.PlayerProfessionPath, "player_tailoring.json");
        public static readonly string PlayerEnchantingJson = Path.Combine(Plugin.PlayerProfessionPath, "player_enchanting.json");
        public static readonly string PlayerAlchemyJson = Path.Combine(Plugin.PlayerProfessionPath, "player_alchemy.json");
        public static readonly string PlayerHarvestingJson = Path.Combine(Plugin.PlayerProfessionPath, "player_harvesting.json");
        public static readonly string PlayerSwordExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_sword.json");
        public static readonly string PlayerAxeExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_axe.json");
        public static readonly string PlayerMaceExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_mace.json");
        public static readonly string PlayerSpearExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_spear.json");
        public static readonly string PlayerCrossbowExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_crossbow.json");
        public static readonly string PlayerGreatSwordExpertise = Path.Combine(Plugin.PlayerExpertisePath, "player_greatsword.json");
        public static readonly string PlayerSlashersExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_slashers.json");
        public static readonly string PlayerPistolsExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_pistols.json");
        public static readonly string PlayerReaperExpertise = Path.Combine(Plugin.PlayerExpertisePath, "player_reaper.json");
        public static readonly string PlayerLongbowExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_longbow.json");
        public static readonly string PlayerUnarmedExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_unarmed.json");
        public static readonly string PlayerWhipExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_whip.json");
        public static readonly string PlayerFishingPoleExpertiseJson = Path.Combine(Plugin.PlayerExpertisePath, "player_fishingpole.json");
        public static readonly string PlayerSpellsJson = Path.Combine(Plugin.PlayerLevelingPath, "player_spells.json");
        public static readonly string PlayerWeaponStatsJson = Path.Combine(Plugin.PlayerExpertisePath, "player_weapon_stats.json");
        public static readonly string PlayerWorkerLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_worker.json");
        public static readonly string PlayerWarriorLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_warrior.json");
        public static readonly string PlayerScholarLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_scholar.json");
        public static readonly string PlayerRogueLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_rogue.json");
        public static readonly string PlayerMutantLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_mutant.json");
        public static readonly string PlayerVBloodLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_vblood.json");
        public static readonly string PlayerDraculinLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_draculin.json");
        public static readonly string PlayerImmortalLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_immortal.json");
        public static readonly string PlayerCreatureLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_creature.json");
        public static readonly string PlayerBruteLegacyJson = Path.Combine(Plugin.PlayerBloodPath, "player_brute.json");
        public static readonly string PlayerBloodStatsJson = Path.Combine(Plugin.PlayerBloodPath, "player_blood_stats.json");
        public static readonly string PlayerFamiliarActivesJson = Path.Combine(Plugin.PlayerFamiliarsPath, "player_familiar_actives.json");
        public static readonly string PlayerFamiliarSetsJson = Path.Combine(Plugin.FamiliarUnlocksPath, "player_familiar_sets.json");
    }
}
/*
[AttributeUsage(AttributeTargets.Class)]
public class UpdateInGroups(params Type[] groupTypes) : Attribute
{
    public Type[] GroupTypes { get; } = groupTypes;
}
public struct TeamJob // apply teams based on results of jobs
{
    public NativeList<Entity> Targets;
    public NativeHashMap<Entity, Entity> TeamStorage;
    public NativeArray<bool> IsInTerritory;
    public void Execute()
    {
        for (int i = 0; i < Targets.Length; i++)
        {
            Entity entity = Targets.ElementAt(i);
            if (IsInTerritory[i]) // restore team from TeamStorage or TeamAllies buffer
            {
                if (TeamStorage.TryGetValue(entity, out Entity team))
                {
                    TeamReference teamReference = entity.Read<TeamReference>();
                    teamReference.Value._Value = team;
                    entity.Write(teamReference);
                }
                else // if not in TeamStorage use TeamAllies buffer, bit more complex
                {
                    Entity teamReferenceEntity = entity.Read<TeamReference>().Value._Value;
                    var teamAllies = teamReferenceEntity.ReadBuffer<TeamAllies>();
                    for (int j = 0; j < teamAllies.Length; j++)
                    {
                        Entity allyEntity = teamAllies[j].Value;
                        if (allyEntity.Has<ClanTeam>())
                        {
                            var syncBuffer = allyEntity.ReadBuffer<SyncToUserBuffer>();
                            for (int k = 0; k < syncBuffer.Length; k++)
                            {
                                if (syncBuffer[k].UserEntity.Read<User>().CharacterName.Value == entity.Read<PlayerCharacter>().Name.Value)
                                {
                                    teamReferenceEntity.Write(allyEntity);
                                    entity.Write(teamReferenceEntity);
                                    break;
                                }
                            }
                        }
                        else if (allyEntity.Has<CastleTeam>())
                        {
                            var alliesBuffer = allyEntity.ReadBuffer<TeamAllies>();
                            for (int k = 0; k < alliesBuffer.Length; k++)
                            {
                                if (alliesBuffer[k].Value.Has<UserTeam>() && alliesBuffer[k].Value.Read<UserTeam>().UserEntity.Read<User>().CharacterName.Value == entity.Read<PlayerCharacter>().Name.Value)
                                {
                                    teamReferenceEntity.Write(allyEntity);
                                    entity.Write(teamReferenceEntity);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else // apply universal team
            {
                Entity teamReferenceEntity = entity.Read<TeamReference>().Value._Value;
                TeamData teamData = teamReferenceEntity.Read<TeamData>();
                teamData.TeamValue = 0;
                teamReferenceEntity.Write(teamData);
                entity.Write(teamReferenceEntity);
            }
        }
    }
}
public struct TerritoryJob // determine which entities are in territories; if so, there original UserTeam will be applied back to them, if not then the universal UserTeam
{
    public NativeList<Entity> Targets;
    public NativeArray<bool> IsInTerritory;
    //public MapZoneCollection MapZoneCollection;
    //public EntityManager EntityManager;
    public void Execute()
    {
        for (int i = 0; i < Targets.Length; i++)
        {
            Entity entity = Targets[i];
            //if (CastleTerritoryExtensions.TryGetCastleTerritory(ref MapZoneCollection, ref EntityManager, entity.Read<TilePosition>().Tile, out CastleTerritory _)) IsInTerritory[i] = true;
            //else IsInTerritory[i] = false;
        }
    }
}
public struct ExcludeJob // validate if entity should be modified based on territory or not; checking for UserTeams should be sufficient for this, add to TeamStorage for later
{
    public NativeList<Entity> Targets;
    public NativeArray<Entity> Entities;
    public NativeHashMap<Entity, Entity> TeamStorage;
    public void Execute()
    {
        try
        {
            for (int i = 0; i < Entities.Length; i++)
            {
                Entity entity = Entities[i];
                Entity teamReferenceEntity = entity.Read<TeamReference>().Value._Value;
                if (teamReferenceEntity.Has<UserTeam>() && !Targets.Contains(entity))
                {
                    Targets.Add(ref entity);
                    TeamStorage.TryAdd(entity, teamReferenceEntity);
                }
            }
        }
        finally
        {
            Entities.Dispose();
        }
    }
}





if (!Core.hasInitialized) return;

NativeArray<Entity> TeamQuery = Core.TeamQuery.ToEntityArray(Allocator.TempJob);
NativeHashMap<Entity, Entity> TeamStorage = new(100, Allocator.TempJob);
NativeList<Entity> Targets = new(200, Allocator.TempJob);
NativeArray<bool> IsInTerritory = new(200, Allocator.TempJob);

ExcludeJob excludeJob = new()
{
    Targets = Targets,
    TeamStorage = TeamStorage,
    Entities = TeamQuery
};

TerritoryJob territoryJob = new()
{
    Targets = Targets,
    IsInTerritory = IsInTerritory
    //MapZoneCollection = Core.MapZoneCollection,
    //EntityManager = EntityManager
};

TeamJob teamJob = new()
{
    Targets = Targets,
    TeamStorage = TeamStorage,
    IsInTerritory = IsInTerritory
};

JobHandle excludeHandle = IJobExtensions.Schedule(excludeJob);
JobHandle territoryHandle = IJobExtensions.Schedule(territoryJob, excludeHandle);
JobHandle teamHandle = IJobExtensions.Schedule(teamJob, territoryHandle);
Dependency = teamHandle;
*/
