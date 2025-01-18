using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Quests.QuestSystem;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Commands;

[CommandGroup(name: "quest")]
internal static class QuestCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    [Command(name: "log", adminOnly: false, usage: ".quest log", description: "Toggles quest progress logging.")]
    public static void LogQuestCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, QUEST_LOG_KEY);
        LocalizationService.HandleReply(ctx, $"Quest logging is now {(GetPlayerBool(steamId, QUEST_LOG_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "progress", shortHand: "p", adminOnly: false, usage: ".quest p [QuestType]", description: "Display your current quest progress.")]
    public static void DailyQuestProgressCommand(ChatCommandContext ctx, string questType)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        questType = questType.ToLower();
        if (!Enum.TryParse(questType, true, out QuestType typeEnum))
        {
            if (questType == "d")
            {
                typeEnum = QuestType.Daily;
            }
            else if (questType == "w")
            {
                typeEnum = QuestType.Weekly;
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid quest type. (daily/weekly or d/w)");
                return;
            }
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (steamId.TryGetPlayerQuests(out var questData))
        {
            Quests.QuestObjectiveReply(ctx, questData, typeEnum);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any quests yet, check back soon.");
        }
    }

    [Command(name: "track", shortHand: "t", adminOnly: false, usage: ".quest t [QuestType]", description: "Locate and track quest target.")]
    public static void LocateTargetCommand(ChatCommandContext ctx, string questType)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        questType = questType.ToLower();
        if (!Enum.TryParse(questType, true, out QuestType typeEnum))
        {
            if (questType == "d")
            {
                typeEnum = QuestType.Daily;
            }
            else if (questType == "w")
            {
                typeEnum = QuestType.Weekly;
            }
            else
            {
                LocalizationService.HandleReply(ctx, "Invalid quest type. (daily/weekly or d/w)");
                return;
            }
        }

        if (QuestService._lastUpdate == default)
        {
            LocalizationService.HandleReply(ctx, "Target cache isn't ready yet, check back shortly!");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (steamId.TryGetPlayerQuests(out var questData))
        {
            Quests.QuestTrackReply(ctx, questData, typeEnum);
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any quests yet, check back soon!");
        }
    }

    [Command(name: "refresh", shortHand: "rf", adminOnly: true, usage: ".quest rf [Name]", description: "Refreshes daily and weekly quests for player.")]
    public static void ForceRefreshQuests(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;

        int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)playerInfo.CharEntity.Read<Equipment>().GetFullLevel();
        ForceRefresh(steamId, level);

        LocalizationService.HandleReply(ctx, $"Quests for <color=green>{playerInfo.User.CharacterName.Value}</color> have been refreshed.");
    }

    [Command(name: "reroll", shortHand: "r", adminOnly: false, usage: ".quest r [QuestType]", description: "Reroll quest for cost (daily only currently).")]
    public static void RerollQuestCommand(ChatCommandContext ctx, string questType)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        questType = questType.ToLower();
        if (questType == "d")
        {
            questType = "Daily";
        }
        else if (questType == "w")
        {
            questType = "Weekly";
        }

        if (!Enum.TryParse(questType, true, out QuestType type))
        {
            LocalizationService.HandleReply(ctx, "Invalid quest type. (Daily/Weekly)");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        if (type.Equals(QuestType.Daily))
        {
            if (steamId.TryGetPlayerQuests(out var questData) && questData[QuestType.Daily].Objective.Complete && !ConfigService.InfiniteDailies)
            {
                LocalizationService.HandleReply(ctx, "You've already completed your <color=#00FFFF>Daily Quest</color> today. Check back tomorrow.");
                return;
            }
            else if (!ConfigService.RerollDailyPrefab.Equals(0))
            {
                PrefabGUID item = new(ConfigService.RerollDailyPrefab);
                int quantity = ConfigService.RerollDailyAmount;

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                    {
                        int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)ctx.Event.SenderCharacterEntity.Read<Equipment>().GetFullLevel();
                        ForceDaily(ctx.Event.User.PlatformId, level);

                        LocalizationService.HandleReply(ctx, $"Your <color=#00FFFF>Daily Quest</color> has been rerolled for <color=#C0C0C0>{item.GetLocalizedName()}</color> x<color=white>{quantity}</color>!");
                        Quests.QuestObjectiveReply(ctx, questData, type);
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You couldn't afford to reroll your daily... (<color=#C0C0C0>{item.GetLocalizedName()}</color> x<color=white>{quantity}</color>)");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "No daily reroll item configured or couldn't find daily quest entry for player.");
            }
        }
        else if (type.Equals(QuestType.Weekly))
        {
            if (steamId.TryGetPlayerQuests(out var questData) && questData[QuestType.Weekly].Objective.Complete)
            {
                LocalizationService.HandleReply(ctx, "You've already completed your <color=#BF40BF>Weekly Quest</color>. Check back next week.");
                return;
            }
            else if (!ConfigService.RerollWeeklyPrefab.Equals(0))
            {
                PrefabGUID item = new(ConfigService.RerollWeeklyPrefab);
                int quantity = ConfigService.RerollWeeklyAmount;

                if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
                {
                    if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                    {
                        int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)ctx.Event.SenderCharacterEntity.Read<Equipment>().GetFullLevel();
                        ForceWeekly(ctx.Event.User.PlatformId, level);

                        LocalizationService.HandleReply(ctx, $"Your <color=#BF40BF>Weekly Quest</color> has been rerolled for <color=#C0C0C0>{item.GetLocalizedName()}</color> x<color=white>{quantity}</color>!");
                        Quests.QuestObjectiveReply(ctx, questData, type);
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You couldn't afford to reroll your wekly... (<color=#C0C0C0>{item.GetLocalizedName()}</color> x<color=white>{quantity}</color>)");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "No weekly reroll item configured or couldn't find weekly quest entry for player.");
            }
        }
    }

    [Command(name: "complete", shortHand: "c", adminOnly: true, usage: ".quest c [Name] [QuestType]", description: "Forcibly completes a specified quest for a player.")]
    public static void ForceCompleteQuest(ChatCommandContext ctx, string name, string questTypeName)
    {
        if (!ConfigService.QuestSystem)
        {
            LocalizationService.HandleReply(ctx, "Quests are not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply("Couldn't find player...");
            return;
        }

        User user = playerInfo.User;
        ulong steamId = user.PlatformId;

        if (!steamId.TryGetPlayerQuests(out var questData))
        {
            ctx.Reply("Player has no active quests!");
            return;
        }

        questTypeName = questTypeName.ToLower();
        if (questTypeName == "d")
        {
            questTypeName = "Daily";
        }
        else if (questTypeName == "w")
        {
            questTypeName = "Weekly";
        }

        if (!Enum.TryParse<QuestType>(questTypeName, true, out var questType))
        {
            ctx.Reply($"Invalid quest type '{questTypeName}'. Valid values are: {string.Join(", ", Enum.GetNames(typeof(QuestType)))}");
            return;
        }

        if (!questData.ContainsKey(questType))
        {
            ctx.Reply($"Player does not have an active {questType} quest to complete.");
            return;
        }

        var quest = questData[questType];
        if (quest.Objective.Complete)
        {
            ctx.Reply($"The {questType} quest is already complete for {playerInfo.User.CharacterName.Value}.");
            return;
        }

        PrefabGUID target = quest.Objective.Target;

        int currentProgress = quest.Progress;
        int required = quest.Objective.RequiredAmount;

        int toAdd = required - currentProgress;
        if (toAdd <= 0) toAdd = required;

        ProcessQuestProgress(questData, target, toAdd, user);

        ctx.Reply($"Completed {Quests.QuestTypeColor[questType]} for <color=green>{playerInfo.User.CharacterName.Value}</color>!");
    }
}
