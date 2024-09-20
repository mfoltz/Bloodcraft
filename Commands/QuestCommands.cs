using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Quests.QuestSystem;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;

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

        var SteamID = ctx.Event.User.PlatformId;
        PlayerUtilities.
                TogglePlayerBool(SteamID, "QuestLogging");
        LocalizationService.HandleReply(ctx, $"Quest logging is now {(PlayerUtilities.GetPlayerBool(SteamID, "QuestLogging") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
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
        if (steamID.TryGetPlayerQuests(out var questData))
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

        if (steamID.TryGetPlayerQuests(out var questData))
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

        if (QuestService.LastUpdate == default)
        {
            LocalizationService.HandleReply(ctx, "Targets haven't been tracked yet, check back shortly.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;
        User user = ctx.Event.User;
        Entity character = ctx.Event.SenderCharacterEntity;
        Entity userEntity = ctx.Event.SenderUserEntity;

        if (steamId.TryGetPlayerQuests(out var questData))
        {
            if (type.Equals(QuestType.Daily) && questData.TryGetValue(QuestType.Daily, out var dailyQuest) && dailyQuest.Objective.Goal.Equals(TargetType.Kill) && !dailyQuest.Objective.Complete)
            {
                if (!QuestService.TargetCache.TryGetValue(dailyQuest.Objective.Target, out HashSet<Entity> entities)) // if no valid targest refresh quest
                {
                    int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                    ForceDaily(user, steamId, level);
                    LocalizationService.HandleReply(ctx, $"No targets found, rerolling <color=#00FFFF>Daily Quest</color>...");
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
                        int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                        ForceDaily(user, steamId, level);
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
            else if (type.Equals(QuestType.Weekly) && questData.TryGetValue(QuestType.Weekly, out var weeklyQuest) && weeklyQuest.Objective.Goal.Equals(TargetType.Kill) && !weeklyQuest.Objective.Complete)
            {
                if (!QuestService.TargetCache.TryGetValue(weeklyQuest.Objective.Target, out HashSet<Entity> entities)) // if no valid targets refresh quest
                {
                    int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                    ForceWeekly(user, steamId, level);
                    LocalizationService.HandleReply(ctx, $"No targets found, rerolling <color=#BF40BF>Weekly Quest</color>...");
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
                        int level = (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var data)) ? data.Key : (int)user.LocalCharacter._Entity.Read<Equipment>().GetFullLevel();
                        ForceWeekly(user, steamId, level);
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
            else
            {
                LocalizationService.HandleReply(ctx, "Tracking only works for incomplete kill quests.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "You don't have any quests yet, check back soon.");
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

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
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
            if (steamId.TryGetPlayerQuests(out var questData) && questData[QuestType.Weekly].Objective.Complete && !ConfigService.InfiniteDailies)
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
                        ForceDaily(ctx.Event.User, ctx.Event.User.PlatformId, level);

                        LocalizationService.HandleReply(ctx, $"Your <color=#00FFFF>Daily Quest</color> has been rerolled for <color=#C0C0C0>{item.GetPrefabName()}</color> x<color=white>{quantity}</color>!");
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You couldn't afford to reroll your daily... (<color=#C0C0C0>{item.GetPrefabName()}</color> x<color=white>{quantity}</color>)");
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
                        ForceWeekly(ctx.Event.User, ctx.Event.User.PlatformId, level);

                        LocalizationService.HandleReply(ctx, $"Your <color=#BF40BF>Weekly Quest</color> has been rerolled for <color=#C0C0C0>{item.GetPrefabName()}</color> x<color=white>{quantity}</color>!");
                    }
                }
                else
                {
                    LocalizationService.HandleReply(ctx, $"You couldn't afford to reroll your wekly... (<color=#C0C0C0>{item.GetPrefabName()}</color> x<color=white>{quantity}</color>)");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "No weekly reroll item configured or couldn't find weekly quest entry for player.");
            }
        }
    }
}
