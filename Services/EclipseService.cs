using Bloodcraft.Patches;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Expertise.WeaponManager.WeaponStats;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Services;
internal class EclipseService
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly WaitForSeconds Delay = new(2.5f);
    static readonly WaitForSeconds NewUserDelay = new(15f);

    const int Attempts = 20;
    static readonly Regex regex = new(@"^\[(\d+)\]:");

    public readonly static HashSet<ulong> RegisteredUsers = [];
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
        if (RegisteredUsers.Contains(steamId)) return;
        else if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
        {
            Core.Log.LogInfo($"User {steamId} registered for Eclipse updates from PlayerCache...");
            RegisteredUsers.Add(steamId);
            SendClientConfig(playerInfo.User);
            SendClientProgress(playerInfo.CharEntity, steamId);
        }
        else // delayed registration, wait for cache to update/player to make character...
        {
            Core.StartCoroutine(DelayedRegistration(steamId));
        }
    }
    static IEnumerator DelayedRegistration(ulong steamId)
    {
        int tries = 0;

        while (tries <= Attempts)
        {
            yield return NewUserDelay;

            if (steamId.TryGetPlayerInfo(out PlayerInfo playerInfo) && playerInfo.CharEntity.Exists())
            {
                Core.Log.LogInfo($"User {steamId} registered for Eclipse updates from PlayerCache...");
                RegisteredUsers.Add(steamId);
                SendClientConfig(playerInfo.User);
                SendClientProgress(playerInfo.CharEntity, steamId);
                yield break;
            }
            else
            {
                tries++;
            }
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

        if (Classes && ClassUtilities.HasClass(SteamID))
        {
            classEnum = (int)ClassUtilities.GetPlayerClass(SteamID) + 1; // 0 for no class on client
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
                    IPrestigeHandler prestigeHandler = PrestigeHandlerFactory.GetPrestigeHandler(BloodSystem.BloodTypeToPrestigeMap[bloodType]);
                    legacyPrestige = prestigeHandler.GetPrestigeLevel(SteamID);
                }

                if (SteamID.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
                {
                    //if (stats.Count != 0) bonusStats = int.Parse(string.Join("", stats.Select(stat => ((int)stat + 1).ToString("D2"))));
                    var limitedStats = stats.Take(3).Select(stat => ((int)stat + 1).ToString("D2"));
                    if (limitedStats.Any())
                    {
                        bonusStats = int.Parse(string.Join("", limitedStats));
                    }
                }
            }
            else if (bloodType.Equals(BloodType.None))
            {
                legacyEnum = (int)bloodType;
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
            WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(character.Read<Equipment>().WeaponSlot.SlotEntity._Entity);
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
                    //if (stats.Count != 0) bonusStats = int.Parse(string.Join("", stats.Select(stat => ((int)stat + 1).ToString("D2"))));
                    var limitedStats = stats.Take(3).Select(stat => ((int)stat + 1).ToString("D2"));
                    if (limitedStats.Any())
                    {
                        bonusStats = int.Parse(string.Join("", limitedStats));
                    }
                }
            }
        }

        return (expertisePercent, expertiseLevel, expertisePrestige, expertiseEnum, bonusStats);
    }
    static (int Type, int Progress, int Goal, string Target, string IsVBlood) GetQuestData(ulong SteamID, Systems.Quests.QuestSystem.QuestType questType)
    {
        int type = 0; // kill by default
        int progress = 0;
        int goal = 0;
        string target = "";
        string isVBlood = "false";

        if (ConfigService.QuestSystem && SteamID.TryGetPlayerQuests(out var questData))
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
    static string BuildProgressMessage((int Percent, int Level, int Prestige, int Class) experienceData, //want to send bonuses as well
        (int Percent, int Level, int Prestige, int Enum, int BonusStats) legacyData,
        (int Percent, int Level, int Prestige, int Enum, int BonusStats) expertiseData,
        (int Type, int Progress, int Goal, string Target, string IsVBlood) dailyQuestData,
        (int Type, int Progress, int Goal, string Target, string IsVBlood) weeklyQuestData)
    {
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ProgressToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3},", experienceData.Percent, experienceData.Level, experienceData.Prestige, experienceData.Class)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", legacyData.Percent, legacyData.Level, legacyData.Prestige, legacyData.Enum, legacyData.BonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2},{1:D2},{2:D2},{3:D2},{4:D6},", expertiseData.Percent, expertiseData.Level, expertiseData.Prestige, expertiseData.Enum, expertiseData.BonusStats)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4},", dailyQuestData.Type, dailyQuestData.Progress, dailyQuestData.Goal, dailyQuestData.Target, dailyQuestData.IsVBlood)
            .AppendFormat(CultureInfo.InvariantCulture, "{0},{1:D2},{2:D2},{3},{4}", weeklyQuestData.Type, weeklyQuestData.Progress, weeklyQuestData.Goal, weeklyQuestData.Target, weeklyQuestData.IsVBlood);

        return sb.ToString();
    }
    static string BuildConfigMessage()
    {
        // need prestige stat multipliers, class stat synergies, and bonus stat base values
        List<float> weaponStatValues = Enum.GetValues(typeof(WeaponStatType)).Cast<WeaponStatType>().Select(stat => WeaponStatValues[stat]).ToList();
        List<float> bloodStatValues = Enum.GetValues(typeof(BloodStatType)).Cast<BloodStatType>().Select(stat => BloodStatValues[stat]).ToList();

        float prestigeStatMultiplier = ConfigService.PrestigeStatMultiplier;
        float statSynergyMultiplier = ConfigService.StatSynergyMultiplier;

        int maxPlayerLevel = ConfigService.MaxLevel;
        int maxLegacyLevel = ConfigService.MaxBloodLevel;
        int maxExpertiseLevel = ConfigService.MaxExpertiseLevel;

        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "[{0}]:", (int)NetworkEventSubType.ConfigsToClient)
            .AppendFormat(CultureInfo.InvariantCulture, "{0:F2},{1:F2},{2},{3},{4},", prestigeStatMultiplier, statSynergyMultiplier, maxPlayerLevel, maxLegacyLevel, maxExpertiseLevel); // Add multipliers to the message

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
    static IEnumerator ClientUpdateLoop()
    {
        while (true)
        {
            if (RegisteredUsers.Count == 0)
            {
                yield return Delay; // Wait 30 seconds if no players
                continue;
            }

            Dictionary<string, PlayerInfo> players = new(OnlineCache); // Shallow copy of the player cache to make sure updates to that don't interfere with loop
            HashSet<ulong> users = new(RegisteredUsers);

            foreach (ulong steamId in users)
            {
                if (players.TryGetValue(steamId.ToString(), out PlayerInfo playerInfo))
                {
                    try
                    {
                        //Core.Log.LogInfo("Sending client progress (OnlineCache)...");
                        SendClientProgress(playerInfo.CharEntity, playerInfo.User.PlatformId);
                    }
                    catch (Exception e)
                    {
                        Core.Log.LogError($"Error sending Eclipse progress to {playerInfo.User.PlatformId}: {e}");
                    }
                }
                yield return null;
            }
            yield return Delay;
        }
    }
}
