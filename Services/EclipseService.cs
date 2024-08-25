using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Services.PlayerService;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Services;
internal static class EclipseService
{
    static EntityManager EntityManager => Core.EntityManager;

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
        if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.UserEntity.Exists())
        {
            Core.Log.LogInfo($"User {steamId} registered for Eclipse updates...");
            RegisteredUsers.Add(steamId);
            SendClientProgress(playerInfo.CharEntity, steamId);
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
            BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.Read<Blood>().BloodType);
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
            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
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

        if (ConfigService.QuestSystem && SteamID.TryGetPlayerQuests(out var questData))
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
    static string BuildProgressMessage((int Percent, int Level, int Prestige) experienceData, //want to send bonuses as well
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
}
