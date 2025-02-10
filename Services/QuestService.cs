using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Quests.QuestSystem;

namespace Bloodcraft.Services;
internal class QuestService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds _updateDelay = new(60);

    static readonly ComponentType[] _unitComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Movement>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
        ComponentType.ReadOnly(Il2CppType.Of<Team>()),
        ComponentType.ReadOnly(Il2CppType.Of<Translation>())
    ];

    static readonly ComponentType[] _craftingComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<InventoryItem>()),
        ComponentType.ReadOnly(Il2CppType.Of<ItemData>())
    ];

    static readonly ComponentType[] _resourceComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<YieldResourcesOnDamageTaken>()),
        ComponentType.ReadOnly(Il2CppType.Of<DropTableBuffer>())
    ];

    static readonly ComponentType[] _noneComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<SpawnTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<Minion>())
    ];

    static EntityQuery _unitQuery;
    static EntityQuery _itemQuery;
    static EntityQuery _resourceQuery;

    public static Dictionary<PrefabGUID, HashSet<Entity>> _targetCache = [];
    public static DateTime _lastUpdate;

    static readonly PrefabGUID _manticore = new(-393555055);
    static readonly PrefabGUID _dracula = new(-327335305);
    static readonly PrefabGUID _monster = new(1233988687);
    static readonly PrefabGUID _solarus = new(-740796338);

    static bool _shardBearersReset = false;
    static bool _craftAndGather = false;
    public QuestService()
    {
        _unitQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _unitComponents,
            None = _noneComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        _itemQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _craftingComponents,
            Options = EntityQueryOptions.IncludeAll
        });

        _resourceQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _resourceComponents,
            Options = EntityQueryOptions.IncludeAll
        });

        Configuration.QuestRewardItems();
        QuestUpdateLoop().Start();
    }
    static IEnumerator QuestUpdateLoop()
    {
        while (true)
        {
            if (ConfigService.EliteShardBearers && !_shardBearersReset) // makes sure server doesn't un-elite shard bearers on restarts by forcing them to spawn again
            {
                IEnumerable<Entity> vBloods = Queries.GetEntitiesEnumerable(_unitQuery);
                foreach (Entity entity in vBloods)
                {
                    PrefabGUID vBloodPrefab = entity.Read<PrefabGUID>();
                    if (vBloodPrefab == _manticore || vBloodPrefab == _dracula || vBloodPrefab == _monster || vBloodPrefab == _solarus)
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                }

                _shardBearersReset = true;
            }

            if (!_craftAndGather)
            {
                IEnumerable<Entity> entities = Queries.GetEntitiesEnumerable(_itemQuery, (int)TargetType.Craft);
                foreach (Entity entity in entities)
                {
                    if (entity.TryGetComponent(out PrefabGUID prefab) && !entity.Has<ShatteredItem>() && !entity.Has<UpgradeableLegendaryItem>())
                    {
                        if (entity.Has<Equippable>() && entity.TryGetComponent(out Salvageable salveageable) && salveageable.RecipeGUID.HasValue()) CraftPrefabs.Add(prefab); // checking for non-empty salvage recipes for equipment craft targets
                        else if (entity.Has<ConsumableCondition>()) CraftPrefabs.Add(prefab); // checking for consumableCondition for consumable craft targets
                    }
                }

                entities = Queries.GetEntitiesEnumerable(_resourceQuery, (int)TargetType.Gather);
                foreach (Entity entity in entities)
                {
                    if (entity.TryGetComponent(out PrefabGUID prefab))
                    {
                        ResourcePrefabs.Add(prefab);
                    }
                }

                _craftAndGather = true;

                _itemQuery.Dispose();
                _resourceQuery.Dispose();
            }

            _targetCache = Queries.GetEntitiesEnumerable(_unitQuery, (int)TargetType.Kill)
                .GroupBy(entity => entity.Read<PrefabGUID>())
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        if (_targetCache.TryGetValue(group.Key, out var existingSet))
                        {
                            existingSet.Clear();
                            existingSet.UnionWith(group);
                            return existingSet;
                        }
                        else
                        {
                            return new HashSet<Entity>(group);
                        }
                    }
                );

            if (_targetCache.ContainsKey(_manticore)) _targetCache.Remove(_manticore);
            if (_targetCache.ContainsKey(_dracula)) _targetCache.Remove(_dracula);
            if (_targetCache.ContainsKey(_monster)) _targetCache.Remove(_monster);
            if (_targetCache.ContainsKey(_solarus)) _targetCache.Remove(_solarus);

            Dictionary<ulong, PlayerInfo> players = new(PlayerCache);
            foreach (PlayerInfo playerInfo in players.Values)
            {
                User user = playerInfo.User;
                ulong steamId = playerInfo.User.PlatformId;

                if (!ConfigService.LevelingSystem)
                {
                    Entity character = playerInfo.CharEntity.Has<Equipment>() ? playerInfo.CharEntity : Entity.Null;
                    if (!character.Exists()) continue;

                    RefreshQuests(user, steamId, (int)playerInfo.CharEntity.Read<Equipment>().GetFullLevel());
                }
                else if (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var xpData))
                {
                    RefreshQuests(user, steamId, xpData.Key);
                }
            }

            _lastUpdate = DateTime.UtcNow;
            yield return _updateDelay;
        }
    }
}

