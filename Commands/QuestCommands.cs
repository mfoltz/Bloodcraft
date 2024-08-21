using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.SystemUtilities.Quests.QuestSystem;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft.Commands;

[CommandGroup(name: "quest")]
internal static class QuestCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PlayerService PlayerService => Core.PlayerService;

    [Command(name: "log", adminOnly: false, usage: ".quest log", description: "Toggles quest progress logging.")]
    public static void LogQuestCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        var SteamID = ctx.Event.User.PlatformId;

        if (PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["QuestLogging"] = !bools["QuestLogging"];
        }

        SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Quest logging is now {(bools["QuestLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "daily", shortHand: "d", adminOnly: false, usage: ".quest d", description: "Display your current daily quest progress.")]
    public static void DailyQuestProgressCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        if (PlayerQuests.TryGetValue(steamID, out var questData))
        {
            if (questData.TryGetValue(QuestType.Daily, out var dailyQuest) && !dailyQuest.Objective.Complete)
            {
                DateTime lastDaily = dailyQuest.LastReset;
                DateTime nextDaily = lastDaily.AddDays(1);
                DateTime now = DateTime.UtcNow;
                TimeSpan untilReset = nextDaily - now;

                string timeLeft = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                                untilReset.Hours,
                                                untilReset.Minutes,
                                                untilReset.Seconds);

                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>Daily Quest</color>: <color=green>{dailyQuest.Objective.Goal}</color> <color=white>{dailyQuest.Objective.Target.GetPrefabName()}</color>x<color=#FFC0CB>{dailyQuest.Objective.RequiredAmount}</color> [<color=white>{dailyQuest.Progress}</color>/<color=yellow>{dailyQuest.Objective.RequiredAmount}</color>]");
                LocalizationService.HandleReply(ctx, $"Time until daily reset: <color=yellow>{timeLeft}</color> | Character Prefab: <color=white>{dailyQuest.Objective.Target.LookupName()}</color>");
            }
            else if (dailyQuest.Objective.Complete)
            {
                LocalizationService.HandleReply(ctx, "You've already completed your <color=#00FFFF>Daily Quest</color>. Check back after midnight.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You don't have a <color=#00FFFF>Daily Quest</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any quests yet, check back soon.");
        }
    }

    [Command(name: "weekly", shortHand: "w", adminOnly: false, usage: ".quest w", description: "Display your current weekly quest progress.")]
    public static void WeeklyQuestProgressCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;

        if (PlayerQuests.TryGetValue(steamID, out var questData))
        {
            if (questData.TryGetValue(QuestType.Weekly, out var weeklyQuest) && !weeklyQuest.Objective.Complete)
            {
                DateTime lastWeekly = weeklyQuest.LastReset;
                DateTime nextWeekly = lastWeekly.AddDays(7);
                DateTime now = DateTime.UtcNow;
                TimeSpan untilReset = nextWeekly - now;

                string timeLeft = string.Format("{0:D1}:{1:D2}:{2:D2}:{3:D2}",
                                                untilReset.Days,
                                                untilReset.Hours,
                                                untilReset.Minutes,
                                                untilReset.Seconds);

                LocalizationService.HandleReply(ctx, $"<color=#BF40BF>Weekly Quest</color>: <color=green>{weeklyQuest.Objective.Goal}</color> <color=white>{weeklyQuest.Objective.Target.GetPrefabName()}</color>x<color=#FFC0CB>{weeklyQuest.Objective.RequiredAmount}</color> [<color=white>{weeklyQuest.Progress}</color>/<color=yellow>{weeklyQuest.Objective.RequiredAmount}</color>]");
                LocalizationService.HandleReply(ctx, $"Time until weekly reset: <color=yellow>{timeLeft}</color> | Character Prefab: <color=white>{weeklyQuest.Objective.Target.LookupName()}</color>");
            }
            else if (weeklyQuest.Objective.Complete)
            {
                DateTime lastWeekly = weeklyQuest.LastReset;
                DateTime nextWeekly = lastWeekly.AddDays(7);
                DateTime now = DateTime.UtcNow;
                TimeSpan untilReset = nextWeekly - now;

                string timeLeft = string.Format("{0:D1}:{1:D2}:{2:D2}:{3:D2}",
                                                untilReset.Days,
                                                untilReset.Hours,
                                                untilReset.Minutes,
                                                untilReset.Seconds);

                LocalizationService.HandleReply(ctx, "You've already completed your <color=#BF40BF>Weekly Quest</color>.");
                LocalizationService.HandleReply(ctx, $"Time until weekly reset: <color=yellow>{timeLeft}</color>");
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You don't have a <color=#BF40BF>Weekly Quest</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any quests yet, check back soon.");
        }
    }

    [Command(name: "track", shortHand: "t", adminOnly: false, usage: ".quest t [QuestType]", description: "Locate and track quest target, rerolls quest if none found.")]
    public static void LocateTargetCommand(ChatCommandContext ctx, string questType)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        if (!Enum.TryParse(questType, true, out QuestType type))
        {
            LocalizationService.HandleReply(ctx, "Invalid quest type. (Daily/Weekly)");
            return;
        }

        if (QuestService.LastUpdate == default)
        {
            LocalizationService.HandleReply(ctx, "Targets haven't been tracked yet, check back shortly.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        User user = ctx.Event.User;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;

        if (PlayerQuests.TryGetValue(steamID, out var questData))
        {
            if (type.Equals(QuestType.Daily) && questData.TryGetValue(QuestType.Daily, out var dailyQuest) && !dailyQuest.Objective.Complete)
            {
                if (!QuestService.TargetCache.TryGetValue(dailyQuest.Objective.Target, out HashSet<Entity> entities)) // if no valid targest refresh quest
                {
                    int level = ConfigService.LevelingSystem ? PlayerExperience[steamID].Key : (int)character.Read<Equipment>().GetFullLevel();
                    ForceDaily(user, steamID, level);
                }
                else if (entities.Count > 0)
                {
                    float3 userPosition = character.Read<Translation>().Value;
                    Entity closest = entities
                        .Where(entity => EntityManager.Exists(entity))
                        .OrderBy(entity => math.distance(userPosition, entity.Read<Translation>().Value))
                        .FirstOrDefault();
                    if (closest == Entity.Null)
                    {
                        LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn and check back soon!");
                        return;
                    }
                    float3 targetPosition = closest.Read<Translation>().Value;
                    if (closest.Has<VBloodConsumeSource>())
                    {
                        LocalizationService.HandleReply(ctx, "Use the VBlood menu to track boss units.");
                        return;
                    }
                    if (math.distance(userPosition, targetPosition) > 5000f) // usually means non-ideal CHAR prefab that spawns rarely or strangely for w/e reason
                    {
                        int level = ConfigService.LevelingSystem ? PlayerExperience[steamID].Key : (int)character.Read<Equipment>().GetFullLevel();
                        ForceDaily(user, steamID, level);
                        return;
                    }
                    float distance = math.distance(userPosition, targetPosition);
                    float3 direction = math.normalize(targetPosition - userPosition);
                    string cardinalDirection = $"<color=white>{GetCardinalDirection(direction)}</color>";
                    double seconds = (DateTime.UtcNow - QuestService.LastUpdate).TotalSeconds;
                    LocalizationService.HandleReply(ctx, $"Nearest <color=white>{dailyQuest.Objective.Target.GetPrefabName()}</color> was <color=#00FFFF>{(int)distance}</color>f away to the <color=yellow>{cardinalDirection}</color> <color=#F88380>{(int)seconds}</color>s ago.");
                }
                else if (entities.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn and check back soon!");
                }
            }
            else if (type.Equals(QuestType.Weekly) && questData.TryGetValue(QuestType.Weekly, out var weeklyQuest) && !weeklyQuest.Objective.Complete)
            {
                if (!QuestService.TargetCache.TryGetValue(weeklyQuest.Objective.Target, out HashSet<Entity> entities)) // if no valid targets refresh quest
                {
                    int level = ConfigService.LevelingSystem ? PlayerExperience[steamID].Key : (int)character.Read<Equipment>().GetFullLevel();
                    ForceWeekly(user, steamID, level);
                }
                else if (entities.Count > 0)
                {
                    float3 userPosition = character.Read<Translation>().Value;
                    Entity closest = entities
                        .Where(entity => EntityManager.Exists(entity))
                        .OrderBy(entity => math.distance(userPosition, entity.Read<Translation>().Value))
                        .FirstOrDefault();
                    if (closest == Entity.Null)
                    {
                        LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn and check back soon!");
                        return;
                    }
                    float3 targetPosition = closest.Read<Translation>().Value;
                    if (closest.Has<VBloodConsumeSource>())
                    {
                        LocalizationService.HandleReply(ctx, "Use the VBlood menu to track boss units.");
                        return;
                    }
                    if (math.distance(userPosition, targetPosition) > 5000f) // usually means non-ideal CHAR prefab that spawns rarely or strangely for w/e reason, resetting with this should take precedence over prefab being seen probably
                    {
                        int level = ConfigService.LevelingSystem ? PlayerExperience[steamID].Key : (int)character.Read<Equipment>().GetFullLevel();
                        ForceWeekly(user, steamID, level);
                        return;
                    }
                    float distance = math.distance(userPosition, targetPosition);
                    float3 direction = math.normalize(targetPosition - userPosition);
                    string cardinalDirection = $"<color=white>{GetCardinalDirection(direction)}</color>";
                    double seconds = (DateTime.UtcNow - QuestService.LastUpdate).TotalSeconds;
                    LocalizationService.HandleReply(ctx, $"Nearest <color=white>{weeklyQuest.Objective.Target.GetPrefabName()}</color> was <color=#00FFFF>{(int)distance}</color>f away to the <color=yellow>{cardinalDirection}</color> <color=#F88380>{(int)seconds}</color>s ago.");
                }
                else if (entities.Count == 0)
                {
                    LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn and check back soon!");
                }
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any quests yet, check back soon.");
        }
    }

    [Command(name: "refresh", shortHand: "r", adminOnly: true, usage: ".quest r [Name]", description: "Refreshes daily and weekly quests for player.")]
    public static void ForceRefreshQuests(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.UserCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();

        int level = ConfigService.LevelingSystem ? PlayerExperience[foundUser.PlatformId].Key : (int)foundUser.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
        ForceRefresh(foundUser.PlatformId, level);

        LocalizationService.HandleReply(ctx, $"Quests for <color=green>{foundUser.CharacterName.Value}</color> have been refreshed.");
    }
}
