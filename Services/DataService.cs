using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.ConfigService;
using static Bloodcraft.Services.ConfigService.ConfigInitialization;
using static Bloodcraft.Services.DataService.FamiliarEquipment;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarUnlocksManager;
using static Bloodcraft.Services.DataService.PlayerDictionaries;
using static Bloodcraft.Services.DataService.PlayerPersistence;
using static Bloodcraft.Services.DataService.PlayerPersistence.JsonFilePaths;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Leveling.ClassManager;
using static Bloodcraft.Utilities.Familiars;
using static Bloodcraft.Utilities.Misc;
using static Bloodcraft.Utilities.Shapeshifts;
using WeaponType = Bloodcraft.Interfaces.WeaponType;

namespace Bloodcraft.Services;
internal static class DataService
{
    static readonly object PersistenceSuppressionLock = new();
    static int persistenceSuppressionDepth;

    internal static IDisposable SuppressPersistence()
    {
        return new PersistenceSuppressionScope();
    }

    static bool IsPersistenceSuppressed
    {
        get
        {
            lock (PersistenceSuppressionLock)
            {
                return persistenceSuppressionDepth > 0;
            }
        }
    }

    sealed class PersistenceSuppressionScope : IDisposable
    {
        bool disposed;

        public PersistenceSuppressionScope()
        {
            lock (PersistenceSuppressionLock)
            {
                persistenceSuppressionDepth++;
            }
        }

        public void Dispose()
        {
            lock (PersistenceSuppressionLock)
            {
                if (disposed)
                {
                    return;
                }

                persistenceSuppressionDepth--;
                disposed = true;
            }
        }
    }

    public static bool TryGetPlayerExperience(this ulong steamId, out KeyValuePair<int, float> experience)
    {
        return _playerExperience.TryGetValue(steamId, out experience);
    }
    public static bool TryGetPlayerRestedXP(this ulong steamId, out KeyValuePair<DateTime, float> restedXP)
    {
        return _playerRestedXP.TryGetValue(steamId, out restedXP);
    }
    public static bool TryGetPlayerClass(this ulong steamId, out PlayerClass playerClass)
    {
        return _playerClass.TryGetValue(steamId, out playerClass);
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
        return _playerGreatSwordExpertise.TryGetValue(steamId, out expertise);
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
        return _playerReaperExpertise.TryGetValue(steamId, out expertise);
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
    public static bool TryGetPlayerTwinBladesExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerTwinBladesExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerDaggersExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerDaggersExpertise.TryGetValue(steamId, out expertise);
    }
    public static bool TryGetPlayerClawsExpertise(this ulong steamId, out KeyValuePair<int, float> expertise)
    {
        return _playerClawsExpertise.TryGetValue(steamId, out expertise);
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
    public static bool TryGetPlayerCorruptionLegacy(this ulong steamId, out KeyValuePair<int, float> corruptedLegacy)
    {
        return _playerCorruptionLegacy.TryGetValue(steamId, out corruptedLegacy);
    }
    public static bool TryGetPlayerBloodStats(this ulong steamId, out Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> bloodStats)
    {
        return _playerBloodStats.TryGetValue(steamId, out bloodStats);
    }
    public static bool TryGetFamiliarBox(this ulong steamId, out string familiarSet)
    {
        return _playerFamiliarBox.TryGetValue(steamId, out familiarSet);
    }
    public static bool TryGetBindingIndex(this ulong steamId, out int index)
    {
        if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;

            if (playerCharacter.Has<BagHolder>())
            {
                BagHolder bagHolder = playerCharacter.Read<BagHolder>();
                index = bagHolder.BagInstance0.InventoryIndex;

                return true;
            }
        }

        return _playerBindingIndex.TryGetValue(steamId, out index);
    }
    public static bool TryGetPlayerShapeshift(this ulong steamId, out ShapeshiftType shapeshift)
    {
        shapeshift = default;

        if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;
            BagHolder bagHolder = playerCharacter.Read<BagHolder>();
            int bagInstanceIndex = bagHolder.BagInstance2.InventoryIndex;

            foreach (var kvp in ShapeshiftBuffs)
            {
                if (kvp.Value.GuidHash == bagInstanceIndex)
                {
                    shapeshift = kvp.Key;
                    return true;
                }
            }
        }

        return false;
    }
    public static bool TryGetPlayerQuests(this ulong steamId, out Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> quests)
    {
        return _playerQuests.TryGetValue(steamId, out quests);
    }
    public static void SetPlayerExperience(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerExperience[steamId] = data;
        if (IsPersistenceSuppressed)
        {
            return;
        }

        SavePlayerExperience();
    }
    public static void SetPlayerRestedXP(this ulong steamId, KeyValuePair<DateTime, float> data)
    {
        _playerRestedXP[steamId] = data;
        if (IsPersistenceSuppressed)
        {
            return;
        }

        SavePlayerRestedXP();
    }
    public static void SetPlayerClass(this ulong steamId, PlayerClass playerClass)
    {
        _playerClass[steamId] = playerClass;
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
        _playerGreatSwordExpertise[steamId] = data;
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
        _playerReaperExpertise[steamId] = data;
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
    public static void SetPlayerTwinBladesExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerTwinBladesExpertise[steamId] = data;
        SavePlayerTwinBladesExpertise();
    }
    public static void SetPlayerDaggersExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerDaggersExpertise[steamId] = data;
        SavePlayerDaggersExpertise();
    }
    public static void SetPlayerClawsExpertise(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerClawsExpertise[steamId] = data;
        SavePlayerClawsExpertise();
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
    public static void SetPlayerCorruptionLegacy(this ulong steamId, KeyValuePair<int, float> data)
    {
        _playerCorruptionLegacy[steamId] = data;
        SavePlayerCorruptionLegacy();
    }
    public static void SetPlayerBloodStats(this ulong steamId, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>> data)
    {
        _playerBloodStats[steamId] = data;
        SavePlayerBloodStats();
    }
    public static void SetFamiliarBox(this ulong steamId, string data = null)
    {
        FamiliarUnlocksData familiarBoxes = LoadFamiliarUnlocksData(steamId);

        if (string.IsNullOrEmpty(data) && steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;

            /*
            if (playerCharacter.TryGetComponent(out Energy energy) && energy.MaxEnergy._Value <= familiarBoxes.UnlockedFamiliars.Count)
            {
                int index = (int)energy.MaxEnergy._Value;
                List<string> boxes = [..familiarBoxes.UnlockedFamiliars.Keys];

                if (index < boxes.Count)
                {
                    data = boxes[index];
                }
            }
            */

            if (playerCharacter.TryGetComponent(out BagHolder bagHolder))
            {
                List<string> boxes = [..familiarBoxes.FamiliarUnlocks.Keys];
                int index = bagHolder.BagInstance1.InventoryIndex;

                if (boxes.IsIndexWithinRange(index))
                {
                    data = boxes[index];
                }
            }
        }
        else if (!string.IsNullOrEmpty(data) && familiarBoxes.FamiliarUnlocks.ContainsKey(data) && steamId.TryGetPlayerInfo(out playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;
            int index = familiarBoxes.FamiliarUnlocks.Keys.ToList().IndexOf(data);

            playerCharacter.HasWith((ref BagHolder bagHolder) => bagHolder.BagInstance1.InventoryIndex = index);
        }

        _playerFamiliarBox[steamId] = data;
    }
    public static void SetBindingIndex(this ulong steamId, int index)
    {
        if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;

            playerCharacter.HasWith((ref BagHolder bagHolder) => bagHolder.BagInstance0.InventoryIndex = index);
        }

        _playerBindingIndex[steamId] = index;
    }
    public static void SetPlayerQuests(this ulong steamId, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)> data)
    {
        _playerQuests[steamId] = data;
        SavePlayerQuests();
    }
    public static void SetPlayerShapeshift(this ulong steamId, ShapeshiftType shapeshiftType)
    {
        if (!ShapeshiftBuffs.TryGetValue(shapeshiftType, out PrefabGUID shapeshiftBuff))
        {
            Core.Log.LogWarning($"[DataService.SetPlayerShapeshift] ShapeshiftType {shapeshiftType} not found in ShapeshiftBuffs!");
            return;
        }
        else if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo))
        {
            Entity playerCharacter = playerInfo.CharEntity;

            playerCharacter.HasWith((ref BagHolder bagHolder) => bagHolder.BagInstance2.InventoryIndex = shapeshiftBuff.GuidHash);

            ShapeshiftCache.SetShapeshiftBuff(steamId, shapeshiftType);
        }
    }
    public static class PlayerDictionaries
    {
        // exoform data
        public static ConcurrentDictionary<ulong, KeyValuePair<DateTime, float>> _playerExoFormData = [];

        // leveling data
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerExperience = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<DateTime, float>> _playerRestedXP = [];

        // class data
        public static ConcurrentDictionary<ulong, ClassManager.PlayerClass> _playerClass = [];

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
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerTwinBladesExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerDaggersExpertise = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerClawsExpertise = [];
        public static ConcurrentDictionary<ulong, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>>> _playerWeaponStats = [];
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerUnarmedExpertise = [];
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
        public static ConcurrentDictionary<ulong, KeyValuePair<int, float>> _playerCorruptionLegacy = [];
        public static ConcurrentDictionary<ulong, Dictionary<BloodType, List<BloodManager.BloodStats.BloodStatType>>> _playerBloodStats = [];

        // familiar data
        public static ConcurrentDictionary<ulong, string> _playerFamiliarBox = [];
        public static ConcurrentDictionary<ulong, int> _playerBindingIndex = [];

        // battle arena centers
        public static List<List<float>> _familiarBattleCoords = [];

        // players filtered from appearing in prestige leaderboard data (for accounts unconcerned with progression existing only for the fulfillment of admin-related duties)
        public static List<ulong> _ignorePrestigeLeaderboard = [];

        // players banned from receiving shared experience due to bad behaviour or whatever
        public static List<ulong> _ignoreSharedExperience = [];

        // quests data
        public static ConcurrentDictionary<ulong, Dictionary<QuestSystem.QuestType, (QuestSystem.QuestObjective Objective, int Progress, DateTime LastReset)>> _playerQuests = [];
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
            {"Experience", PlayerExperienceJson},
            {"RestedXP", PlayerRestedXPJson },
            {"Quests", PlayerQuestsJson },
            {"Classes", PlayerClassesJson },
            {"Prestiges", PlayerPrestigesJson },
            {"ExoFormData", PlayerExoFormsJson },
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
            {"TwinBladesExpertise", PlayerTwinBladesExpertiseJson},
            {"DaggersExpertise", PlayerDaggersExpertiseJson},
            {"ClawsExpertise", PlayerClawsExpertiseJson},
            {"PlayerSpells", PlayerSpellsJson},
            {"WeaponStats", PlayerWeaponStatsJson},
            {"WorkerLegacy", PlayerWorkerLegacyJson},
            {"WarriorLegacy", PlayerWarriorLegacyJson},
            {"ScholarLegacy", PlayerScholarLegacyJson},
            {"RogueLegacy", PlayerRogueLegacyJson},
            {"MutantLegacy", PlayerMutantLegacyJson},
            {"DraculinLegacy", PlayerDraculinLegacyJson},
            {"ImmortalLegacy", PlayerImmortalLegacyJson},
            {"CreatureLegacy", PlayerCreatureLegacyJson},
            {"BruteLegacy", PlayerBruteLegacyJson},
            {"CorruptionLegacy", PlayerCorruptionLegacyJson},
            {"BloodStats", PlayerBloodStatsJson},
            {"FamiliarBattleCoords", FamiliarBattleCoordsJson },
            {"IgnoredPrestigeLeaderboard", IgnoredPrestigeLeaderboardJson},
            {"IgnoredSharedExperience", IgnoredSharedExperienceJson}
        };
        public static class JsonFilePaths
        {
            public static readonly string PlayerExperienceJson = Path.Combine(DirectoryPaths[1], "player_experience.json");
            public static readonly string PlayerRestedXPJson = Path.Combine(DirectoryPaths[1], "player_rested_xp.json");
            public static readonly string PlayerQuestsJson = Path.Combine(DirectoryPaths[2], "player_quests.json");
            public static readonly string PlayerPrestigesJson = Path.Combine(DirectoryPaths[1], "player_prestiges.json");
            public static readonly string PlayerExoFormsJson = Path.Combine(DirectoryPaths[1], "player_exoforms.json");
            public static readonly string PlayerClassesJson = Path.Combine(DirectoryPaths[0], "player_classes.json");
            public static readonly string PlayerPartiesJson = Path.Combine(DirectoryPaths[0], "player_parties.json");
            public static readonly string PlayerWoodcuttingJson = Path.Combine(DirectoryPaths[5], "player_woodcutting.json");
            public static readonly string PlayerMiningJson = Path.Combine(DirectoryPaths[5], "player_mining.json");
            public static readonly string PlayerFishingJson = Path.Combine(DirectoryPaths[5], "player_fishing.json");
            public static readonly string PlayerBlacksmithingJson = Path.Combine(DirectoryPaths[5], "player_blacksmithing.json");
            public static readonly string PlayerTailoringJson = Path.Combine(DirectoryPaths[5], "player_tailoring.json");
            public static readonly string PlayerEnchantingJson = Path.Combine(DirectoryPaths[5], "player_enchanting.json");
            public static readonly string PlayerAlchemyJson = Path.Combine(DirectoryPaths[5], "player_alchemy.json");
            public static readonly string PlayerHarvestingJson = Path.Combine(DirectoryPaths[5], "player_harvesting.json");
            public static readonly string PlayerSwordExpertiseJson = Path.Combine(DirectoryPaths[3], "player_sword.json");
            public static readonly string PlayerAxeExpertiseJson = Path.Combine(DirectoryPaths[3], "player_axe.json");
            public static readonly string PlayerMaceExpertiseJson = Path.Combine(DirectoryPaths[3], "player_mace.json");
            public static readonly string PlayerSpearExpertiseJson = Path.Combine(DirectoryPaths[3], "player_spear.json");
            public static readonly string PlayerCrossbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_crossbow.json");
            public static readonly string PlayerGreatSwordExpertise = Path.Combine(DirectoryPaths[3], "player_greatsword.json");
            public static readonly string PlayerSlashersExpertiseJson = Path.Combine(DirectoryPaths[3], "player_slashers.json");
            public static readonly string PlayerPistolsExpertiseJson = Path.Combine(DirectoryPaths[3], "player_pistols.json");
            public static readonly string PlayerReaperExpertise = Path.Combine(DirectoryPaths[3], "player_reaper.json");
            public static readonly string PlayerLongbowExpertiseJson = Path.Combine(DirectoryPaths[3], "player_longbow.json");
            public static readonly string PlayerUnarmedExpertiseJson = Path.Combine(DirectoryPaths[3], "player_unarmed.json");
            public static readonly string PlayerWhipExpertiseJson = Path.Combine(DirectoryPaths[3], "player_whip.json");
            public static readonly string PlayerFishingPoleExpertiseJson = Path.Combine(DirectoryPaths[3], "player_fishingpole.json");
            public static readonly string PlayerTwinBladesExpertiseJson = Path.Combine(DirectoryPaths[3], "player_twinblades.json");
            public static readonly string PlayerDaggersExpertiseJson = Path.Combine(DirectoryPaths[3], "player_daggers.json");
            public static readonly string PlayerClawsExpertiseJson = Path.Combine(DirectoryPaths[3], "player_claws.json");
            public static readonly string PlayerSpellsJson = Path.Combine(DirectoryPaths[1], "player_spells.json");
            public static readonly string PlayerWeaponStatsJson = Path.Combine(DirectoryPaths[3], "player_weapon_stats.json");
            public static readonly string PlayerWorkerLegacyJson = Path.Combine(DirectoryPaths[4], "player_worker.json");
            public static readonly string PlayerWarriorLegacyJson = Path.Combine(DirectoryPaths[4], "player_warrior.json");
            public static readonly string PlayerScholarLegacyJson = Path.Combine(DirectoryPaths[4], "player_scholar.json");
            public static readonly string PlayerRogueLegacyJson = Path.Combine(DirectoryPaths[4], "player_rogue.json");
            public static readonly string PlayerMutantLegacyJson = Path.Combine(DirectoryPaths[4], "player_mutant.json");
            public static readonly string PlayerDraculinLegacyJson = Path.Combine(DirectoryPaths[4], "player_draculin.json");
            public static readonly string PlayerImmortalLegacyJson = Path.Combine(DirectoryPaths[4], "player_immortal.json");
            public static readonly string PlayerCreatureLegacyJson = Path.Combine(DirectoryPaths[4], "player_creature.json");
            public static readonly string PlayerBruteLegacyJson = Path.Combine(DirectoryPaths[4], "player_brute.json");
            public static readonly string PlayerCorruptionLegacyJson = Path.Combine(DirectoryPaths[4], "player_corruption.json");
            public static readonly string PlayerBloodStatsJson = Path.Combine(DirectoryPaths[4], "player_blood_stats.json");
            public static readonly string FamiliarBattleCoordsJson = Path.Combine(DirectoryPaths[6], "familiar_battle_coords.json");
            public static readonly string IgnoredPrestigeLeaderboardJson = Path.Combine(DirectoryPaths[1], "ignored_prestige_leaderboard.json");
            public static readonly string IgnoredSharedExperienceJson = Path.Combine(DirectoryPaths[1], "ignored_shared_experience.json");
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
                Core.Log.LogWarning($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void LoadData<T>(ref List<List<float>> dataStructure, string key)
        {
            string path = _filePaths[key];
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                dataStructure = [];
                // Core.Log.LogInfo($"{key} file created...");

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
                Core.Log.LogWarning($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void LoadData<T>(ref List<ulong> dataStructure, string key)
        {
            string path = _filePaths[key];
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                dataStructure = [];
                // Core.Log.LogInfo($"{key} file created...");

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
                    var data = JsonSerializer.Deserialize<List<ulong>>(json, _jsonOptions);
                    dataStructure = data ?? [];
                }
            }
            catch (IOException ex)
            {
                Core.Log.LogWarning($"Failed to read {key} data from file: {ex.Message}");
            }
        }
        static void SaveData<T>(ConcurrentDictionary<ulong, T> data, string key)
        {
            if (IsPersistenceSuppressed)
            {
                return;
            }

            string path = _filePaths[key];
            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(path, json);
            }
            catch (IOException ex)
            {
                Core.Log.LogWarning($"Failed to write {key} data to file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Core.Log.LogWarning($"JSON serialization error when saving {key} data: {ex.Message}");
            }
        }
        static void SaveData<T>(List<List<float>> data, string key)
        {
            if (IsPersistenceSuppressed)
            {
                return;
            }

            string path = _filePaths[key];

            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(path, json);
            }
            catch (IOException ex)
            {
                Core.Log.LogWarning($"Failed to write {key} data to file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Core.Log.LogWarning($"JSON serialization error when saving {key} data: {ex.Message}");
            }
        }
        static void SaveData<T>(List<ulong> data, string key)
        {
            if (IsPersistenceSuppressed)
            {
                return;
            }

            string path = _filePaths[key];

            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOptions);
                File.WriteAllText(path, json);
            }
            catch (IOException ex)
            {
                Core.Log.LogWarning($"Failed to write {key} data to file: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Core.Log.LogWarning($"JSON serialization error saving {key} - {ex.Message}");
            }
        }

        // load methods
        public static void LoadPlayerExperience() => LoadData(ref _playerExperience, "Experience");
        public static void LoadPlayerRestedXP() => LoadData(ref _playerRestedXP, "RestedXP");
        public static void LoadPlayerQuests() => LoadData(ref _playerQuests, "Quests");
        public static void LoadPlayerClasses() => LoadData(ref _playerClass, "Classes");
        public static void LoadPlayerPrestiges() => LoadData(ref _playerPrestiges, "Prestiges");
        public static void LoadPlayerExoFormData() => LoadData(ref _playerExoFormData, "ExoFormData");
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
        public static void LoadPlayerGreatSwordExpertise() => LoadData(ref _playerGreatSwordExpertise, "GreatSwordExpertise");
        public static void LoadPlayerSlashersExpertise() => LoadData(ref _playerSlashersExpertise, "SlashersExpertise");
        public static void LoadPlayerPistolsExpertise() => LoadData(ref _playerPistolsExpertise, "PistolsExpertise");
        public static void LoadPlayerReaperExpertise() => LoadData(ref _playerReaperExpertise, "ReaperExpertise");
        public static void LoadPlayerLongbowExpertise() => LoadData(ref _playerLongbowExpertise, "LongbowExpertise");
        public static void LoadPlayerWhipExpertise() => LoadData(ref _playerWhipExpertise, "WhipExpertise");
        public static void LoadPlayerFishingPoleExpertise() => LoadData(ref _playerFishingPoleExpertise, "FishingPoleExpertise");
        public static void LoadPlayerUnarmedExpertise() => LoadData(ref _playerUnarmedExpertise, "UnarmedExpertise");
        public static void LoadPlayerTwinBladesExpertise() => LoadData(ref _playerTwinBladesExpertise, "TwinBladesExpertise");
        public static void LoadPlayerDaggersExpertise() => LoadData(ref _playerDaggersExpertise, "DaggersExpertise");
        public static void LoadPlayerClawsExpertise() => LoadData(ref _playerClawsExpertise, "ClawsExpertise");
        public static void LoadPlayerSpells() => LoadData(ref _playerSpells, "PlayerSpells");
        public static void LoadPlayerWeaponStats() => LoadData(ref _playerWeaponStats, "WeaponStats");
        public static void LoadPlayerWorkerLegacy() => LoadData(ref _playerWorkerLegacy, "WorkerLegacy");
        public static void LoadPlayerWarriorLegacy() => LoadData(ref _playerWarriorLegacy, "WarriorLegacy");
        public static void LoadPlayerScholarLegacy() => LoadData(ref _playerScholarLegacy, "ScholarLegacy");
        public static void LoadPlayerRogueLegacy() => LoadData(ref _playerRogueLegacy, "RogueLegacy");
        public static void LoadPlayerMutantLegacy() => LoadData(ref _playerMutantLegacy, "MutantLegacy");
        public static void LoadPlayerDraculinLegacy() => LoadData(ref _playerDraculinLegacy, "DraculinLegacy");
        public static void LoadPlayerImmortalLegacy() => LoadData(ref _playerImmortalLegacy, "ImmortalLegacy");
        public static void LoadPlayerCreatureLegacy() => LoadData(ref _playerCreatureLegacy, "CreatureLegacy");
        public static void LoadPlayerBruteLegacy() => LoadData(ref _playerBruteLegacy, "BruteLegacy");
        public static void LoadPlayerCorruptionLegacy() => LoadData(ref _playerCorruptionLegacy, "CorruptionLegacy");
        public static void LoadPlayerBloodStats() => LoadData(ref _playerBloodStats, "BloodStats");
        public static void LoadFamiliarBattleCoords() => LoadData<List<float>>(ref _familiarBattleCoords, "FamiliarBattleCoords");
        public static void LoadIgnoredPrestigeLeaderboard() => LoadData<List<ulong>>(ref _ignorePrestigeLeaderboard, "IgnoredPrestigeLeaderboard");
        public static void LoadIgnoredSharedExperience() => LoadData<List<ulong>>(ref _ignoreSharedExperience, "IgnoredSharedExperience");

        // save methods
        public static void SavePlayerExperience() => SaveData(_playerExperience, "Experience");
        public static void SavePlayerRestedXP() => SaveData(_playerRestedXP, "RestedXP");
        public static void SavePlayerQuests() => SaveData(_playerQuests, "Quests");
        public static void SavePlayerClasses() => SaveData(_playerClass, "Classes");
        public static void SavePlayerPrestiges() => SaveData(_playerPrestiges, "Prestiges");
        public static void SavePlayerExoFormData() => SaveData(_playerExoFormData, "ExoFormData");
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
        public static void SavePlayerGreatSwordExpertise() => SaveData(_playerGreatSwordExpertise, "GreatSwordExpertise");
        public static void SavePlayerSlashersExpertise() => SaveData(_playerSlashersExpertise, "SlashersExpertise");
        public static void SavePlayerPistolsExpertise() => SaveData(_playerPistolsExpertise, "PistolsExpertise");
        public static void SavePlayerReaperExpertise() => SaveData(_playerReaperExpertise, "ReaperExpertise");
        public static void SavePlayerLongbowExpertise() => SaveData(_playerLongbowExpertise, "LongbowExpertise");
        public static void SavePlayerWhipExpertise() => SaveData(_playerWhipExpertise, "WhipExpertise");
        public static void SavePlayerFishingPoleExpertise() => SaveData(_playerFishingPoleExpertise, "FishingPoleExpertise");
        public static void SavePlayerUnarmedExpertise() => SaveData(_playerUnarmedExpertise, "UnarmedExpertise");
        public static void SavePlayerTwinBladesExpertise() => SaveData(_playerTwinBladesExpertise, "TwinBladesExpertise");
        public static void SavePlayerDaggersExpertise() => SaveData(_playerDaggersExpertise, "DaggersExpertise");
        public static void SavePlayerClawsExpertise() => SaveData(_playerClawsExpertise, "ClawsExpertise");
        public static void SavePlayerSpells() => SaveData(_playerSpells, "PlayerSpells");
        public static void SavePlayerWeaponStats() => SaveData(_playerWeaponStats, "WeaponStats");
        public static void SavePlayerWorkerLegacy() => SaveData(_playerWorkerLegacy, "WorkerLegacy");
        public static void SavePlayerWarriorLegacy() => SaveData(_playerWarriorLegacy, "WarriorLegacy");
        public static void SavePlayerScholarLegacy() => SaveData(_playerScholarLegacy, "ScholarLegacy");
        public static void SavePlayerRogueLegacy() => SaveData(_playerRogueLegacy, "RogueLegacy");
        public static void SavePlayerMutantLegacy() => SaveData(_playerMutantLegacy, "MutantLegacy");
        public static void SavePlayerDraculinLegacy() => SaveData(_playerDraculinLegacy, "DraculinLegacy");
        public static void SavePlayerImmortalLegacy() => SaveData(_playerImmortalLegacy, "ImmortalLegacy");
        public static void SavePlayerCreatureLegacy() => SaveData(_playerCreatureLegacy, "CreatureLegacy");
        public static void SavePlayerBruteLegacy() => SaveData(_playerBruteLegacy, "BruteLegacy");
        public static void SavePlayerCorruptionLegacy() => SaveData(_playerCorruptionLegacy, "CorruptionLegacy");
        public static void SavePlayerBloodStats() => SaveData(_playerBloodStats, "BloodStats");
        public static void SaveFamiliarBattleCoords() => SaveData<List<float>>(_familiarBattleCoords, "FamiliarBattleCoords");
        public static void SaveIgnoredPrestigeLeaderboard() => SaveData<List<ulong>>(_ignorePrestigeLeaderboard, "IgnoredPrestigeLeaderboard");
        public static void SaveIgnoredSharedExperience() => SaveData<List<ulong>>(_ignoreSharedExperience, "IgnoredSharedExperience");
    }
    public static class PlayerBoolsManager
    {
        static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[9], $"{steamId}_player_bools.json");
        public static void SavePlayerBools(ulong steamId, Dictionary<string, bool> preferences)
        {
            string filePath = GetFilePath(steamId);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(preferences, options);

            File.WriteAllText(filePath, jsonString);
        }
        public static Dictionary<string, bool> LoadPlayerBools(ulong steamId)
        {
            string filePath = GetFilePath(steamId);

            if (!File.Exists(filePath))
                return [];

            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(jsonString);
        }
        public static Dictionary<string, bool> GetOrInitializePlayerBools(ulong steamId, Dictionary<string, bool> defaultBools)
        {
            var bools = LoadPlayerBools(steamId);

            foreach (var key in defaultBools.Keys)
            {
                if (!bools.ContainsKey(key))
                {
                    bools[key] = defaultBools[key];
                }
            }

            bools[Misc.PlayerBoolsManager.CLASS_BUFFS_KEY] = false;
            SavePlayerBools(steamId, bools);
            return bools;
        }
    }
    public static class FamiliarPersistence
    {
        static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
        };
        public static class FamiliarUnlocksManager
        {
            [Serializable]
            public class FamiliarUnlocksData
            {
                public Dictionary<string, List<int>> FamiliarUnlocks { get; set; } = [];
            }
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
            [Serializable]
            public class FamiliarExperienceData
            {
                public Dictionary<int, KeyValuePair<int, float>> FamiliarExperience { get; set; } = [];
            }
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
        public static class FamiliarPrestigeManager // redo this with class for prestige data when adding traits or something
        {
            [Serializable]
            public class FamiliarPrestigeData
            {
                public Dictionary<int, int> FamiliarPrestige { get; set; } = [];
            }
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[7], $"{steamId}_familiar_prestige.json");
            public static void SaveFamiliarPrestigeData(ulong steamId, FamiliarPrestigeData data)
            {
                string filePath = GetFilePath(steamId);
                string jsonString = JsonSerializer.Serialize(data, _jsonOptions);

                File.WriteAllText(filePath, jsonString);
            }
            public static FamiliarPrestigeData LoadFamiliarPrestigeData(ulong steamId)
            {
                string filePath = GetFilePath(steamId);

                if (!File.Exists(filePath))
                    return new FamiliarPrestigeData();

                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<FamiliarPrestigeData>(jsonString);
            }
        }
        public static class FamiliarBuffsManager
        {
            [Serializable]
            public class FamiliarBuffsData
            {
                public Dictionary<int, List<int>> FamiliarBuffs { get; set; } = []; // can use actual perma buffs or just musb_dots from traits I guess maybe?
            }
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
        public static class FamiliarBattleGroupsManager
        {
            const int MAX_BATTLE_GROUPS = 10;
            static readonly ConcurrentDictionary<ulong, FamiliarBattleGroup> _activeFamiliarBattleGroup = []; // use first battle group for player if nothing here

            [Serializable]
            public class FamiliarBattleGroup
            {
                public string Name { get; set; }
                public List<int> Familiars { get; set; } = [0, 0, 0];
            }

            [Serializable]
            public class FamiliarBattleGroupsData
            {
                public List<FamiliarBattleGroup> BattleGroups { get; set; } = [];
            }
            static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[11], $"{steamId}_familiar_battle_groups.json");
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
            public static bool AssignFamiliarToGroup(ChatCommandContext ctx, ulong steamId, string groupName, int slotIndex)
            {
                var data = LoadFamiliarBattleGroupsData(steamId);
                if (!data.BattleGroups.Any(bg => bg.Name == groupName)) return false;

                var battleGroup = data.BattleGroups.First(bg => bg.Name == groupName);

                if (battleGroup.Familiars.Contains(ActiveFamiliarManager.GetActiveFamiliarData(steamId).FamiliarId))
                {
                    LocalizationService.HandleReply(ctx, "Active familiar already present in battle group!");
                    return false;
                }

                battleGroup.Familiars[slotIndex - 1] = ActiveFamiliarManager.GetActiveFamiliarData(steamId).FamiliarId;
                SaveFamiliarBattleGroupsData(steamId, data);
                return true;
            }
            public static bool AssignFamiliarToGroupDebug(ulong steamId, string groupName, int slotIndex, int familiarId)
            {
                var data = LoadFamiliarBattleGroupsData(steamId);
                var battleGroup = data.BattleGroups.FirstOrDefault(bg => bg.Name == groupName);

                if (battleGroup == null)
                {
                    Core.Log.LogWarning($"Battle group {groupName} not found for {steamId} for auto queue testing!");
                    return false;
                }

                battleGroup.Familiars[slotIndex] = familiarId;
                SaveFamiliarBattleGroupsData(steamId, data);
                Core.Log.LogWarning($"Assigned familiar {familiarId} to {groupName} slot {slotIndex} for {steamId} for auto queue testing!");

                return true;
            }
            public static FamiliarBattleGroup GetFamiliarBattleGroup(ulong steamId, string groupName)
            {
                FamiliarBattleGroupsData data = LoadFamiliarBattleGroupsData(steamId);
                return data.BattleGroups.FirstOrDefault(bg => bg.Name == groupName);
            }
            public static bool CreateBattleGroup(ChatCommandContext ctx, ulong steamId, string groupName)
            {
                var data = LoadFamiliarBattleGroupsData(steamId);
                if (data.BattleGroups.Count >= MAX_BATTLE_GROUPS)
                {
                    LocalizationService.HandleReply(ctx, $"Can't have more than <color=white>{MAX_BATTLE_GROUPS}</color> battle groups!");
                    return false;
                }

                if (data.BattleGroups.Any(bg => bg.Name == groupName)) return false;

                data.BattleGroups.Add(new FamiliarBattleGroup { Name = groupName });
                SaveFamiliarBattleGroupsData(steamId, data);
                return true;
            }
            public static bool DeleteBattleGroup(ChatCommandContext ctx, ulong steamId, string groupName)
            {
                var data = LoadFamiliarBattleGroupsData(steamId);
                var group = data.BattleGroups.FirstOrDefault(bg => bg.Name == groupName);
                if (group == null) return false;

                data.BattleGroups.Remove(group);
                SaveFamiliarBattleGroupsData(steamId, data);
                LocalizationService.HandleReply(ctx, $"Deleted battle group <color=white>{groupName}</color>!");
                return true;
            }
            public static bool SetActiveBattleGroup(ChatCommandContext ctx, ulong steamId, string groupName)
            {
                var data = LoadFamiliarBattleGroupsData(steamId);
                var battleGroup = data.BattleGroups.FirstOrDefault(bg => bg.Name == groupName);
                if (battleGroup == null) return false;

                _activeFamiliarBattleGroup[steamId] = battleGroup;
                return true;
            }
            public static string GetActiveBattleGroupName(ulong steamId)
            {
                return _activeFamiliarBattleGroup.TryGetValue(steamId, out var battleGroup) ? battleGroup.Name : string.Empty;
            }
            public static void HandleBattleGroupDetailsReply(ChatCommandContext ctx, ulong steamId, FamiliarBattleGroup battleGroup)
            {
                if (battleGroup.Familiars.Any(x => x != 0))
                {
                    FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);
                    FamiliarPrestigeData prestigeData = LoadFamiliarPrestigeData(steamId);
                    List<string> familiars = [];

                    BuildBattleGroupDetailsReply(steamId, buffsData, prestigeData, battleGroup, ref familiars);

                    string familiarReply = string.Join(", ", familiars);
                    LocalizationService.HandleReply(ctx, $"Battle Group - {familiarReply}");
                }
                else
                {
                    LocalizationService.HandleReply(ctx, "No familiars in battle group!");
                }
            }
            static void BuildBattleGroupDetailsReply(ulong steamId, FamiliarBuffsData buffsData, FamiliarPrestigeData prestigeData, FamiliarBattleGroup battleGroup, ref List<string> familiars)
            {
                foreach (int famKey in battleGroup.Familiars)
                {
                    if (famKey == 0) continue;

                    PrefabGUID famPrefab = new(famKey);
                    string famName = famPrefab.GetLocalizedName();
                    string colorCode = "<color=#FF69B4>";

                    int level = FamiliarLevelingSystem.GetFamiliarExperience(steamId, famKey).Key;
                    int prestiges = prestigeData.FamiliarPrestige.ContainsKey(famKey) ? prestigeData.FamiliarPrestige[famKey] : 0;

                    if (buffsData.FamiliarBuffs.ContainsKey(famKey) && FamiliarUnlockSystem.ShinyBuffColorHexes.TryGetValue(new(buffsData.FamiliarBuffs[famKey][0]), out var hexColor))
                    {
                        colorCode = $"<color={hexColor}>";
                    }

                    familiars.Add($"<color=white>{battleGroup.Familiars.IndexOf(famKey) + 1}</color>: <color=green>{famName}</color>{(buffsData.FamiliarBuffs.ContainsKey(famKey) ? $"{colorCode}*</color>" : "")} [<color=white>{level}</color>][<color=#90EE90>{prestiges}</color>]");
                }
            }
        }

        /*
        public static class FamiliarEquipmentManager
        {
            static SystemService SystemService => Core.SystemService;
            static JewelSpawnSystem JewelSpawnSystem => SystemService.JewelSpawnSystem;

            const int EQUIPMENT_SLOTS = 7;
            static readonly PrefabGUID _bonusStatsBuff = Buffs.BonusPlayerStatsBuff;

            static readonly JsonSerializerOptions _jsonOptions = new()
            {
                WriteIndented = true,
                Converters = { new FamiliarEquipment.EquipmentBaseConverter() }
            };

            static string GetFilePath(ulong steamId) =>Path.Combine(DirectoryPaths[10], $"{steamId}_familiar_equipment.json");

            public static void SaveFamiliarEquipmentData(ulong steamId, FamiliarEquipment.FamiliarEquipmentData data)
            {
                try
                {
                    File.WriteAllText(GetFilePath(steamId), JsonSerializer.Serialize(data, _jsonOptions));
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"[SaveFamiliarEquipmentData] Failed to save familiar equipment ({steamId}) - {ex.Message}");
                }
            }

            public static FamiliarEquipment.FamiliarEquipmentData LoadFamiliarEquipment(ulong steamId)
            {
                string filePath = GetFilePath(steamId);

                try
                {
                    return File.Exists(filePath)
                        ? JsonSerializer.Deserialize<FamiliarEquipment.FamiliarEquipmentData>(File.ReadAllText(filePath), _jsonOptions) ?? new FamiliarEquipment.FamiliarEquipmentData()
                        : new FamiliarEquipment.FamiliarEquipmentData();
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"[LoadFamiliarEquipment] Failed to load familiar equipment ({steamId}) - {ex.Message}");
                    return new FamiliarEquipment.FamiliarEquipmentData();
                }
            }

            public static List<FamiliarEquipment.EquipmentBase> GetFamiliarEquipment(ulong steamId, int famKey)
            {
                var equipmentData = LoadFamiliarEquipment(steamId);

                if (!equipmentData.FamiliarEquipment.TryGetValue(famKey, out var familiarEquipment))
                {
                    familiarEquipment = [..Enumerable.Range(0, EQUIPMENT_SLOTS).Select(_ => (FamiliarEquipment.EquipmentBase)new FamiliarEquipment.StandardEquipment { Equipment = 0, Quality = 0 })];

                    equipmentData.FamiliarEquipment[famKey] = familiarEquipment;
                    SaveFamiliarEquipmentData(steamId, equipmentData);
                }

                return familiarEquipment;
            }

            public static void SaveFamiliarEquipment(ulong steamId, int famKey, List<FamiliarEquipment.EquipmentBase> familiarEquipment)
            {
                var equipmentData = LoadFamiliarEquipment(steamId);
                equipmentData.FamiliarEquipment[famKey] = familiarEquipment;
                SaveFamiliarEquipmentData(steamId, equipmentData);
            }

            public static void EquipFamiliar(ulong steamId, int famKey, Entity servant, Entity familiar)
            {
                EntityManager entityManager = Core.EntityManager;
                bool professions = ConfigService.ProfessionSystem;
                List<FamiliarEquipment.EquipmentBase> familiarEquipment = GetFamiliarEquipment(steamId, famKey);

                Entity inventory = InventoryUtilities.TryGetInventoryEntity(entityManager, servant, out inventory) ? inventory : Entity.Null;
                if (!inventory.Exists()) return;

                foreach (var equipment in familiarEquipment)
                {
                    if (equipment.Equipment.Equals(0)) continue;

                    PrefabGUID equipmentPrefabGuid = new(equipment.Equipment);
                    int professionLevel = professions ? equipment.Quality : 0;

                    AddItemResponse addItemResponse = InventoryUtilitiesServer.TryAddItem(Core.GetAddItemSettings(), inventory, equipmentPrefabGuid, 1);
                    Entity equipmentEntity = addItemResponse.NewEntity;

                    // CreateLegendaryWeaponDebugEvent
                    // SystemService.DebugEventsSystem.CreateLegendaryWeaponEvent
                    // would need user to have spare slot to then equip on familiar, could try tryAddItem with the inventoryBuffer option? and haxemptyslot or w/e idk geez

                    if (equipmentEntity.IsAncestralWeapon() && equipment is FamiliarEquipmentModel.AncestralWeapon ancestralWeapon)
                    {
                        equipmentEntity.AddWith((ref LegendaryItemInstance legendaryItem) =>
                        {
                            legendaryItem.TierIndex = (byte)ancestralWeapon.Tier;
                        });

                        equipmentEntity.AddWith((ref LegendaryItemSpellModSetComponent spellModSet) =>
                        {
                            spellModSet.StatMods.Mod0.Id = new PrefabGUID(ancestralWeapon.StatMods[0].StatMod);
                            spellModSet.StatMods.Mod0.Power = ancestralWeapon.StatMods[0].Value;
                            spellModSet.StatMods.Mod1.Id = new PrefabGUID(ancestralWeapon.StatMods[1].StatMod);
                            spellModSet.StatMods.Mod1.Power = ancestralWeapon.StatMods[1].Value;
                            spellModSet.StatMods.Mod2.Id = new PrefabGUID(ancestralWeapon.StatMods[2].StatMod);
                            spellModSet.StatMods.Mod2.Power = ancestralWeapon.StatMods[2].Value;
                            spellModSet.AbilityMods0.Mod0.Id = SpellSchoolInfusionMap.SpellSchoolInfusions[ancestralWeapon.Infusion];
                        });

                        JewelSpawnSystem.InitializeLegendaryItemData(equipmentEntity); // can save out the powers from 0-1 but still need what that is for the actual stat, ugh
                    }

                    if (professionLevel > 0 && equipmentEntity.Exists()) EquipmentQualityManager.ApplyFamiliarEquipmentStats(professionLevel, equipmentEntity);
                }

                // familiar.TryApplyBuff(_bonusStatsBuff);
                // Buffs.RefreshStats(familiar);
            }

            public static List<FamiliarEquipment.EquipmentBase> UnequipFamiliar(Entity servant)
            {
                servant.TryRemove<Disabled>();

                List<FamiliarEquipment.EquipmentBase> familiarEquipment = [];
                bool professions = ConfigService.ProfessionSystem;

                if (servant.TryGetComponent(out ServantEquipment servantEquipment))
                {
                    foreach (FamiliarEquipmentType familiarEquipmentType in Enum.GetValues(typeof(FamiliarEquipmentType)))
                    {
                        if (FamiliarEquipmentMap.TryGetValue(familiarEquipmentType, out EquipmentType equipmentType) && servantEquipment.IsEquipped(equipmentType))
                        {
                            Entity equipmentEntity = servantEquipment.GetEquipmentEntity(equipmentType).GetEntityOnServer();
                            PrefabGUID equipmentPrefabGuid = servantEquipment.GetEquipmentItemId(equipmentType);
                            int professionLevel = professions ? EquipmentQualityManager.CalculateProfessionLevelOfEquipmentFromMaxDurability(equipmentEntity) : 0;

                            if (!equipmentEntity.IsAncestralWeapon())
                            {
                                familiarEquipment.Add(new FamiliarEquipment.StandardEquipment { Equipment = equipmentPrefabGuid.GuidHash, Quality = professionLevel });
                                continue;
                            }
                            else
                            {
                                LegendaryItemInstance legendaryItemInstance = equipmentEntity.Read<LegendaryItemInstance>();
                                LegendaryItemSpellModSetComponent legendaryItemSpellModSet = equipmentEntity.Read<LegendaryItemSpellModSetComponent>();
                                SpellModSet statModSet = legendaryItemSpellModSet.StatMods;
                                PrefabGUID spellSchoolInfusion = legendaryItemSpellModSet.AbilityMods0.Mod0.Id;

                                var statMods = new FamiliarEquipment.StatMods[statModSet.Count];
                                for (int i = 0; i < statModSet.Count; i++)
                                {
                                    statMods[i] = new FamiliarEquipment.StatMods
                                    {
                                        StatMod = statModSet[i].Id.GuidHash,
                                        Value = statModSet[i].Power
                                    };
                                }

                                familiarEquipment.Add(new FamiliarEquipment.AncestralWeapon
                                {
                                    Equipment = equipmentPrefabGuid.GuidHash,
                                    Tier = legendaryItemInstance.TierIndex,
                                    Quality = professionLevel,
                                    Infusion = SpellSchoolInfusionMap.SpellSchoolInfusions[spellSchoolInfusion],
                                    StatMods = statMods
                                });
                            }
                        }
                        else
                        {
                            familiarEquipment.Add(new FamiliarEquipment.StandardEquipment { Equipment = 0, Quality = 0 });
                        }
                    }

                    servant.Destroy();
                    return familiarEquipment;
                }

                return [..Enumerable.Range(0, EQUIPMENT_SLOTS).Select(_ => (FamiliarEquipment.EquipmentBase)new FamiliarEquipment.StandardEquipment { Equipment = 0, Quality = 0 })];
            }

            public static List<FamiliarEquipment.EquipmentBase> GetFamiliarEquipment(Entity servant)
            {
                List<FamiliarEquipment.EquipmentBase> familiarEquipment = [];
                bool professions = ConfigService.ProfessionSystem;

                if (servant.TryGetComponent(out ServantEquipment servantEquipment))
                {
                    foreach (FamiliarEquipmentType familiarEquipmentType in Enum.GetValues(typeof(FamiliarEquipmentType)))
                    {
                        if (FamiliarEquipmentMap.TryGetValue(familiarEquipmentType, out EquipmentType equipmentType) &&
                            servantEquipment.IsEquipped(equipmentType))
                        {
                            Entity equipmentEntity = servantEquipment.GetEquipmentEntity(equipmentType).GetEntityOnServer();
                            PrefabGUID equipmentPrefabGuid = servantEquipment.GetEquipmentItemId(equipmentType);
                            int professionLevel = professions
                                ? EquipmentQualityManager.CalculateProfessionLevelOfEquipmentFromMaxDurability(equipmentEntity)
                                : 0;

                            if (!equipmentEntity.IsAncestralWeapon())
                            {
                                familiarEquipment.Add(new FamiliarEquipment.StandardEquipment
                                {
                                    Equipment = equipmentPrefabGuid.GuidHash,
                                    Quality = professionLevel
                                });

                                continue;
                            }

                            // Handle Ancestral Weapon
                            LegendaryItemInstance legendaryItemInstance = equipmentEntity.Read<LegendaryItemInstance>();
                            LegendaryItemSpellModSetComponent legendaryItemSpellModSet = equipmentEntity.Read<LegendaryItemSpellModSetComponent>();
                            SpellModSet statModSet = legendaryItemSpellModSet.StatMods;
                            PrefabGUID spellSchoolInfusion = legendaryItemSpellModSet.AbilityMods0.Mod0.Id;

                            var statMods = new FamiliarEquipment.StatMods[statModSet.Count];
                            for (int i = 0; i < statModSet.Count; i++)
                            {
                                statMods[i] = new FamiliarEquipment.StatMods
                                {
                                    StatMod = statModSet[i].Id.GuidHash,
                                    Value = statModSet[i].Power
                                };
                            }

                            familiarEquipment.Add(new FamiliarEquipment.AncestralWeapon
                            {
                                Equipment = equipmentPrefabGuid.GuidHash,
                                Tier = legendaryItemInstance.TierIndex,
                                Quality = professionLevel,
                                Infusion = SpellSchoolInfusionMap.SpellSchoolInfusions[spellSchoolInfusion],
                                StatMods = statMods
                            });
                        }
                        else
                        {
                            familiarEquipment.Add(new FamiliarEquipment.StandardEquipment
                            {
                                Equipment = 0,
                                Quality = 0
                            });
                        }
                    }
                }
                else
                {
                    familiarEquipment.AddRange(Enumerable.Range(0, EQUIPMENT_SLOTS).Select(_ =>
                        (FamiliarEquipment.EquipmentBase)new FamiliarEquipment.StandardEquipment { Equipment = 0, Quality = 0 }));
                }

                return familiarEquipment;
            }
        }
        */

        static readonly Dictionary<WeaponType, PrefabGUID> _sanguineWeapons = new()
        {
            { WeaponType.Sword, new PrefabGUID(-774462329) },
            { WeaponType.Axe, new PrefabGUID(-2044057823) },
            { WeaponType.Mace, new PrefabGUID(-126076280) },
            { WeaponType.Spear, new PrefabGUID(-850142339) },
            { WeaponType.Crossbow, new PrefabGUID(1389040540) },
            { WeaponType.GreatSword, new PrefabGUID(147836723) },
            { WeaponType.Slashers, new PrefabGUID(1322545846) },
            { WeaponType.Pistols, new PrefabGUID(1071656850) },
            { WeaponType.Reaper, new PrefabGUID(-2053917766) },
            { WeaponType.Longbow, new PrefabGUID(1860352606) },
            { WeaponType.Whip, new PrefabGUID(-655095317) },
            { WeaponType.TwinBlades, new PrefabGUID(-297349982) },
            { WeaponType.Daggers, new PrefabGUID(1031107636) },
            { WeaponType.Claws, new PrefabGUID(-1777908217) }
        };

        static readonly List<PrefabGUID> _shardNecklaces =
        [
            PrefabGUIDs.Item_MagicSource_SoulShard_Manticore,
            PrefabGUIDs.Item_MagicSource_SoulShard_Solarus,
            PrefabGUIDs.Item_MagicSource_SoulShard_Dracula,
            PrefabGUIDs.Item_MagicSource_SoulShard_Monster,
            PrefabGUIDs.Item_MagicSource_SoulShard_Morgana
        ];
        public static class FamiliarEquipmentManager
        {
            static EntityManager EntityManager => Core.EntityManager;
            static SystemService SystemService => Core.SystemService;
            static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

            static readonly EquipmentBaseV2 _emptySlot = CreateEmpty();
            const int EQUIPMENT_SLOTS = 6;

            static readonly JsonSerializerOptions _jsonOptions = new()
            {
                WriteIndented = true,
                Converters = { new EquipmentBaseConverter() }
            };

            static readonly JsonSerializerOptions _jsonOptionsV2 = new()
            {
                WriteIndented = true,
                Converters = { new EquipmentBaseV2Converter() }
            };
            static StandardEquipmentV2 CreateEmpty()
                => new() { Equipment = 0, Quality = 0, Durability = 0 };
            static string GetFilePath(ulong steamId)
                => Path.Combine(DirectoryPaths[10], $"{steamId}_familiar_equipment.json");
            public static void SaveFamiliarEquipmentData(ulong steamId, FamiliarEquipmentDataV2 data)
            {
                try
                {
                    File.WriteAllText(GetFilePath(steamId), JsonSerializer.Serialize(data, _jsonOptions));
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"[SaveFamiliarEquipmentData] ({steamId}) – {ex.Message}");
                }
            }
            public static FamiliarEquipmentDataV2 LoadFamiliarEquipment(ulong steamId)
            {
                string path = GetFilePath(steamId);

                // 1️⃣  load – same logic as before
                var dataV2 =
                      TryLoad<FamiliarEquipmentDataV2>(path, _jsonOptionsV2)
                   ?? ConvertFromV1(TryLoad<FamiliarEquipmentData>(path, _jsonOptions) ?? new())
                   ?? new();

                // 2️⃣  **always** normalise the items we just loaded
                foreach (var key in dataV2.FamiliarEquipment.Keys.ToArray())   // ToArray → safe copy
                    dataV2.FamiliarEquipment[key] =
                        ConvertList(dataV2.FamiliarEquipment[key]);            // ← mapping

                SaveFamiliarEquipmentData(steamId, dataV2);
                return dataV2;
            }
            static T TryLoad<T>(string p, JsonSerializerOptions opts)
                => File.Exists(p)
                   ? JsonSerializer.Deserialize<T>(File.ReadAllText(p), opts)
                   : default;
            static FamiliarEquipmentDataV2 ConvertFromV1(FamiliarEquipmentData oldData)
            {
                var result = new FamiliarEquipmentDataV2();

                foreach (var (famGuid, oldList) in oldData.FamiliarEquipment)
                    result.FamiliarEquipment[famGuid] = ConvertList(oldList);

                return result;
            }
            static List<EquipmentBaseV2> ConvertList<T>(IEnumerable<T> source)
            {
                var outList = new List<EquipmentBaseV2>(EQUIPMENT_SLOTS);

                foreach (object item in source)
                {
                    switch (item)
                    {
                        case StandardEquipment std:
                            outList.Add(ToStdV2(std.Equipment, std.Quality));
                            break;
                        case AncestralWeapon anc:
                            outList.Add(ToStdV2(anc.Equipment, anc.Quality));
                            break;
                        case StandardEquipmentV2 std2:
                            outList.Add(ToStdV2(std2.Equipment, std2.Quality, std2.Durability));
                            break;
                        case AncestralWeaponV2 anc2:
                            outList.Add(ToStdV2(anc2.Equipment, anc2.Quality, anc2.Durability));
                            break;
                    }
                }

                while (outList.Count < EQUIPMENT_SLOTS)
                    outList.Add(new StandardEquipmentV2 { Equipment = 0, Quality = 0, Durability = 0 });

                return outList;
            }
            static StandardEquipmentV2 ToStdV2(int guidHash, int quality, int durability = 0)
            {
                guidHash = MapToSanguineOrKey(guidHash);
                return new StandardEquipmentV2
                {
                    Equipment = guidHash,
                    Quality = quality,
                    Durability = durability
                };
            }
            static int MapToSanguineOrKey(int guidHash)
            {
                PrefabGUID itemPrefabGuid = new(guidHash);
                var prefabEntities = PrefabCollectionSystem._PrefabGuidToEntityMap;

                if (_shardNecklaces.Contains(itemPrefabGuid))
                    return PrefabGUIDs.Item_MagicSource_BloodKey_T01.GuidHash;

                if (itemPrefabGuid.HasValue()
                    && prefabEntities.TryGetValue(itemPrefabGuid, out var entity) && !entity.Has<ArmorLevelSource>() && entity.IsAncestralWeapon())
                {
                    WeaponType wt = WeaponSystem.GetWeaponTypeFromWeaponEntity(entity);

                    if (_sanguineWeapons.TryGetValue(wt, out PrefabGUID sanguine))
                        return sanguine.GuidHash;
                }

                return guidHash;
            }
            public static List<EquipmentBaseV2> GetFamiliarEquipment(ulong steamId, int famKey)
            {
                var data = LoadFamiliarEquipment(steamId);

                if (!data.FamiliarEquipment.TryGetValue(famKey, out var equipment))
                {
                    // Core.Log.LogWarning($"[GetFamiliarEquipment] No equipment found for {new PrefabGUID(famKey)}, initializing with empty slots...");
                    equipment = [..Enumerable.Range(0, EQUIPMENT_SLOTS).Select(_ => (EquipmentBaseV2)new StandardEquipmentV2 { Equipment = 0, Quality = 0, Durability = 0 })];

                    data.FamiliarEquipment[famKey] = equipment;
                    SaveFamiliarEquipmentData(steamId, data);
                }

                return equipment;
            }
            public static void SaveFamiliarEquipment(ulong steamId, int famKey, List<EquipmentBaseV2> equipment)
            {
                var data = LoadFamiliarEquipment(steamId);
                data.FamiliarEquipment[famKey] = equipment;
                SaveFamiliarEquipmentData(steamId, data);
            }
            public static void EquipFamiliar(ulong steamId, int famKey, Entity servant, Entity familiar)
            {
                bool professions = ConfigService.ProfessionSystem;
                List<EquipmentBaseV2> familiarEquipment = GetFamiliarEquipment(steamId, famKey);

                foreach (var equipment in familiarEquipment)
                {
                    if (equipment.Equipment.Equals(0)) continue;

                    PrefabGUID equipmentPrefabGuid = new(equipment.Equipment);
                    int professionLevel = professions ? equipment.Quality : 0;

                    AddItemResponse addItemResponse = InventoryUtilitiesServer.TryAddItem(Core.GetAddItemSettings(), servant, equipmentPrefabGuid, 1); // inventory or servant entity?
                    Entity equipmentEntity = addItemResponse.NewEntity;
                    int durability = equipment.Durability;

                    if (equipmentEntity.Exists()) EquipmentQualityManager.ApplyFamiliarEquipmentStats(professionLevel, durability, equipmentEntity);
                    // Core.Log.LogWarning($"[EquipFamiliar] Equipment Entity exists: {equipmentEntity.Exists()}, name: {equipmentPrefabGuid.GetPrefabName()}, durability: {durability}");
                }
            }
            public static List<EquipmentBaseV2> UnequipFamiliar(Entity playerCharacter)
            {
                // servant.Remove<Disabled>();
                Entity familiarServant = GetFamiliarServant(playerCharacter);

                List<EquipmentBaseV2> familiarEquipment = [];
                bool professions = ConfigService.ProfessionSystem;

                if (familiarServant.TryGetComponent(out ServantEquipment servantEquipment))
                {
                    foreach (FamiliarEquipmentType familiarEquipmentType in Enum.GetValues<FamiliarEquipmentType>())
                    {
                        if (FamiliarEquipmentMap.TryGetValue(familiarEquipmentType, out EquipmentType equipmentType) && servantEquipment.IsEquipped(equipmentType))
                        {
                            Entity equipmentEntity = servantEquipment.GetEquipmentEntity(equipmentType).GetEntityOnServer();
                            PrefabGUID equipmentPrefabGuid = servantEquipment.GetEquipmentItemId(equipmentType);
                            int professionLevel = professions ? EquipmentQualityManager.CalculateProfessionLevelOfEquipmentFromMaxDurability(equipmentEntity) : 0;

                            if (!equipmentEntity.IsAncestralWeapon())
                            {
                                familiarEquipment.Add(
                                    new StandardEquipmentV2
                                    {
                                        Equipment = equipmentPrefabGuid.GuidHash,
                                        Quality = professionLevel,
                                        Durability = (int)equipmentEntity.GetDurability()
                                    });
                            }
                        }
                    }

                    // servant.Destroy();
                    DestroyFamiliarServant(familiarServant);
                    return familiarEquipment;
                }

                return [..Enumerable.Range(0, EQUIPMENT_SLOTS).Select(_ => (EquipmentBaseV2)new StandardEquipmentV2 { Equipment = 0, Quality = 0, Durability = 0 })];
            }
            public static List<EquipmentBaseV2> GetFamiliarEquipment(Entity servant)
            {
                List<EquipmentBaseV2> familiarEquipment = [];
                bool professions = ConfigService.ProfessionSystem;

                if (servant.TryGetComponent(out ServantEquipment servantEquipment))
                {
                    foreach (FamiliarEquipmentType familiarEquipmentType in Enum.GetValues(typeof(FamiliarEquipmentType)))
                    {
                        if (FamiliarEquipmentMap.TryGetValue(familiarEquipmentType, out EquipmentType equipmentType) &&
                            servantEquipment.IsEquipped(equipmentType))
                        {
                            Entity equipmentEntity = servantEquipment.GetEquipmentEntity(equipmentType).GetEntityOnServer();
                            PrefabGUID equipmentPrefabGuid = servantEquipment.GetEquipmentItemId(equipmentType);
                            int professionLevel = professions
                                ? EquipmentQualityManager.CalculateProfessionLevelOfEquipmentFromMaxDurability(equipmentEntity)
                                : 0;

                            if (!equipmentEntity.IsAncestralWeapon())
                            {
                                familiarEquipment.Add(new StandardEquipmentV2
                                {
                                    Equipment = equipmentPrefabGuid.GuidHash,
                                    Quality = professionLevel,
                                    Durability = (int)equipmentEntity.GetDurability()
                                });
                            }
                        }
                        else
                        {
                            familiarEquipment.Add(new StandardEquipmentV2
                            {
                                Equipment = 0,
                                Quality = 0,
                                Durability = 0
                            });
                        }
                    }
                }
                else
                {
                    familiarEquipment.AddRange(Enumerable.Range(0, EQUIPMENT_SLOTS).Select(_ =>
                        (EquipmentBaseV2)new StandardEquipmentV2 { Equipment = 0, Quality = 0, Durability = 0 }));
                }

                return familiarEquipment;
            }

            /*
public static FamiliarEquipmentDataV2 LoadFamiliarEquipment(ulong steamId)
{
    string path = GetFilePath(steamId);
    FamiliarEquipmentDataV2 dataV2;
    FamiliarEquipmentData data;

    var prefabEntities = PrefabCollectionSystem._PrefabGuidToEntityMap;

    try
    {
        dataV2 = File.Exists(path)
             ? JsonSerializer.Deserialize<FamiliarEquipmentDataV2>(
                   File.ReadAllText(path), _jsonOptionsV2)
               ?? new()
             : new();

        foreach (var kvp in dataV2.FamiliarEquipment)
        {
            List<EquipmentBaseV2> equipment = kvp.Value;

            foreach (var item in equipment)
            {
                if (item is StandardEquipmentV2 standardEquipment)
                {
                    int itemGuidHash = standardEquipment.Equipment;
                    PrefabGUID itemPrefabGuid = new(itemGuidHash);

                    if (_shardNecklaces.Contains(itemPrefabGuid))
                    {
                        itemGuidHash = PrefabGUIDs.Item_MagicSource_BloodKey_T01.GuidHash;
                    }
                    else if (itemPrefabGuid.HasValue() && prefabEntities.TryGetValue(itemPrefabGuid, out Entity itemEntity))
                    {
                        WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(itemEntity);
                        PrefabGUID sanguineWeapon = _sanguineWeapons[weaponType];
                        itemGuidHash = sanguineWeapon.GuidHash;
                    }

                    equipment.Add(new StandardEquipmentV2
                    {
                        Equipment = itemGuidHash,
                        Quality = standardEquipment.Quality,
                        Durability = 0
                    });
                }
                else if (item is AncestralWeaponV2 ancestralWeapon)
                {
                    PrefabGUID equipmentPrefabGuid = new(ancestralWeapon.Equipment);

                    if (equipmentPrefabGuid.HasValue() && prefabEntities.TryGetValue(equipmentPrefabGuid, out Entity itemEntity))
                    {
                        WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(itemEntity);
                        PrefabGUID sanguineWeapon = _sanguineWeapons[weaponType];

                        equipment.Add(new StandardEquipmentV2
                        {
                            Equipment = sanguineWeapon.GuidHash,
                            Quality = ancestralWeapon.Quality,
                            Durability = 0
                        });
                    }
                }
            }

            dataV2.FamiliarEquipment[kvp.Key] = equipment;
        }
    }
    catch
    {
        // Core.Log.LogWarning($"[LoadFamiliarEquipment] V1 -> V2 ({steamId}) – {primaryEx.Message}");

        dataV2 = new();
        List<EquipmentBaseV2> equipmentV2 = [..Enumerable.Range(0, EQUIPMENT_SLOTS).Select(_ => (EquipmentBaseV2)new StandardEquipmentV2 { Equipment = 0, Quality = 0, Durability = 0 })];

        try
        {
            data = File.Exists(path)
                 ? JsonSerializer.Deserialize<FamiliarEquipmentData>(
                       File.ReadAllText(path), _jsonOptions)
                   ?? new()
                 : new();

            foreach (var kvp in data.FamiliarEquipment)
            {
                List<EquipmentBase> equipment = kvp.Value;

                foreach (var item in equipment)
                {
                    if (item is StandardEquipment standardEquipment)
                    {
                        int itemGuidHash = standardEquipment.Equipment;
                        PrefabGUID itemPrefabGuid = new(itemGuidHash);

                        if (_shardNecklaces.Contains(itemPrefabGuid))
                        {
                            itemGuidHash = PrefabGUIDs.Item_MagicSource_BloodKey_T01.GuidHash;
                        }
                        else if (itemPrefabGuid.HasValue() && prefabEntities.TryGetValue(itemPrefabGuid, out Entity itemEntity))
                        {
                            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(itemEntity);
                            PrefabGUID sanguineWeapon = _sanguineWeapons[weaponType];
                            itemGuidHash = sanguineWeapon.GuidHash;
                        }

                        equipmentV2.Add(new StandardEquipmentV2
                        {
                            Equipment = itemGuidHash,
                            Quality = standardEquipment.Quality,
                            Durability = 0
                        });
                    }
                    else if (item is AncestralWeapon ancestralWeapon)
                    {
                        PrefabGUID equipmentPrefabGuid = new(ancestralWeapon.Equipment);

                        if (equipmentPrefabGuid.HasValue() && prefabEntities.TryGetValue(equipmentPrefabGuid, out Entity itemEntity))
                        {
                            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(itemEntity);
                            PrefabGUID sanguineWeapon = _sanguineWeapons[weaponType];

                            equipmentV2.Add(new StandardEquipmentV2
                            {
                                Equipment = sanguineWeapon.GuidHash,
                                Quality = ancestralWeapon.Quality,
                                Durability = 0
                            });
                        }
                    }
                }

                dataV2.FamiliarEquipment[kvp.Key] = equipmentV2;
            }
        }
        catch (Exception secondaryEx)
        {
            Core.Log.LogWarning($"[LoadFamiliarEquipment] ({steamId}) – {secondaryEx.Message}");
            dataV2 = new();
        }
    }

    SaveFamiliarEquipmentData(steamId, dataV2);

    return dataV2;
}
*/
        }
    }
    public static class FamiliarEquipment
    {
        [Serializable]
        public abstract class EquipmentBase
        {
            public int Equipment { get; set; }
            public int Quality { get; set; }
        }

        [Serializable]
        public abstract class EquipmentBaseV2
        {
            public int Equipment { get; set; }
            public int Quality { get; set; }
            public int Durability { get; set; }
        }

        [Serializable]
        public class StandardEquipment : EquipmentBase { }

        [Serializable]
        public class StandardEquipmentV2 : EquipmentBaseV2 { }

        [Serializable]
        public class AncestralWeapon : EquipmentBase
        {
            public int Tier { get; set; }
            public SpellSchool Infusion { get; set; }
            public StatMods[] StatMods { get; set; }
        }

        [Serializable]
        public class AncestralWeaponV2 : EquipmentBaseV2
        {
            public int Tier { get; set; }
            public SpellSchool Infusion { get; set; }
            public StatMods[] StatMods { get; set; }
        }

        [Serializable]
        public class StatMods
        {
            public int StatMod { get; set; }
            public float Value { get; set; }
        }

        [Serializable]
        public class FamiliarEquipmentData
        {
            public Dictionary<int, List<EquipmentBase>> FamiliarEquipment { get; set; } = [];
        }

        [Serializable]
        public class FamiliarEquipmentDataV2
        {
            public Dictionary<int, List<EquipmentBaseV2>> FamiliarEquipment { get; set; } = [];
        }
        public class EquipmentBaseConverter : JsonConverter<EquipmentBase>
        {
            public override EquipmentBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("Equipment", out _))
                    throw new JsonException("Invalid Equipment data!");

                try
                {
                    return root.TryGetProperty("Infusion", out _)
                        ? JsonSerializer.Deserialize<AncestralWeapon>(root.GetRawText(), options)
                        : JsonSerializer.Deserialize<StandardEquipment>(root.GetRawText(), options);
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"[EquipmentBaseConverter] Failed to deserialize familiar equipment - {ex.Message}");
                    return new StandardEquipment { Equipment = 0, Quality = 0 };
                }
            }
            public override void Write(Utf8JsonWriter writer, EquipmentBase value, JsonSerializerOptions options)
            {
                if (value is AncestralWeapon ancestralWeapon)
                    JsonSerializer.Serialize(writer, ancestralWeapon, options);
                else if (value is StandardEquipment standardEquipment)
                    JsonSerializer.Serialize(writer, standardEquipment, options);
            }
        }
        public class EquipmentBaseV2Converter : JsonConverter<EquipmentBaseV2>
        {
            public override EquipmentBaseV2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement root = doc.RootElement;

                if (!root.TryGetProperty("Equipment", out _))
                    throw new JsonException("Invalid Equipment data!");

                try
                {
                    return root.TryGetProperty("Infusion", out _)
                        ? JsonSerializer.Deserialize<AncestralWeaponV2>(root.GetRawText(), options)
                        : JsonSerializer.Deserialize<StandardEquipmentV2>(root.GetRawText(), options);
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"[EquipmentBaseConverter] Failed to deserialize familiar equipment - {ex.Message}");
                    return new StandardEquipmentV2 { Equipment = 0, Quality = 0, Durability = 0 };
                }
            }
            public override void Write(Utf8JsonWriter writer, EquipmentBaseV2 value, JsonSerializerOptions options)
            {
                if (value is AncestralWeaponV2 ancestralWeapon)
                    JsonSerializer.Serialize(writer, ancestralWeapon, options);
                else if (value is StandardEquipmentV2 standardEquipment)
                    JsonSerializer.Serialize(writer, standardEquipment, options);
            }
        }
    }
    public static class PlayerDataInitialization
    {
        public static void LoadPlayerData()
        {
            if (ConfigService.ClassSystem)
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

            if (ConfigService.LegacySystem)
            {
                foreach (var loadFunction in _loadLegacies)
                {
                    loadFunction();
                }
            }

            if (ConfigService.ProfessionSystem)
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
            LoadPlayerExoFormData,
            LoadIgnoredPrestigeLeaderboard,
            LoadIgnoredSharedExperience
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
            LoadPlayerTwinBladesExpertise,
            LoadPlayerDaggersExpertise,
            LoadPlayerClawsExpertise,
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
            LoadPlayerDraculinLegacy,
            LoadPlayerImmortalLegacy,
            LoadPlayerCreatureLegacy,
            LoadPlayerBruteLegacy,
            LoadPlayerCorruptionLegacy,
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
            LoadFamiliarBattleCoords
        ];
    }
}

/*
public static class FamiliarEquipmentManager
{
    static readonly PrefabGUID _bonusStatsBuff = new(737485591);

    static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new EquipmentBaseConverter() }
    };
    public class EquipmentBaseConverter : JsonConverter<EquipmentBase>
    {
        public override EquipmentBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            if (!root.TryGetProperty("Equipment", out _))
                throw new JsonException("Invalid Equipment data!");

            try
            {
                return root.TryGetProperty("Infusion", out _) // If "Infusion" exists, it's AncestralWeapon
                    ? JsonSerializer.Deserialize<AncestralWeapon>(root.GetRawText(), options)
                    : JsonSerializer.Deserialize<StandardEquipment>(root.GetRawText(), options);
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"[EquipmentBaseConverter] Failed to deserialize familiar equipment - {ex.Message}");
                return new StandardEquipment { Equipment = 0, Quality = 0 }; // Return a default item instead of failing
            }
        }
        public override void Write(Utf8JsonWriter writer, EquipmentBase value, JsonSerializerOptions options)
        {
            if (value is AncestralWeapon ancestralWeapon)
                JsonSerializer.Serialize(writer, ancestralWeapon, options);
            else if (value is StandardEquipment standardEquipment)
                JsonSerializer.Serialize(writer, standardEquipment, options);
        }
    }

    [Serializable]
    public abstract class EquipmentBase
    {
        public int Equipment { get; set; }
        public int Quality { get; set; }
    }

    [Serializable]
    public class StandardEquipment : EquipmentBase
    {

    }

    [Serializable]
    public class StatMod
    {
        public UnitStatType UnitStatType { get; set; }
        public float Value { get; set; }
    }

    [Serializable]
    public class AncestralWeapon : EquipmentBase
    {
        public SpellSchool Infusion { get; set; }
        public StatMod[] StatMods { get; set; }
    }

    [Serializable]
    public class FamiliarEquipmentData
    {
        public Dictionary<int, List<EquipmentBase>> FamiliarEquipment { get; set; } = [];
    }

    static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[10], $"{steamId}_familiar_equipment.json");
    public static void SaveFamiliarEquipmentData(ulong steamId, FamiliarEquipmentData data)
    {
        string filePath = GetFilePath(steamId);
        try
        {
            string jsonString = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(filePath, jsonString);
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[SaveFamiliarEquipmentData] Failed to save familiar equipment ({steamId}) - {ex.Message}");
        }
    }
    public static FamiliarEquipmentData LoadFamiliarEquipment(ulong steamId)
    {
        string filePath = GetFilePath(steamId);
        try
        {
            string jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FamiliarEquipmentData>(jsonString, _jsonOptions) ?? new FamiliarEquipmentData();
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[LoadFamiliarEquipment] Failed to load familiar equipment ({steamId}) - {ex.Message}");
            return new FamiliarEquipmentData(); // Return an empty set instead of failing
        }
    }
    public static List<EquipmentBase> GetFamiliarEquipment(ulong steamId, int famKey)
    {
        FamiliarEquipmentData equipmentData = LoadFamiliarEquipment(steamId);

        if (!equipmentData.FamiliarEquipment.TryGetValue(famKey, out var familiarEquipment))
        {
            StandardEquipment equipment = new()
            {
                Equipment = 0,
                Quality = 0
            };

            familiarEquipment = [equipment, equipment, equipment, equipment, equipment, equipment, equipment];
            equipmentData.FamiliarEquipment[famKey] = familiarEquipment;

            SaveFamiliarEquipmentData(steamId, equipmentData);
        }

        return familiarEquipment;
    }
    public static void SaveFamiliarEquipment(ulong steamId, int famKey, List<EquipmentBase> familiarEquipment)
    {
        FamiliarEquipmentData equipmentData = LoadFamiliarEquipment(steamId);
        equipmentData.FamiliarEquipment[famKey] = familiarEquipment;

        SaveFamiliarEquipmentData(steamId, equipmentData);
    }
    public static void EquipFamiliar(ulong steamId, int famKey, Entity servant, Entity familiar)
    {
        EntityManager EntityManager = Core.EntityManager;
        bool professions = ConfigService.ProfessionSystem;

        List<EquipmentBase> familiarEquipment = GetFamiliarEquipment(steamId, famKey);

        Entity inventory = InventoryUtilities.TryGetInventoryEntity(EntityManager, servant, out inventory) ? inventory : Entity.Null;
        if (!inventory.Exists()) return;

        for (int i = 0; i < familiarEquipment.Count; i++)
        {
            PrefabGUID equipmentPrefabGuid = new(familiarEquipment[i].Equipment);
            int professionLevel = professions ? familiarEquipment[i].Quality : 0;

            if (equipmentPrefabGuid.HasValue())
            {
                AddItemResponse addItemResponse = InventoryUtilitiesServer.TryAddItem(Core.GetAddItemSettings(), inventory, equipmentPrefabGuid, 1);

                if (professionLevel > 0)
                {
                    EquipmentManager.ApplyEquipmentStats(professionLevel, addItemResponse.NewEntity);
                }
            }
        }

        familiar.TryApplyBuff(_bonusStatsBuff);
    }
    public static List<EquipmentBase> UnequipFamiliar(Entity servant)
    {
        List<EquipmentBase> familiarEquipment = [];

        servant.TryRemove<Disabled>();
        bool professions = ConfigService.ProfessionSystem;

        if (servant.TryGet(out ServantEquipment servantEquipment))
        {
            foreach (FamiliarEquipmentType familiarEquipmentType in Enum.GetValues(typeof(FamiliarEquipmentType)))
            {
                if (Enum.TryParse(familiarEquipmentType.ToString(), true, out EquipmentType equipmentType) && servantEquipment.IsEquipped(equipmentType))
                {
                    PrefabGUID equipmentPrefabGuid = servantEquipment.GetEquipmentItemId(equipmentType);
                    int professionLevel = professions ? EquipmentManager.CalculateProfessionLevelOfEquipmentFromMaxDurability(
                        servantEquipment.GetEquipmentEntity(equipmentType).GetEntityOnServer()) : 0;

                    familiarEquipment.Add(new StandardEquipment { Equipment = equipmentPrefabGuid.GuidHash, Quality = professionLevel});
                }
                else
                {
                    familiarEquipment.Add(new StandardEquipment { Equipment = 0, Quality = 0 });
                }
            }

            servant.Destroy();
            return familiarEquipment;
        }

        familiarEquipment = [..Enumerable.Range(0, 7).Select(_ => (EquipmentBase)new StandardEquipment { Equipment = 0, Quality = 0 })];
        return familiarEquipment;
    }
}
*/

/* need to rethink some of these since primaryAttackSpeed is garbage for most if not unused on units entirely and depends on how equipment stats pan out
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