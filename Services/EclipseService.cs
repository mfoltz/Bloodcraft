using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Services.PlayerService;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Services;
internal class EclipseService
{
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    public static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    public static readonly bool Leveling = ConfigService.LevelingSystem;
    public static readonly bool Legacies = ConfigService.BloodSystem;
    public static readonly bool Expertise = ConfigService.ExpertiseSystem;
    public static readonly bool Prestige = ConfigService.PrestigeSystem;
    public static readonly bool Familiars = ConfigService.FamiliarSystem;
    public static readonly bool FamiliarPrestige = ConfigService.FamiliarPrestige;
    public static readonly bool Professions = ConfigService.ProfessionSystem;
    public static readonly bool Quests = ConfigService.QuestSystem;

    public static readonly int MaxLevel = ConfigService.MaxLevel;
    public static readonly int MaxLegacyLevel = ConfigService.MaxBloodLevel;
    public static readonly int MaxExpertiseLevel = ConfigService.MaxExpertiseLevel;
    public static readonly int MaxFamiliarLevel = ConfigService.MaxFamiliarLevel;
    public static readonly int MaxProfessionLevel = ConfigService.MaxProfessionLevel;

    public static readonly float PrestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
    public static readonly float ClassStatMultiplier = ConfigService.StatSynergyMultiplier;

    static readonly WaitForSeconds Delay = new(2.5f);
    static readonly WaitForSeconds NewUserDelay = new(15f);

    const int MAX_RETRIES = 20;
    //static readonly Regex regex = new(@"^\[(\d+)\]:");
    static readonly Regex regex = new(@"^\[(\d+)\]:(?<payload>.+)$");

    public static readonly Dictionary<ulong, string> RegisteredUsersAndClientVersions = [];
    public EclipseService()
    {
        Core.StartCoroutine(ClientUpdateLoop());
    }
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient,
        ConfigsToClient // need to send bonus stat base values and prestige multipliers for stats as well as class stat synergies, need another method for this
    }
    public static void HandleClientMessage(string message)
    {
        /*
        int eventType = int.Parse(regex.Match(message).Groups[1].Value);
        switch (eventType)
        {
            case (int)NetworkEventSubType.RegisterUser:
                ulong steamId = ulong.Parse(regex.Replace(message, ""));
                RegisterUser(steamId);
                break;
        }
        */

        Match match = regex.Match(message);
        if (!match.Success)
        {
            Core.Log.LogWarning("Invalid message in HandleClientMessage!");
            return;
        }

        // Extract the event type
        int eventType = int.Parse(match.Groups[1].Value);

        // Extract the payload (modVersion and stringId)
        string payload = match.Groups["payload"].Value;

        switch (eventType)
        {
            case (int)NetworkEventSubType.RegisterUser:
                // Parse modVersion and stringId from payload
                string[] parts = payload.Split(';');
                if (parts.Length != 2)
                {
                    Core.Log.LogWarning("Invalid payload in HandleClientMessage!");
                    return;
                }

                string modVersion = parts[0];
                if (!ulong.TryParse(parts[1], out ulong steamId))
                {
                    Core.Log.LogWarning("Invalid steamId in HandleClientMessage!");
                    return;
                }

                RegisterUser(steamId, modVersion);
                break;

            default:
                Core.Log.LogError($"Unknown networkEventSubtype: {eventType}");
                break;
        }
    }
    static void RegisterUser(ulong steamId, string version)
    {
        if (RegisteredUsersAndClientVersions.ContainsKey(steamId)) return;
        else if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
        {
            if (HandleRegistration(playerInfo, steamId, version))
            {
                Core.Log.LogInfo($"{steamId}:Eclipse{version} registered for Eclipse updates from PlayerCache~ (RegisterUser)");
            }

            //RegisteredUsersAndClientVersions.TryAdd(steamId, version);
            //SendClientConfigV1_2_1(playerInfo.User);
            //SendClientProgressV1_2_1(playerInfo.CharEntity, steamId);
        }
        else // delayed registration, wait for cache to update/player to make character...
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
                    case "1.1.1":
                        // Handle version 1.1.1
                        IVersionHandler<ProgressDataV1_1_1> versionHandlerV1_1_1 = VersionHandler.GetHandler<ProgressDataV1_1_1>(version);

                        versionHandlerV1_1_1?.SendClientConfig(playerInfo.User);
                        versionHandlerV1_1_1?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

                        return true;
                    case "1.2.1":
                        // Handle version 1.2.1
                        IVersionHandler<ProgressDataV1_2_1> versionHandlerV1_2_1 = VersionHandler.GetHandler<ProgressDataV1_2_1>(version);

                        versionHandlerV1_2_1?.SendClientConfig(playerInfo.User);
                        versionHandlerV1_2_1?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

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

    /*
    public static void SendClientConfigV1_1_1(User user)
    {
        string message = BuildConfigMessageV1_1_1();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(EntityManager, user, messageWithMAC);
    }
    public static void SendClientConfigV1_2_1(User user)
    {
        string message = BuildConfigMessageV1_2_1();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(EntityManager, user, messageWithMAC);
    }
    public static void SendClientProgressV1_1_1(Entity character, ulong steamId)
    {
        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        var experienceData = GetExperienceData(steamId);
        var legacyData = GetLegacyData(character, steamId);
        var expertiseData = GetExpertiseData(character, steamId);
        var dailyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Daily);
        var weeklyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Weekly);

        string message = BuildProgressMessageV1_1_1(experienceData, legacyData, expertiseData, dailyQuestData, weeklyQuestData);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(EntityManager, user, messageWithMAC);
    }
    public static void SendClientProgressV1_2_1(Entity character, ulong steamId)
    {
        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        var experienceData = GetExperienceData(steamId);
        var legacyData = GetLegacyData(character, steamId);
        var expertiseData = GetExpertiseData(character, steamId);
        var familiarData = GetFamiliarData(character, steamId);
        var professionData = GetProfessionData(steamId);
        var dailyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Daily);
        var weeklyQuestData = GetQuestData(steamId, Systems.Quests.QuestSystem.QuestType.Weekly);

        string message = BuildProgressMessageV1_2_1(experienceData, legacyData, expertiseData, familiarData, professionData, dailyQuestData, weeklyQuestData);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(EntityManager, user, messageWithMAC);
    }
    */
    public static (int Percent, int Level, int Prestige, int Class) GetExperienceData(ulong steamId)
    {
        int experiencePercent = 0;
        int experienceLevel = 0;
        int experiencePrestige = 0;
        int classEnum = 0;

        if (Leveling)
        {
            experiencePercent = LevelingSystem.GetLevelProgress(steamId);
            experienceLevel = LevelingSystem.GetLevel(steamId);

            if (Prestige)
            {
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(PrestigeType.Experience);
                experiencePrestige = prestigeHandler.GetPrestigeLevel(steamId);
            }
        }

        if (Classes && ClassUtilities.HasClass(steamId))
        {
            classEnum = (int)ClassUtilities.GetPlayerClass(steamId) + 1; // 0 for no class on client
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

        if (Legacies)
        {
            BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.Read<Blood>().BloodType);
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyPercent = BloodSystem.GetLevelProgress(steamId, bloodHandler);
                legacyLevel = BloodSystem.GetLevel(steamId, bloodHandler);
                legacyEnum = (int)bloodType;

                if (Prestige)
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

        if (Expertise)
        {
            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
            IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler != null)
            {
                expertisePercent = WeaponSystem.GetLevelProgress(steamId, expertiseHandler);
                expertiseLevel = WeaponSystem.GetLevel(steamId, expertiseHandler);
                expertiseEnum = (int)weaponType;

                if (Prestige)
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

        if (Familiars)
        {
            Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);

            if (!familiar.Exists())
            {
                return (familiarPercent, familiarLevel, familiarPrestige, familiarName, familiarStats);
            }

            PrefabGUID familiarPrefabGUID = familiar.GetPrefabGUID();

            int familiarId = familiarPrefabGUID.GuidHash;
            familiarName = familiarPrefabGUID.GetPrefabName();

            KeyValuePair<int, float> familiarXP = FamiliarLevelingSystem.GetFamiliarExperience(steamId, familiarId);

            familiarPercent = FamiliarLevelingSystem.GetLevelProgress(steamId, familiarId);
            familiarLevel = familiarXP.Key;

            if (FamiliarPrestige)
            {
                familiarPrestige = FamiliarPrestigeManager.GetFamiliarPrestigeLevel(FamiliarPrestigeManager.LoadFamiliarPrestige(steamId), familiarId);
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

        if (Professions)
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

        if (Quests && steamId.TryGetPlayerQuests(out var questData))
        {
            if (questData.TryGetValue(questType, out var quest) && !quest.Objective.Complete)
            {
                type = (int)quest.Objective.Goal;
                progress = quest.Progress;
                goal = quest.Objective.RequiredAmount;
                target = quest.Objective.Target.GetPrefabName();
                if (type == 0 && PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(quest.Objective.Target)) isVBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[quest.Objective.Target].Has<VBloodConsumeSource>().ToString();
            }
        }

        return (type, progress, goal, target, isVBlood);
    }

    /*
    static string BuildProgressMessageV1_1_1((int Percent, int Level, int Prestige, int Class) experienceData, //want to send bonuses as well
    (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) legacyData,
    (int Percent, int Level, int Prestige, int Enum, int ExpertiseBonusStats) expertiseData,
    (int Type, int Progress, int Goal, string Target, string IsVBlood) dailyQuestData,
    (int Type, int Progress, int Goal, string Target, string IsVBlood) weeklyQuestData)
    {
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},", experienceData.Percent, experienceData.Level, experienceData.Prestige, experienceData.Class)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", legacyData.Percent, legacyData.Level, legacyData.Prestige, legacyData.Enum, legacyData.LegacyBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", expertiseData.Percent, expertiseData.Level, expertiseData.Prestige, expertiseData.Enum, expertiseData.ExpertiseBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", dailyQuestData.Type, dailyQuestData.Progress, dailyQuestData.Goal, dailyQuestData.Target, dailyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4}", weeklyQuestData.Type, weeklyQuestData.Progress, weeklyQuestData.Goal, weeklyQuestData.Target, weeklyQuestData.IsVBlood);

        return sb.ToString();
    }
    static string BuildProgressMessageV1_2_1((int Percent, int Level, int Prestige, int Class) experienceData, //want to send bonuses as well
        (int Percent, int Level, int Prestige, int Enum, int LegacyBonusStats) legacyData,
        (int Percent, int Level, int Prestige, int Enum, int ExpertiseBonusStats) expertiseData,
        (int Percent, int Level, int Prestige, string Name, string FamiliarStats) familiarData,
        (int EnchantingProgress, int EnchantingLevel, int AlchemyProgress, int AlchemyLevel, 
        int HarvestingProgress, int HarvestingLevel, int BlacksmithingProgress, int BlacksmithingLevel, 
        int TailoringProgress, int TailoringLevel, int WoodcuttingProgress, int WoodcuttingLevel, 
        int MiningProgress, int MiningLevel, int FishingProgress, int FishingLevel) professionData,
        (int Type, int Progress, int Goal, string Target, string IsVBlood) dailyQuestData,
        (int Type, int Progress, int Goal, string Target, string IsVBlood) weeklyQuestData)
    {
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},", experienceData.Percent, experienceData.Level, experienceData.Prestige, experienceData.Class)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", legacyData.Percent, legacyData.Level, legacyData.Prestige, legacyData.Enum, legacyData.LegacyBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", expertiseData.Percent, expertiseData.Level, expertiseData.Prestige, expertiseData.Enum, expertiseData.ExpertiseBonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},{4},", familiarData.Percent, familiarData.Level, familiarData.Prestige, familiarData.Name, familiarData.FamiliarStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D2},{5:D2},{6:D2},{7:D2},{8:D2},{9:D2},{10:D2},{11:D2},{12:D2},{13:D2},{14:D2},{15:D2},", professionData.EnchantingProgress, professionData.EnchantingLevel, professionData.AlchemyProgress, professionData.AlchemyLevel, 
            professionData.HarvestingProgress, professionData.HarvestingLevel, professionData.BlacksmithingProgress, professionData.BlacksmithingLevel, professionData.TailoringProgress, professionData.TailoringLevel, 
            professionData.WoodcuttingProgress, professionData.WoodcuttingLevel, professionData.MiningProgress, professionData.MiningLevel, professionData.FishingProgress, professionData.FishingLevel)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", dailyQuestData.Type, dailyQuestData.Progress, dailyQuestData.Goal, dailyQuestData.Target, dailyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4}", weeklyQuestData.Type, weeklyQuestData.Progress, weeklyQuestData.Goal, weeklyQuestData.Target, weeklyQuestData.IsVBlood);

        return sb.ToString();
    }
    static string BuildConfigMessageV1_1_1()
    {
        // need prestige stat multipliers, class stat synergies, and bonus stat base values
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();

        float prestigeStatMultiplier = PrestigeStatMultiplier;
        float statSynergyMultiplier = ClassStatMultiplier;

        int maxPlayerLevel = MaxLevel;
        int maxLegacyLevel = MaxLegacyLevel;
        int maxExpertiseLevel = MaxExpertiseLevel;
        int maxFamiliarLevel = MaxFamiliarLevel;
        int maxProfessionLevel = MaxProfessionLevel;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},{5},{6},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel, maxExpertiseLevel, maxFamiliarLevel, maxProfessionLevel); // Add multipliers to the message

        sb.Append(string.Join(",", weaponStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        // Append blood stat values as comma-separated string versions of their original values
        sb.Append(string.Join(",", bloodStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        // Iterate over each class and its synergies
        foreach (var classEntry in LevelingSystem.ClassWeaponBloodEnumMap)
        {
            var playerClass = classEntry.Key;
            var (weaponSynergies, bloodSynergies) = classEntry.Value;

            // Append class enum as an integer
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D2},", (int)playerClass + 1);

            // Append weapon synergies as a concatenated string of integers
            sb.Append(string.Join("", weaponSynergies.Select(s => (s + 1).ToString("D2"))));

            // Add a separator between weapon and blood synergies
            sb.Append(',');

            // Append blood synergies as a concatenated string of integers
            sb.Append(string.Join("", bloodSynergies.Select(s => (s + 1).ToString("D2"))));

            // Add a separator if there are more classes to handle
            sb.Append(',');
        }

        // Remove the last unnecessary separator
        if (sb[^1] == ',')
            sb.Length--;

        return sb.ToString();
    }
    static string BuildConfigMessageV1_2_1()
    {
        // need prestige stat multipliers, class stat synergies, and bonus stat base values
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();

        float prestigeStatMultiplier = PrestigeStatMultiplier;
        float statSynergyMultiplier = ClassStatMultiplier;

        int maxPlayerLevel = MaxLevel;
        int maxLegacyLevel = MaxLegacyLevel;
        int maxExpertiseLevel = MaxExpertiseLevel;
        int maxFamiliarLevel = MaxFamiliarLevel;
        int maxProfessionLevel = MaxProfessionLevel;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},{5},{6},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel, maxExpertiseLevel, maxFamiliarLevel, maxProfessionLevel); // Add multipliers to the message

        sb.Append(string.Join(",", weaponStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        // Append blood stat values as comma-separated string versions of their original values
        sb.Append(string.Join(",", bloodStatValues.Select(val => val.ToString("F2"))))
            .Append(',');

        // Iterate over each class and its synergies
        foreach (var classEntry in LevelingSystem.ClassWeaponBloodEnumMap)
        {
            var playerClass = classEntry.Key;
            var (weaponSynergies, bloodSynergies) = classEntry.Value;

            // Append class enum as an integer
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D2},", (int)playerClass + 1);

            // Append weapon synergies as a concatenated string of integers
            sb.Append(string.Join("", weaponSynergies.Select(s => (s + 1).ToString("D2"))));

            // Add a separator between weapon and blood synergies
            sb.Append(',');

            // Append blood synergies as a concatenated string of integers
            sb.Append(string.Join("", bloodSynergies.Select(s => (s + 1).ToString("D2"))));

            // Add a separator if there are more classes to handle
            sb.Append(',');
        }

        // Remove the last unnecessary separator
        if (sb[^1] == ',')
            sb.Length--;

        return sb.ToString();
    }
    */
    static IEnumerator DelayedRegistration(ulong steamId, string version)
    {
        int tries = 0;

        while (tries <= MAX_RETRIES)
        {
            yield return NewUserDelay;

            if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
            {
                if (HandleRegistration(playerInfo, steamId, version))
                {
                    Core.Log.LogInfo($"{steamId}:Eclipse{version} registered for Eclipse updates from PlayerCache~ (DelayedRegistration)");
                }

                //RegisteredUsersAndClientVersions.TryAdd(steamId, version);
                //SendClientConfigV1_2_1(playerInfo.User);
                //SendClientProgressV1_2_1(playerInfo.CharEntity, steamId);

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
            if (RegisteredUsersAndClientVersions.Count == 0)
            {
                yield return Delay; // Wait 30 seconds if no players
                continue;
            }

            Dictionary<string, PlayerInfo> players = new(OnlineCache); // Shallow copy of the player cache to make sure updates to that don't interfere with loop
            Dictionary<ulong, string> users = new(RegisteredUsersAndClientVersions);

            foreach (ulong steamId in users.Keys)
            {
                if (players.TryGetValue(steamId.ToString(), out PlayerInfo playerInfo))
                {
                    string version = users[steamId];

                    /*
                    try
                    {
                        //Core.Log.LogInfo("Sending client progress (OnlineCache)...");
                        SendClientProgressV1_2_1(playerInfo.CharEntity, playerInfo.User.PlatformId);
                    }
                    catch (Exception e)
                    {
                        Core.Log.LogError($"Error sending Eclipse progress to {playerInfo.User.PlatformId}: {e}");
                    }
                    */

                    try
                    {
                        switch (version)
                        {
                            case "1.1.1":
                                // Handle version 1.1.1
                                IVersionHandler<ProgressDataV1_1_1> versionHandlerV1_1_1 = VersionHandler.GetHandler<ProgressDataV1_1_1>(version);
                                versionHandlerV1_1_1?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

                                break;
                            case "1.2.1":
                                // Handle version 1.2.1
                                IVersionHandler<ProgressDataV1_2_1> versionHandlerV1_2_1 = VersionHandler.GetHandler<ProgressDataV1_2_1>(version);
                                versionHandlerV1_2_1?.SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);

                                break;
                            default:
                                // Handle unsupported versions or fallback
                                Core.Log.LogWarning($"Unsupported client version in EclipseService! {steamId}:Eclipse{version}");

                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Core.Log.LogError($"Failed sending progress in EclipseService! {steamId}:Eclipse{version}, Error - {e}");
                    }
                }

                yield return null;
            }

            yield return Delay;
        }
    }
}
