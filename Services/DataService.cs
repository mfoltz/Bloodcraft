using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using ProjectM;
using System.Collections.Concurrent;
using System.Text.Json;
using Unity.Entities;
using static Bloodcraft.Services.ConfigService;
using static Bloodcraft.Services.ConfigService.ConfigInitialization;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.DataService.PlayerPersistence;
using static Bloodcraft.Services.DataService.PlayerPersistence.JsonFilePaths;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Familiars.FamiliarSummonSystem;
using static Bloodcraft.Utilities.Misc;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Services;
internal static class DataService
{
    /* old, pending removal
    public static bool TryGetPlayerBools(this ulong steamId, out ConcurrentDictionary<string, bool> bools)
    {
        bools = [];

        if (!_playerBools.Any()) return false;
        else return _playerBools.TryGetValue(steamId, out bools);
    }
    public static void SetPlayerBools(this ulong steamId, ConcurrentDictionary<string, bool> data)
    {
        _playerBools[steamId] = data;
        SavePlayerBools();
    }
    */
    public static bool TryGetPlayerExperience(this ulong steamId, out KeyValuePair<int, float> experience)
    {
        return _playerExperience.TryGetValue(steamId, out experience);
    }
    public static bool TryGetPlayerRestedXP(this ulong steamId, out KeyValuePair<DateTime, float> restedXP)
    {
        return _playerRestedXP.TryGetValue(steamId, out restedXP);
    }
    public static bool TryGetPlayerClasses(this ulong steamId, out Dictionary<Classes.PlayerClass, (List<int>, List<int>)> classes)
    {
        return _playerClass.TryGetValue(steamId, out classes);
    }
    public static bool TryGetPlayerPrestiges(this ulong steamId, out Dictionary<PrestigeType, int> prestiges)
    {
        return _playerPrestiges.TryGetValue(steamId, out prestiges);
    }
    public static bool TryGetPlayerExoFormData(this ulong steamId, out KeyValuePair<DateTime, float> exoFormData)
    {
        return _playerExoFormData.TryGetValue(steamId, out exoFormData);
    }
    public static bool TryGetPlayerWoodcutting(this ulong steamId, out KeyValuePair<int, float> woodcutting)
    {
        return _playerWoodcutting.TryGetValue(steamId, out woodcutting);
    }
    public static bool TryGetPlayerMining(this ulong steamId, out KeyValuePair<int, float> mining)
    {
        return _playerMining.TryGetValue(steamId, out mining);
    }
    public static bool TryGetPlayerFishing(this ulong steamId, out KeyValuePair<int, float> fishing)
    {
        return _playerFishing.TryGetValue(steamId, out fishing);
    }
    public static bool TryGetPlayerBlacksmithing(this ulong steamId, out KeyValuePair<int, float> blacksmithing)
    {
        return _playerBlacksmithing.TryGetValue(steamId, out blacksmithing);
    }
    public static bool TryGetPlayerTailoring(this ulong steamId, out KeyValuePair<int, float> tailoring)
    {
        return _playerTailoring.TryGetValue(steamId, out tailoring);
    }
    public static bool TryGetPlayerEnchanting(this ulong steamId, out KeyValuePair<int, float> enchanting)
    {
        return _playerEnchanting.TryGetValue(steamId, out enchanting);
    }
    public static bool TryGetPlayerAlchemy(this ulong steamId, out KeyValuePair<int, float> alchemy)
    {
        return _playerAlchemy.TryGetValue(steamId, out alchemy);
    }
    public static bool TryGetPlayerHarvesting(this ulong steamId, out KeyValuePair<int, float> harvesting)
    {
        return _playerHarvesting.TryGetValue(steamId, out harvesting);
    }
    public static bool TryGetPlayerSwordExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerSwordExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerAxeExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerAxeExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerMaceExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerMaceExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerSpearExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerSpearExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerCrossbowExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerCrossbowExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerGreatSwordExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return PlayerDictionaries._playerGreatSwordExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerSlashersExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerSlashersExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerPistolsExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerPistolsExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerReaperExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return PlayerDictionaries._playerReaperExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerLongbowExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerLongbowExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerWhipExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerWhipExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerFishingPoleExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerFishingPoleExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerUnarmedExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerUnarmedExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerWeaponStats(this ulong steamId, out Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> weaponStats)
    {
        return _playerWeaponStats.TryGetValue(steamId, out weaponStats);
    }
    public static bool TryGetPlayerSpells(this ulong steamId, out (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells)
    {
        return _playerSpells.TryGetValue(steamId, out spells);
    }
    public static bool TryGetPlayerWorkerLegacy(this ulong steamId, out KeyValuePair<int, float> workerLegacy)
    {
        return _playerWorkerLegacy.TryGetValue(steamId, out workerLegacy);
    }
    public static bool TryGetPlayerWarriorLegacy(this ulong steamId, out KeyValuePair<int, float> warriorLegacy)
    {
        return _playerWarriorLegacy.TryGetValue(steamId, out warriorLegacy);
    }
    public static bool TryGetPlayerScholarLegacy(this ulong steamId, out KeyValuePair<int, float> scholarLegacy)
    {
        return _playerScholarLegacy.TryGetValue(steamId, out scholarLegacy);
    }
    public static bool TryGetPlayerRogueLegacy(this ulong steamId, out KeyValuePair<int, float> rogueLegacy)
    {
        return _playerRogueLegacy.TryGetValue(steamId, out rogueLegacy);
    }
    public static bool TryGetPlayerMutantLegacy(this ulong steamId, out KeyValuePair<int, float> mutantLegacy)
    {
        return _playerMutantLegacy.TryGetValue(steamId, out mutantLegacy);
    }
    public static bool TryGetPlayerVBloodLegacy(this ulong steamId, out KeyValuePair<int, float> vBloodLegacy)
    {
        return _playerVBloodLegacy.TryGetValue(steamId, out vBloodLegacy);
    }
    public static bool TryGetPlayerDraculinLegacy(this ulong steamId, out KeyValuePair<int, float> draculinLegacy)
    {
        return _playerDraculinLegacy.TryGetValue(steamId, out draculinLegacy);
    }
    public static bool TryGetPlayerImmortalLegacy(this ulong steamId, out KeyValuePair<int, float> immortalLegacy)
    {
        return _playerImmortalLegacy.TryGetValue(steamId, out immortalLegacy);
    }
    public static bool TryGetPlayerCreatureLegacy(this ulong steamId, out KeyValuePair<int, float> creatureLegacy)
    {
        return _playerCreatureLegacy.TryGetValue(steamId, out creatureLegacy);
    }
    public static bool TryGetPlayerBruteLegacy(this ulong steamId, out KeyValuePair<int, float> bruteLegacy)
    {
        return _playerBruteLegacy.TryGetValue(steamId, out bruteLegacy);
    }
    public static bool TryGetPlayerBloodStats(this ulong steamId, out Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> bloodStats)
    {
        return _playerBloodStats.TryGetValue(steamId, out bloodStats);
    }
    public static bool TryGetFamiliarActives(this ulong steamId, out (Entity Familiar, int FamKey) activeFamiliar)
    {
        return _familiarActives.TryGetValue(steamId, out activeFamiliar);
    }
    public static bool TryGetFamiliarBox(this ulong steamId, out string familiarSet)
    {
        return _familiarBoxes.TryGetValue(steamId, out familiarSet);
    }
    public static bool TryGetFamiliarBoxPreset(this ulong steamId, out int defaultFamiliar)
    {
        return _familiarBoxIndex.TryGetValue(steamId, out defaultFamiliar);
    }
    public static bool TryGetFamiliarBattleGroup(this ulong steamId, out List<int> battleGroup)
    {
        return _familiarBattleGroups.TryGetValue(steamId, out battleGroup);
    }
    public static bool TryGetPlayerQuests(this ulong steamId, out Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> quests)
    {
        return _playerQuests.TryGetValue(steamId, out quests);
    }
    public static bool TryGetPlayerParties(this ulong steamId, out ConcurrentList<string> parties)
    {
        return _playerParties.TryGetValue(steamId, out parties);
    }
    public static void SetPlayerExperience(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerExperience[steamId] = data;
        SavePlayerExperience();
    }
    public static void SetPlayerRestedXP(this ulong steamId, KeyValuePair<DateTime, float> data)
    {
        _playerRestedXP[steamId] = data;
        SavePlayerRestedXP();
    }
    public static void SetPlayerClasses(this ulong steamId, Dictionary<Classes.PlayerClass, (List<int>, List<int>)> data)
    {
        _playerClass[steamId] = data;
        SavePlayerClasses();
    }
    public static void SetPlayerPrestiges(this ulong steamId, Dictionary<PrestigeType, int> data)
    {
        _playerPrestiges[steamId] = data;
        SavePlayerPrestiges();
    }
    public static void SetPlayerExoFormData(this ulong steamId, KeyValuePair<DateTime, float> data)
    {
        _playerExoFormData[steamId] = data;
        SavePlayerExoFormData();
    }
    public static void SetPlayerWoodcutting(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerWoodcutting[steamId] = data;
        SavePlayerWoodcutting();
    }
    public static void SetPlayerMining(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerMining[steamId] = data;
        SavePlayerMining();
    }
    public static void SetPlayerFishing(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerFishing[steamId] = data;
        SavePlayerFishing();
    }
    public static void SetPlayerBlacksmithing(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerBlacksmithing[steamId] = data;
        SavePlayerBlacksmithing();
    }
    public static void SetPlayerTailoring(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerTailoring[steamId] = data;
        SavePlayerTailoring();
    }
    public static void SetPlayerEnchanting(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerEnchanting[steamId] = data;
        SavePlayerEnchanting();
    }
    public static void SetPlayerAlchemy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerAlchemy[steamId] = data;
        SavePlayerAlchemy();
    }
    public static void SetPlayerHarvesting(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerHarvesting[steamId] = data;
        SavePlayerHarvesting();
    }
    public static void SetPlayerSwordExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerSwordExpertise[steamId] = data;
        SavePlayerSwordExpertise();
    }
    public static void SetPlayerAxeExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerAxeExpertise[steamId] = data;
        SavePlayerAxeExpertise();
    }
    public static void SetPlayerMaceExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerMaceExpertise[steamId] = data;
        SavePlayerMaceExpertise();
    }
    public static void SetPlayerSpearExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerSpearExpertise[steamId] = data;
        SavePlayerSpearExpertise();
    }
    public static void SetPlayerCrossbowExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerCrossbowExpertise[steamId] = data;
        SavePlayerCrossbowExpertise();
    }
    public static void SetPlayerGreatSwordExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        PlayerDictionaries._playerGreatSwordExpertise[steamId] = data;
        SavePlayerGreatSwordExpertise();
    }
    public static void SetPlayerSlashersExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerSlashersExpertise[steamId] = data;
        SavePlayerSlashersExpertise();
    }
    public static void SetPlayerPistolsExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerPistolsExpertise[steamId] = data;
        SavePlayerPistolsExpertise();
    }
    public static void SetPlayerReaperExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        PlayerDictionaries._playerReaperExpertise[steamId] = data;
        SavePlayerReaperExpertise();
    }
    public static void SetPlayerLongbowExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerLongbowExpertise[steamId] = data;
        SavePlayerLongbowExpertise();
    }
    public static void SetPlayerWhipExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerWhipExpertise[steamId] = data;
        SavePlayerWhipExpertise();
    }
    public static void SetPlayerFishingPoleExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerFishingPoleExpertise[steamId] = data;
        SavePlayerFishingPoleExpertise();
    }
    public static void SetPlayerUnarmedExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerUnarmedExpertise[steamId] = data;
        SavePlayerUnarmedExpertise();
    }
    public static void SetPlayerWeaponStats(this ulong steamId, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> data)
    {
        _playerWeaponStats[steamId] = data;
        SavePlayerWeaponStats();
    }
    public static void SetPlayerSpells(this ulong steamId, (int FirstUnarmed, int SecondUnarmed, int ClassSpell) data)
    {
        _playerSpells[steamId] = data;
        SavePlayerSpells();
    }
    public static void SetPlayerWorkerLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerWorkerLegacy[steamId] = data;
        SavePlayerWorkerLegacy();
    }
    public static void SetPlayerWarriorLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerWarriorLegacy[steamId] = data;
        SavePlayerWarriorLegacy();
    }
    public static void SetPlayerScholarLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerScholarLegacy[steamId] = data;
        SavePlayerScholarLegacy();
    }
    public static void SetPlayerRogueLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerRogueLegacy[steamId] = data;
        SavePlayerRogueLegacy();
    }
    public static void SetPlayerMutantLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerMutantLegacy[steamId] = data;
        SavePlayerMutantLegacy();
    }
    public static void SetPlayerVBloodLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerVBloodLegacy[steamId] = data;
        SavePlayerVBloodLegacy();
    }
    public static void SetPlayerDraculinLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerDraculinLegacy[steamId] = data;
        SavePlayerDraculinLegacy();
    }
    public static void SetPlayerImmortalLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerImmortalLegacy[steamId] = data;
        SavePlayerImmortalLegacy();
    }
    public static void SetPlayerCreatureLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerCreatureLegacy[steamId] = data;
        SavePlayerCreatureLegacy();
    }
    public static void SetPlayerBruteLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerBruteLegacy[steamId] = data;
        SavePlayerBruteLegacy();
    }
    public static void SetPlayerBloodStats(this ulong steamId, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> data)
    {
        _playerBloodStats[steamId] = data;
        SavePlayerBloodStats();
    }
    public static void SetFamiliarActives(this ulong steamId, (Entity Familiar, int FamKey) data)
    {
        _familiarActives[steamId] = data;
    }
    public static void SetFamiliarBox(this ulong steamId, string data = null)
    {
        FamiliarUnlocksData familiarBoxes = FamiliarUnlocksManager.LoadFamiliarUnlocksData(steamId);

        if (string.IsNullOrEmpty(data) && steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;

            if (playerCharacter.TryGetComponent(out Energy energy) && energy.MaxEnergy._Value <= familiarBoxes.UnlockedFamiliars.Count)
            {
                List<string> boxes = [.. familiarBoxes.UnlockedFamiliars.Keys];
                data = boxes[(int)energy.MaxEnergy._Value];
            }
        }
        else if (!string.IsNullOrEmpty(data) && familiarBoxes.UnlockedFamiliars.ContainsKey(data) && steamId.TryGetPlayerInfo(out playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;
            int index = familiarBoxes.UnlockedFamiliars.Keys.ToList().IndexOf(data);

            if (playerCharacter.Has<Energy>())
            {
                playerCharacter.With((ref Energy energy) =>
                {
                    energy.MaxEnergy._Value = index;
                });
            }
        }

        _familiarBoxes[steamId] = data;
    }
    public static void SetFamiliarBoxPreset(this ulong steamId, int data)
    {
        _familiarBoxIndex[steamId] = data;
    }
    public static void SetFamiliarBattleGroup(this ulong steamId, List<int> data)
    {
        _familiarBattleGroups[steamId] = data;
        SaveFamiliarBattleGroups();
    }
    public static void SetPlayerQuests(this ulong steamId, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> data)
    {
        _playerQuests[steamId] = data;
        SavePlayerQuests();
    }
    public static void SetPlayerParties(this ulong steamId, ConcurrentList<string> data)
    {
        _playerParties[steamId] = data;
    }
    public static class PlayerDictionaries
    {
        // exoform data
        public static ConcurrentDictionary<ulong, KeyValuePair<DateTime, float>> _playerExoFormData = [];

        // leveling data
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerExperience = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<DateTime, float>> _playerRestedXP = [];

        // old implementation of bools
        public static ConcurrentDictionary<ulong, ConcurrentDictionary<string, bool>> _playerBools = [];

        // class data
        public static ConcurrentDictionary<ulong, Dictionary<Classes.PlayerClass, (List<int> WeaponStats, List<int> BloodStats)>> _playerClass = [];

        // prestige data
        public static ConcurrentDictionary<ulong, Dictionary<PrestigeType, int>> _playerPrestiges = [];

        // profession data
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWoodcutting = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerMining = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerFishing = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerBlacksmithing = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerTailoring = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerEnchanting = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerAlchemy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerHarvesting = [];

        // weapon expertise data
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerSwordExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerAxeExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerMaceExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerSpearExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerCrossbowExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerGreatSwordExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerSlashersExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerPistolsExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerReaperExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerLongbowExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWhipExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerFishingPoleExpertise = [];
        public static ConcurrentDictionary<ulong, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>>> _playerWeaponStats = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerUnarmedExpertise = []; // this is unarmed and needs to be renamed to match the rest
        public static ConcurrentDictionary<ulong, (int FirstUnarmed, int SecondUnarmed, int ClassSpell)> _playerSpells = [];

        // blood legacy data
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWorkerLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerWarriorLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerScholarLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerRogueLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerMutantLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerVBloodLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerDraculinLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerImmortalLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerCreatureLegacy = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerBruteLegacy = [];
        public static ConcurrentDictionary<ulong, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>>> _playerBloodStats = [];

        // familiar data
        public static ConcurrentDictionary<ulong, (Entity Familiar, int FamKey)> _familiarActives = []; // mmm should probably either refactor to not need this or give everyone their own file, or why is this even a file geez cache it up
        public static ConcurrentDictionary<ulong, string> _familiarBoxes = [];
        public static ConcurrentDictionary<ulong, int> _familiarBoxIndex = [];
        public static ConcurrentDictionary<ulong, List<int>> _familiarBattleGroups = [];
        public static List<List<float>> _familiarBattleCoords = [];

        // quests data
        public static ConcurrentDictionary<ulong, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)>> _playerQuests = [];

        // parties cache
        public static ConcurrentDictionary<ulong, ConcurrentList<string>> _playerParties = [];
    }
    public static class PlayerPersistence
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
            // {"FamiliarActives", _playerFamiliarActivesJson},
            // {"FamiliarSets", _playerFamiliarSetsJson },
            {"FamiliarBattleCoords", _familiarBattleCoordsJson },
            {"FamiliarBattleGroups", _playerFamiliarBattleGroupsJson },
            {"FamiliarBattleTeams", _familiarBattleTeamsJson}
        };
        public static class JsonFilePaths
        {
            public static readonly string _playerExperienceJson = Path.Combine(DirectoryPaths[1], "player_experience.json");
            public static readonly string _playerRestedXPJson = Path.Combine(DirectoryPaths[1], "player_rested_xp.json");
            public static readonly string _playerQuestsJson = Path.Combine(DirectoryPaths[2], "player_quests.json");
            public static readonly string _playerPrestigesJson = Path.Combine(DirectoryPaths[1], "player_prestiges.json");
            public static readonly string _playerExoFormsJson = Path.Combine(DirectoryPaths[1], "player_exoforms.json");
            public static readonly string _playerClassesJson = Path.Combine(DirectoryPaths[0], "player_classes.json");
            public static readonly string PlayerBoolsJson = Path.Combine(DirectoryPaths[0], "player_bools.json");
            public static readonly string _playerPartiesJson = Path.Combine(DirectoryPaths[0], "player_parties.json");
            public static readonly string _playerWoodcuttingJson = Path.Combine(DirectoryPaths[5], "player_woodcutting.json");
            public static readonly string _playerMiningJson = Path.Combine(DirectoryPaths[5], "player_mining.json");
            public static readonly string _playerFishingJson = Path.Combine(DirectoryPaths[5], "player_fishing.json");
            public static readonly string _playerBlacksmithingJson = Path.Combine(DirectoryPaths[5], "player_blacksmithing.json");
            public static readonly string _playerTailoringJson = Path.Combine(DirectoryPaths[5], "player_tailoring.json");
            public static readonly string _playerEnchantingJson = Path.Combine(DirectoryPaths[5], "player_enchanting.json");
            public static readonly string _playerAlchemyJson = Path.Combine(DirectoryPaths[5], "player_alchemy.json");
            public static readonly string _playerHarvestingJson = Path.Combine(DirectoryPaths[5], "player_harvesting.json");
            public static readonly string _playerSwordExpertiseJson = Path.Combine(DirectoryPaths[3], "player_sword.json");
            public static readonly string _playerAxeExpertiseJson = Path.Combine(DirectoryPaths[3], "player_axe.json");
            public static readonly string _playerMaceExpertiseJson = Path.Combine(DirectoryPaths[3], "player_mace.json");
            public static readonly string _playerSpearExpertiseJson = Path.Combine(DirectoryPaths[3], "player_spear.json");
            public static readonly string _playerCrossbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_crossbow.json");
            public static readonly string _playerGreatSwordExpertise = Path.Combine(DirectoryPaths[3], "player_greatsword.json");
            public static readonly string _playerSlashersExpertiseJson = Path.Combine(DirectoryPaths[3], "player_slashers.json");
            public static readonly string _playerPistolsExpertiseJson = Path.Combine(DirectoryPaths[3], "player_pistols.json");
            public static readonly string _playerReaperExpertise = Path.Combine(DirectoryPaths[3], "player_reaper.json");
            public static readonly string _playerLongbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_longbow.json");
            public static readonly string _playerUnarmedExpertiseJson = Path.Combine(DirectoryPaths[3], "player_unarmed.json");
            public static readonly string _playerWhipExpertiseJson = Path.Combine(DirectoryPaths[3], "player_whip.json");
            public static readonly string _playerFishingPoleExpertiseJson = Path.Combine(DirectoryPaths[3], "player_fishingpole.json");
            public static readonly string _playerSpellsJson = Path.Combine(DirectoryPaths[1], "player_spells.json");
            public static readonly string _playerWeaponStatsJson = Path.Combine(DirectoryPaths[3], "player_weapon_stats.json");
            public static readonly string _playerWorkerLegacyJson = Path.Combine(DirectoryPaths[4], "player_worker.json");
            public static readonly string _playerWarriorLegacyJson = Path.Combine(DirectoryPaths[4], "player_warrior.json");
            public static readonly string _playerScholarLegacyJson = Path.Combine(DirectoryPaths[4], "player_scholar.json");
            public static readonly string _playerRogueLegacyJson = Path.Combine(DirectoryPaths[4], "player_rogue.json");
            public static readonly string _playerMutantLegacyJson = Path.Combine(DirectoryPaths[4], "player_mutant.json");
            public static readonly string _playerVBloodLegacyJson = Path.Combine(DirectoryPaths[4], "player_vblood.json");
            public static readonly string _playerDraculinLegacyJson = Path.Combine(DirectoryPaths[4], "player_draculin.json");
            public static readonly string _playerImmortalLegacyJson = Path.Combine(DirectoryPaths[4], "player_immortal.json");
            public static readonly string _playerCreatureLegacyJson = Path.Combine(DirectoryPaths[4], "player_creature.json");
            public static readonly string _playerBruteLegacyJson = Path.Combine(DirectoryPaths[4], "player_brute.json");
            public static readonly string _playerBloodStatsJson = Path.Combine(DirectoryPaths[4], "player_blood_stats.json");
            // public static readonly string _playerFamiliarActivesJson = Path.Combine(DirectoryPaths[6], "player_familiar_actives.json");
            // public static readonly string _playerFamiliarSetsJson = Path.Combine(DirectoryPaths[8], "player_familiar_sets.json");
            public static readonly string _familiarBattleCoordsJson = Path.Combine(DirectoryPaths[6], "familiar_battle_coords.json");
            public static readonly string _playerFamiliarBattleGroupsJson = Path.Combine(DirectoryPaths[6], "player_familiar_battle_groups.json");
            public static readonly string _familiarBattleTeamsJson = Path.Combine(DirectoryPaths[6], "familiar_battle_teams.json");
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

        // public static void LoadPlayerFamiliarBoxes() => LoadData(ref _familiarBoxes, "FamiliarSets");
        public static void LoadFamiliarBattleCoords() => LoadData<List<float>>(ref _familiarBattleCoords, "FamiliarBattleCoords");
        public static void LoadFamiliarBattleGroups() => LoadData(ref _familiarBattleGroups, "FamiliarBattleGroups");
        public static void SavePlayerExperience() => SaveData(_playerExperience, "Experience");
        public static void SavePlayerRestedXP() => SaveData(_playerRestedXP, "RestedXP");
        public static void SavePlayerQuests() => SaveData(_playerQuests, "Quests");
        public static void SavePlayerClasses() => SaveData(_playerClass, "Classes");
        public static void SavePlayerPrestiges() => SaveData(_playerPrestiges, "Prestiges");
        public static void SavePlayerExoFormData() => SaveData(_playerExoFormData, "ExoFormData");
        public static void SavePlayerBools() => SaveData(_playerBools, "PlayerBools");
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

        // public static void SavePlayerFamiliarActives() => SaveData(_familiarActives, "FamiliarActives");
        public static void SavePlayerFamiliarSets() => SaveData(_familiarBoxes, "FamiliarSets");
        public static void SaveFamiliarBattleCoords() => SaveData<List<float>>(_familiarBattleCoords, "FamiliarBattleCoords");
        public static void SaveFamiliarBattleGroups() => SaveData(_familiarBattleGroups, "FamiliarBattleGroups");
    }
    public static class PlayerBoolsManager
    {
        static string GetFilePath(ulong playerId) => Path.Combine(DirectoryPaths[9], $"{playerId}_bools.json");
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
    public static class FamiliarPersistence
    {
        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
        };

        [Serializable]
        public class FamiliarUnlocksData
        {
            public Dictionary<string, List<int>> UnlockedFamiliars { get; set; } = [];
        }

        [Serializable]
        public class FamiliarExperienceData
        {
            public Dictionary<int, KeyValuePair<int, float>> FamiliarExperience { get; set; } = [];
        }

        [Serializable]
        public class FamiliarPrestigeData
        {
            public Dictionary<int, KeyValuePair<int, List<FamiliarStatType>>> FamiliarPrestige { get; set; } = [];
        }

        [Serializable]
        public class FamiliarPrestigeData_V2
        {
            public Dictionary<int, KeyValuePair<int, List<int>>> FamiliarPrestige { get; set; } = [];
        }

        [Serializable]
        public class FamiliarBuffsData
        {
            public Dictionary<int, List<int>> FamiliarBuffs { get; set; } = []; // can use actual perma buffs or just musb_dots from traits
        }

        [Serializable]
        public class FamiliarBattleGroupsData
        {
            public Dictionary<int, List<int>> FamiliarBattleGroups { get; set; } = [];
        }

        [Serializable]
        public class FamiliarEquipmentData
        {
            public Dictionary<int, List<int>> FamiliarEquipment { get; set; } = [];
        }

        [Serializable]
        public class FamiliarTraitsData
        {
            public Dictionary<int, Dictionary<string, Dictionary<FamiliarStatType, float>>> FamiliarTraitModifiers { get; set; } = [];
        }

        [Serializable]
        public class FamiliarTrait(string name, Dictionary<FamiliarStatType, float> familiarTraits)
        {
            public string Name = name;
            public Dictionary<FamiliarStatType, float> FamiliarTraits = familiarTraits;
        }
        public static class FamiliarUnlocksManager
        {
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[8], $"{steamId}_familiar_unlocks.json");
            public static void SaveFamiliarUnlocksData(ulong steamId, FamiliarUnlocksData data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarUnlocksData LoadFamiliarUnlocksData(ulong steamId)
            {
                string filePath = GetFilePath(steamId);
                if (!File.Exists(filePath))
                    return new FamiliarUnlocksData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarUnlocksData>(jsonString);
            }
        }
        public static class FamiliarExperienceManager
        {
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[7], $"{steamId}_familiar_experience.json");
            public static void SaveFamiliarExperienceData(ulong steamId, FamiliarExperienceData data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarExperienceData LoadFamiliarExperienceData(ulong steamId)
            {
                string filePath = GetFilePath(steamId);
                if (!File.Exists(filePath))
                    return new FamiliarExperienceData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarExperienceData>(jsonString);
            }
        }
        public static class FamiliarPrestigeManager
        {
            public static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[7], $"{steamId}_familiar_prestige.json");
            public static void SaveFamiliarPrestigeData(ulong steamId, FamiliarPrestigeData data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarPrestigeData LoadFamiliarPrestigeData(ulong steamId)
            {
                string filePath = GetFilePath(steamId);
                if (!File.Exists(filePath)) return null;
                // return new FamiliarPrestigeData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarPrestigeData>(jsonString);
            }
        }
        public static class FamiliarPrestigeManager_V2
        {
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[7], $"{steamId}_familiar_prestige_v2.json");
            public static void SaveFamiliarPrestigeData_V2(ulong steamId, FamiliarPrestigeData_V2 data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarPrestigeData_V2 LoadFamiliarPrestigeData_V2(ulong steamId)
            {
                string filePath = GetFilePath(steamId);

                if (!File.Exists(filePath))
                    return new FamiliarPrestigeData_V2();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarPrestigeData_V2>(jsonString);
            }
            public static void MigrateToV2(FamiliarPrestigeData oldData, ulong steamId)
            {
                FamiliarPrestigeData_V2 newData = new();

                foreach (var entry in oldData.FamiliarPrestige)
                {
                    int famKey = entry.Key;
                    int prestigeLevel = entry.Value.Key;

                    List<int> newStatIndices = entry.Value.Value
                        .Select(stat => (int)stat)
                        .ToList();

                    newData.FamiliarPrestige[famKey] = new(prestigeLevel, newStatIndices);
                }

                Core.Log.LogInfo($"Migration to V2 prestige format completed - {steamId}");
                SaveFamiliarPrestigeData_V2(steamId, newData);

                string oldPath = FamiliarPrestigeManager.GetFilePath(steamId);
                if (File.Exists(oldPath))
                {
                    File.Delete(oldPath);
                }
            }
        }
        public static class FamiliarBuffsManager
        {
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[8], $"{steamId}_familiar_buffs.json");
            public static void SaveFamiliarBuffsData(ulong steamId, FamiliarBuffsData data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarBuffsData LoadFamiliarBuffsData(ulong steamId)
            {
                string filePath = GetFilePath(steamId);
                if (!File.Exists(filePath))
                    return new FamiliarBuffsData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarBuffsData>(jsonString);
            }
        }
        public static class FamiliarBattleGroupsManager // will be implementing soon'ish to support saving multiple battle groups
        {
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[10], $"{steamId}_battle_groups.json");
            public static void SaveFamiliarBattleGroupsData(ulong steamId, FamiliarBattleGroupsData data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarBattleGroupsData LoadFamiliarBattleGroupsData(ulong steamId)
            {
                string filePath = GetFilePath(steamId);
                if (!File.Exists(filePath))
                    return new FamiliarBattleGroupsData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarBattleGroupsData>(jsonString);
            }
        }
        public static class FamiliarEquipmentManager
        {
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[8], $"{steamId}_familiar_equipment.json");
            public static void SaveFamiliarEquipmentData(ulong steamId, FamiliarEquipmentData data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarEquipmentData LoadFamiliarEquipment(ulong steamId)
            {
                string filePath = GetFilePath(steamId);
                if (!File.Exists(filePath))
                    return new FamiliarEquipmentData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarEquipmentData>(jsonString);
            }
            public static List<int> GetFamiliarEquipment(ulong steamId, int famKey)
            {
                var equipmentData = LoadFamiliarEquipment(steamId);

                if (!equipmentData.FamiliarEquipment.TryGetValue(famKey, out var equipment))
                {
                    equipment = [0, 0, 0, 0, 0, 0];
                    equipmentData.FamiliarEquipment[famKey] = equipment;

                    SaveFamiliarEquipmentData(steamId, equipmentData);
                }

                return equipment;
            }
            public static void SaveFamiliarEquipment(ulong steamId, int famKey, List<int> equipment)
            {
                var equipmentData = LoadFamiliarEquipment(steamId);
                equipmentData.FamiliarEquipment[famKey] = equipment;

                SaveFamiliarEquipmentData(steamId, equipmentData);
            }
        }

        /* need to rethink some of these since primaryAttackSpeed is garbage for most if not unused on units entirely and depends on how equipment stats pan out
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
        */
    }
    public static class PlayerDataInitialization
    {
        public static void LoadPlayerData()
        {
            try // guess this could be where all the migrating stuff and rely on enumerating files in directories instead of PlayerInfo from cache since that won't be ready yet, noting for later
            {
                string playerSanguimancyJson = Path.Combine(DirectoryPaths[3], "player_sanguimancy.json"); // handle old format to new
                string playerUnarmedJson = Path.Combine(DirectoryPaths[3], "player_unarmed.json");
                string playerFamiliarActivesJson = Path.Combine(DirectoryPaths[6], "player_familiar_actives.json");

                if (File.Exists(playerSanguimancyJson) && !File.Exists(playerUnarmedJson))
                {
                    File.Copy(playerSanguimancyJson, _playerUnarmedExpertiseJson, overwrite: false);
                }

                if (File.Exists(playerFamiliarActivesJson))
                {
                    File.Delete(playerFamiliarActivesJson);
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to migrate sanguimancy data to unarmed: {ex}");
            }

            // LoadPlayerBools();

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
            LoadFamiliarBattleCoords,
            LoadFamiliarBattleGroups
        ];
    }
}
