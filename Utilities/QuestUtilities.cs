using Bloodcraft.Services;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using VampireCommandFramework;
using static Bloodcraft.Systems.Quests.QuestSystem;

namespace Bloodcraft.Utilities;

internal static class QuestUtilities
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly PrefabGUID ImprisonedBuff = new(1603329680);

    static readonly Dictionary<QuestType, string> QuestTypeColor = new()
    {
        { QuestType.Daily, "<color=#00FFFF>Daily Quest</color>" },
        { QuestType.Weekly, "<color=#BF40BF>Weekly Quest</color>" }
    };
    public static void TrackQuestReply(ChatCommandContext ctx, Dictionary<QuestType, (QuestObjective Objective, int Progress, DateTime LastReset)> questData, QuestType questType)
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
                float3 userPosition = character.Read<Translation>().Value;
                Entity closest = entities
                    .Where(entity => EntityManager.Exists(entity) && !entity.HasBuff(ImprisonedBuff) && !entity.Has<BlockFeedBuff>())
                    .OrderBy(entity => math.distance(userPosition, entity.Read<Translation>().Value))
                    .FirstOrDefault();

                if (!closest.Exists())
                {
                    LocalizationService.HandleReply(ctx, "Targets have all been killed, give them a chance to respawn!");
                    return;
                }
                else if (closest.Has<VBloodConsumeSource>())
                {
                    LocalizationService.HandleReply(ctx, "Use the VBlood menu to track bosses!");
                    return;
                }

                float3 targetPosition = closest.Read<Translation>().Value;
                float distance = math.distance(userPosition, targetPosition);

                float3 direction = math.normalize(targetPosition - userPosition);
                string cardinalDirection = $"<color=white>{GetCardinalDirection(direction)}</color>";
                double seconds = (DateTime.UtcNow - QuestService.LastUpdate).TotalSeconds;

                LocalizationService.HandleReply(ctx, $"Nearest <color=white>{questObjective.Objective.Target.GetPrefabName()}</color> was <color=#00FFFF>{(int)distance}</color>f away to the <color=yellow>{cardinalDirection}</color> <color=#F88380>{(int)seconds}</color>s ago.");
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
}
