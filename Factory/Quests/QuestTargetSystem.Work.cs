using System;
using Bloodcraft.Factory;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Systems.Quests;

public partial class QuestTargetSystem
{
    public new sealed class Work : ISystemWork
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

        readonly ISystemWork _implementation;
        readonly SystemWorkBuilder.QueryHandleHolder _targetQuery;
        readonly SystemWorkBuilder.QueryHandleHolder _imprisonedQuery;
        readonly SystemWorkBuilder.QueryHandleHolder _networkSingleton;
        readonly SystemWorkBuilder.ComponentLookupHandle<PrefabGUID> _prefabGuidLookup;
        readonly SystemWorkBuilder.ComponentLookupHandle<Buff> _buffLookup;
        readonly SystemWorkBuilder.NativeContainerHolder<NativeParallelMultiHashMap<PrefabGUID, Entity>> _targetUnits;
        readonly SystemWorkBuilder.NativeContainerHolder<NativeParallelHashSet<Entity>> _imprisonedUnits;
        readonly SystemWorkBuilder.NativeContainerHolder<NativeParallelHashSet<PrefabGUID>> _blacklistedUnits;

        QuestTargetSystem _system;
        ServerGameManager _serverGameManager;

        internal NativeParallelMultiHashMap<PrefabGUID, Entity>.ReadOnly TargetCache =>
            _targetUnits.TryGetValue(out var targetUnits) && targetUnits.IsCreated
                ? targetUnits.AsReadOnly()
                : default;

        public Work()
        {
            var primaryDescriptor = TargetQueryDescriptor;

            var builder = new SystemWorkBuilder()
                .WithQuery(primaryDescriptor);

            _targetQuery = builder.WithPrimaryQuery(requireForUpdate: true);

            var imprisonedDescriptor = ImprisonedQueryDescriptor;
            _imprisonedQuery = builder.WithQuery(ref imprisonedDescriptor);
            _networkSingleton = builder.RequireSingleton<NetworkIdSystem.Singleton>();

            _prefabGuidLookup = builder.WithLookup<PrefabGUID>(isReadOnly: true);
            _buffLookup = builder.WithLookup<Buff>(isReadOnly: true);

            _targetUnits = builder.WithNativeContainer(_ =>
                new NativeParallelMultiHashMap<PrefabGUID, Entity>(1024, Allocator.Persistent));
            _imprisonedUnits = builder.WithNativeContainer(_ =>
                new NativeParallelHashSet<Entity>(512, Allocator.Persistent));
            _blacklistedUnits = builder.WithNativeContainer(_ =>
                new NativeParallelHashSet<PrefabGUID>(256, Allocator.Persistent));

            builder.OnCreate(context =>
            {
                _system = (QuestTargetSystem)context.System;
                _system.SetInstance();
            });

            builder.OnStartRunning(context =>
            {
                _serverGameManager = context.System.World.GetExistingSystemManaged<ServerScriptMapper>().GetServerGameManager();

                if (!_blacklistedUnits.TryGetValue(out var blacklistedUnits))
                {
                    return;
                }

                blacklistedUnits.Clear();

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
                            blacklistedUnits.Add(prefabGuid);
                            break;
                        }
                    }
                }

                foreach (var prefabGuid in ShardBearers)
                {
                    blacklistedUnits.Add(prefabGuid);
                }
            });

            builder.OnUpdate(context =>
            {
                var networkSingleton = _networkSingleton.Handle;
                if (networkSingleton == null || networkSingleton.IsDisposed)
                {
                    return;
                }

                var targetQuery = _targetQuery.Handle;
                var imprisonedQuery = _imprisonedQuery.Handle;

                if (targetQuery == null || targetQuery.IsDisposed || imprisonedQuery == null || imprisonedQuery.IsDisposed)
                {
                    return;
                }

                if (!_targetUnits.TryGetValue(out var targetUnits)
                    || !_imprisonedUnits.TryGetValue(out var imprisonedUnits)
                    || !_blacklistedUnits.TryGetValue(out var blacklistedUnits))
                {
                    return;
                }

                imprisonedUnits.Clear();
                targetUnits.Clear();

                SystemWorkBuilder.ForEachChunk(context, imprisonedQuery)
                    .ForEach(chunkContext =>
                    {
                        var entities = chunkContext.Entities;

                        for (int i = 0; i < chunkContext.Count; ++i)
                        {
                            var entity = entities[i];

                            if (!chunkContext.TryGetComponent(_buffLookup, entity, out var buff))
                            {
                                continue;
                            }

                            var target = buff.Target;

                            if (chunkContext.Exists(target))
                            {
                                imprisonedUnits.Add(target);
                            }
                        }
                    });

                SystemWorkBuilder.ForEachChunk(context, targetQuery)
                    .ForEach(chunkContext =>
                    {
                        var entities = chunkContext.Entities;

                        for (int i = 0; i < chunkContext.Count; ++i)
                        {
                            var entity = entities[i];

                            if (!chunkContext.TryGetComponent(_prefabGuidLookup, entity, out var prefabGuid))
                            {
                                continue;
                            }

                            if (blacklistedUnits.Contains(prefabGuid) || imprisonedUnits.Contains(entity))
                            {
                                continue;
                            }

                            targetUnits.Add(prefabGuid, entity);
                        }
                    });

                _system.PlayQueuedSequences(_serverGameManager);
            });

            builder.OnDestroy(context =>
            {
                _serverGameManager = default;

                ClearInstance();
                _system = null;
            });

            _implementation = builder.Build();
        }

        public void Build(ref EntityQueryBuilder builder) =>
            _implementation.Build(ref builder);

        public void OnCreate(SystemContext context) =>
            _implementation.OnCreate(context);

        public void OnStartRunning(SystemContext context) =>
            _implementation.OnStartRunning(context);

        public void OnUpdate(SystemContext context) =>
            _implementation.OnUpdate(context);

        public void OnStopRunning(SystemContext context) =>
            _implementation.OnStopRunning(context);

        public void OnDestroy(SystemContext context) =>
            _implementation.OnDestroy(context);

    }
}
