using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Cobalt.Systems.Expertise;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using System.Collections;
using System.Text.Json;
using Unity.Entities;
using UnityEngine;

namespace Cobalt;

internal static class Core
{
    public static World Server { get; } = GetWorld("Server") ?? throw new System.Exception("There is no Server world (yet)...");

    // V Rising systems
    public static EntityManager EntityManager { get; } = Server.EntityManager;

    public static PrefabCollectionSystem PrefabCollectionSystem { get; internal set; }
    public static ServerGameSettingsSystem ServerGameSettingsSystem { get; internal set; }
    public static ServerScriptMapper ServerScriptMapper { get; internal set; }
    public static DebugEventsSystem DebugEventsSystem { get; internal set; }

    public static double ServerTime => ServerGameManager.ServerTime;
    public static ServerGameManager ServerGameManager => ServerScriptMapper.GetServerGameManager();

    public static ModificationsRegistry ModificationsRegistry => ServerGameManager.Modifications;

    // BepInEx services
    public static ManualLogSource Log => Plugin.LogInstance;

    private static bool hasInitialized;

    public static void Initialize()
    {
        if (hasInitialized) return;

        PrefabCollectionSystem = Server.GetExistingSystemManaged<PrefabCollectionSystem>();
        ServerGameSettingsSystem = Server.GetExistingSystemManaged<ServerGameSettingsSystem>();
        DebugEventsSystem = Server.GetExistingSystemManaged<DebugEventsSystem>();
        ServerScriptMapper = Server.GetExistingSystemManaged<ServerScriptMapper>();

        // Initialize utility services

        Core.Log.LogInfo("Cobalt initialized...");

        hasInitialized = true;
    }

    private static World GetWorld(string name)
    {
        foreach (var world in World.s_AllWorlds)
        {
            if (world.Name == name)
            {
                return world;
            }
        }

        return null;
    }

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
        private static Dictionary<ulong, KeyValuePair<int, float>> playerPrestige = [];
        private static Dictionary<ulong, Dictionary<string, bool>> playerBools = [];

        private static Dictionary<ulong, KeyValuePair<int, float>> playerWoodcutting = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerMining = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerFishing = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerBlacksmithing = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerTailoring = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerJewelcrafting = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerAlchemy = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerHarvesting = [];

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
        private static Dictionary<ulong, Dictionary<ExpertiseSystem.WeaponType, List<WeaponStats.WeaponStatManager.WeaponStatType>>> playerWeaponChoices = [];

        private static Dictionary<ulong, KeyValuePair<int, float>> playerSanguimancy = []; // this is unarmed basically
        private static Dictionary<ulong, (PrefabGUID, PrefabGUID)> playerSanguimancySpells = [];

        private static Dictionary<ulong, KeyValuePair<int, float>> playerWorkerBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerWarriorBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerScholarBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerRogueBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerMutantBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerVBloodBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerDraculinBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerImmortalBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerCreatureBloodline = [];
        private static Dictionary<ulong, KeyValuePair<int, float>> playerBruteBloodline = [];

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerExperience
        {
            get => playerExperience;
            set => playerExperience = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerPrestige
        {
            get => playerPrestige;
            set => playerPrestige = value;
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

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerJewelcrafting
        {
            get => playerJewelcrafting;
            set => playerJewelcrafting = value;
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

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerSanguimancy
        {
            get => playerSanguimancy;
            set => playerSanguimancy = value;
        }

        public static Dictionary<ulong, (PrefabGUID, PrefabGUID)> PlayerSanguimancySpells
        {
            get => playerSanguimancySpells;
            set => playerSanguimancySpells = value;
        }

        public static Dictionary<ulong, Dictionary<ExpertiseSystem.WeaponType, List<WeaponStats.WeaponStatManager.WeaponStatType>>> PlayerWeaponChoices // weapon, then list of stats for the weapon in string form
        {
            get => playerWeaponChoices;
            set => playerWeaponChoices = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerWorkerBloodline
        {
            get => playerWorkerBloodline;
            set => playerWorkerBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerWarriorBloodline
        {
            get => playerWarriorBloodline;
            set => playerWarriorBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerScholarBloodline
        {
            get => playerScholarBloodline;
            set => playerScholarBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerRogueBloodline
        {
            get => playerRogueBloodline;
            set => playerRogueBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerMutantBloodline
        {
            get => playerMutantBloodline;
            set => playerMutantBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerVBloodBloodline
        {
            get => playerVBloodBloodline;
            set => playerVBloodBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerDraculinBloodline
        {
            get => playerDraculinBloodline;
            set => playerDraculinBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerImmortalBloodline
        {
            get => playerImmortalBloodline;
            set => playerImmortalBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerCreatureBloodline
        {
            get => playerCreatureBloodline;
            set => playerCreatureBloodline = value;
        }

        public static Dictionary<ulong, KeyValuePair<int, float>> PlayerBruteBloodline
        {
            get => playerBruteBloodline;
            set => playerBruteBloodline = value;
        }

        // cache-only

        private static Dictionary<NetworkId, Dictionary<ulong, List<(PrefabGUID, int)>>> playerCraftingJobs = [];

        public static Dictionary<NetworkId, Dictionary<ulong, List<(PrefabGUID, int)>>> PlayerCraftingJobs
        {
            get => playerCraftingJobs;
            set => playerCraftingJobs = value;
        }

        // file paths dictionary
        private static readonly Dictionary<string, string> filePaths = new()
        {
            {"Experience", Core.JsonFiles.PlayerExperienceJson},
            {"Prestige", Core.JsonFiles.PlayerPrestigeJson },
            {"PlayerBools", Core.JsonFiles.PlayerBoolsJson},
            {"Woodcutting", Core.JsonFiles.PlayerWoodcuttingJson},
            {"Mining", Core.JsonFiles.PlayerMiningJson},
            {"Fishing", Core.JsonFiles.PlayerFishingJson},
            {"Blacksmithing", Core.JsonFiles.PlayerBlacksmithingJson},
            {"Tailoring", Core.JsonFiles.PlayerTailoringJson},
            {"Jewelcrafting", Core.JsonFiles.PlayerJewelcraftingJson},
            {"Alchemy", Core.JsonFiles.PlayerAlchemyJson},
            {"Harvesting", Core.JsonFiles.PlayerHarvestingJson},
            {"SwordExpertise", Core.JsonFiles.PlayerSwordExpertiseJson },
            {"AxeExpertise", Core.JsonFiles.PlayerAxeExpertiseJson},
            {"MaceExpertise", Core.JsonFiles.PlayerMaceExpertiseJson},
            {"SpearExpertise", Core.JsonFiles.PlayerSpearExpertiseJson},
            {"CrossbowExpertise", Core.JsonFiles.PlayerCrossbowExpertiseJson},
            {"GreatSwordExpertise", Core.JsonFiles.PlayerGreatSwordExpertise},
            {"SlashersExpertise", Core.JsonFiles.PlayerSlashersExpertiseJson},
            {"PistolsExpertise", Core.JsonFiles.PlayerPistolsExpertiseJson},
            {"ReaperExpertise", Core.JsonFiles.PlayerReaperExpertise},
            {"LongbowExpertise", Core.JsonFiles.PlayerLongbowExpertiseJson},
            {"WhipExpertise", Core.JsonFiles.PlayerWhipExpertiseJson},
            {"UnarmedExpertise", Core.JsonFiles.PlayerUnarmedExpertiseJson},
            {"Sanguimancy", Core.JsonFiles.PlayerSanguimancyJson},
            {"SanguimancySpells", Core.JsonFiles.PlayerSanguimancySpellsJson},
            {"WeaponChoices", Core.JsonFiles.PlayerWeaponChoicesJson},
            {"WorkerBloodline", Core.JsonFiles.PlayerWorkerBloodlineJson},
            {"WarriorBloodline", Core.JsonFiles.PlayerWarriorBloodlineJson},
            {"ScholarBloodline", Core.JsonFiles.PlayerScholarBloodlineJson},
            {"RogueBloodline", Core.JsonFiles.PlayerRogueBloodlineJson},
            {"MutantBloodline", Core.JsonFiles.PlayerMutantBloodlineJson},
            {"VBloodBloodline", Core.JsonFiles.PlayerVBloodBloodlineJson},
            {"DraculinBloodline", Core.JsonFiles.PlayerDraculinBloodlineJson},
            {"ImmortalBloodline", Core.JsonFiles.PlayerImmortalBloodlineJson},
            {"CreatureBloodline", Core.JsonFiles.PlayerCreatureBloodlineJson},
            {"BruteBloodline", Core.JsonFiles.PlayerBruteBloodlineJson},
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
                Core.Log.LogInfo($"{key} file created as it did not exist.");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<Dictionary<ulong, T>>(json, prettyJsonOptions);
                dataStructure = data ?? []; // Ensure non-null assignment.
                Core.Log.LogInfo($"{key} data loaded successfully.");
            }
            catch (IOException ex)
            {
                Core.Log.LogError($"Error reading {key} data from file: {ex.Message}");
                dataStructure = []; // Provide default empty dictionary on error.
            }
            catch (JsonException ex)
            {
                Core.Log.LogError($"JSON deserialization error when loading {key} data: {ex.Message}");
                dataStructure = []; // Provide default empty dictionary on error.
            }
        }

        public static void LoadPlayerExperience() => LoadData(ref playerExperience, "Experience");

        public static void LoadPlayerPrestige() => LoadData(ref playerPrestige, "Prestige");

        public static void LoadPlayerBools() => LoadData(ref playerBools, "PlayerBools");

        public static void LoadPlayerWoodcutting() => LoadData(ref playerWoodcutting, "Woodcutting");

        public static void LoadPlayerMining() => LoadData(ref playerMining, "Mining");

        public static void LoadPlayerFishing() => LoadData(ref playerFishing, "Fishing");

        public static void LoadPlayerBlacksmithing() => LoadData(ref playerBlacksmithing, "Blacksmithing");

        public static void LoadPlayerTailoring() => LoadData(ref playerTailoring, "Tailoring");

        public static void LoadPlayerJewelcrafting() => LoadData(ref playerJewelcrafting, "Jewelcrafting");

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

        public static void LoadPlayerSanguimancy() => LoadData(ref playerSanguimancy, "Sanguimancy");

        public static void LoadPlayerSanguimancySpells() => LoadData(ref playerSanguimancySpells, "SanguimancySpells");

        public static void LoadPlayerWeaponChoices() => LoadData(ref playerWeaponChoices, "WeaponChoices");

        public static void LoadPlayerWorkerBloodline() => LoadData(ref playerWorkerBloodline, "WorkerBloodline");

        public static void LoadPlayerWarriorBloodline() => LoadData(ref playerWarriorBloodline, "WarriorBloodline");

        public static void LoadPlayerScholarBloodline() => LoadData(ref playerScholarBloodline, "ScholarBloodline");

        public static void LoadPlayerRogueBloodline() => LoadData(ref playerRogueBloodline, "RogueBloodline");

        public static void LoadPlayerMutantBloodline() => LoadData(ref playerMutantBloodline, "MutantBloodline");

        public static void LoadPlayerVBloodBloodline() => LoadData(ref playerVBloodBloodline, "VBloodBloodline");

        public static void LoadPlayerDraculinBloodline() => LoadData(ref playerDraculinBloodline, "DraculinBloodline");

        public static void LoadPlayerImmortalBloodline() => LoadData(ref playerImmortalBloodline, "ImmortalBloodline");

        public static void LoadPlayerCreatureBloodline() => LoadData(ref playerCreatureBloodline, "CreatureBloodline");

        public static void LoadPlayerBruteBloodline() => LoadData(ref playerBruteBloodline, "BruteBloodline");

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
                Core.Log.LogError($"Failed to write {key} data to file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Core.Log.LogError($"JSON serialization error when saving {key} data: {ex.Message}");
            }
        }

        public static void SavePlayerExperience() => SaveData(PlayerExperience, "Experience");

        public static void SavePlayerPrestige() => SaveData(PlayerPrestige, "Prestige");

        public static void SavePlayerBools() => SaveData(PlayerBools, "PlayerBools");

        public static void SavePlayerWoodcutting() => SaveData(PlayerWoodcutting, "Woodcutting");

        public static void SavePlayerMining() => SaveData(PlayerMining, "Mining");

        public static void SavePlayerFishing() => SaveData(PlayerFishing, "Fishing");

        public static void SavePlayerBlacksmithing() => SaveData(PlayerBlacksmithing, "Blacksmithing");

        public static void SavePlayerTailoring() => SaveData(PlayerTailoring, "Tailoring");

        public static void SavePlayerJewelcrafting() => SaveData(PlayerJewelcrafting, "Jewelcrafting");

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

        public static void SavePlayerSanguimancy() => SaveData(PlayerSanguimancy, "Sanguimancy");

        public static void SavePlayerSanguimancySpells() => SaveData(PlayerSanguimancySpells, "SanguimancySpells");

        public static void SavePlayerWeaponChoices() => SaveData(PlayerWeaponChoices, "WeaponChoices");

        public static void SavePlayerWorkerBloodline() => SaveData(PlayerWorkerBloodline, "WorkerBloodline");

        public static void SavePlayerWarriorBloodline() => SaveData(PlayerWarriorBloodline, "WarriorBloodline");

        public static void SavePlayerScholarBloodline() => SaveData(PlayerScholarBloodline, "ScholarBloodline");

        public static void SavePlayerRogueBloodline() => SaveData(PlayerRogueBloodline, "RogueBloodline");

        public static void SavePlayerMutantBloodline() => SaveData(PlayerMutantBloodline, "MutantBloodline");

        public static void SavePlayerVBloodBloodline() => SaveData(PlayerVBloodBloodline, "VBloodBloodline");

        public static void SavePlayerDraculinBloodline() => SaveData(PlayerDraculinBloodline, "DraculinBloodline");

        public static void SavePlayerImmortalBloodline() => SaveData(PlayerImmortalBloodline, "ImmortalBloodline");

        public static void SavePlayerCreatureBloodline() => SaveData(PlayerCreatureBloodline, "CreatureBloodline");

        public static void SavePlayerBruteBloodline() => SaveData(PlayerBruteBloodline, "BruteBloodline");
    }

    public class JsonFiles
    {
        public static readonly string PlayerExperienceJson = Path.Combine(Plugin.ConfigPath, "player_experience.json");
        public static readonly string PlayerPrestigeJson = Path.Combine(Plugin.ConfigPath, "player_prestige.json");
        public static readonly string PlayerBoolsJson = Path.Combine(Plugin.ConfigPath, "player_bools.json");
        public static readonly string PlayerWoodcuttingJson = Path.Combine(Plugin.ConfigPath, "player_woodcutting.json");
        public static readonly string PlayerMiningJson = Path.Combine(Plugin.ConfigPath, "player_mining.json");
        public static readonly string PlayerFishingJson = Path.Combine(Plugin.ConfigPath, "player_fishing.json");
        public static readonly string PlayerBlacksmithingJson = Path.Combine(Plugin.ConfigPath, "player_blacksmithing.json");
        public static readonly string PlayerTailoringJson = Path.Combine(Plugin.ConfigPath, "player_tailoring.json");
        public static readonly string PlayerJewelcraftingJson = Path.Combine(Plugin.ConfigPath, "player_jewelcrafting.json");
        public static readonly string PlayerAlchemyJson = Path.Combine(Plugin.ConfigPath, "player_alchemy.json");
        public static readonly string PlayerHarvestingJson = Path.Combine(Plugin.ConfigPath, "player_harvesting.json");
        public static readonly string PlayerSwordExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_sword.json");
        public static readonly string PlayerAxeExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_axe.json");
        public static readonly string PlayerMaceExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_mace.json");
        public static readonly string PlayerSpearExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_spear.json");
        public static readonly string PlayerCrossbowExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_crossbow.json");
        public static readonly string PlayerGreatSwordExpertise = Path.Combine(Plugin.ConfigPath, "player_greatsword.json");
        public static readonly string PlayerSlashersExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_slashers.json");
        public static readonly string PlayerPistolsExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_pistols.json");
        public static readonly string PlayerReaperExpertise = Path.Combine(Plugin.ConfigPath, "player_reaper.json");
        public static readonly string PlayerLongbowExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_longbow.json");
        public static readonly string PlayerUnarmedExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_unarmed.json");
        public static readonly string PlayerWhipExpertiseJson = Path.Combine(Plugin.ConfigPath, "player_whip.json");
        public static readonly string PlayerSanguimancyJson = Path.Combine(Plugin.ConfigPath, "player_sanguimancy.json");
        public static readonly string PlayerSanguimancySpellsJson = Path.Combine(Plugin.ConfigPath, "player_sanguimancy_spells.json");
        public static readonly string PlayerWeaponChoicesJson = Path.Combine(Plugin.ConfigPath, "player_weapon_choices.json");
        public static readonly string PlayerWorkerBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_worker_bloodline.json");
        public static readonly string PlayerWarriorBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_warrior_bloodline.json");
        public static readonly string PlayerScholarBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_scholar_bloodline.json");
        public static readonly string PlayerRogueBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_rogue_bloodline.json");
        public static readonly string PlayerMutantBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_mutant_bloodline.json");
        public static readonly string PlayerVBloodBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_vblood_bloodline.json");
        public static readonly string PlayerDraculinBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_draculin_bloodline.json");
        public static readonly string PlayerImmortalBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_immortal_bloodline.json");
        public static readonly string PlayerCreatureBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_creature_bloodline.json");
        public static readonly string PlayerBruteBloodlineJson = Path.Combine(Plugin.ConfigPath, "player_brute_bloodline.json");
    }
}