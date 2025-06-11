using Bloodcraft.Interfaces;
using Bloodcraft.Patches;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Leveling.ClassManager;
using WeaponType = Bloodcraft.Interfaces.WeaponType;

namespace Bloodcraft.Services;
internal class EclipseService
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _shiftSpell = ConfigService.ShiftSlot;
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPrestige = ConfigService.FamiliarPrestige;
    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly WaitForSeconds _delay = new(2.5f);
    static readonly WaitForSeconds _newUserDelay = new(15f);

    const int MAX_RETRIES = 20;
    const string V1_3 = "1.3";

    static readonly Regex _oldRegex = new(@"^\[(\d+)\]:(\d+)$");
    static readonly Regex _regex = new(@"^\[ECLIPSE\]\[(\d+)\]:(\d+\.\d+\.\d+);(\d+)$");

    const string VERSION_1_3 = "1.3";
    public static IReadOnlyDictionary<ulong, string> PendingRegistration => _pendingRegistration;
    static readonly ConcurrentDictionary<ulong, string> _pendingRegistration = [];
    public static IReadOnlyDictionary<ulong, string> RegisteredUsersAndClientVersions => _registeredUsersAndClientVersions;
    static readonly ConcurrentDictionary<ulong, string> _registeredUsersAndClientVersions = [];
    public EclipseService()
    {
        EclipseServiceRoutine().Start();
    }
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient,
        ConfigsToClient
    }
    public static void HandleClientMessage(string message)
    {
        Match match = _regex.Match(message);

        if (match.Success)
        {
            int eventType = int.Parse(match.Groups[1].Value);
            string modVersion = match.Groups[2].Value;

            if (!ulong.TryParse(match.Groups[3].Value, out ulong steamId))
            {
                Core.Log.LogWarning($"Couldn't parse steamId for Eclipse[{V1_3}]!");
                return;
            }

            switch (eventType)
            {
                case (int)NetworkEventSubType.RegisterUser:
                    // Core.Log.LogWarning($"[EclipseService.HandleClientMessage] {steamId}:Eclipse{modVersion} ({DateTime.Now})");
                    RegisterUser(steamId, modVersion);
                    break;
                default:
                    Core.Log.LogError($"Unknown networkEventSubtype in Eclipse message: {eventType}");
                    break;
            }

            return;
        }

        Core.Log.LogWarning("Failed to parse client registration message from Eclipse!");
    }
    static void RegisterUser(ulong steamId, string version)
    {
        if (RegisteredUsersAndClientVersions.ContainsKey(steamId)) return;
        else if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
        {
            if (HandleRegistration(playerInfo, steamId, version))
            {
                // Core.Log.LogInfo($"{steamId}:Eclipse{version} registered!");
            }
        }

        /*
        else
        {
            // DelayedRegistrationRoutine(steamId, version).Start();
            _pendingRegistration.TryAdd(steamId, version);
            Core.Log.LogInfo($"{steamId}:Eclipse{version} pending registration...");
        }
        */
    }
    public static void HandlePreRegistration(ulong steamId)
    {
        _pendingRegistration.TryAdd(steamId, V1_3);
    }
    public static void TryRemovePreRegistration(ulong steamId)
    {
        _pendingRegistration.TryRemove(steamId, out var _);
    }
    public static bool HandleRegistration(PlayerInfo playerInfo, ulong steamId, string version)
    {
        // Core.Log.LogWarning($"[EclipseService.HandleRegistration] {steamId}:Eclipse{version}");
        if (_registeredUsersAndClientVersions.TryAdd(steamId, version))
        {
            try
            {
                switch (version)
                {
                    default:
                        if (IsVersion1_3(version))
                        {
                            // Core.Log.LogWarning($"[EclipseService.HandleRegistration] - {version}");
                            IVersionHandler<ProgressDataV1_3> versionHandler13X = VersionHandler.GetHandler<ProgressDataV1_3>(VERSION_1_3);
                            versionHandler13X?.SendClientConfig(playerInfo.User);
                            versionHandler13X?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);
                            _pendingRegistration.TryRemove(steamId, out var _);
                            return true;
                        }

                        Core.Log.LogWarning($"Unsupported client version! {steamId}:Eclipse{version}");
                        return false;
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Error sending config/progress in HandleRegistration! {steamId}:Eclipse{version}, Error - {e}");
                return false;
            }
        }
        else
        {
            Core.Log.LogWarning($"Failed to add {steamId}:Eclipse{version} to RegisteredUsersAndClientVersions dictionary!");
            return false;
        }
    }
    static IEnumerator DelayedRegistrationRoutine(ulong steamId, string version)
    {
        int tries = 0;

        while (tries <= MAX_RETRIES)
        {
            yield return _newUserDelay;

            if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists() && playerInfo.User.IsConnected)
            {
                if (HandleRegistration(playerInfo, steamId, version))
                {
                    Core.Log.LogInfo($"{steamId}:Eclipse{version} registered for Eclipse updates from PlayerCache | (DelayedRegistration)");
                }

                yield break;
            }
            else
            {
                tries++;
            }
        }
    }
    static IEnumerator EclipseServiceRoutine()
    {
        while (true)
        {
            if (!RegisteredUsersAndClientVersions.Any())
            {
                yield return _delay;

                continue;
            }

            foreach (PlayerInfo playerInfo in SteamIdOnlinePlayerInfoCache.Values)
            {
                ulong steamId = playerInfo.User.PlatformId;

                if (RegisteredUsersAndClientVersions.TryGetValue(steamId, out string version))
                {
                    try
                    {
                        switch (version)
                        {
                            /*
                            case V1_1_2:
                                IVersionHandler<ProgressDataV1_1_2> versionHandlerV1_1_2 = VersionHandler.GetHandler<ProgressDataV1_1_2>(version);
                                versionHandlerV1_1_2?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);
                                break;
                            case V1_2_2:
                                IVersionHandler<ProgressDataV1_2_2> versionHandlerV1_2_2 = VersionHandler.GetHandler<ProgressDataV1_2_2>(version);
                                versionHandlerV1_2_2.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);
                                break;
                            case V1_3_2:
                                IVersionHandler<ProgressDataV1_3> versionHandlerV1_3_2 = VersionHandler.GetHandler<ProgressDataV1_3>(version);
                                versionHandlerV1_3_2?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);
                                break;
                            */
                            default:
                                if (IsVersion1_3(version))
                                {
                                    if ((_legacies || _expertise || _classes) && !playerInfo.CharEntity.HasBuff(Buffs.BonusStatsBuff)) playerInfo.CharEntity.TryApplyBuff(Buffs.BonusStatsBuff);
                                    IVersionHandler<ProgressDataV1_3> versionHandler13X = VersionHandler.GetHandler<ProgressDataV1_3>(V1_3);
                                    versionHandler13X?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);
                                    break;
                                }
                                else
                                {
                                    Core.Log.LogWarning($"Unsupported client version in EclipseService! {steamId}:Eclipse{version}, unregistering user to avoid console spam...");
                                    TryUnregisterUser(steamId);
                                    break;
                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Log.LogWarning($"Failed sending progress in EclipseService! {steamId}:Eclipse{version}, Error - {ex}");
                    }
                }

                yield return null;
            }

            yield return _delay;
        }
    }
    static bool IsVersion1_3(string version) => version.StartsWith("1.3");
    public static void TryUnregisterUser(ulong steamId)
    {
        _registeredUsersAndClientVersions.TryRemove(steamId, out var _);
    }
    public static (int Percent, int Level, int Prestige, int Class) GetExperienceData(ulong steamId)
    {
        int experiencePercent = 0;
        int experienceLevel = 0;
        int experiencePrestige = 0;
        int classEnum = 0;

        if (_leveling)
        {
            experiencePercent = LevelingSystem.GetLevelProgress(steamId);
            experienceLevel = LevelingSystem.GetLevel(steamId);

            if (_prestige)
            {
                IPrestige prestigeHandler = PrestigeFactory.GetPrestige(PrestigeType.Experience);
                experiencePrestige = prestigeHandler.GetPrestigeLevel(steamId);
            }
        }

        if (_classes && steamId.HasClass(out PlayerClass? playerClass) && playerClass.HasValue)
        {
            classEnum = (int)playerClass.Value + 1;
        }

        return (experiencePercent, experienceLevel, experiencePrestige, classEnum);
    }
    public static (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) GetLegacyData(Entity character, ulong steamId)
    {
        int legacyPercent = 0;
        int legacyLevel = 0;
        int legacyPrestige = 0;
        int legacyEnum = 0;
        int legacyBonusStats = 0;

        if (_legacies)
        {
            BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.Read<Blood>().BloodType);
            IBloodLegacy bloodHandler = BloodLegacyFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyPercent = BloodSystem.GetLevelProgress(steamId, bloodHandler);
                legacyLevel = BloodSystem.GetLevel(steamId, bloodHandler);
                legacyEnum = (int)bloodType;

                if (_prestige)
                {
                    IPrestige prestigeHandler = PrestigeFactory.GetPrestige(BloodSystem.BloodPrestigeTypes[bloodType]);
                    legacyPrestige = prestigeHandler.GetPrestigeLevel(steamId);
                }

                if (steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
                {
                    var bonusStats = stats.Take(3).Select(stat => ((int)stat + 1).ToString("D2"));

                    if (bonusStats.Any())
                    {
                        legacyBonusStats = int.Parse(string.Join("", bonusStats), CultureInfo.InvariantCulture);
                    }
                }
            }
            else if (bloodType.Equals(BloodType.None))
            {
                legacyEnum = (int)bloodType;
            }
        }

        return (legacyPercent, legacyLevel, legacyPrestige, legacyEnum, legacyBonusStats);
    }
    public static (int Percent, int Level, int Prestige, int Enum, int ExpertiseBonusStats) GetExpertiseData(Entity character, ulong steamId)
    {
        int expertisePercent = 0;
        int expertiseLevel = 0;
        int expertisePrestige = 0;
        int expertiseEnum = 0;
        int expertiseBonusStats = 0;

        if (_expertise)
        {
            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
            IWeaponExpertise expertiseHandler = WeaponExpertiseFactory.GetExpertise(weaponType);

            if (expertiseHandler != null)
            {
                expertisePercent = WeaponSystem.GetLevelProgress(steamId, expertiseHandler);
                expertiseLevel = WeaponSystem.GetLevel(steamId, expertiseHandler);
                expertiseEnum = (int)weaponType;

                if (_prestige)
                {
                    IPrestige prestigeHandler = PrestigeFactory.GetPrestige(WeaponSystem.WeaponPrestigeTypes[weaponType]);
                    expertisePrestige = prestigeHandler.GetPrestigeLevel(steamId);
                }

                if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
                {
                    var bonusStats = stats.Take(3).Select(stat => ((int)stat + 1).ToString("D2"));

                    if (bonusStats.Any())
                    {
                        expertiseBonusStats = int.Parse(string.Join("", bonusStats), CultureInfo.InvariantCulture);
                    }
                }
            }
        }

        return (expertisePercent, expertiseLevel, expertisePrestige, expertiseEnum, expertiseBonusStats);
    }
    public static (int Percent, int Level, int Prestige, string Name, string FamiliarStats) GetFamiliarData(Entity character, ulong steamId)
    {
        int familiarPercent = 0;
        int familiarLevel = 0;
        int familiarPrestige = 0;
        string familiarName = "";
        string familiarStats = "";

        if (_familiars)
        {
            Entity familiar = Familiars.GetActiveFamiliar(character);

            if (!familiar.Exists())
            {
                return (familiarPercent, familiarLevel, familiarPrestige, familiarName, familiarStats);
            }

            PrefabGUID familiarId = familiar.GetPrefabGuid();

            int famKey = familiarId.GuidHash;
            familiarName = familiarId.GetLocalizedName();

            KeyValuePair<int, float> familiarXP = FamiliarLevelingSystem.GetFamiliarExperience(steamId, famKey);

            familiarPercent = FamiliarLevelingSystem.GetLevelProgress(steamId, famKey);
            familiarLevel = familiarXP.Key;

            if (_familiarPrestige)
            {
                familiarPrestige = FamiliarPrestigeManager.LoadFamiliarPrestigeData(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData > 0 ? prestigeData : familiarPrestige;
            }

            UnitStats unitStats = familiar.Read<UnitStats>();
            Health health = familiar.Read<Health>();

            int maxHealth = (int)health.MaxHealth._Value;
            int physicalPower = (int)unitStats.PhysicalPower._Value;
            int spellPower = (int)unitStats.SpellPower._Value;

            familiarStats = string.Concat(maxHealth.ToString("D4"), physicalPower.ToString("D3"), spellPower.ToString("D3"));
        }

        return (familiarPercent, familiarLevel, familiarPrestige, familiarName, familiarStats);
    }
    public static (int EnchantingProgress, int EnchantingLevel,
        int AlchemyProgress, int AlchemyLevel,
        int HarvestingProgress, int HarvestingLevel,
        int BlacksmithingProgress, int BlacksmithingLevel,
        int TailoringProgress, int TailoringLevel,
        int WoodcuttingProgress, int WoodcuttingLevel,
        int MiningProgress, int MiningLevel,
        int FishingProgress, int FishingLevel)
        GetProfessionData(ulong steamId)
    {
        int enchantingProgress = 0;
        int enchantingLevel = 0;
        int alchemyProgress = 0;
        int alchemyLevel = 0;
        int harvestingProgress = 0;
        int harvestingLevel = 0;
        int blacksmithingProgress = 0;
        int blacksmithingLevel = 0;
        int tailoringProgress = 0;
        int tailoringLevel = 0;
        int woodcuttingProgress = 0;
        int woodcuttingLevel = 0;
        int miningProgress = 0;
        int miningLevel = 0;
        int fishingProgress = 0;
        int fishingLevel = 0;

        if (_professions)
        {
            IProfession profession = ProfessionFactory.GetProfession(ProfessionType.Enchanting);
            enchantingLevel = profession.GetProfessionData(steamId).Key;
            enchantingProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            profession = ProfessionFactory.GetProfession(ProfessionType.Alchemy);
            alchemyLevel = profession.GetProfessionData(steamId).Key;
            alchemyProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            profession = ProfessionFactory.GetProfession(ProfessionType.Harvesting);
            harvestingLevel = profession.GetProfessionData(steamId).Key;
            harvestingProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            profession = ProfessionFactory.GetProfession(ProfessionType.Blacksmithing);
            blacksmithingLevel = profession.GetProfessionData(steamId).Key;
            blacksmithingProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            profession = ProfessionFactory.GetProfession(ProfessionType.Tailoring);
            tailoringLevel = profession.GetProfessionData(steamId).Key;
            tailoringProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            profession = ProfessionFactory.GetProfession(ProfessionType.Woodcutting);
            woodcuttingLevel = profession.GetProfessionData(steamId).Key;
            woodcuttingProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            profession = ProfessionFactory.GetProfession(ProfessionType.Mining);
            miningLevel = profession.GetProfessionData(steamId).Key;
            miningProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            profession = ProfessionFactory.GetProfession(ProfessionType.Fishing);
            fishingLevel = profession.GetProfessionData(steamId).Key;
            fishingProgress = ProfessionSystem.GetLevelProgress(steamId, profession);

            return (enchantingProgress, enchantingLevel, alchemyProgress, alchemyLevel, harvestingProgress,
                harvestingLevel, blacksmithingProgress, blacksmithingLevel, tailoringProgress, tailoringLevel,
                woodcuttingProgress, woodcuttingLevel, miningProgress, miningLevel, fishingProgress, fishingLevel);
        }

        return (enchantingProgress, enchantingLevel, alchemyProgress, alchemyLevel, harvestingProgress,
            harvestingLevel, blacksmithingProgress, blacksmithingLevel, tailoringProgress, tailoringLevel,
            woodcuttingProgress, woodcuttingLevel, miningProgress, miningLevel, fishingProgress, fishingLevel);
    }
    public static (int Type, int Progress, int Goal, string Target, string IsVBlood) GetQuestData(ulong steamId, Systems.Quests.QuestSystem.QuestType questType)
    {
        int type = 0;
        int progress = 0;
        int goal = 0;
        string target = "";
        string isVBlood = "false";

        if (_quests && steamId.TryGetPlayerQuests(out var questData))
        {
            if (questData.TryGetValue(questType, out var quest) && !quest.Objective.Complete)
            {
                type = (int)quest.Objective.Goal;
                progress = quest.Progress;
                goal = quest.Objective.RequiredAmount;
                target = quest.Objective.Target.GetLocalizedName();
                if (type == 0 && PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(quest.Objective.Target)) isVBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[quest.Objective.Target].Has<VBloodConsumeSource>().ToString();
            }
        }

        return (type, progress, goal, target, isVBlood);
    }
    public static int GetShiftSpellData(Entity playerCharacter)
    {
        int index = -1;

        if (_classes && _shiftSpell)
        {
            Entity abilityGroup = ServerGameManager.GetAbilityGroup(playerCharacter, 3);

            if (abilityGroup.Exists() && !abilityGroup.Has<VBloodAbilityData>())
            {
                index = abilityGroup.TryGetComponent(out PrefabGUID prefabGuid) && AbilityRunScriptsSystemPatch.ClassSpells.TryGetValue(prefabGuid, out int spellIndex) ? spellIndex : index;
            }
        }

        return index;
    }
}
