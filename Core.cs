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
        private static Dictionary<ulong, Dictionary<ExpertiseSystem.WeaponType, List<WeaponStats.WeaponStatManager.WeaponStatType>>> playerWeaponStats = [];

        private static Dictionary<ulong, KeyValuePair<int, float>> playerSanguimancy = []; // this is unarmed basically
        private static Dictionary<ulong, (int, int)> playerSanguimancySpells = [];

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

        public static Dictionary<ulong, (int, int)> PlayerSanguimancySpells
        {
            get => playerSanguimancySpells;
            set => playerSanguimancySpells = value;
        }

        public static Dictionary<ulong, Dictionary<ExpertiseSystem.WeaponType, List<WeaponStats.WeaponStatManager.WeaponStatType>>> PlayerWeaponStats // weapon, then list of stats for the weapon in string form
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
            {"WeaponStats", Core.JsonFiles.PlayerWeaponStatsJson},
            {"WorkerLegacy", Core.JsonFiles.PlayerWorkerLegacyJson},
            {"WarriorLegacy", Core.JsonFiles.PlayerWarriorLegacyJson},
            {"ScholarLegacy", Core.JsonFiles.PlayerScholarLegacyJson},
            {"RogueLegacy", Core.JsonFiles.PlayerRogueLegacyJson},
            {"MutantLegacy", Core.JsonFiles.PlayerMutantLegacyJson},
            {"VBloodLegacy", Core.JsonFiles.PlayerVBloodLegacyJson},
            {"DraculinLegacy", Core.JsonFiles.PlayerDraculinLegacyJson},
            {"ImmortalLegacy", Core.JsonFiles.PlayerImmortalLegacyJson},
            {"CreatureLegacy", Core.JsonFiles.PlayerCreatureLegacyJson},
            {"BruteLegacy", Core.JsonFiles.PlayerBruteLegacyJson},
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
        public static readonly string PlayerWeaponStatsJson = Path.Combine(Plugin.ConfigPath, "player_weapon_stats.json");
        public static readonly string PlayerWorkerLegacyJson = Path.Combine(Plugin.ConfigPath, "player_worker.json");
        public static readonly string PlayerWarriorLegacyJson = Path.Combine(Plugin.ConfigPath, "player_warrior.json");
        public static readonly string PlayerScholarLegacyJson = Path.Combine(Plugin.ConfigPath, "player_scholar.json");
        public static readonly string PlayerRogueLegacyJson = Path.Combine(Plugin.ConfigPath, "player_rogue.json");
        public static readonly string PlayerMutantLegacyJson = Path.Combine(Plugin.ConfigPath, "player_mutant.json");
        public static readonly string PlayerVBloodLegacyJson = Path.Combine(Plugin.ConfigPath, "player_vblood.json");
        public static readonly string PlayerDraculinLegacyJson = Path.Combine(Plugin.ConfigPath, "player_draculin.json");
        public static readonly string PlayerImmortalLegacyJson = Path.Combine(Plugin.ConfigPath, "player_immortal.json");
        public static readonly string PlayerCreatureLegacyJson = Path.Combine(Plugin.ConfigPath, "player_creature.json");
        public static readonly string PlayerBruteLegacyJson = Path.Combine(Plugin.ConfigPath, "player_brute.json");
    }
}