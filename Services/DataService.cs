using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using System.Collections.Concurrent;
using System.Text.Json;
using Unity.Entities;
using static Bloodcraft.Services.ConfigService;
using static Bloodcraft.Services.ConfigService.ConfigInitialization;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.DataService.PlayerPersistence;
using static Bloodcraft.Services.DataService.PlayerPersistence.JsonFilePaths;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Utilities.Misc;

namespace Bloodcraft.Services;
internal static class DataService
{
    public static bool TryGetPlayerBools(this ulong steamID, out ConcurrentDictionary<string, bool> bools)
    {
        bools = [];

        if (!_playerBools.Any()) return false;
        else return _playerBools.TryGetValue(steamID, out bools);
    }
    public static void SetPlayerBools(this ulong steamID, ConcurrentDictionary<string, bool> data)
    {
        _playerBools[steamID] = data;
        SavePlayerBools();
    }
    public static bool TryGetPlayerExperience(this ulong steamID, out KeyValuePair<int, float> experience)
    {
        return _playerExperience.TryGetValue(steamID, out experience);
    }
    public static bool TryGetPlayerRestedXP(this ulong steamID, out KeyValuePair<DateTime, float> restedXP)
    {
        return _playerRestedXP.TryGetValue(steamID, out restedXP);
    }
    public static bool TryGetPlayerClasses(this ulong steamID, out Dictionary<Classes.PlayerClass, (List<int>, List<int>)> classes)
    {
        return _playerClass.TryGetValue(steamID, out classes);
    }
    public static bool TryGetPlayerPrestiges(this ulong steamID, out Dictionary<PrestigeType, int> prestiges)
    {
        return _playerPrestiges.TryGetValue(steamID, out prestiges);
    }
    public static bool TryGetPlayerExoFormData(this ulong steamID, out KeyValuePair<DateTime, float> exoFormData)
    {
        return _playerExoFormData.TryGetValue(steamID, out exoFormData);
    }
    public static bool TryGetPlayerWoodcutting(this ulong steamID, out KeyValuePair<int, float> woodcutting)
    {
        return _playerWoodcutting.TryGetValue(steamID, out woodcutting);
    }
    public static bool TryGetPlayerMining(this ulong steamID, out KeyValuePair<int, float> mining)
    {
        return _playerMining.TryGetValue(steamID, out mining);
    }
    public static bool TryGetPlayerFishing(this ulong steamID, out KeyValuePair<int, float> fishing)
    {
        return _playerFishing.TryGetValue(steamID, out fishing);
    }
    public static bool TryGetPlayerBlacksmithing(this ulong steamID, out KeyValuePair<int, float> blacksmithing)
    {
        return _playerBlacksmithing.TryGetValue(steamID, out blacksmithing);
    }
    public static bool TryGetPlayerTailoring(this ulong steamID, out KeyValuePair<int, float> tailoring)
    {
        return _playerTailoring.TryGetValue(steamID, out tailoring);
    }
    public static bool TryGetPlayerEnchanting(this ulong steamID, out KeyValuePair<int, float> enchanting)
    {
        return _playerEnchanting.TryGetValue(steamID, out enchanting);
    }
    public static bool TryGetPlayerAlchemy(this ulong steamID, out KeyValuePair<int, float> alchemy)
    {
        return _playerAlchemy.TryGetValue(steamID, out alchemy);
    }
    public static bool TryGetPlayerHarvesting(this ulong steamID, out KeyValuePair<int, float> harvesting)
    {
        return _playerHarvesting.TryGetValue(steamID, out harvesting);
    }
    public static bool TryGetPlayerSwordExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerSwordExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerAxeExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerAxeExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerMaceExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerMaceExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerSpearExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerSpearExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerCrossbowExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerCrossbowExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerGreatSwordExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return PlayerDictionaries._playerGreatSwordExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerSlashersExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerSlashersExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerPistolsExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerPistolsExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerReaperExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return PlayerDictionaries._playerReaperExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerLongbowExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerLongbowExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerWhipExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerWhipExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerFishingPoleExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerFishingPoleExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerUnarmedExpertise(this ulong steamID, out KeyValuePair<int, float> expertise)
    {
        return _playerUnarmedExpertise.TryGetValue(steamID, out expertise);
    }
    public static bool TryGetPlayerWeaponStats(this ulong steamID, out Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> weaponStats)
    {
        return _playerWeaponStats.TryGetValue(steamID, out weaponStats);
    }
    public static bool TryGetPlayerSpells(this ulong steamID, out (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells)
    {
        return _playerSpells.TryGetValue(steamID, out spells);
    }
    public static bool TryGetPlayerWorkerLegacy(this ulong steamID, out KeyValuePair<int, float> workerLegacy)
    {
        return _playerWorkerLegacy.TryGetValue(steamID, out workerLegacy);
    }
    public static bool TryGetPlayerWarriorLegacy(this ulong steamID, out KeyValuePair<int, float> warriorLegacy)
    {
        return _playerWarriorLegacy.TryGetValue(steamID, out warriorLegacy);
    }
    public static bool TryGetPlayerScholarLegacy(this ulong steamID, out KeyValuePair<int, float> scholarLegacy)
    {
        return _playerScholarLegacy.TryGetValue(steamID, out scholarLegacy);
    }
    public static bool TryGetPlayerRogueLegacy(this ulong steamID, out KeyValuePair<int, float> rogueLegacy)
    {
        return _playerRogueLegacy.TryGetValue(steamID, out rogueLegacy);
    }
    public static bool TryGetPlayerMutantLegacy(this ulong steamID, out KeyValuePair<int, float> mutantLegacy)
    {
        return _playerMutantLegacy.TryGetValue(steamID, out mutantLegacy);
    }
    public static bool TryGetPlayerVBloodLegacy(this ulong steamID, out KeyValuePair<int, float> vBloodLegacy)
    {
        return _playerVBloodLegacy.TryGetValue(steamID, out vBloodLegacy);
    }
    public static bool TryGetPlayerDraculinLegacy(this ulong steamID, out KeyValuePair<int, float> draculinLegacy)
    {
        return _playerDraculinLegacy.TryGetValue(steamID, out draculinLegacy);
    }
    public static bool TryGetPlayerImmortalLegacy(this ulong steamID, out KeyValuePair<int, float> immortalLegacy)
    {
        return _playerImmortalLegacy.TryGetValue(steamID, out immortalLegacy);
    }
    public static bool TryGetPlayerCreatureLegacy(this ulong steamID, out KeyValuePair<int, float> creatureLegacy)
    {
        return _playerCreatureLegacy.TryGetValue(steamID, out creatureLegacy);
    }
    public static bool TryGetPlayerBruteLegacy(this ulong steamID, out KeyValuePair<int, float> bruteLegacy)
    {
        return _playerBruteLegacy.TryGetValue(steamID, out bruteLegacy);
    }
    public static bool TryGetPlayerBloodStats(this ulong steamID, out Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> bloodStats)
    {
        return _playerBloodStats.TryGetValue(steamID, out bloodStats);
    }
    public static bool TryGetFamiliarActives(this ulong steamID, out (Entity Familiar, int FamKey) activeFamiliar)
    {
        return _familiarActives.TryGetValue(steamID, out activeFamiliar);
    }
    public static bool TryGetFamiliarBox(this ulong steamID, out string familiarSet)
    {
        return _familiarBox.TryGetValue(steamID, out familiarSet);
    }
    public static bool TryGetFamiliarDefault(this ulong steamID, out int defaultFamiliar)
    {
        return _familiarDefault.TryGetValue(steamID, out defaultFamiliar);
    }
    public static bool TryGetFamiliarBattleGroup(this ulong steamID, out List<int> battleGroup)
    {
        return _playerBattleGroups.TryGetValue(steamID, out battleGroup);
    }
    public static bool TryGetPlayerQuests(this ulong steamID, out Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> quests)
    {
        return _playerQuests.TryGetValue(steamID, out quests);
    }
    public static bool TryGetPlayerParties(this ulong steamID, out ConcurrentList<string> parties)
    {
        return _playerParties.TryGetValue(steamID, out parties);
    }
    public static void SetPlayerExperience(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerExperience[steamID] = data;
        SavePlayerExperience();
    }
    public static void SetPlayerRestedXP(this ulong steamID, KeyValuePair<DateTime, float> data)
    {
        _playerRestedXP[steamID] = data;
        SavePlayerRestedXP();
    }
    public static void SetPlayerClasses(this ulong steamID, Dictionary<Classes.PlayerClass, (List<int>, List<int>)> data)
    {
        _playerClass[steamID] = data;
        SavePlayerClasses();
    }
    public static void SetPlayerPrestiges(this ulong steamID, Dictionary<PrestigeType, int> data)
    {
        _playerPrestiges[steamID] = data;
        SavePlayerPrestiges();
    }
    public static void SetPlayerExoFormData(this ulong steamID, KeyValuePair<DateTime, float> data)
    {
        _playerExoFormData[steamID] = data;
        SavePlayerExoFormData();
    }
    public static void SetPlayerWoodcutting(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerWoodcutting[steamID] = data;
        SavePlayerWoodcutting();
    }
    public static void SetPlayerMining(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerMining[steamID] = data;
        SavePlayerMining();
    }
    public static void SetPlayerFishing(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerFishing[steamID] = data;
        SavePlayerFishing();
    }
    public static void SetPlayerBlacksmithing(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerBlacksmithing[steamID] = data;
        SavePlayerBlacksmithing();
    }
    public static void SetPlayerTailoring(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerTailoring[steamID] = data;
        SavePlayerTailoring();
    }
    public static void SetPlayerEnchanting(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerEnchanting[steamID] = data;
        SavePlayerEnchanting();
    }
    public static void SetPlayerAlchemy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerAlchemy[steamID] = data;
        SavePlayerAlchemy();
    }
    public static void SetPlayerHarvesting(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerHarvesting[steamID] = data;
        SavePlayerHarvesting();
    }
    public static void SetPlayerSwordExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerSwordExpertise[steamID] = data;
        SavePlayerSwordExpertise();
    }
    public static void SetPlayerAxeExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerAxeExpertise[steamID] = data;
        SavePlayerAxeExpertise();
    }
    public static void SetPlayerMaceExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerMaceExpertise[steamID] = data;
        SavePlayerMaceExpertise();
    }
    public static void SetPlayerSpearExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerSpearExpertise[steamID] = data;
        SavePlayerSpearExpertise();
    }
    public static void SetPlayerCrossbowExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerCrossbowExpertise[steamID] = data;
        SavePlayerCrossbowExpertise();
    }
    public static void SetPlayerGreatSwordExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        PlayerDictionaries._playerGreatSwordExpertise[steamID] = data;
        SavePlayerGreatSwordExpertise();
    }
    public static void SetPlayerSlashersExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerSlashersExpertise[steamID] = data;
        SavePlayerSlashersExpertise();
    }
    public static void SetPlayerPistolsExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerPistolsExpertise[steamID] = data;
        SavePlayerPistolsExpertise();
    }
    public static void SetPlayerReaperExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        PlayerDictionaries._playerReaperExpertise[steamID] = data;
        SavePlayerReaperExpertise();
    }
    public static void SetPlayerLongbowExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerLongbowExpertise[steamID] = data;
        SavePlayerLongbowExpertise();
    }
    public static void SetPlayerWhipExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerWhipExpertise[steamID] = data;
        SavePlayerWhipExpertise();
    }
    public static void SetPlayerFishingPoleExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerFishingPoleExpertise[steamID] = data;
        SavePlayerFishingPoleExpertise();
    }
    public static void SetPlayerUnarmedExpertise(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerUnarmedExpertise[steamID] = data;
        SavePlayerUnarmedExpertise();
    }
    public static void SetPlayerWeaponStats(this ulong steamID, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> data)
    {
        _playerWeaponStats[steamID] = data;
        SavePlayerWeaponStats();
    }
    public static void SetPlayerSpells(this ulong steamID, (int FirstUnarmed, int SecondUnarmed, int ClassSpell) data)
    {
        _playerSpells[steamID] = data;
        SavePlayerSpells();
    }
    public static void SetPlayerWorkerLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerWorkerLegacy[steamID] = data;
        SavePlayerWorkerLegacy();
    }
    public static void SetPlayerWarriorLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerWarriorLegacy[steamID] = data;
        SavePlayerWarriorLegacy();
    }
    public static void SetPlayerScholarLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerScholarLegacy[steamID] = data;
        SavePlayerScholarLegacy();
    }
    public static void SetPlayerRogueLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerRogueLegacy[steamID] = data;
        SavePlayerRogueLegacy();
    }
    public static void SetPlayerMutantLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerMutantLegacy[steamID] = data;
        SavePlayerMutantLegacy();
    }
    public static void SetPlayerVBloodLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerVBloodLegacy[steamID] = data;
        SavePlayerVBloodLegacy();
    }
    public static void SetPlayerDraculinLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerDraculinLegacy[steamID] = data;
        SavePlayerDraculinLegacy();
    }
    public static void SetPlayerImmortalLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerImmortalLegacy[steamID] = data;
        SavePlayerImmortalLegacy();
    }
    public static void SetPlayerCreatureLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerCreatureLegacy[steamID] = data;
        SavePlayerCreatureLegacy();
    }
    public static void SetPlayerBruteLegacy(this ulong steamID, KeyValuePair<int, float> data)
    {
        _playerBruteLegacy[steamID] = data;
        SavePlayerBruteLegacy();
    }
    public static void SetPlayerBloodStats(this ulong steamID, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> data)
    {
        _playerBloodStats[steamID] = data;
        SavePlayerBloodStats();
    }
    public static void SetFamiliarActives(this ulong steamID, (Entity Familiar, int FamKey) data)
    {
        _familiarActives[steamID] = data;
        SavePlayerFamiliarActives();
    }
    public static void SetFamiliarBox(this ulong steamID, string data)
    {
        _familiarBox[steamID] = data;
        SavePlayerFamiliarSets();
    }
    public static void SetFamiliarDefault(this ulong steamID, int data)
    {
        _familiarDefault[steamID] = data;
        SavePlayerFamiliarSets();
    }
    public static void SetFamiliarBattleGroup(this ulong steamID, List<int> data)
    {
        _playerBattleGroups[steamID] = data;
        SaveFamiliarBattleGroups();
    }
    public static void SetPlayerQuests(this ulong steamID, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> data)
    {
        _playerQuests[steamID] = data;
        SavePlayerQuests();
    }
    public static void SetPlayerParties(this ulong steamID, ConcurrentList<string> data)
    {
        _playerParties[steamID] = data;
        // SavePlayerParties();
    }
    internal static class PlayerDictionaries
    {
        // exoform timestamp & cooldown
        internal static ConcurrentDictionary<ulong, KeyValuePair<DateTime, float>> _playerExoFormData = [];

        // leveling
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerExperience = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<DateTime, float>> _playerRestedXP = [];

        // old implementation of bools
        internal static ConcurrentDictionary<ulong, ConcurrentDictionary<string, bool>> _playerBools = [];

        // classes
        internal static ConcurrentDictionary<ulong, Dictionary<Classes.PlayerClass, (List<int> WeaponStats, List<int> BloodStats)>> _playerClass = [];

        // prestiges
        internal static ConcurrentDictionary<ulong, Dictionary<PrestigeType, int>> _playerPrestiges = [];

        // professions
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWoodcutting = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerMining = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerFishing = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerBlacksmithing = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerTailoring = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerEnchanting = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerAlchemy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerHarvesting = [];

        // weapon expertise
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerSwordExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerAxeExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerMaceExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerSpearExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerCrossbowExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerGreatSwordExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerSlashersExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerPistolsExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerReaperExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerLongbowExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWhipExpertise = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerFishingPoleExpertise = [];
        internal static ConcurrentDictionary<ulong, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>>> _playerWeaponStats = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerUnarmedExpertise = []; // this is unarmed and needs to be renamed to match the rest
        internal static ConcurrentDictionary<ulong, (int FirstUnarmed, int SecondUnarmed, int ClassSpell)> _playerSpells = [];

        // blood legacies
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWorkerLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWarriorLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerScholarLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerRogueLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerMutantLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerVBloodLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerDraculinLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerImmortalLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerCreatureLegacy = [];
        internal static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerBruteLegacy = [];
        internal static ConcurrentDictionary<ulong, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>>> _playerBloodStats = [];

        // familiar data
        internal static ConcurrentDictionary<ulong, (Entity Familiar, int FamKey)> _familiarActives = []; // mmm should probably either refactor to not need this or give everyone their own file
        internal static ConcurrentDictionary<ulong, string> _familiarBox = [];
        internal static ConcurrentDictionary<ulong, int> _familiarDefault = [];
        internal static ConcurrentDictionary<ulong, List<int>> _playerBattleGroups = [];
        internal static List<List<float>> _familiarBattleCoords = [];

        // quest data
        internal static ConcurrentDictionary<ulong, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)>> _playerQuests = [];

        // parties cache
        internal static ConcurrentDictionary<ulong, ConcurrentList<string>> _playerParties = [];
    }
    internal static class PlayerPersistence
    {
        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            IncludeFields = true
        };

        static readonly Dictionary<string, string> _filePaths = new()
        {
            {"Experience", _playerExperienceJson},
            {"RestedXP", _playerRestedXPJson },
            {"Quests", _playerQuestsJson },
            {"Classes", _playerClassesJson },
            {"Prestiges", _playerPrestigesJson },
            {"ExoFormData", _playerExoFormsJson },
            {"PlayerBools", PlayerBoolsJson},
            {"PlayerParties", _playerPartiesJson},
            {"Woodcutting", _playerWoodcuttingJson},
            {"Mining", _playerMiningJson},
            {"Fishing", _playerFishingJson},
            {"Blacksmithing", _playerBlacksmithingJson},
            {"Tailoring", _playerTailoringJson},
            {"Enchanting", _playerEnchantingJson},
            {"Alchemy", _playerAlchemyJson},
            {"Harvesting", _playerHarvestingJson},
            {"SwordExpertise", _playerSwordExpertiseJson },
            {"AxeExpertise", _playerAxeExpertiseJson},
            {"MaceExpertise", _playerMaceExpertiseJson},
            {"SpearExpertise", _playerSpearExpertiseJson},
            {"CrossbowExpertise", _playerCrossbowExpertiseJson},
            {"GreatSwordExpertise", JsonFilePaths._playerGreatSwordExpertise},
            {"SlashersExpertise", _playerSlashersExpertiseJson},
            {"PistolsExpertise", _playerPistolsExpertiseJson},
            {"ReaperExpertise", JsonFilePaths._playerReaperExpertise},
            {"LongbowExpertise", _playerLongbowExpertiseJson},
            {"WhipExpertise", _playerWhipExpertiseJson},
            {"FishingPoleExpertise", _playerFishingPoleExpertiseJson},
            {"UnarmedExpertise", _playerUnarmedExpertiseJson},
            {"PlayerSpells", _playerSpellsJson},
            {"WeaponStats", _playerWeaponStatsJson},
            {"WorkerLegacy", _playerWorkerLegacyJson},
            {"WarriorLegacy", _playerWarriorLegacyJson},
            {"ScholarLegacy", _playerScholarLegacyJson},
            {"RogueLegacy", _playerRogueLegacyJson},
            {"MutantLegacy", _playerMutantLegacyJson},
            {"VBloodLegacy", _playerVBloodLegacyJson},
            {"DraculinLegacy", _playerDraculinLegacyJson},
            {"ImmortalLegacy", _playerImmortalLegacyJson},
            {"CreatureLegacy", _playerCreatureLegacyJson},
            {"BruteLegacy", _playerBruteLegacyJson},
            {"BloodStats", _playerBloodStatsJson},
            {"FamiliarActives", _playerFamiliarActivesJson},
            {"FamiliarSets", _playerFamiliarSetsJson },
            {"FamiliarBattleCoords", _familiarBattleCoordsJson },
            {"FamiliarBattleGroups", _playerFamiliarBattleGroupsJson },
            {"FamiliarBattleTeams", _familiarBattleTeamsJson}
        };
        internal static class JsonFilePaths
        {
            internal static readonly string _playerExperienceJson = Path.Combine(DirectoryPaths[1], "player_experience.json");
            internal static readonly string _playerRestedXPJson = Path.Combine(DirectoryPaths[1], "player_rested_xp.json");
            internal static readonly string _playerQuestsJson = Path.Combine(DirectoryPaths[2], "player_quests.json");
            internal static readonly string _playerPrestigesJson = Path.Combine(DirectoryPaths[1], "player_prestiges.json");
            internal static readonly string _playerExoFormsJson = Path.Combine(DirectoryPaths[1], "player_exoforms.json");
            internal static readonly string _playerClassesJson = Path.Combine(DirectoryPaths[0], "player_classes.json");
            public static readonly string PlayerBoolsJson = Path.Combine(DirectoryPaths[0], "player_bools.json");
            internal static readonly string _playerPartiesJson = Path.Combine(DirectoryPaths[0], "player_parties.json");
            internal static readonly string _playerWoodcuttingJson = Path.Combine(DirectoryPaths[5], "player_woodcutting.json");
            internal static readonly string _playerMiningJson = Path.Combine(DirectoryPaths[5], "player_mining.json");
            internal static readonly string _playerFishingJson = Path.Combine(DirectoryPaths[5], "player_fishing.json");
            internal static readonly string _playerBlacksmithingJson = Path.Combine(DirectoryPaths[5], "player_blacksmithing.json");
            internal static readonly string _playerTailoringJson = Path.Combine(DirectoryPaths[5], "player_tailoring.json");
            internal static readonly string _playerEnchantingJson = Path.Combine(DirectoryPaths[5], "player_enchanting.json");
            internal static readonly string _playerAlchemyJson = Path.Combine(DirectoryPaths[5], "player_alchemy.json");
            internal static readonly string _playerHarvestingJson = Path.Combine(DirectoryPaths[5], "player_harvesting.json");
            internal static readonly string _playerSwordExpertiseJson = Path.Combine(DirectoryPaths[3], "player_sword.json");
            internal static readonly string _playerAxeExpertiseJson = Path.Combine(DirectoryPaths[3], "player_axe.json");
            internal static readonly string _playerMaceExpertiseJson = Path.Combine(DirectoryPaths[3], "player_mace.json");
            internal static readonly string _playerSpearExpertiseJson = Path.Combine(DirectoryPaths[3], "player_spear.json");
            internal static readonly string _playerCrossbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_crossbow.json");
            internal static readonly string _playerGreatSwordExpertise = Path.Combine(DirectoryPaths[3], "player_greatsword.json");
            internal static readonly string _playerSlashersExpertiseJson = Path.Combine(DirectoryPaths[3], "player_slashers.json");
            internal static readonly string _playerPistolsExpertiseJson = Path.Combine(DirectoryPaths[3], "player_pistols.json");
            internal static readonly string _playerReaperExpertise = Path.Combine(DirectoryPaths[3], "player_reaper.json");
            internal static readonly string _playerLongbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_longbow.json");
            internal static readonly string _playerUnarmedExpertiseJson = Path.Combine(DirectoryPaths[3], "player_unarmed.json");
            internal static readonly string _playerWhipExpertiseJson = Path.Combine(DirectoryPaths[3], "player_whip.json");
            internal static readonly string _playerFishingPoleExpertiseJson = Path.Combine(DirectoryPaths[3], "player_fishingpole.json");
            internal static readonly string _playerSpellsJson = Path.Combine(DirectoryPaths[1], "player_spells.json");
            internal static readonly string _playerWeaponStatsJson = Path.Combine(DirectoryPaths[3], "player_weapon_stats.json");
            internal static readonly string _playerWorkerLegacyJson = Path.Combine(DirectoryPaths[4], "player_worker.json");
            internal static readonly string _playerWarriorLegacyJson = Path.Combine(DirectoryPaths[4], "player_warrior.json");
            internal static readonly string _playerScholarLegacyJson = Path.Combine(DirectoryPaths[4], "player_scholar.json");
            internal static readonly string _playerRogueLegacyJson = Path.Combine(DirectoryPaths[4], "player_rogue.json");
            internal static readonly string _playerMutantLegacyJson = Path.Combine(DirectoryPaths[4], "player_mutant.json");
            internal static readonly string _playerVBloodLegacyJson = Path.Combine(DirectoryPaths[4], "player_vblood.json");
            internal static readonly string _playerDraculinLegacyJson = Path.Combine(DirectoryPaths[4], "player_draculin.json");
            internal static readonly string _playerImmortalLegacyJson = Path.Combine(DirectoryPaths[4], "player_immortal.json");
            internal static readonly string _playerCreatureLegacyJson = Path.Combine(DirectoryPaths[4], "player_creature.json");
            internal static readonly string _playerBruteLegacyJson = Path.Combine(DirectoryPaths[4], "player_brute.json");
            internal static readonly string _playerBloodStatsJson = Path.Combine(DirectoryPaths[4], "player_blood_stats.json");
            internal static readonly string _playerFamiliarActivesJson = Path.Combine(DirectoryPaths[6], "player_familiar_actives.json");
            internal static readonly string _playerFamiliarSetsJson = Path.Combine(DirectoryPaths[8], "player_familiar_sets.json");
            internal static readonly string _familiarBattleCoordsJson = Path.Combine(DirectoryPaths[6], "familiar_battle_coords.json");
            internal static readonly string _playerFamiliarBattleGroupsJson = Path.Combine(DirectoryPaths[6], "player_familiar_battle_groups.json");
            internal static readonly string _familiarBattleTeamsJson = Path.Combine(DirectoryPaths[6], "familiar_battle_teams.json");
        }
        static void LoadData<T>(ref ConcurrentDictionary<ulong, T> dataStructure, string key)
        {
            string path = _filePaths[key];

            if (!File.Exists(path))
            {
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
                    var data = JsonSerializer.Deserialize<ConcurrentDictionary<ulong, T>>(json, _jsonOptions);
                    dataStructure = data ?? [];
                }
            }
            catch (IOException ex)
            {
                Core.Log.LogInfo($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void LoadData<T>(ref Dictionary<ulong, T> dataStructure, string key)
        {
            string path = _filePaths[key];
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
                    var data = JsonSerializer.Deserialize<Dictionary<ulong, T>>(json, _jsonOptions);
                    dataStructure = data ?? [];
                }
            }
            catch (IOException ex)
            {
                Core.Log.LogInfo($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void LoadData<T>(ref List<List<float>> dataStructure, string key)
        {
            string path = _filePaths[key];
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

                if (string.IsNullOrWhiteSpace(json))
                {
                    dataStructure = [];
                }
                else
                {
                    var data = JsonSerializer.Deserialize<List<List<float>>>(json, _jsonOptions);
                    dataStructure = data ?? [];
                }
            }
            catch (IOException ex)
            {
                Core.Log.LogInfo($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void LoadData<T>(ref List<int> dataStructure, string key)
        {
            string path = _filePaths[key];
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

                if (string.IsNullOrWhiteSpace(json))
                {
                    dataStructure = [];
                }
                else
                {
                    var data = JsonSerializer.Deserialize<List<int>>(json, _jsonOptions);
                    dataStructure = data ?? [];
                }
            }
            catch (IOException ex)
            {
                Core.Log.LogInfo($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void SaveData<T>(ConcurrentDictionary<ulong, T> data, string key)
        {
            string path = _filePaths[key];
            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
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
        static void SaveData<T>(Dictionary<ulong, T> data, string key)
        {
            string path = _filePaths[key];
            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
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
        static void SaveData<T>(List<List<float>> data, string key)
        {
            string path = _filePaths[key];

            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(path, json);
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
        static void SaveData<T>(List<int> data, string key)
        {
            string path = _filePaths[key];

            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(path, json);
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
        public static void LoadPlayerExperience() => LoadData(ref _playerExperience, "Experience");

        public static void LoadPlayerRestedXP() => LoadData(ref _playerRestedXP, "RestedXP");

        public static void LoadPlayerQuests() => LoadData(ref _playerQuests, "Quests");

        public static void LoadPlayerClasses() => LoadData(ref _playerClass, "Classes");

        public static void LoadPlayerPrestiges() => LoadData(ref _playerPrestiges, "Prestiges");

        public static void LoadPlayerExoFormData() => LoadData(ref _playerExoFormData, "ExoFormData");

        public static void LoadPlayerBools() => LoadData(ref _playerBools, "PlayerBools");

        // public static void LoadPlayerParties() => LoadData(ref _playerParties, "PlayerParties");

        public static void LoadPlayerWoodcutting() => LoadData(ref _playerWoodcutting, "Woodcutting");

        public static void LoadPlayerMining() => LoadData(ref _playerMining, "Mining");

        public static void LoadPlayerFishing() => LoadData(ref _playerFishing, "Fishing");

        public static void LoadPlayerBlacksmithing() => LoadData(ref _playerBlacksmithing, "Blacksmithing");

        public static void LoadPlayerTailoring() => LoadData(ref _playerTailoring, "Tailoring");

        public static void LoadPlayerEnchanting() => LoadData(ref _playerEnchanting, "Enchanting");

        public static void LoadPlayerAlchemy() => LoadData(ref _playerAlchemy, "Alchemy");

        public static void LoadPlayerHarvesting() => LoadData(ref _playerHarvesting, "Harvesting");

        public static void LoadPlayerSwordExpertise() => LoadData(ref _playerSwordExpertise, "SwordExpertise");

        public static void LoadPlayerAxeExpertise() => LoadData(ref _playerAxeExpertise, "AxeExpertise");

        public static void LoadPlayerMaceExpertise() => LoadData(ref _playerMaceExpertise, "MaceExpertise");

        public static void LoadPlayerSpearExpertise() => LoadData(ref _playerSpearExpertise, "SpearExpertise");

        public static void LoadPlayerCrossbowExpertise() => LoadData(ref _playerCrossbowExpertise, "CrossbowExpertise");

        public static void LoadPlayerGreatSwordExpertise() => LoadData(ref PlayerDictionaries._playerGreatSwordExpertise, "GreatSwordExpertise");

        public static void LoadPlayerSlashersExpertise() => LoadData(ref _playerSlashersExpertise, "SlashersExpertise");

        public static void LoadPlayerPistolsExpertise() => LoadData(ref _playerPistolsExpertise, "PistolsExpertise");

        public static void LoadPlayerReaperExpertise() => LoadData(ref PlayerDictionaries._playerReaperExpertise, "ReaperExpertise");

        public static void LoadPlayerLongbowExpertise() => LoadData(ref _playerLongbowExpertise, "LongbowExpertise");

        public static void LoadPlayerWhipExpertise() => LoadData(ref _playerWhipExpertise, "WhipExpertise");

        public static void LoadPlayerFishingPoleExpertise() => LoadData(ref _playerFishingPoleExpertise, "FishingPoleExpertise");

        public static void LoadPlayerUnarmedExpertise() => LoadData(ref _playerUnarmedExpertise, "UnarmedExpertise");

        public static void LoadPlayerSpells() => LoadData(ref _playerSpells, "PlayerSpells");

        public static void LoadPlayerWeaponStats() => LoadData(ref _playerWeaponStats, "WeaponStats");

        public static void LoadPlayerWorkerLegacy() => LoadData(ref _playerWorkerLegacy, "WorkerLegacy");

        public static void LoadPlayerWarriorLegacy() => LoadData(ref _playerWarriorLegacy, "WarriorLegacy");

        public static void LoadPlayerScholarLegacy() => LoadData(ref _playerScholarLegacy, "ScholarLegacy");

        public static void LoadPlayerRogueLegacy() => LoadData(ref _playerRogueLegacy, "RogueLegacy");

        public static void LoadPlayerMutantLegacy() => LoadData(ref _playerMutantLegacy, "MutantLegacy");

        public static void LoadPlayerVBloodLegacy() => LoadData(ref _playerVBloodLegacy, "VBloodLegacy");

        public static void LoadPlayerDraculinLegacy() => LoadData(ref _playerDraculinLegacy, "DraculinLegacy");

        public static void LoadPlayerImmortalLegacy() => LoadData(ref _playerImmortalLegacy, "ImmortalLegacy");

        public static void LoadPlayerCreatureLegacy() => LoadData(ref _playerCreatureLegacy, "CreatureLegacy");

        public static void LoadPlayerBruteLegacy() => LoadData(ref _playerBruteLegacy, "BruteLegacy");

        public static void LoadPlayerBloodStats() => LoadData(ref _playerBloodStats, "BloodStats");

        public static void LoadPlayerFamiliarActives() => LoadData(ref _familiarActives, "FamiliarActives");

        public static void LoadPlayerFamiliarSets() => LoadData(ref _familiarBox, "FamiliarSets");

        public static void LoadFamiliarBattleCoords() => LoadData<List<float>>(ref _familiarBattleCoords, "FamiliarBattleCoords");

        public static void LoadFamiliarBattleGroups() => LoadData(ref _playerBattleGroups, "FamiliarBattleGroups");

        public static void SavePlayerExperience() => SaveData(_playerExperience, "Experience");

        public static void SavePlayerRestedXP() => SaveData(_playerRestedXP, "RestedXP");

        public static void SavePlayerQuests() => SaveData(_playerQuests, "Quests");

        public static void SavePlayerClasses() => SaveData(_playerClass, "Classes");

        public static void SavePlayerPrestiges() => SaveData(_playerPrestiges, "Prestiges");

        public static void SavePlayerExoFormData() => SaveData(_playerExoFormData, "ExoFormData");

        public static void SavePlayerBools() => SaveData(_playerBools, "PlayerBools");

        // public static void SavePlayerParties() => SaveData(_playerParties, "PlayerParties");

        public static void SavePlayerWoodcutting() => SaveData(_playerWoodcutting, "Woodcutting");

        public static void SavePlayerMining() => SaveData(_playerMining, "Mining");

        public static void SavePlayerFishing() => SaveData(_playerFishing, "Fishing");

        public static void SavePlayerBlacksmithing() => SaveData(_playerBlacksmithing, "Blacksmithing");

        public static void SavePlayerTailoring() => SaveData(_playerTailoring, "Tailoring");

        public static void SavePlayerEnchanting() => SaveData(_playerEnchanting, "Enchanting");

        public static void SavePlayerAlchemy() => SaveData(_playerAlchemy, "Alchemy");

        public static void SavePlayerHarvesting() => SaveData(_playerHarvesting, "Harvesting");

        public static void SavePlayerSwordExpertise() => SaveData(_playerSwordExpertise, "SwordExpertise");

        public static void SavePlayerAxeExpertise() => SaveData(_playerAxeExpertise, "AxeExpertise");

        public static void SavePlayerMaceExpertise() => SaveData(_playerMaceExpertise, "MaceExpertise");

        public static void SavePlayerSpearExpertise() => SaveData(_playerSpearExpertise, "SpearExpertise");

        public static void SavePlayerCrossbowExpertise() => SaveData(_playerCrossbowExpertise, "CrossbowExpertise");

        public static void SavePlayerGreatSwordExpertise() => SaveData(PlayerDictionaries._playerGreatSwordExpertise, "GreatSwordExpertise");

        public static void SavePlayerSlashersExpertise() => SaveData(_playerSlashersExpertise, "SlashersExpertise");

        public static void SavePlayerPistolsExpertise() => SaveData(_playerPistolsExpertise, "PistolsExpertise");

        public static void SavePlayerReaperExpertise() => SaveData(PlayerDictionaries._playerReaperExpertise, "ReaperExpertise");

        public static void SavePlayerLongbowExpertise() => SaveData(_playerLongbowExpertise, "LongbowExpertise");

        public static void SavePlayerWhipExpertise() => SaveData(_playerWhipExpertise, "WhipExpertise");

        public static void SavePlayerFishingPoleExpertise() => SaveData(_playerFishingPoleExpertise, "FishingPoleExpertise");

        public static void SavePlayerUnarmedExpertise() => SaveData(_playerUnarmedExpertise, "UnarmedExpertise");

        public static void SavePlayerSpells() => SaveData(_playerSpells, "PlayerSpells");

        public static void SavePlayerWeaponStats() => SaveData(_playerWeaponStats, "WeaponStats");

        public static void SavePlayerWorkerLegacy() => SaveData(_playerWorkerLegacy, "WorkerLegacy");

        public static void SavePlayerWarriorLegacy() => SaveData(_playerWarriorLegacy, "WarriorLegacy");

        public static void SavePlayerScholarLegacy() => SaveData(_playerScholarLegacy, "ScholarLegacy");

        public static void SavePlayerRogueLegacy() => SaveData(_playerRogueLegacy, "RogueLegacy");

        public static void SavePlayerMutantLegacy() => SaveData(_playerMutantLegacy, "MutantLegacy");

        public static void SavePlayerVBloodLegacy() => SaveData(_playerVBloodLegacy, "VBloodLegacy");

        public static void SavePlayerDraculinLegacy() => SaveData(_playerDraculinLegacy, "DraculinLegacy");

        public static void SavePlayerImmortalLegacy() => SaveData(_playerImmortalLegacy, "ImmortalLegacy");

        public static void SavePlayerCreatureLegacy() => SaveData(_playerCreatureLegacy, "CreatureLegacy");

        public static void SavePlayerBruteLegacy() => SaveData(_playerBruteLegacy, "BruteLegacy");

        public static void SavePlayerBloodStats() => SaveData(_playerBloodStats, "BloodStats");

        public static void SavePlayerFamiliarActives() => SaveData(_familiarActives, "FamiliarActives");

        public static void SavePlayerFamiliarSets() => SaveData(_familiarBox, "FamiliarSets");

        public static void SaveFamiliarBattleCoords() => SaveData<List<float>>(_familiarBattleCoords, "FamiliarBattleCoords");

        public static void SaveFamiliarBattleGroups() => SaveData(_playerBattleGroups, "FamiliarBattleGroups");
    }
    public static class PlayerBoolsManager
    {
        static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[0], $"{playerId}_bools.json");
        public static void SavePlayerBools(ulong playerId, Dictionary<string, bool> preferences)
        {
            string filePath = GetFilePath(playerId);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(preferences, options);

            File.WriteAllText(filePath, jsonString);
        }
        public static Dictionary<string, bool> LoadPlayerBools(ulong playerId)
        {
            string filePath = GetFilePath(playerId);

            if (!File.Exists(filePath))
                return [];

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(jsonString);
        }
        public static Dictionary<string, bool> GetOrInitializePlayerBools(ulong playerId, Dictionary<string, bool> defaultBools)
        {
            var bools = LoadPlayerBools(playerId);

            // Ensure all default keys exist
            foreach (var key in defaultBools.Keys)
            {
                if (!bools.ContainsKey(key))
                {
                    bools[key] = defaultBools[key];
                }
            }

            SavePlayerBools(playerId, bools);
            return bools;
        }
    }
    internal static class FamiliarPersistence
    {
        [Serializable]
        internal class FamiliarUnlocksData
        {
            public Dictionary<string, List<int>> UnlockedFamiliars { get; set; } = [];
        }

        [Serializable]
        internal class FamiliarExperienceData
        {
            public Dictionary<int, KeyValuePair<int, float>> FamiliarLevels { get; set; } = [];
        }

        [Serializable]
        internal class FamiliarPrestigeData
        {
            public Dictionary<int, KeyValuePair<int, List<FamiliarStatType>>> FamiliarPrestiges { get; set; } = [];
        }

        [Serializable]
        internal class FamiliarBuffsData
        {
            public Dictionary<int, List<int>> FamiliarBuffs { get; set; } = []; // can use actual perma buffs or just musb_dots from traits
        }

        [Serializable]
        internal class FamiliarEquipmentData
        {
            public Dictionary<int, List<int>> FamiliarEquipment { get; set; } = [];
        }

        [Serializable]
        public class FamiliarTraitData
        {
            public Dictionary<int, Dictionary<string, Dictionary<FamiliarStatType, float>>> FamiliarTraitModifiers { get; set; } = [];
        }

        [Serializable]
        public class FamiliarTrait(string name, Dictionary<FamiliarStatType, float> familiarTraits)
        {
            public string Name = name;
            public Dictionary<FamiliarStatType, float> FamiliarTraits = familiarTraits;
        }

        internal static Dictionary<ulong, FamiliarUnlocksData> _unlockedFamiliars = [];
        internal static Dictionary<ulong, FamiliarExperienceData> _familiarExperience = [];
        internal static Dictionary<ulong, FamiliarPrestigeData> _familiarPrestiges = [];
        internal static Dictionary<ulong, FamiliarBuffsData> _familiarBuffs = [];
        internal static Dictionary<ulong, FamiliarEquipmentData> _familiarEquipment = [];
        internal static Dictionary<ulong, FamiliarTraitData> _familiarTraits = [];
        internal static class FamiliarUnlocksManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[8], $"{playerId}_familiar_unlocks.json");

            public static void SaveUnlockedFamiliars(ulong playerId, FamiliarUnlocksData data)
            {
                string filePath = GetFilePath(playerId);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, jsonString);
            }

            public static FamiliarUnlocksData LoadUnlockedFamiliars(ulong playerId)
            {
                string filePath = GetFilePath(playerId);
                if (!File.Exists(filePath))
                    return new FamiliarUnlocksData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarUnlocksData>(jsonString);
            }
        }

        internal static class FamiliarExperienceManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[7], $"{playerId}_familiar_experience.json");
            public static void SaveFamiliarExperienceData(ulong playerId, FamiliarExperienceData data)
            {
                string filePath = GetFilePath(playerId);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarExperienceData LoadFamiliarExperienceData(ulong playerId)
            {
                string filePath = GetFilePath(playerId);
                if (!File.Exists(filePath))
                    return new FamiliarExperienceData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarExperienceData>(jsonString);
            }
            public static KeyValuePair<int, float> LoadFamiliarExperience(ulong playerId, int famKey)
            {
                var experienceData = LoadFamiliarExperienceData(playerId);

                if (experienceData.FamiliarLevels.TryGetValue(famKey, out var experience))
                    return experience;

                return new KeyValuePair<int, float>(1, Progression.ConvertLevelToXp(1)); // Default experience value if not found
            }
            public static void SaveFamiliarExperience(ulong playerId, int famKey, KeyValuePair<int, float> data)
            {
                var experienceData = LoadFamiliarExperienceData(playerId);
                experienceData.FamiliarLevels[famKey] = data;

                SaveFamiliarExperienceData(playerId, experienceData);
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
            public static int GetFamiliarPrestigeLevel(FamiliarPrestigeData familiarPrestigeData, int familiarId)
            {
                return familiarPrestigeData.FamiliarPrestiges.TryGetValue(familiarId, out var prestigeData) ? prestigeData.Key : 0;
            }
        }
        internal static class FamiliarBuffsManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[8], $"{playerId}_familiar_buffs.json");
            public static void SaveFamiliarBuffsData(ulong playerId, FamiliarBuffsData data)
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
            public static List<int> GetFamiliarBuffs(ulong playerId, int famKey)
            {
                var buffsData = LoadFamiliarBuffs(playerId);

                if (buffsData.FamiliarBuffs.TryGetValue(famKey, out var buffs))
                    return buffs;

                return [];
            }
            public static void SaveFamiliarBuffs(ulong playerId, int famKey, List<int> buffs)
            {
                var buffsData = LoadFamiliarBuffs(playerId);
                buffsData.FamiliarBuffs[famKey] = buffs;

                SaveFamiliarBuffsData(playerId, buffsData);
            }
        }
        internal static class FamiliarEquipmentManager
        {
            static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[8], $"{playerId}_familiar_equipment.json");
            public static void SaveFamiliarEquipmentData(ulong playerId, FamiliarEquipmentData data)
            {
                string filePath = GetFilePath(playerId);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(data, options);
                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarEquipmentData LoadFamiliarEquipment(ulong playerId)
            {
                string filePath = GetFilePath(playerId);
                if (!File.Exists(filePath))
                    return new FamiliarEquipmentData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarEquipmentData>(jsonString);
            }
            public static List<int> GetFamiliarEquipment(ulong playerId, int famKey)
            {
                var equipmentData = LoadFamiliarEquipment(playerId);

                if (!equipmentData.FamiliarEquipment.TryGetValue(famKey, out var equipment))
                {
                    equipment = [0, 0, 0, 0, 0, 0];
                    equipmentData.FamiliarEquipment[famKey] = equipment;

                    SaveFamiliarEquipmentData(playerId, equipmentData);
                }

                return equipment;
            }
            public static void SaveFamiliarEquipment(ulong playerId, int famKey, List<int> equipment)
            {
                var equipmentData = LoadFamiliarEquipment(playerId);
                equipmentData.FamiliarEquipment[famKey] = equipment;

                SaveFamiliarEquipmentData(playerId, equipmentData);
            }
        }

        public static readonly List<FamiliarTrait> FamiliarTraits =
        [
        // Offensive Traits
        new("Dextrous", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PrimaryAttackSpeed, 1.1f } // Primary Attack Speed +10%
        }),

        new("Alacritous", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.AttackSpeed, 1.1f } // Cast Speed +10%
        }),

        new("Powerful", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalPower, 1.15f } // Physical Power +15%
        }),

        new("Savant", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.SpellPower, 1.15f } // Spell Power +15%
        }),

        new("Piercing", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalCritChance, 1.2f }, // Physical Crit Chance +20%
            { FamiliarStatType.PhysicalCritDamage, 1.1f }  // Physical Crit Damage +10%
        }),

        new("Cognizant", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.SpellCritChance, 1.2f }, // Spell Crit Chance +20%
            { FamiliarStatType.SpellCritDamage, 1.1f }  // Spell Crit Damage +10%
        }),

        // Defensive Traits
        new("Fortified", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalResistance, 1.2f }, // Physical Resistance +20%
            { FamiliarStatType.DamageReduction, 1.1f }     // Damage Reduction +10%
        }),

        new("Aegis", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.SpellResistance, 1.2f }, // Spell Resistance +20%
            { FamiliarStatType.DamageReduction, 1.1f }  // Damage Reduction +10%
        }),

        new("Stalwart", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalResistance, 1.15f }, // Physical Resistance +15%
            { FamiliarStatType.MaxHealth, 1.1f }            // MaxHealth +10%
        }),

        new("Barrier", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.SpellResistance, 1.15f }, // Spell Resistance +15%
            { FamiliarStatType.MaxHealth, 1.1f }         // CC Reduction +10%
        }),

        new("Resilient", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.DamageReduction, 1.15f },  // Damage Reduction +15%
            { FamiliarStatType.PhysicalResistance, 1.1f } // Physical Resistance +10%
        }),

        // Movement Traits
        new("Nimble", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.MovementSpeed, 1.2f } // Movement Speed +20%
        }),

        new("Spry", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.MovementSpeed, 1.1f } // Movement Speed +10%
        }),

        // Mixed traits
        new("Brave", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalPower, 1.1f },      // Physical Power +10%
            { FamiliarStatType.PhysicalResistance, 1.05f } // Physical Resistance +5%
        }),

        new("Fearless", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalPower, 1.1f },     // Physical Power +10%
            { FamiliarStatType.PrimaryAttackSpeed, 1.1f } // CC Reduction +10%
        }),

        new("Ferocious", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalPower, 1.15f }, // Physical Power +15%
            { FamiliarStatType.MovementSpeed, 1.1f }   // Movement Speed +10%
        }),

        new("Bulwark", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalResistance, 1.15f }, // Physical Resistance +15%
            { FamiliarStatType.DamageReduction, 1.05f }     // Damage Reduction +5%
        }),

        new("Champion", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalPower, 1.2f },     // Physical Power +20%
            { FamiliarStatType.PhysicalResistance, 1.1f } // Physical Resistance +10%
        }),

        // High-tier traits
        new("Legend", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalPower, 1.2f },      // Physical Power +20%
            { FamiliarStatType.PhysicalResistance, 1.2f }, // Physical Resistance +20%
            { FamiliarStatType.MovementSpeed, 1.15f }      // Movement Speed +15%
        }),

        new("Swift", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.MovementSpeed, 1.3f } // Movement Speed +30%
        }),

        new("Ironclad", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalResistance, 1.25f }, // Physical Resistance +25%
            { FamiliarStatType.DamageReduction, 1.15f }     // Damage Reduction +15%
        }),

        new("Furious", new Dictionary<FamiliarStatType, float>
        {
            { FamiliarStatType.PhysicalPower, 1.25f }, // Physical Power +25%
            { FamiliarStatType.AttackSpeed, 1.1f }     // Attack Speed +10%
        })
        ];
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
                    File.Copy(PlayerSanguimancyJson, _playerUnarmedExpertiseJson, overwrite: false);
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to migrate sanguimancy data to unarmed: {ex}");
            }

            LoadPlayerBools();

            /*
            if (PlayerParties)
            {
                LoadPlayerParties();
            }
            */

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
                foreach (var loadFunction in _loadLeveling)
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
                foreach (var loadFunction in _loadExpertises)
                {
                    loadFunction();
                }
            }

            if (ConfigService.BloodSystem)
            {
                foreach (var loadFunction in _loadLegacies)
                {
                    loadFunction();
                }
            }

            if (ProfessionSystem)
            {
                foreach (var loadFunction in _loadProfessions)
                {
                    loadFunction();
                }
            }

            if (FamiliarSystem)
            {
                foreach (var loadFunction in _loadFamiliars)
                {
                    loadFunction();
                }
            }
        }

        static readonly Action[] _loadLeveling =
        [
            LoadPlayerExperience,
            LoadPlayerPrestiges,
            LoadPlayerExoFormData
        ];

        static readonly Action[] _loadExpertises =
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

        static readonly Action[] _loadLegacies =
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

        static readonly Action[] _loadProfessions =
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

        static readonly Action[] _loadFamiliars =
        [
            LoadPlayerFamiliarActives,
            LoadPlayerFamiliarSets,
            LoadFamiliarBattleCoords,
            LoadFamiliarBattleGroups
        ];
    }
}
