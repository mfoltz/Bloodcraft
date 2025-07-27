using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Quests;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using VampireCommandFramework;
using static Bloodcraft.Systems.Quests.QuestSystem;

namespace Bloodcraft.Utilities;
internal static class Quests
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly PrefabGUID _imprisonedBuff = PrefabGUIDs.ImprisonedBuff;

    const float MAX_DISTANCE = 2000f;

    public static readonly Dictionary<QuestType, string> QuestTypeColor = new()
    {
        { QuestType.Daily, "<color=#00FFFF>Daily Quest</color>" },
        { QuestType.Weekly, "<color=#BF40BF>Weekly Quest</color>" }
    };

    static readonly Dictionary<TargetType, string> _questTargetType = new()
    {
        { TargetType.Kill, "Unit" },
        { TargetType.Craft, "Item" },
        { TargetType.Gather, "Resource" },
        { TargetType.Fish, "Fishing" }
    };

    /*
    public static void QuestTrackReply(ChatCommandContext ctx, Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, QuestType questType)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        if (questData.TryGetValue(questType, out var questObjective) && questObjective.Objective.Goal.Equals(TargetType.Kill) && !questObjective.Objective.Complete)
        {
            if (!QuestService.TargetCache.TryGetValue(questObjective.Objective.Target, out HashSet<Entity> entities))
            {
                LocalizationService.HandleReply(ctx, $"Targets have all been killed, give them a chance to respawn! If this doesn't seem right consider rerolling your {QuestTypeColor[questType]}.");
            }
            else if (entities.Count > 0)
            {
                float3 userPosition = character.GetPosition();
                Entity closest = entities
                    .Where(entity =>
                        entity.Exists()
                        && !entity.HasBuff(_imprisonedBuff)
                        && !entity.IsFamiliar())
                    .Select(entity => new
                    {
                        Entity = entity,
                        Distance = math.distance(userPosition, entity.GetPosition())
                    })
                    .Where(x => x.Distance <= MAX_DISTANCE)
                    .OrderBy(x => x.Distance)
                    .Select(x => x.Entity)
                    .FirstOrDefault();

                if (!closest.Exists())
                {
                    LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn!");
                    return;
                }
                else if (closest.IsVBloodOrGateBoss())
                {
                    LocalizationService.HandleReply(ctx, "Use the VBlood menu to track bosses!");
                    return;
                }

                float3 targetPosition = closest.GetPosition();
                float distance = math.distance(userPosition, targetPosition);

                float3 direction = math.normalize(targetPosition - userPosition);
                string cardinalDirection = $"<color=yellow>{GetCardinalDirection(direction)}</color>";
                double seconds = (DateTime.UtcNow - QuestService._lastUpdate).TotalSeconds;

                LocalizationService.HandleReply(ctx, $"Nearest <color=white>{questObjective.Objective.Target.GetLocalizedName()}</color> was <color=#00FFFF>{(int)distance}</color>f away to the {cardinalDirection} <color=#F88380>{(int)seconds}</color>s ago.");
            }
            else if (entities.Count == 0)
            {
                LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn!");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Tracking is only available for incomplete kill quests.");
        }
    }
    */

    public static void QuestTrackReply(ChatCommandContext ctx, Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, QuestType questType)
    {
        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamId = ctx.Event.User.PlatformId;

        if (questData.TryGetValue(questType, out var questObjective) &&
            questObjective.Objective.Goal.Equals(TargetType.Kill) &&
            !questObjective.Objective.Complete)
        {
            var targetCache = QuestTargetSystem.TargetCache;

            if (!targetCache.IsCreated || !targetCache.ContainsKey(questObjective.Objective.Target))
            {
                LocalizationService.HandleReply(ctx, $"Targets have all been killed, give them a chance to respawn! If this doesn't seem right consider rerolling your {QuestTypeColor[questType]}.");
                return;
            }

            float3 userPosition = character.GetPosition();

            bool found = targetCache.TryGetFirstValue(questObjective.Objective.Target,
                out Entity targetEntity,
                out NativeParallelMultiHashMapIterator<PrefabGUID> iterator);

            Entity closest = Entity.Null;
            float closestDist = float.MaxValue;

            while (found)
            {
                if (targetEntity.Exists()
                    && !targetEntity.HasBuff(_imprisonedBuff)
                    && !targetEntity.IsFamiliar())
                {
                    float dist = math.distance(userPosition, targetEntity.GetPosition());

                    if (dist <= MAX_DISTANCE && dist < closestDist)
                    {
                        closest = targetEntity;
                        closestDist = dist;
                    }
                }

                found = targetCache.TryGetNextValue(out targetEntity, ref iterator);
            }

            if (!closest.Exists())
            {
                LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn!");
                return;
            }

            if (closest.IsVBloodOrGateBoss())
            {
                LocalizationService.HandleReply(ctx, "Use the VBlood menu to track bosses!");
                return;
            }

            float3 targetPosition = closest.GetPosition();
            float distance = math.distance(userPosition, targetPosition);
            float3 direction = math.normalize(targetPosition - userPosition);
            string cardinalDirection = $"<color=yellow>{GetCardinalDirection(direction)}</color>";
            double seconds = (DateTime.UtcNow - QuestService._lastUpdate).TotalSeconds;

            // LocalizationService.HandleReply(ctx, $"Nearest <color=white>{questObjective.Objective.Target.GetLocalizedName()}</color> was <color=#00FFFF>{(int)distance}</color>f away to the {cardinalDirection} <color=#F88380>{(int)seconds}</color>s ago.");
            LocalizationService.HandleReply(ctx, $"Nearest <color=white>{questObjective.Objective.Target.GetLocalizedName()}</color> was <color=#00FFFF>{(int)distance}</color>f away to the {cardinalDirection}!");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "Tracking is only available for incomplete kill quests.");
        }
    }
    public static void QuestObjectiveReply(ChatCommandContext ctx, Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, QuestType questType)
    {
        if (questData.TryGetValue(questType, out var questObjective) && !questObjective.Objective.Complete)
        {
            string timeLeft = GetTimeUntilReset(questObjective, questType);
            LocalizationService.HandleReply(ctx, $"{QuestTypeColor[questType]}: <color=green>{questObjective.Objective.Goal}</color> <color=white>{questObjective.Objective.Target.GetLocalizedName()}</color>x<color=#FFC0CB>{questObjective.Objective.RequiredAmount}</color> [<color=white>{questObjective.Progress}</color>/<color=yellow>{questObjective.Objective.RequiredAmount}</color>]");
            LocalizationService.HandleReply(ctx, $"Time until {questType} reset - <color=yellow>{timeLeft}</color> | {_questTargetType[questObjective.Objective.Goal]} Prefab: <color=white>{questObjective.Objective.Target.GetPrefabName()}</color>");
        }
        else if (questObjective.Objective.Complete)
        {
            string timeLeft = GetTimeUntilReset(questObjective, questType);
            LocalizationService.HandleReply(ctx, $"You've already completed your {QuestTypeColor[questType]}! Time until {questType} reset - <color=yellow>{timeLeft}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You don't have a {QuestTypeColor[questType]}.");
        }
    }
    static string GetTimeUntilReset((QuestObjective Objective, int Progress, DateTime LastReset) questObjective, QuestType questType)
    {
        DateTime lastReset = questObjective.LastReset;
        DateTime nextReset = questType.Equals(QuestType.Daily) ? lastReset.AddDays(1) : lastReset.AddDays(7);

        DateTime now = DateTime.UtcNow;
        TimeSpan untilReset = nextReset - now;

        string timeLeft = "";
        if (questType.Equals(QuestType.Daily))
        {
            timeLeft = string.Format("{0:D2}:{1:D2}:{2:D2}",
                            untilReset.Hours,
                            untilReset.Minutes,
                            untilReset.Seconds);
        }
        else if (questType.Equals(QuestType.Weekly))
        {
            timeLeft = string.Format("{0:D1}:{1:D2}:{2:D2}:{3:D2}",
                            untilReset.Days,
                            untilReset.Hours,
                            untilReset.Minutes,
                            untilReset.Seconds);
        }

        return timeLeft;
    }
    static string GetCardinalDirection(float3 direction)
    {
        float angle = math.degrees(math.atan2(direction.z, direction.x));
        if (angle < 0) angle += 360;

        if (angle >= 337.5 || angle < 22.5)
            return "East";
        else if (angle >= 22.5 && angle < 67.5)
            return "Northeast";
        else if (angle >= 67.5 && angle < 112.5)
            return "North";
        else if (angle >= 112.5 && angle < 157.5)
            return "Northwest";
        else if (angle >= 157.5 && angle < 202.5)
            return "West";
        else if (angle >= 202.5 && angle < 247.5)
            return "Southwest";
        else if (angle >= 247.5 && angle < 292.5)
            return "South";
        else
            return "Southeast";
    }
}
