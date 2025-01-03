using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Professions;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Services;
internal class EclipseService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPrestige = ConfigService.FamiliarPrestige;
    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _quests = ConfigService.QuestSystem;

    static readonly WaitForSeconds _delay = new(2.5f);
    static readonly WaitForSeconds _newUserDelay = new(15f);

    const int MAX_RETRIES = 20;
    const string LEGACY_VERSION = "1.1.2"; // Default version for old messages

    //static readonly Regex oldRegex = new(@"^\[(\d+)\]:");
    static readonly Regex _oldRegex = new(@"^\[(\d+)\]:(\d+)$");

    //static readonly Regex regex = new(@"^\[(\d+)\]:(\d+\.\d+\.\d+);(\d+)$");
    static readonly Regex _regex = new(@"^\[ECLIPSE\]\[(\d+)\]:(\d+\.\d+\.\d+);(\d+)$");

    public static readonly ConcurrentDictionary<ulong, string> RegisteredUsersAndClientVersions = [];
    public EclipseService()
    {
        Core.StartCoroutine(ClientUpdateLoop());
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
            // Extract the event type
            int eventType = int.Parse(match.Groups[1].Value);

            // Extract the payload (modVersion and stringId)
            //string payload = newMatch.Groups["payload"].Value;

            string modVersion = match.Groups[2].Value;

            if (!ulong.TryParse(match.Groups[3].Value, out ulong steamId))
            {
                Core.Log.LogWarning("Invalid steamId in new (>=1.2.2) format message!");
                return;
            }

            switch (eventType)
            {
                case (int)NetworkEventSubType.RegisterUser:
                    RegisterUser(steamId, modVersion);
                    break;
                default:
                    Core.Log.LogError($"Unknown networkEventSubtype in Eclipse message: {eventType}");
                    break;
            }

            return;
        }

        // If new regex didn't match, try the old regex format
        Match oldMatch = _oldRegex.Match(message);

        if (oldMatch.Success)
        {
            // Extract the event type
            //int eventType = int.Parse(oldMatch.Groups[1].Value);

            int eventType = int.Parse(oldMatch.Groups[1].Value);

            if (!ulong.TryParse(oldMatch.Groups[2].Value, out ulong steamId))
            {
                Core.Log.LogWarning("Invalid steamId in legacy (<1.2.2) Eclipse message!");
                return;
            }

            switch (eventType)
            {
                case (int)NetworkEventSubType.RegisterUser:
                    RegisterUser(steamId, LEGACY_VERSION);
                    break;
                default:
                    Core.Log.LogError($"Unknown networkEventSubtype encountered while handling legacy version (<1.2.2) of Eclipse! {eventType}");
                    break;
            }

            return;
        }

        // If neither regex matches, log an error
        Core.Log.LogWarning("Failed to parse client registration message from Eclipse!");
    }
    static void RegisterUser(ulong steamId, string version)
    {
        if (RegisteredUsersAndClientVersions.ContainsKey(steamId)) return;
        //else if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists() && playerInfo.User.IsConnected)
        else if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
        {
            if (HandleRegistration(playerInfo, steamId, version))
            {
                Core.Log.LogInfo($"{steamId}:Eclipse{version} registered for Eclipse updates from PlayerCache | (RegisterUser)");
            }
        }
        else // delayed registration, wait for cache to update/player to make character
        {
            Core.StartCoroutine(DelayedRegistration(steamId, version));
        }
    }
    static bool HandleRegistration(PlayerInfo playerInfo, ulong steamId, string version)
    {
        if (RegisteredUsersAndClientVersions.TryAdd(steamId, version))
        {
            try
            {
                switch (version)
                {
                    case "1.1.2":
                        // Handle version 1.1.2
                        IVersionHandler<ProgressDataV1_1_2> versionHandlerV1_1_2 = VersionHandler.GetHandler<ProgressDataV1_1_2>(version);

                        versionHandlerV1_1_2?.SendClientConfig(playerInfo.User);
                        versionHandlerV1_1_2?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

                        return true;
                    case "1.2.2":
                        // Handle version 1.2.2
                        IVersionHandler<ProgressDataV1_2_2> versionHandlerV1_2_2 = VersionHandler.GetHandler<ProgressDataV1_2_2>(version);

                        versionHandlerV1_2_2?.SendClientConfig(playerInfo.User);
                        versionHandlerV1_2_2?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

                        return true;
                    default:
                        // Handle unsupported versions or fallback
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
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(PrestigeType.Experience);
                experiencePrestige = prestigeHandler.GetPrestigeLevel(steamId);
            }
        }

        if (_classes && Utilities.Classes.HasClass(steamId))
        {
            classEnum = (int)Utilities.Classes.GetPlayerClass(steamId) + 1; // 0 for no class on client
        }

        return (experiencePercent, experienceLevel, experiencePrestige, classEnum);
    }
    public static (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) GetLegacyData(Entity character, ulong steamId) // add bonus stats as enums for one 
    {
        int legacyPercent = 0;
        int legacyLevel = 0;
        int legacyPrestige = 0;
        int legacyEnum = 0;
        int legacyBonusStats = 0;

        if (_legacies)
        {
            BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.ReadRO<Blood>().BloodType);
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyPercent = BloodSystem.GetLevelProgress(steamId, bloodHandler);
                legacyLevel = BloodSystem.GetLevel(steamId, bloodHandler);
                legacyEnum = (int)bloodType;

                if (_prestige)
                {
                    IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(BloodSystem.BloodTypeToPrestigeMap[bloodType]);
                    legacyPrestige = prestigeHandler.GetPrestigeLevel(steamId);
                }

                if (steamId.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
                {
                    var bonusStats = stats.Take(3).Select(stat => ((int)stat + 1).ToString("D2"));

                    if (bonusStats.Any())
                    {
                        legacyBonusStats = int.Parse(string.Join("", bonusStats));
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
            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(character.ReadRO<Equipment>().WeaponSlot.SlotEntity._Entity);
            IWeaponHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler != null)
            {
                expertisePercent = WeaponSystem.GetLevelProgress(steamId, expertiseHandler);
                expertiseLevel = WeaponSystem.GetLevel(steamId, expertiseHandler);
                expertiseEnum = (int)weaponType;

                if (_prestige)
                {
                    IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(WeaponSystem.WeaponPrestigeMap[weaponType]);
                    expertisePrestige = prestigeHandler.GetPrestigeLevel(steamId);
                }

                if (steamId.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
                {
                    var bonusStats = stats.Take(3).Select(stat => ((int)stat + 1).ToString("D2"));

                    if (bonusStats.Any())
                    {
                        expertiseBonusStats = int.Parse(string.Join("", bonusStats));
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
            Entity familiar = Utilities.Familiars.FindPlayerFamiliar(character);

            if (!familiar.Exists())
            {
                return (familiarPercent, familiarLevel, familiarPrestige, familiarName, familiarStats);
            }

            PrefabGUID familiarPrefabGUID = familiar.GetPrefabGuid();

            int familiarId = familiarPrefabGUID.GuidHash;
            familiarName = familiarPrefabGUID.GetLocalizedName();

            KeyValuePair<int, float> familiarXP = FamiliarLevelingSystem.GetFamiliarExperience(steamId, familiarId);

            familiarPercent = FamiliarLevelingSystem.GetLevelProgress(steamId, familiarId);
            familiarLevel = familiarXP.Key;

            if (_familiarPrestige)
            {
                familiarPrestige = FamiliarPrestigeManager.GetFamiliarPrestigeLevel(FamiliarPrestigeManager.LoadFamiliarPrestige(steamId), familiarId);
            }

            UnitStats unitStats = familiar.ReadRO<UnitStats>();
            Health health = familiar.ReadRO<Health>();

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
            int FishingProgress, int FishingLevel) GetProfessionData(ulong steamId)
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
            IProfessionHandler professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "enchanting");
            enchantingLevel = professionHandler.GetProfessionData(steamId).Key;
            enchantingProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

            professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "alchemy");
            alchemyLevel = professionHandler.GetProfessionData(steamId).Key;
            alchemyProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

            professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "harvesting");
            harvestingLevel = professionHandler.GetProfessionData(steamId).Key;
            harvestingProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

            professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "blacksmithing");
            blacksmithingLevel = professionHandler.GetProfessionData(steamId).Key;
            blacksmithingProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

            professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "tailoring");
            tailoringLevel = professionHandler.GetProfessionData(steamId).Key;
            tailoringProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

            professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "woodcutting");
            woodcuttingLevel = professionHandler.GetProfessionData(steamId).Key;
            woodcuttingProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

            professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "mining");
            miningLevel = professionHandler.GetProfessionData(steamId).Key;
            miningProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

            professionHandler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID.Empty, "fishing");
            fishingLevel = professionHandler.GetProfessionData(steamId).Key;
            fishingProgress = ProfessionSystem.GetLevelProgress(steamId, professionHandler);

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
        int type = 0; // kill by default
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
    static IEnumerator DelayedRegistration(ulong steamId, string version)
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
    static IEnumerator ClientUpdateLoop()
    {
        while (true)
        {
            if (RegisteredUsersAndClientVersions.IsEmpty)
            {
                yield return _delay;

                continue;
            }

            HashSet<PlayerInfo> playerInfos = [.. OnlineCache.Values];
            Dictionary<ulong, string> registeredUsers = new(RegisteredUsersAndClientVersions);

            foreach (PlayerInfo playerInfo in playerInfos)
            {
                ulong steamId = playerInfo.User.PlatformId;

                if (registeredUsers.TryGetValue(steamId, out string version))
                {
                    try
                    {
                        switch (version)
                        {
                            case "1.1.2":
                                // Handle version 1.1.1
                                IVersionHandler<ProgressDataV1_1_2> versionHandlerV1_1_2 = VersionHandler.GetHandler<ProgressDataV1_1_2>(version);
                                versionHandlerV1_1_2?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

                                break;
                            case "1.2.2":
                                // Handle version 1.2.1
                                IVersionHandler<ProgressDataV1_2_2> versionHandlerV1_2_2 = VersionHandler.GetHandler<ProgressDataV1_2_2>(version);
                                versionHandlerV1_2_2.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

                                break;
                            default:
                                // Handle unsupported versions or fallback
                                Core.Log.LogWarning($"Unsupported client version in EclipseService! {steamId}:Eclipse{version}, unregistering user to avoid console spam...");

                                if (RegisteredUsersAndClientVersions.ContainsKey(steamId))
                                {
                                    RegisteredUsersAndClientVersions.TryRemove(steamId, out var _);
                                }

                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Log.LogWarning($"Failed sending progress in EclipseService! {steamId}:Eclipse{version}, Error - {ex}");
                    }

                    yield return null;
                }
            }

            yield return _delay;
        }
    }
}
