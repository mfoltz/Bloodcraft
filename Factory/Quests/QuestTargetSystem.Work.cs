using System;
using Bloodcraft.Factory;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Systems.Quests;

public partial class QuestTargetSystem
{
    public sealed class Work : ISystemWork
    {
        static readonly QueryDescriptor TargetQueryDescriptor = QueryDescriptor.Create()
            .WithAll<PrefabGUID>()
            .WithAll<Health>()
            .WithAll<UnitLevel>()
            .WithAll<UnitStats>()
            .WithAll<Movement>()
            .WithAll<AggroConsumer>()
            .WithNone<Minion>()
            .WithNone<DestroyOnSpawn>()
            .WithNone<Trader>()
            .WithNone<BlockFeedBuff>()
            .IncludeDisabled();

        static readonly QueryDescriptor ImprisonedQueryDescriptor = QueryDescriptor.Create()
            .WithAll<Buff>()
            .WithAll<ImprisonedBuff>()
            .IncludeDisabled();

        QuestTargetSystem _system;
        ServerGameManager _serverGameManager;

        QueryHandle _targetQuery;
        QueryHandle _imprisonedQuery;

        SystemWorkBuilder.ComponentTypeHandleHandle<PrefabGUID> _prefabGuidHandle;
        SystemWorkBuilder.ComponentTypeHandleHandle<Buff> _buffHandle;

        NativeParallelMultiHashMap<PrefabGUID, Entity> _targetUnits;
        NativeParallelHashSet<Entity> _imprisonedUnits;
        NativeParallelHashSet<PrefabGUID> _blacklistedUnits;

        internal NativeParallelMultiHashMap<PrefabGUID, Entity>.ReadOnly TargetCache =>
            _targetUnits.IsCreated
                ? _targetUnits.AsReadOnly()
                : default;

        public void Build(ref EntityQueryBuilder builder) =>
            TargetQueryDescriptor.Configure(ref builder);

        public void OnCreate(SystemContext context)
        {
            _system = (QuestTargetSystem)context.System;
            _system.SetInstance();

            _targetUnits = new NativeParallelMultiHashMap<PrefabGUID, Entity>(1024, Allocator.Persistent);
            _imprisonedUnits = new NativeParallelHashSet<Entity>(512, Allocator.Persistent);
            _blacklistedUnits = new NativeParallelHashSet<PrefabGUID>(256, Allocator.Persistent);

            _targetQuery = context.WithQuery(context.Query, requireForUpdate: true);
            _imprisonedQuery = context.CreateQuery(ImprisonedQueryDescriptor);

            _prefabGuidHandle = SystemWorkBuilder.CreateComponentTypeHandle<PrefabGUID>(context, isReadOnly: true);
            _buffHandle = SystemWorkBuilder.CreateComponentTypeHandle<Buff>(context, isReadOnly: true);
        }

        public void OnStartRunning(SystemContext context)
        {
            _serverGameManager = context.System.World.GetExistingSystemManaged<ServerScriptMapper>().GetServerGameManager();

            if (!_blacklistedUnits.IsCreated)
            {
                return;
            }

            _blacklistedUnits.Clear();

            var prefabCollection = context.System.World.GetExistingSystemManaged<PrefabCollectionSystem>();
            var lookup = prefabCollection._SpawnableNameToPrefabGuidDictionary;

            foreach (var kvp in lookup)
            {
                var prefabGuid = kvp.Value;
                var prefabName = kvp.Key;

                foreach (var filter in FilteredStrings)
                {
                    if (prefabName.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _blacklistedUnits.Add(prefabGuid);
                        break;
                    }
                }
            }

            foreach (var prefabGuid in ShardBearers)
            {
                _blacklistedUnits.Add(prefabGuid);
            }
        }

        public void OnUpdate(SystemContext context)
        {
            if (_targetQuery == null || _imprisonedQuery == null)
            {
                return;
            }

            if (!_targetUnits.IsCreated || !_imprisonedUnits.IsCreated || !_blacklistedUnits.IsCreated)
            {
                return;
            }

            _imprisonedUnits.Clear();
            _targetUnits.Clear();

            SystemWorkBuilder.ForEachChunk(context, _imprisonedQuery, chunk =>
            {
                var buffs = chunk.GetNativeArray(_buffHandle);

                for (int i = 0; i < chunk.Count; ++i)
                {
                    var target = buffs[i].Target;

                    if (context.Exists(target))
                    {
                        _imprisonedUnits.Add(target);
                    }
                }
            });

            SystemWorkBuilder.ForEachChunk(context, _targetQuery, chunk =>
            {
                var prefabGuids = chunk.GetNativeArray(_prefabGuidHandle);
                var entities = chunk.Entities;

                for (int i = 0; i < chunk.Count; ++i)
                {
                    var prefabGuid = prefabGuids[i];
                    var entity = entities[i];

                    if (!context.Exists(entity))
                    {
                        continue;
                    }

                    if (_blacklistedUnits.Contains(prefabGuid) || _imprisonedUnits.Contains(entity))
                    {
                        continue;
                    }

                    _targetUnits.Add(prefabGuid, entity);
                }
            });

            _system.PlayQueuedSequences(_serverGameManager);
        }

        public void OnDestroy(SystemContext context)
        {
            if (_targetUnits.IsCreated)
            {
                _targetUnits.Dispose();
                _targetUnits = default;
            }

            if (_imprisonedUnits.IsCreated)
            {
                _imprisonedUnits.Dispose();
                _imprisonedUnits = default;
            }

            if (_blacklistedUnits.IsCreated)
            {
                _blacklistedUnits.Dispose();
                _blacklistedUnits = default;
            }

            _targetQuery = null;
            _imprisonedQuery = null;

            _prefabGuidHandle = null;
            _buffHandle = null;

            _serverGameManager = default;

            ClearInstance();
            _system = null;
        }

    }
}
