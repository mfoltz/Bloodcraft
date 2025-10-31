using System;
using System.Collections.Generic;
using Bloodcraft.Factory;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Systems.Quests;
public partial class QuestTargetSystem : VSystemBase<QuestTargetSystem.Work>
{
    public static QuestTargetSystem Instance { get; private set; }

    static readonly HashSet<string> _filteredStrings = QuestService.FilteredTargetUnits;
    static readonly HashSet<PrefabGUID> _shardBearers = [.. QuestService.ShardBearers];

    public static NativeParallelMultiHashMap<PrefabGUID, Entity>.ReadOnly TargetCache
    {
        get
        {
            var work = Instance?.GetActiveWork();

            if (work == null)
            {
                return default;
            }

            return work.TargetCache;
        }
    }

    internal static IReadOnlyCollection<string> FilteredStrings => _filteredStrings;

    internal static IReadOnlyCollection<PrefabGUID> ShardBearers => _shardBearers;

    internal void PlayQueuedSequences(ServerGameManager serverGameManager)
    {
        if (EqualityComparer<ServerGameManager>.Default.Equals(serverGameManager, default))
        {
            return;
        }

        while (Sequences.TryDequeue(out var sequenceRequest))
        {
            serverGameManager.PlaySequenceOnTarget(
                sequenceRequest.Target,
                sequenceRequest.SequenceGuid,
                sequenceRequest.Scale,
                sequenceRequest.Secondary);
        }
    }

    internal void SetInstance() => Instance = this;

    internal static void ClearInstance() => Instance = null;

    internal Work GetActiveWork() => base.Work;
}
