using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Quests;
using ProjectM.Network;
using Stunlock.Core;
using System.Text.Json;
using Unity.Entities;
using static Bloodcraft.Services.DataService.PlayerPersistence.JsonFilePaths;
using static Bloodcraft.Services.DataService.PlayerPersistence;
using static Bloodcraft.Services.ConfigService.ConfigInitialization;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using Bloodcraft.Systems.Leveling;
using static Bloodcraft.Services.ConfigService;
using Epic.OnlineServices.Achievements;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft.Services;
internal static class DataService
{
    public static bool TryGetPlayerExperience(this ulong steamID, out KeyValuePair<int, float> experience)
    {
        return playerExperience.TryGetValue(steamID, out experience);
    }
    public static bool TryGetPlayerRestedXP(this ulong steamID, out KeyValuePair<DateTime, float> restedXP)
    {
        return playerRestedXP.TryGetValue(steamID, out restedXP);
    }
    public static bool TryGetPlayerBools(this ulong steamID, out Dictionary<string, bool> bools)
    {
        return playerBools.TryGetValue(steamID, out bools);
    }
    public static bool TryGetPlayerClasses(this ulong steamID, out Dictionary<LevelingSystem.PlayerClasses, (List<int>, List<int>)> classes)
    {
        return playerClass.TryGetValue(steamID, out classes);
    }
    public static bool TryGetPlayerPrestiges(this ulong steamID, out Dictionary<PrestigeType, int> prestiges)
    {
        return playerPrestiges.TryGetValue(steamID, out prestiges);
    }
    public static bool TryGetPlayerWoodcutting(this ulong steamID, out KeyValuePair<int, float> woodcutting)
    {
        return playerWoodcutting.TryGetValue(steamID, out woodcutting);
    }
    public static bool TryGetPlayerMining(this ulong steamID, out KeyValuePair<int, float> mining)
    {
        return playerMining.TryGetValue(steamID, out mining);
    }
    public static bool TryGetPlayerFishing(this ulong steamID, out KeyValuePair<int, float> fishing)
    {
        return playerFishing.TryGetValue(steamID, out fishing);
    }
    public static bool TryGetPlayerBlacksmithing(this ulong steamID, out KeyValuePair<int, float> blacksmithing)
    {
        return playerBlacksmithing.TryGetValue(steamID, out blacksmithing);
    }
    public static bool TryGetPlayerTailoring(this ulong steamID, out KeyValuePair<int, float> tailoring)
    {
        return playerTailoring.TryGetValue(steamID, out tailoring);
    }
    public static bool TryGetPlayerEnchanting(this ulong steamID, out KeyValuePair<int, float> enchanting)
    {
        return playerEnchanting.TryGetValue(steamID, out enchanting);
    }
    public static bool TryGetPlayerAlchemy(this ulong steamID, out KeyValuePair<int, float> alchemy)
    {
        return playerAlchemy.TryGetValue(steamID, out alchemy);
    }
    public static bool TryGetPlayerHarvesting(this ulong steamID, out KeyValuePair<int, float> harvesting)
    {
        return playerHarvesting.TryGetValue(steamID, out harvesting);
    }
    public static bool TryGetPlayerSwordExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerSwordExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerAxeExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerAxeExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerMaceExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerMaceExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerSpearExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerSpearExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerCrossbowExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerCrossbowExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerGreatSwordExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerGreatSwordExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerSlashersExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerSlashersExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerPistolsExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerPistolsExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerReaperExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerReaperExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerLongbowExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerLongbowExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerWhipExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerWhipExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerFishingPoleExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerFishingPoleExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerUnarmedExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return playerUnarmedExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerWeaponStats(this ulong steamID, out Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> weaponStats)
    {
        return playerWeaponStats.TryGetValue(steamID, out weaponStats);
    }
    public static bool TryGetPlayerSpells(this ulong steamID, out (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells)
    {
        return playerSpells.TryGetValue(steamID, out spells);
    }
    public static bool TryGetPlayerWorkerLegacy(this ulong steamID, out KeyValuePair<int, float> workerLegacy)
    {
        return playerWorkerLegacy.TryGetValue(steamID, out workerLegacy);
    }
    public static bool TryGetPlayerWarriorLegacy(this ulong steamID, out KeyValuePair<int, float> warriorLegacy)
    {
        return playerWarriorLegacy.TryGetValue(steamID, out warriorLegacy);
    }
    public static bool TryGetPlayerScholarLegacy(this ulong steamID, out KeyValuePair<int, float> scholarLegacy)
    {
        return playerScholarLegacy.TryGetValue(steamID, out scholarLegacy);
    }
    public static bool TryGetPlayerRogueLegacy(this ulong steamID, out KeyValuePair<int, float> rogueLegacy)
    {
        return playerRogueLegacy.TryGetValue(steamID, out rogueLegacy);
    }
    public static bool TryGetPlayerMutantLegacy(this ulong steamID, out KeyValuePair<int, float> mutantLegacy)
    {
        return playerMutantLegacy.TryGetValue(steamID, out mutantLegacy);
    }
    public static bool TryGetPlayerVBloodLegacy(this ulong steamID, out KeyValuePair<int, float> vBloodLegacy)
    {
        return playerVBloodLegacy.TryGetValue(steamID, out vBloodLegacy);
    }
    public static bool TryGetPlayerDraculinLegacy(this ulong steamID, out KeyValuePair<int, float> draculinLegacy)
    {
        return playerDraculinLegacy.TryGetValue(steamID, out draculinLegacy);
    }
    public static bool TryGetPlayerImmortalLegacy(this ulong steamID, out KeyValuePair<int, float> immortalLegacy)
    {
        return playerImmortalLegacy.TryGetValue(steamID, out immortalLegacy);
    }
    public static bool TryGetPlayerCreatureLegacy(this ulong steamID, out KeyValuePair<int, float> creatureLegacy)
    {
        return playerCreatureLegacy.TryGetValue(steamID, out creatureLegacy);
    }
    public static bool TryGetPlayerBruteLegacy(this ulong steamID, out KeyValuePair<int, float> bruteLegacy)
    {
        return playerBruteLegacy.TryGetValue(steamID, out bruteLegacy);
    }
    public static bool TryGetPlayerBloodStats(this ulong steamID, out Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> bloodStats)
    {
        return playerBloodStats.TryGetValue(steamID, out bloodStats);
    }
    public static bool TryGetFamiliarActives(this ulong steamID, out (Entity Familiar, int FamKey) activeFamiliar)
    {
        return familiarActives.TryGetValue(steamID, out activeFamiliar);
    }
    public static bool TryGetFamiliarBox(this ulong steamID, out string familiarSet)
    {
        return familiarBox.TryGetValue(steamID, out familiarSet);
    }
    public static bool TryGetFamiliarDefault(this ulong steamID, out int defaultFamiliar)
    {
        return familiarDefault.TryGetValue(steamID, out defaultFamiliar);
    }
    public static bool TryGetPlayerQuests(this ulong steamID, out Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> quests)
    {
        return playerQuests.TryGetValue(steamID, out quests);
    }
    public static bool TryGetPlayerParties(this ulong steamID, out HashSet<string> parties)
    {
        return playerParties.TryGetValue(steamID, out parties);
    }
    public static bool TryGetPlayerCraftingJobs(this ulong steamID, out Dictionary<PrefabGUID, int> craftingJobs)
    {
        craftingJobs = [];
        if (steamID.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            return playerCraftingJobs.TryGetValue(playerInfo.UserEntity, out craftingJobs);
        }
        return false;
    }
    public static bool TryGetPlayerMaxWeaponLevels(this ulong steamID, out int maxWeaponLevels)
    {
        return playerMaxWeaponLevels.TryGetValue(steamID, out maxWeaponLevels);
    }
    public static void SetPlayerExperience(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerExperience[steamID] = data;
        SavePlayerExperience();
    }
    public static void SetPlayerRestedXP(this ulong steamID, KeyValuePair<DateTime, float> data)
    {
        playerRestedXP[steamID] = data;
        SavePlayerRestedXP();
    }
    public static void SetPlayerBools(this ulong steamID, Dictionary<string, bool> data)
    {
        playerBools[steamID] = data;
        SavePlayerBools();
    }
    public static void SetPlayerClasses(this ulong steamID, Dictionary<LevelingSystem.PlayerClasses, (List<int>, List<int>)> data)
    {
        playerClass[steamID] = data;
        SavePlayerClasses();
    }
    public static void SetPlayerPrestiges(this ulong steamID, Dictionary<PrestigeType, int> data)
    {
        playerPrestiges[steamID] = data;
        SavePlayerPrestiges();
    }
    public static void SetPlayerWoodcutting(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerWoodcutting[steamID] = data;
        SavePlayerWoodcutting();
    }
    public static void SetPlayerMining(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerMining[steamID] = data;
        SavePlayerMining();
    }
    public static void SetPlayerFishing(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerFishing[steamID] = data;
        SavePlayerFishing();
    }
    public static void SetPlayerBlacksmithing(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerBlacksmithing[steamID] = data;
        SavePlayerBlacksmithing();
    }
    public static void SetPlayerTailoring(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerTailoring[steamID] = data;
        SavePlayerTailoring();
    }
    public static void SetPlayerEnchanting(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerEnchanting[steamID] = data;
        SavePlayerEnchanting();
    }
    public static void SetPlayerAlchemy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerAlchemy[steamID] = data;
        SavePlayerAlchemy();
    }
    public static void SetPlayerHarvesting(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerHarvesting[steamID] = data;
        SavePlayerHarvesting();
    }
    public static void SetPlayerSwordExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerSwordExpertise[steamID] = data;
        SavePlayerSwordExpertise();
    }
    public static void SetPlayerAxeExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerAxeExpertise[steamID] = data;
        SavePlayerAxeExpertise();
    }
    public static void SetPlayerMaceExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerMaceExpertise[steamID] = data;
        SavePlayerMaceExpertise();
    }
    public static void SetPlayerSpearExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerSpearExpertise[steamID] = data;
        SavePlayerSpearExpertise();
    }
    public static void SetPlayerCrossbowExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerCrossbowExpertise[steamID] = data;
        SavePlayerCrossbowExpertise();
    }
    public static void SetPlayerGreatSwordExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerGreatSwordExpertise[steamID] = data;
        SavePlayerGreatSwordExpertise();
    }
    public static void SetPlayerSlashersExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerSlashersExpertise[steamID] = data;
        SavePlayerSlashersExpertise();
    }
    public static void SetPlayerPistolsExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerPistolsExpertise[steamID] = data;
        SavePlayerPistolsExpertise();
    }
    public static void SetPlayerReaperExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerReaperExpertise[steamID] = data;
        SavePlayerReaperExpertise();
    }
    public static void SetPlayerLongbowExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerLongbowExpertise[steamID] = data;
        SavePlayerLongbowExpertise();
    }
    public static void SetPlayerWhipExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerWhipExpertise[steamID] = data;
        SavePlayerWhipExpertise();
    }
    public static void SetPlayerFishingPoleExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerFishingPoleExpertise[steamID] = data;
        SavePlayerFishingPoleExpertise();
    }
    public static void SetPlayerUnarmedExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerUnarmedExpertise[steamID] = data;
        SavePlayerUnarmedExpertise();
    }
    public static void SetPlayerWeaponStats(this ulong steamID, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> data)
    {
        playerWeaponStats[steamID] = data;
        SavePlayerWeaponStats();
    }
    public static void SetPlayerSpells(this ulong steamID, (int FirstUnarmed, int SecondUnarmed, int ClassSpell) data)
    {
        playerSpells[steamID] = data;
        SavePlayerSpells();
    }
    public static void SetPlayerWorkerLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerWorkerLegacy[steamID] = data;
        SavePlayerWorkerLegacy();
    }
    public static void SetPlayerWarriorLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerWarriorLegacy[steamID] = data;
        SavePlayerWarriorLegacy();
    }
    public static void SetPlayerScholarLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerScholarLegacy[steamID] = data;
        SavePlayerScholarLegacy();
    }
    public static void SetPlayerRogueLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerRogueLegacy[steamID] = data;
        SavePlayerRogueLegacy();
    }
    public static void SetPlayerMutantLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerMutantLegacy[steamID] = data;
        SavePlayerMutantLegacy();
    }
    public static void SetPlayerVBloodLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerVBloodLegacy[steamID] = data;
        SavePlayerVBloodLegacy();
    }
    public static void SetPlayerDraculinLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerDraculinLegacy[steamID] = data;
        SavePlayerDraculinLegacy();
    }
    public static void SetPlayerImmortalLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerImmortalLegacy[steamID] = data;
        SavePlayerImmortalLegacy();
    }
    public static void SetPlayerCreatureLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerCreatureLegacy[steamID] = data;
        SavePlayerCreatureLegacy();
    }
    public static void SetPlayerBruteLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        playerBruteLegacy[steamID] = data;
        SavePlayerBruteLegacy();
    }
    public static void SetPlayerBloodStats(this ulong steamID, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> data)
    {
        playerBloodStats[steamID] = data;
        SavePlayerBloodStats();
    }
    public static void SetFamiliarActives(this ulong steamID, (Entity Familiar, int FamKey) data)
    {
        familiarActives[steamID] = data;
        SavePlayerFamiliarActives();
    }
    public static void SetFamiliarBox(this ulong steamID, string data)
    {
        familiarBox[steamID] = data;
        SavePlayerFamiliarSets();
    }
    public static void SetFamiliarDefault(this ulong steamID, int data)
    {
        familiarDefault[steamID] = data;
        SavePlayerFamiliarSets();
    }
    public static void SetPlayerQuests(this ulong steamID, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> data)
    {
        playerQuests[steamID] = data;
        SavePlayerQuests();
    }
    public static void SetPlayerParties(this ulong steamID, HashSet<string> data)
    {
        playerParties[steamID] = data;
        SavePlayerParties();
    }
    public static void SetPlayerCraftingJobs(this ulong steamID, Dictionary<PrefabGUID, int> data)
    {
        if (steamID.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            playerCraftingJobs[playerInfo.UserEntity] = data;
        }
    }
    public static void SetPlayerMaxWeaponLevels(this ulong steamID, int data)
    {
        playerMaxWeaponLevels[steamID] = data;
    }
    internal static class PlayerDictionaries
    {
        // leveling
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerExperience = [];
        internal static Dictionary<ulong, KeyValuePair<DateTime, float>> playerRestedXP = [];

        // bools
        internal static Dictionary<ulong, Dictionary<string, bool>> playerBools = [];

        // classes
        internal static Dictionary<ulong, Dictionary<LevelingSystem.PlayerClasses, (List<int> WeaponStats, List<int> BloodStats)>> playerClass = [];

        // prestiges
        internal static Dictionary<ulong, Dictionary<PrestigeType, int>> playerPrestiges = [];

        // professions
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerWoodcutting = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerMining = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerFishing = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerBlacksmithing = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerTailoring = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerEnchanting = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerAlchemy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerHarvesting = [];

        // weapon expertise
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerSwordExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerAxeExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerMaceExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerSpearExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerCrossbowExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerGreatSwordExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerSlashersExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerPistolsExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerReaperExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerLongbowExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerWhipExpertise = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerFishingPoleExpertise = [];
        internal static Dictionary<ulong, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>>> playerWeaponStats = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerUnarmedExpertise = []; // this is unarmed and needs to be renamed to match the rest
        internal static Dictionary<ulong, (int FirstUnarmed, int SecondUnarmed, int ClassSpell)> playerSpells = [];

        // blood legacies
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerWorkerLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerWarriorLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerScholarLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerRogueLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerMutantLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerVBloodLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerDraculinLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerImmortalLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerCreatureLegacy = [];
        internal static Dictionary<ulong, KeyValuePair<int, float>> playerBruteLegacy = [];
        internal static Dictionary<ulong, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>>> playerBloodStats = [];

        // familiar data
        internal static Dictionary<ulong, (Entity Familiar, int FamKey)> familiarActives = [];
        internal static Dictionary<ulong, string> familiarBox = [];
        internal static Dictionary<ulong, int> familiarDefault = [];

        // quest data
        internal static Dictionary<ulong, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)>> playerQuests = [];

        // parties
        internal static Dictionary<ulong, HashSet<string>> playerParties = [];

        // cache-only
        internal static Dictionary<Entity, Dictionary<PrefabGUID, int>> playerCraftingJobs = []; // userEntities
        internal static Dictionary<ulong, int> playerMaxWeaponLevels = [];
    }
    internal static class PlayerPersistence
    {
        static readonly JsonSerializerOptions prettyJsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };
        static readonly Dictionary<string, string> filePaths = new()
        {
            {"Experience", PlayerExperienceJson},
            {"RestedXP", PlayerRestedXPJson },
            {"Quests", PlayerQuestsJson },
            {"Classes", PlayerClassesJson },
            {"Prestiges", PlayerPrestigesJson },
            {"PlayerBools", PlayerBoolsJson},
            {"PlayerParties", PlayerPartiesJson},
            {"Woodcutting", PlayerWoodcuttingJson},
            {"Mining", PlayerMiningJson},
            {"Fishing", PlayerFishingJson},
            {"Blacksmithing", PlayerBlacksmithingJson},
            {"Tailoring", PlayerTailoringJson},
            {"Enchanting", PlayerEnchantingJson},
            {"Alchemy", PlayerAlchemyJson},
            {"Harvesting", PlayerHarvestingJson},
            {"SwordExpertise", PlayerSwordExpertiseJson },
            {"AxeExpertise", PlayerAxeExpertiseJson},
            {"MaceExpertise", PlayerMaceExpertiseJson},
            {"SpearExpertise", PlayerSpearExpertiseJson},
            {"CrossbowExpertise", PlayerCrossbowExpertiseJson},
            {"GreatSwordExpertise", PlayerGreatSwordExpertise},
            {"SlashersExpertise", PlayerSlashersExpertiseJson},
            {"PistolsExpertise", PlayerPistolsExpertiseJson},
            {"ReaperExpertise", PlayerReaperExpertise},
            {"LongbowExpertise", PlayerLongbowExpertiseJson},
            {"WhipExpertise", PlayerWhipExpertiseJson},
            {"FishingPoleExpertise", PlayerFishingPoleExpertiseJson},
            {"UnarmedExpertise", PlayerUnarmedExpertiseJson},
            {"PlayerSpells", PlayerSpellsJson},
            {"WeaponStats", PlayerWeaponStatsJson},
            {"WorkerLegacy", PlayerWorkerLegacyJson},
            {"WarriorLegacy", PlayerWarriorLegacyJson},
            {"ScholarLegacy", PlayerScholarLegacyJson},
            {"RogueLegacy", PlayerRogueLegacyJson},
            {"MutantLegacy", PlayerMutantLegacyJson},
            {"VBloodLegacy", PlayerVBloodLegacyJson},
            {"DraculinLegacy", PlayerDraculinLegacyJson},
            {"ImmortalLegacy", PlayerImmortalLegacyJson},
            {"CreatureLegacy", PlayerCreatureLegacyJson},
            {"BruteLegacy", PlayerBruteLegacyJson},
            {"BloodStats", PlayerBloodStatsJson},
            {"FamiliarActives", PlayerFamiliarActivesJson},
            {"FamiliarSets", PlayerFamiliarSetsJson }
        };
        internal static class JsonFilePaths
        {
            internal static readonly string PlayerExperienceJson = Path.Combine(DirectoryPaths[1], "player_experience.json");
            internal static readonly string PlayerRestedXPJson = Path.Combine(DirectoryPaths[1], "player_rested_xp.json");
            internal static readonly string PlayerQuestsJson = Path.Combine(DirectoryPaths[2], "player_quests.json");
            internal static readonly string PlayerPrestigesJson = Path.Combine(DirectoryPaths[1], "player_prestiges.json");
            internal static readonly string PlayerClassesJson = Path.Combine(DirectoryPaths[0], "player_classes.json");
            internal static readonly string PlayerBoolsJson = Path.Combine(DirectoryPaths[0], "player_bools.json");
            internal static readonly string PlayerPartiesJson = Path.Combine(DirectoryPaths[0], "player_parties.json");
            internal static readonly string PlayerWoodcuttingJson = Path.Combine(DirectoryPaths[5], "player_woodcutting.json");
            internal static readonly string PlayerMiningJson = Path.Combine(DirectoryPaths[5], "player_mining.json");
            internal static readonly string PlayerFishingJson = Path.Combine(DirectoryPaths[5], "player_fishing.json");
            internal static readonly string PlayerBlacksmithingJson = Path.Combine(DirectoryPaths[5], "player_blacksmithing.json");
            internal static readonly string PlayerTailoringJson = Path.Combine(DirectoryPaths[5], "player_tailoring.json");
            internal static readonly string PlayerEnchantingJson = Path.Combine(DirectoryPaths[5], "player_enchanting.json");
            internal static readonly string PlayerAlchemyJson = Path.Combine(DirectoryPaths[5], "player_alchemy.json");
            internal static readonly string PlayerHarvestingJson = Path.Combine(DirectoryPaths[5], "player_harvesting.json");
            internal static readonly string PlayerSwordExpertiseJson = Path.Combine(DirectoryPaths[3], "player_sword.json");
            internal static readonly string PlayerAxeExpertiseJson = Path.Combine(DirectoryPaths[3], "player_axe.json");
            internal static readonly string PlayerMaceExpertiseJson = Path.Combine(DirectoryPaths[3], "player_mace.json");
            internal static readonly string PlayerSpearExpertiseJson = Path.Combine(DirectoryPaths[3], "player_spear.json");
            internal static readonly string PlayerCrossbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_crossbow.json");
            internal static readonly string PlayerGreatSwordExpertise = Path.Combine(DirectoryPaths[3], "player_greatsword.json");
            internal static readonly string PlayerSlashersExpertiseJson = Path.Combine(DirectoryPaths[3], "player_slashers.json");
            internal static readonly string PlayerPistolsExpertiseJson = Path.Combine(DirectoryPaths[3], "player_pistols.json");
            internal static readonly string PlayerReaperExpertise = Path.Combine(DirectoryPaths[3], "player_reaper.json");
            internal static readonly string PlayerLongbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_longbow.json");
            internal static readonly string PlayerUnarmedExpertiseJson = Path.Combine(DirectoryPaths[3], "player_unarmed.json");
            internal static readonly string PlayerWhipExpertiseJson = Path.Combine(DirectoryPaths[3], "player_whip.json");
            internal static readonly string PlayerFishingPoleExpertiseJson = Path.Combine(DirectoryPaths[3], "player_fishingpole.json");
            internal static readonly string PlayerSpellsJson = Path.Combine(DirectoryPaths[1], "player_spells.json");
            internal static readonly string PlayerWeaponStatsJson = Path.Combine(DirectoryPaths[3], "player_weapon_stats.json");
            internal static readonly string PlayerWorkerLegacyJson = Path.Combine(DirectoryPaths[4], "player_worker.json");
            internal static readonly string PlayerWarriorLegacyJson = Path.Combine(DirectoryPaths[4], "player_warrior.json");
            internal static readonly string PlayerScholarLegacyJson = Path.Combine(DirectoryPaths[4], "player_scholar.json");
            internal static readonly string PlayerRogueLegacyJson = Path.Combine(DirectoryPaths[4], "player_rogue.json");
            internal static readonly string PlayerMutantLegacyJson = Path.Combine(DirectoryPaths[4], "player_mutant.json");
            internal static readonly string PlayerVBloodLegacyJson = Path.Combine(DirectoryPaths[4], "player_vblood.json");
            internal static readonly string PlayerDraculinLegacyJson = Path.Combine(DirectoryPaths[4], "player_draculin.json");
            internal static readonly string PlayerImmortalLegacyJson = Path.Combine(DirectoryPaths[4], "player_immortal.json");
            internal static readonly string PlayerCreatureLegacyJson = Path.Combine(DirectoryPaths[4], "player_creature.json");
            internal static readonly string PlayerBruteLegacyJson = Path.Combine(DirectoryPaths[4], "player_brute.json");
            internal static readonly string PlayerBloodStatsJson = Path.Combine(DirectoryPaths[4], "player_blood_stats.json");
            internal static readonly string PlayerFamiliarActivesJson = Path.Combine(DirectoryPaths[6], "player_familiar_actives.json");
            internal static readonly string PlayerFamiliarSetsJson = Path.Combine(DirectoryPaths[8], "player_familiar_sets.json");
        }
        static void LoadData<T>(ref Dictionary<ulong, T> dataStructure, string key)
        {
            string path = filePaths[key];
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                dataStructure = [];
                Core.Log.LogInfo($"{key} file created as it did not exist.");
                return;
            }
            try
            {
                string json = File.ReadAllText(path);

                json = json.Replace("Sanguimancy", "UnarmedExpertise");

                if (string.IsNullOrWhiteSpace(json))
                {
                    dataStructure = [];
                }
                else
                {
                    var data = JsonSerializer.Deserialize<Dictionary<ulong, T>>(json, prettyJsonOptions);
                    dataStructure = data ?? [];
                }
            }
            catch (IOException ex)
            {
                Core.Log.LogInfo($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void SaveData<T>(Dictionary<ulong, T> data, string key)
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
                Core.Log.LogInfo($"Failed to write {key} data to file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Core.Log.LogInfo($"JSON serialization error when saving {key} data: {ex.Message}");
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

        public static void LoadPlayerFamiliarSets() => LoadData(ref familiarBox, "FamiliarSets");

        public static void SavePlayerExperience() => SaveData(playerExperience, "Experience");

        public static void SavePlayerRestedXP() => SaveData(playerRestedXP, "RestedXP");

        public static void SavePlayerQuests() => SaveData(playerQuests, "Quests");

        public static void SavePlayerClasses() => SaveData(playerClass, "Classes");

        public static void SavePlayerPrestiges() => SaveData(playerPrestiges, "Prestiges");

        public static void SavePlayerBools() => SaveData(playerBools, "PlayerBools");

        public static void SavePlayerParties() => SaveData(playerParties, "PlayerParties");

        public static void SavePlayerWoodcutting() => SaveData(playerWoodcutting, "Woodcutting");

        public static void SavePlayerMining() => SaveData(playerMining, "Mining");

        public static void SavePlayerFishing() => SaveData(playerFishing, "Fishing");

        public static void SavePlayerBlacksmithing() => SaveData(playerBlacksmithing, "Blacksmithing");

        public static void SavePlayerTailoring() => SaveData(playerTailoring, "Tailoring");

        public static void SavePlayerEnchanting() => SaveData(playerEnchanting, "Enchanting");

        public static void SavePlayerAlchemy() => SaveData(playerAlchemy, "Alchemy");

        public static void SavePlayerHarvesting() => SaveData(playerHarvesting, "Harvesting");

        public static void SavePlayerSwordExpertise() => SaveData(playerSwordExpertise, "SwordExpertise");

        public static void SavePlayerAxeExpertise() => SaveData(playerAxeExpertise, "AxeExpertise");

        public static void SavePlayerMaceExpertise() => SaveData(playerMaceExpertise, "MaceExpertise");

        public static void SavePlayerSpearExpertise() => SaveData(playerSpearExpertise, "SpearExpertise");

        public static void SavePlayerCrossbowExpertise() => SaveData(playerCrossbowExpertise, "CrossbowExpertise");

        public static void SavePlayerGreatSwordExpertise() => SaveData(playerGreatSwordExpertise, "GreatSwordExpertise");

        public static void SavePlayerSlashersExpertise() => SaveData(playerSlashersExpertise, "SlashersExpertise");

        public static void SavePlayerPistolsExpertise() => SaveData(playerPistolsExpertise, "PistolsExpertise");

        public static void SavePlayerReaperExpertise() => SaveData(playerReaperExpertise, "ReaperExpertise");

        public static void SavePlayerLongbowExpertise() => SaveData(playerLongbowExpertise, "LongbowExpertise");

        public static void SavePlayerWhipExpertise() => SaveData(playerWhipExpertise, "WhipExpertise");

        public static void SavePlayerFishingPoleExpertise() => SaveData(playerFishingPoleExpertise, "FishingPoleExpertise");

        public static void SavePlayerUnarmedExpertise() => SaveData(playerUnarmedExpertise, "UnarmedExpertise");

        public static void SavePlayerSpells() => SaveData(playerSpells, "PlayerSpells");

        public static void SavePlayerWeaponStats() => SaveData(playerWeaponStats, "WeaponStats");

        public static void SavePlayerWorkerLegacy() => SaveData(playerWorkerLegacy, "WorkerLegacy");

        public static void SavePlayerWarriorLegacy() => SaveData(playerWarriorLegacy, "WarriorLegacy");

        public static void SavePlayerScholarLegacy() => SaveData(playerScholarLegacy, "ScholarLegacy");

        public static void SavePlayerRogueLegacy() => SaveData(playerRogueLegacy, "RogueLegacy");

        public static void SavePlayerMutantLegacy() => SaveData(playerMutantLegacy, "MutantLegacy");

        public static void SavePlayerVBloodLegacy() => SaveData(playerVBloodLegacy, "VBloodLegacy");

        public static void SavePlayerDraculinLegacy() => SaveData(playerDraculinLegacy, "DraculinLegacy");

        public static void SavePlayerImmortalLegacy() => SaveData(playerImmortalLegacy, "ImmortalLegacy");

        public static void SavePlayerCreatureLegacy() => SaveData(playerCreatureLegacy, "CreatureLegacy");

        public static void SavePlayerBruteLegacy() => SaveData(playerBruteLegacy, "BruteLegacy");

        public static void SavePlayerBloodStats() => SaveData(playerBloodStats, "BloodStats");

        public static void SavePlayerFamiliarActives() => SaveData(familiarActives, "FamiliarActives");

        public static void SavePlayerFamiliarSets() => SaveData(familiarBox, "FamiliarSets");
    }
    internal static class FamiliarPersistence
    {
        [Serializable]
        internal class FamiliarExperienceData
        {
            public Dictionary<int, KeyValuePair<int, float>> FamiliarExperience { get; set; } = [];
        }

        [Serializable]
        internal class FamiliarPrestigeData
        {
            public Dictionary<int, KeyValuePair<int, List<FamiliarSummonSystem.FamiliarStatType>>> FamiliarPrestige { get; set; } = [];
        }

        [Serializable]
        internal class UnlockedFamiliarData
        {
            public Dictionary<string, List<int>> UnlockedFamiliars { get; set; } = [];
        }

        [Serializable]
        internal class FamiliarBuffsData
        {
            public Dictionary<int, List<int>> FamiliarBuffs { get; set; } = [];
        }

        internal static Dictionary<ulong, UnlockedFamiliarData> unlockedFamiliars = [];
        internal static Dictionary<ulong, FamiliarExperienceData> familiarExperience = [];
        internal static Dictionary<ulong, FamiliarPrestigeData> familiarPrestiges = [];
        internal static Dictionary<ulong, FamiliarBuffsData> familiarBuffs = [];
        internal static class FamiliarExperienceManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[7], $"{playerId}_familiar_experience.json");

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
        internal static class FamiliarPrestigeManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[7], $"{playerId}_familiar_prestige.json");

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
        internal static class FamiliarBuffsManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[8], $"{playerId}_familiar_buffs.json");

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
        internal static class FamiliarUnlocksManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[8], $"{playerId}_familiar_unlocks.json");

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
    }
    internal static class PlayerDataInitialization
    {
        public static void LoadPlayerData()
        {
            try
            {
                string PlayerSanguimancyJson = Path.Combine(DirectoryPaths[3], "player_sanguimancy.json"); // handle old format to new
                string PlayerUnarmedJson = Path.Combine(DirectoryPaths[3], "player_unarmed.json");

                if (File.Exists(PlayerSanguimancyJson) && !File.Exists(PlayerUnarmedJson))
                {
                    // Copy the old file to the new file name
                    File.Copy(PlayerSanguimancyJson, PlayerUnarmedExpertiseJson, overwrite: false);
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to migrate sanguimancy data to unarmed: {ex}");
            }

            LoadPlayerBools();

            if (PlayerParties)
            {
                LoadPlayerParties();
            }

            if (SoftSynergies || HardSynergies)
            {
                LoadPlayerClasses();
            }

            if (ConfigService.QuestSystem)
            {
                LoadPlayerQuests();
            }

            if (ConfigService.LevelingSystem)
            {
                foreach (var loadFunction in loadLeveling)
                {
                    loadFunction();
                }
                if (RestedXPSystem)
                {
                    LoadPlayerRestedXP();
                }
            }

            if (ExpertiseSystem)
            {
                foreach (var loadFunction in loadExpertises)
                {
                    loadFunction();
                }
            }

            if (ConfigService.BloodSystem)
            {
                foreach (var loadFunction in loadLegacies)
                {
                    loadFunction();
                }
            }

            if (ConfigService.ProfessionSystem)
            {
                foreach (var loadFunction in loadProfessions)
                {
                    loadFunction();
                }
            }

            if (FamiliarSystem)
            {
                foreach (var loadFunction in loadFamiliars)
                {
                    loadFunction();
                }
            }
        }

        static readonly Action[] loadLeveling =
        [
            LoadPlayerExperience,
            LoadPlayerPrestiges
        ];

        static readonly Action[] loadExpertises =
        [
            LoadPlayerSwordExpertise,
            LoadPlayerAxeExpertise,
            LoadPlayerMaceExpertise,
            LoadPlayerSpearExpertise,
            LoadPlayerCrossbowExpertise,
            LoadPlayerGreatSwordExpertise,
            LoadPlayerSlashersExpertise,
            LoadPlayerPistolsExpertise,
            LoadPlayerReaperExpertise,
            LoadPlayerLongbowExpertise,
            LoadPlayerWhipExpertise,
            LoadPlayerFishingPoleExpertise,
            LoadPlayerUnarmedExpertise,
            LoadPlayerSpells,
            LoadPlayerWeaponStats
        ];

        static readonly Action[] loadLegacies =
        [
            LoadPlayerWorkerLegacy,
            LoadPlayerWarriorLegacy,
            LoadPlayerScholarLegacy,
            LoadPlayerRogueLegacy,
            LoadPlayerMutantLegacy,
            LoadPlayerVBloodLegacy,
            LoadPlayerDraculinLegacy,
            LoadPlayerImmortalLegacy,
            LoadPlayerCreatureLegacy,
            LoadPlayerBruteLegacy,
            LoadPlayerBloodStats
        ];

        static readonly Action[] loadProfessions =
        [
            LoadPlayerWoodcutting,
            LoadPlayerMining,
            LoadPlayerFishing,
            LoadPlayerBlacksmithing,
            LoadPlayerTailoring,
            LoadPlayerEnchanting,
            LoadPlayerAlchemy,
            LoadPlayerHarvesting,
        ];

        static readonly Action[] loadFamiliars =
        [
            LoadPlayerFamiliarActives,
            LoadPlayerFamiliarSets
        ];
    }
}
