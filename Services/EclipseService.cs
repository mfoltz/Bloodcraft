using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Entities;
using static Bloodcraft.Systems.Leveling.PrestigeSystem;

namespace Bloodcraft.Services;
internal static class EclipseService
{
    static EntityManager EntityManager => Core.EntityManager;
    static PlayerService PlayerService => Core.PlayerService;
    static LocalizationService LocalizationService => Core.LocalizationService;

    static readonly Regex regex = new(@"^\[(\d+)\]:");

    public static HashSet<ulong> RegisteredUsers = [];
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient
    }
    public static void HandleClientMessage(string message)
    {
        int eventType = int.Parse(regex.Match(message).Groups[1].Value);
        switch (eventType)
        {
            case (int)NetworkEventSubType.RegisterUser:
                ulong steamId = ulong.Parse(regex.Replace(message, ""));
                RegisterUser(steamId);
                break;
        }
    }
    static void RegisterUser(ulong steamId)
    {
        Dictionary<string, Entity> UserCache = new(PlayerService.UserCache);

        Entity userEntity = UserCache
            .Values
            .FirstOrDefault(userEntity => userEntity.Read<User>().PlatformId == steamId);

        if (userEntity.Exists())
        {
            Core.Log.LogInfo($"User {steamId} registered for Eclipse updates...");
            RegisteredUsers.Add(steamId);
            SendClientProgress(userEntity.Read<User>().LocalCharacter._Entity, steamId);
        }
    }
    public static void SendClientProgress(Entity character, ulong SteamID)
    {
        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        var experienceData = GetExperienceData(SteamID);
        var legacyData = GetLegacyData(character, SteamID);
        var expertiseData = GetExpertiseData(character, SteamID);
        var dailyQuestData = GetQuestData(SteamID, Systems.Quests.QuestSystem.QuestType.Daily);
        var weeklyQuestData = GetQuestData(SteamID, Systems.Quests.QuestSystem.QuestType.Weekly);

        string message = BuildProgressMessage(experienceData, legacyData, expertiseData, dailyQuestData, weeklyQuestData);
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(EntityManager, user, messageWithMAC);
    }
    static (int Percent, int Level, int Prestige) GetExperienceData(ulong SteamID)
    {
        int experiencePercent = 0;
        int experienceLevel = 0;
        int experiencePrestige = 0;

        if (ConfigService.LevelingSystem)
        {
            experiencePercent = LevelingSystem.GetLevelProgress(SteamID);
            experienceLevel = LevelingSystem.GetLevel(SteamID);

            if (ConfigService.PrestigeSystem)
            {
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(PrestigeType.Experience);
                experiencePrestige = prestigeHandler.GetPrestigeLevel(SteamID);
            }
        }

        return (experiencePercent, experienceLevel, experiencePrestige);
    }
    static (int Percent, int Level, int Prestige, int Enum) GetLegacyData(Entity character, ulong SteamID)
    {
        int legacyPercent = 0;
        int legacyLevel = 0;
        int legacyPrestige = 0;
        int legacyEnum = 0;

        if (ConfigService.BloodSystem)
        {
            BloodSystem.BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.Read<Blood>().BloodType);
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyPercent = BloodSystem.GetLevelProgress(SteamID, bloodHandler);
                legacyLevel = BloodSystem.GetLevel(SteamID, bloodHandler);
                legacyEnum = (int)bloodType;
            }

            if (ConfigService.PrestigeSystem)
            {
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(BloodSystem.BloodPrestigeMap[bloodType]);
                legacyPrestige = prestigeHandler.GetPrestigeLevel(SteamID);
            }
        }

        return (legacyPercent, legacyLevel, legacyPrestige, legacyEnum);
    }
    static (int Percent, int Level, int Prestige, int Enum) GetExpertiseData(Entity character, ulong SteamID)
    {
        int expertisePercent = 0;
        int expertiseLevel = 0;
        int expertisePrestige = 0;
        int expertiseEnum = 0;

        if (ConfigService.ExpertiseSystem)
        {
            WeaponSystem.WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
            IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler != null)
            {
                expertisePercent = WeaponSystem.GetLevelProgress(SteamID, expertiseHandler);
                expertiseLevel = WeaponSystem.GetLevel(SteamID, expertiseHandler);
                expertiseEnum = (int)weaponType;
            }

            if (ConfigService.PrestigeSystem)
            {
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(WeaponSystem.WeaponPrestigeMap[weaponType]);
                expertisePrestige = prestigeHandler.GetPrestigeLevel(SteamID);
            }
        }

        return (expertisePercent, expertiseLevel, expertisePrestige, expertiseEnum);
    }
    static (int Progress, int Goal, int Target) GetQuestData(ulong SteamID, Systems.Quests.QuestSystem.QuestType questType)
    {
        int progress = 0;
        int goal = 0;
        int target = 0;

        if (ConfigService.QuestSystem && Core.DataStructures.PlayerQuests.TryGetValue(SteamID, out var questData))
        {
            if (questData.TryGetValue(questType, out var quest) && !quest.Objective.Complete)
            {
                progress = quest.Progress;
                goal = quest.Objective.RequiredAmount;
                target = quest.Objective.Target.GuidHash;
            }
        }

        return (progress, goal, target);
    }
    static string BuildProgressMessage((int Percent, int Level, int Prestige) experienceData,
        (int Percent, int Level, int Prestige, int Enum) legacyData,
        (int Percent, int Level, int Prestige, int Enum) expertiseData,
        (int Progress, int Goal, int Target) dailyQuestData,
        (int Progress, int Goal, int Target) weeklyQuestData)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat("{0:D2},{1:D2},{2:D2},", experienceData.Percent, experienceData.Level, experienceData.Prestige)
            .AppendFormat("{0:D2},{1:D2},{2:D2},{3:D2},", legacyData.Percent, legacyData.Level, legacyData.Prestige, legacyData.Enum)
            .AppendFormat("{0:D2},{1:D2},{2:D2},{3:D2},", expertiseData.Percent, expertiseData.Level, expertiseData.Prestige, expertiseData.Enum)
            .AppendFormat("{0:D2},{1:D2},{2},", dailyQuestData.Progress, dailyQuestData.Goal, dailyQuestData.Target)
            .AppendFormat("{0:D2},{1:D2},{2}", weeklyQuestData.Progress, weeklyQuestData.Goal, weeklyQuestData.Target);

        return sb.ToString();
    }
    /*
    public static void SendClientProgress(Entity character, ulong SteamID)
    {
        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
        User user = userEntity.Read<User>();

        int experiencePercent = 0;
        int experienceLevel = 0;
        int experiencePrestige = 0;

        int legacyPercent = 0;
        int legacyLevel = 0;
        int legacyPrestige = 0;
        int legacyEnum = 0;

        int expertisePercent = 0;
        int expertiseLevel = 0;
        int expertisePrestige = 0;
        int expertiseEnum = 0;

        int dailyProgress = 0;
        int dailyGoal = 0;
        int dailyTarget = 0;

        int weeklyProgress = 0;
        int weeklyGoal = 0;
        int weeklyTarget = 0;

        if (ConfigService.LevelingSystem)
        {
            experiencePercent = LevelingSystem.GetLevelProgress(SteamID);
            experienceLevel = LevelingSystem.GetLevel(SteamID);

            if (ConfigService.PrestigeSystem)
            {
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(PrestigeType.Experience);
                experiencePrestige = prestigeHandler.GetPrestigeLevel(SteamID);
            }
        }

        if (ConfigService.BloodSystem)
        {
            BloodSystem.BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.Read<Blood>().BloodType);
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyPercent = BloodSystem.GetLevelProgress(SteamID, bloodHandler);
                legacyLevel = BloodSystem.GetLevel(SteamID, bloodHandler);
                legacyEnum = (int)bloodType;
            }

            if (ConfigService.PrestigeSystem)
            {
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(BloodSystem.BloodPrestigeMap[bloodType]);
                legacyPrestige = prestigeHandler.GetPrestigeLevel(SteamID);
            }
        }

        if (ConfigService.ExpertiseSystem)
        {
            WeaponSystem.WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
            IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler != null)
            {
                expertisePercent = WeaponSystem.GetLevelProgress(SteamID, expertiseHandler);
                expertiseLevel = WeaponSystem.GetLevel(SteamID, expertiseHandler);
                expertiseEnum = (int)weaponType;
            }

            if (ConfigService.PrestigeSystem)
            {
                IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(WeaponSystem.WeaponPrestigeMap[weaponType]);
                expertisePrestige = prestigeHandler.GetPrestigeLevel(SteamID);
            }
        }

        if (ConfigService.QuestSystem && Core.DataStructures.PlayerQuests.TryGetValue(SteamID, out var questData))
        {
            if (questData.TryGetValue(Systems.Quests.QuestSystem.QuestType.Daily, out var dailyQuest) && !dailyQuest.Objective.Complete)
            {
                dailyProgress = dailyQuest.Progress;
                dailyGoal = dailyQuest.Objective.RequiredAmount;
                dailyTarget = dailyQuest.Objective.Target.GuidHash;
            }

            if (questData.TryGetValue(Systems.Quests.QuestSystem.QuestType.Weekly, out var weeklyQuest) && !weeklyQuest.Objective.Complete)
            {
                weeklyProgress = weeklyQuest.Progress;
                weeklyGoal = weeklyQuest.Objective.RequiredAmount;
                weeklyTarget = weeklyQuest.Objective.Target.GuidHash;
            }
        }

        string message = $"[{(int)NetworkEventSubType.ProgressToClient}]:{experiencePercent:D2},{experienceLevel:D2},{experiencePrestige:D2},{legacyPercent:D2},{legacyLevel:D2},{legacyPrestige:D2},{legacyEnum:D2},{expertisePercent:D2},{expertiseLevel:D2},{expertisePrestige:D2},{expertiseEnum:D2},{dailyProgress:D2},{dailyGoal:D2},{dailyTarget},{weeklyProgress:D2},{weeklyGoal:D2},{weeklyTarget}";
        LocalizationService.HandleServerReply(EntityManager, user, message);
    }
    */
}
