using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Quests;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.SystemUtilities.Quests.QuestUtilities;

namespace Bloodcraft.Commands;

internal static class QuestCommands
{
    [Command(name: "logQuestProgress", shortHand: "log q", adminOnly: false, usage: ".log q", description: "Toggles quest progress logging.")]
    public static void LogQuestCommand(ChatCommandContext ctx)
    {
        if (!Plugin.QuestSystem.Value || !Plugin.ProfessionSystem.Value || !Plugin.LevelingSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["QuestLogging"] = !bools["QuestLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Quest logging is now {(bools["QuestLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "getDailyQuestProgress", shortHand: "get dq", adminOnly: false, usage: ".get dq", description: "Display your current daily quest progress.")]
    public static void DailyQuestProgressCommand(ChatCommandContext ctx)
    {
        if (!Plugin.QuestSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerQuests.TryGetValue(steamID, out var questData))
        {
            if (questData.TryGetValue(QuestType.Daily, out var dailyQuest) && !dailyQuest.Objective.Complete)
            {
                LocalizationService.HandleReply(ctx, $"<color=#00FFFF>Daily Quest</color>: <color=green>{dailyQuest.Objective.Goal}</color> <color=white>{dailyQuest.Objective.Target.GetPrefabName()}</color>x<color=#FFC0CB>{dailyQuest.Objective.RequiredAmount}</color> [<color=white>{dailyQuest.Progress}</color>/<color=yellow>{dailyQuest.Objective.RequiredAmount}</color>]");
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
            LocalizationService.HandleReply(ctx, "You don't have any quests yet.");
        }
    }

    [Command(name: "getWeeklyQuestProgress", shortHand: "get wq", adminOnly: false, usage: ".get wq", description: "Display your current weekly quest progress.")]
    public static void WeeklyQuestProgressCommand(ChatCommandContext ctx)
    {
        if (!Plugin.QuestSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerQuests.TryGetValue(steamID, out var questData))
        {
            if (questData.TryGetValue(QuestType.Weekly, out var weeklyQuest) && !weeklyQuest.Objective.Complete)
            {
                LocalizationService.HandleReply(ctx, $"<color=#BF40BF>Weekly Quest</color>: <color=green>{weeklyQuest.Objective.Goal}</color> <color=white>{weeklyQuest.Objective.Target.GetPrefabName()}</color>x<color=#FFC0CB>{weeklyQuest.Objective.RequiredAmount}</color> [<color=white>{weeklyQuest.Progress}</color>/<color=yellow>{weeklyQuest.Objective.RequiredAmount}</color>]");
            }
            else if (weeklyQuest.Objective.Complete)
            {
                LocalizationService.HandleReply(ctx, "You've already completed your <color=#BF40BF>Weekly Quest</color>. Check back after midnight Sunday.");
            }
            else
            {
                LocalizationService.HandleReply(ctx, "You don't have a <color=#BF40BF>Weekly Quest</color>.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any quests yet.");
        }
    }
    [Command(name: "ForceRefreshPlayerQuests", shortHand: "refresh", adminOnly: true, usage: ".refresh [Name]", description: "Display your current weekly quest progress.")]
    public static void ForceRefreshQuests(ChatCommandContext ctx, string name)
    {
        if (!Plugin.QuestSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.GetUserByName(name, true);

        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found...");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();

        int level = Plugin.LevelingSystem.Value ? Core.DataStructures.PlayerExperience[foundUser.PlatformId].Key : (int)foundUser.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();

        QuestUtilities.ForceRefresh(foundUser.PlatformId, level);
        LocalizationService.HandleReply(ctx, $"Quests for <color=green>{foundUser.CharacterName.Value}</color> have been refreshed.");
    }
}
