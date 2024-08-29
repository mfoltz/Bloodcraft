﻿using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Entities;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Utilities;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Services;
internal static class EclipseService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly Regex regex = new(@"^\[(\d+)\]:");

    public static HashSet<ulong> RegisteredUsers = [];
    public enum NetworkEventSubType
    {
        RegisterUser,
        ProgressToClient,
        ConfigsToClient // need to send bonus stat base values and prestige multipliers for stats as well as class stat synergies, need another method for this
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
            SendClientConfig(playerInfo.User);
            SendClientProgress(playerInfo.CharEntity, steamId);
        }
    }
    public static void SendClientConfig(User user)
    {
        string message = BuildConfigMessage();
        string messageWithMAC = $"{message};mac{ChatMessageSystemPatch.GenerateMAC(message)}";

        LocalizationService.HandleServerReply(EntityManager, user, messageWithMAC);
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
    static (int Percent, int Level, int Prestige, int Class) GetExperienceData(ulong SteamID)
    {
        int experiencePercent = 0;
        int experienceLevel = 0;
        int experiencePrestige = 0;
        int classEnum = 0;

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

        if (Classes && HasClass(SteamID))
        {
            classEnum = (int)GetPlayerClass(SteamID) + 1; // 0 for no class on client
        }

        return (experiencePercent, experienceLevel, experiencePrestige, classEnum);
    }
    static (int Percent, int Level, int Prestige, int Enum, int BonusStats) GetLegacyData(Entity character, ulong SteamID) // add bonus stats as enums for one 
    {
        int legacyPercent = 0;
        int legacyLevel = 0;
        int legacyPrestige = 0;
        int legacyEnum = 0;
        int bonusStats = 0;

        if (ConfigService.BloodSystem)
        {
            BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(character.Read<Blood>().BloodType);
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

            if (bloodHandler != null)
            {
                legacyPercent = BloodSystem.GetLevelProgress(SteamID, bloodHandler);
                legacyLevel = BloodSystem.GetLevel(SteamID, bloodHandler);
                legacyEnum = (int)bloodType;

                if (ConfigService.PrestigeSystem)
                {
                    IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(BloodSystem.BloodPrestigeMap[bloodType]);
                    legacyPrestige = prestigeHandler.GetPrestigeLevel(SteamID);
                }

                if (SteamID.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
                {
                    // need to get a 00 value for each stat and add them together but not literally, just concatenating
                    if (stats.Count != 0) bonusStats = int.Parse(string.Join("", stats.Select(stat => ((int)stat + 1).ToString("D2"))));
                }
            }
        }

        return (legacyPercent, legacyLevel, legacyPrestige, legacyEnum, bonusStats);
    }
    static (int Percent, int Level, int Prestige, int Enum, int BonusStats) GetExpertiseData(Entity character, ulong SteamID)
    {
        int expertisePercent = 0;
        int expertiseLevel = 0;
        int expertisePrestige = 0;
        int expertiseEnum = 0;
        int bonusStats = 0;

        if (ConfigService.ExpertiseSystem)
        {
            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
            IExpertiseHandler expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler != null)
            {
                expertisePercent = WeaponSystem.GetLevelProgress(SteamID, expertiseHandler);
                expertiseLevel = WeaponSystem.GetLevel(SteamID, expertiseHandler);
                expertiseEnum = (int)weaponType;

                if (ConfigService.PrestigeSystem)
                {
                    IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(WeaponSystem.WeaponPrestigeMap[weaponType]);
                    expertisePrestige = prestigeHandler.GetPrestigeLevel(SteamID);
                }

                if (SteamID.TryGetPlayerWeaponStats(out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
                {
                    // need to get a 00 value for each stat and add them together but not literally, just concatenating
                    if (stats.Count != 0) bonusStats = int.Parse(string.Join("", stats.Select(stat => ((int)stat + 1).ToString("D2"))));
                }
            }
        }

        return (expertisePercent, expertiseLevel, expertisePrestige, expertiseEnum, bonusStats);
    }
    static (int Progress, int Goal, string Target) GetQuestData(ulong SteamID, Systems.Quests.QuestSystem.QuestType questType)
    {
        int progress = 0;
        int goal = 0;
        string target = "";

        if (ConfigService.QuestSystem && SteamID.TryGetPlayerQuests(out var questData))
        {
            if (questData.TryGetValue(questType, out var quest) && !quest.Objective.Complete)
            {
                progress = quest.Progress;
                goal = quest.Objective.RequiredAmount;
                target = quest.Objective.Target.GetPrefabName();
            }
        }

        return (progress, goal, target);
    }
    static string BuildProgressMessage((int Percent, int Level, int Prestige, int Class) experienceData, //want to send bonuses as well
        (int Percent, int Level, int Prestige, int Enum, int BonusStats) legacyData,
        (int Percent, int Level, int Prestige, int Enum, int BonusStats) expertiseData,
        (int Progress, int Goal, string Target) dailyQuestData,
        (int Progress, int Goal, string Target) weeklyQuestData)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat("{0:D2},{1:D2},{2:D2},{3},", experienceData.Percent, experienceData.Level, experienceData.Prestige, experienceData.Class)
            .AppendFormat("{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", legacyData.Percent, legacyData.Level, legacyData.Prestige, legacyData.Enum, legacyData.BonusStats)
            .AppendFormat("{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", expertiseData.Percent, expertiseData.Level, expertiseData.Prestige, expertiseData.Enum, expertiseData.BonusStats)
            .AppendFormat("{0:D2},{1:D2},{2},", dailyQuestData.Progress, dailyQuestData.Goal, dailyQuestData.Target)
            .AppendFormat("{0:D2},{1:D2},{2}", weeklyQuestData.Progress, weeklyQuestData.Goal, weeklyQuestData.Target);

        return sb.ToString();
    }
    static string BuildConfigMessage()
    {
        // need prestige stat multipliers, class stat synergies, and bonus stat base values
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();
        
        float prestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
        float statSynergyMultiplier = ConfigService.StatSynergyMultiplier;

        var sb = new StringBuilder();
        sb.AppendFormat("[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat("{0:F2},{1:F2},", prestigeStatMultiplier, statSynergyMultiplier); // Add multipliers to the message

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
            sb.AppendFormat("{0:D2},", (int)playerClass + 1);

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
}