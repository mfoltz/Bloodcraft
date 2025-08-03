using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Systems.Quests;
public class QuestTargetSystem : SystemBase
{
    ServerGameManager _serverGameManager;
    public static QuestTargetSystem Instance { get; set; }

    static readonly HashSet<string> _filteredStrings = QuestService.FilteredTargetUnits;
    static readonly HashSet<PrefabGUID> _shardBearers = [..QuestService.ShardBearers];
    public static NativeParallelMultiHashMap<PrefabGUID, Entity>.ReadOnly TargetCache
        => Instance?._targetUnits.IsCreated == true ? Instance._targetUnits.AsReadOnly() : default;

    NativeParallelMultiHashMap<PrefabGUID, Entity> _targetUnits;
    NativeParallelHashSet<Entity> _imprisonedUnits;
    NativeParallelHashSet<PrefabGUID> _blacklistedUnits;

    EntityQuery _targetQuery;
    EntityQuery _imprisonedQuery;

    ComponentTypeHandle<PrefabGUID> _prefabGuidHandle;
    ComponentTypeHandle<Buff> _buffHandle;

    EntityTypeHandle _entityHandle;
    EntityStorageInfoLookup _entityStorageInfoLookup;
    public override void OnCreate()
    {
        Instance = this;

        _targetQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
                ComponentType.ReadOnly(Il2CppType.Of<Health>()),
                ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
                ComponentType.ReadOnly(Il2CppType.Of<UnitStats>()),
                ComponentType.ReadOnly(Il2CppType.Of<Movement>()),
                ComponentType.ReadOnly(Il2CppType.Of<AggroConsumer>())
            },
            None = new[]
            {
                ComponentType.ReadOnly(Il2CppType.Of<Minion>()),
                ComponentType.ReadOnly(Il2CppType.Of<DestroyOnSpawn>()),
                ComponentType.ReadOnly(Il2CppType.Of<Trader>()),
                ComponentType.ReadOnly(Il2CppType.Of<BlockFeedBuff>())
            },
            Options = EntityQueryOptions.IncludeDisabled
        });

        _imprisonedQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new[]
            {
                ComponentType.ReadOnly(Il2CppType.Of<Buff>()),
                ComponentType.ReadOnly(Il2CppType.Of<ImprisonedBuff>())
            },
            Options = EntityQueryOptions.IncludeDisabled
        });

        _targetUnits = new NativeParallelMultiHashMap<PrefabGUID, Entity>(1024, Allocator.Persistent);
        _blacklistedUnits = new NativeParallelHashSet<PrefabGUID>(256, Allocator.Persistent);
        _imprisonedUnits = new NativeParallelHashSet<Entity>(512, Allocator.Persistent);

        _prefabGuidHandle = GetComponentTypeHandle<PrefabGUID>(true);
        _buffHandle = GetComponentTypeHandle<Buff>(true);

        _entityHandle = GetEntityTypeHandle();
        _entityStorageInfoLookup = GetEntityStorageInfoLookup();

        RequireForUpdate(_targetQuery);
        Enabled = true;
    }
    public override void OnStartRunning()
    {
        _serverGameManager = World.GetExistingSystemManaged<ServerScriptMapper>().GetServerGameManager();
        var lookup = World.GetExistingSystemManaged<PrefabCollectionSystem>()._SpawnableNameToPrefabGuidDictionary;

        foreach (var kvp in lookup)
        {
            PrefabGUID prefabGuid = kvp.Value;
            string prefabName = kvp.Key;

            foreach (var filter in _filteredStrings)
            {
                if (prefabName.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                {
                    _blacklistedUnits.Add(prefabGuid);
                    break;
                }
            }
        }

        foreach (var prefabGuid in _shardBearers)
        {
            _blacklistedUnits.Add(prefabGuid);
        }
    }
    public override void OnUpdate()
    {
        _imprisonedUnits.Clear();
        _targetUnits.Clear();

        _buffHandle.Update(this);
        _prefabGuidHandle.Update(this);

        _entityHandle.Update(this);
        _entityStorageInfoLookup.Update(this);

        var imprisonedChunks = _imprisonedQuery.ToArchetypeChunkArray(Allocator.Temp);
        int imprisonedCount = 0;

        try
        {
            foreach (var chunk in imprisonedChunks)
            {
                var buffs = chunk.GetNativeArray(_buffHandle);

                for (int i = 0; i < chunk.Count; ++i)
                {
                    Entity entity = buffs[i].Target;

                    if (_entityStorageInfoLookup.Exists(entity))
                    {
                        _imprisonedUnits.Add(buffs[i].Target);
                        imprisonedCount++;
                    }
                }
            }
        }
        finally
        {
            imprisonedChunks.Dispose();
        }

        var targetChunks = _targetQuery.ToArchetypeChunkArray(Allocator.Temp);
        int targetCount = 0;

        try
        {
            foreach (var chunk in targetChunks)
            {
                var prefabGuids = chunk.GetNativeArray(_prefabGuidHandle);
                var entities = chunk.GetNativeArray(_entityHandle);

                for (int i = 0; i < chunk.Count; ++i)
                {
                    PrefabGUID prefabGuid = prefabGuids[i];
                    Entity entity = entities[i];

                    if (!_entityStorageInfoLookup.Exists(entity))
                        continue;
                    else if (_blacklistedUnits.Contains(prefabGuid)
                        || _imprisonedUnits.Contains(entity))
                        continue;
                    else
                        _targetUnits.Add(prefabGuid, entity);
                        targetCount++;
                }
            }
        }
        finally
        {
            targetChunks.Dispose();
        }

        while (Sequences.TryDequeue(out var sequenceRequest))
        {
            // Core.Log.LogWarning($"{sequenceRequest.SequenceName} = {sequenceRequest.SequenceGuid.GuidHash}");
            _serverGameManager.PlaySequenceOnTarget(
                sequenceRequest.Target,
                sequenceRequest.SequenceGuid,
                sequenceRequest.Scale,
                sequenceRequest.Secondary);
        }
    }
}
