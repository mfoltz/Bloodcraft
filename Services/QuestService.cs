using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Quests.QuestSystem;
using static Bloodcraft.Utilities.EntityQueries;

namespace Bloodcraft.Services;
internal class QuestService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _leveling = ConfigService.LevelingSystem;

    const float START_DELAY = 30f;
    const float ROUTINE_DELAY = 60f;

    static readonly WaitForSeconds _startDelay = new(START_DELAY);
    static readonly WaitForSeconds _routineDelay = new(ROUTINE_DELAY);

    public static DateTime _lastUpdate;

    static readonly ComponentType[] _vBloodUnitAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ComponentType.ReadOnly(Il2CppType.Of<VBloodConsumeSource>()),
        ComponentType.ReadOnly(Il2CppType.Of<VBloodUnit>())
    ];

    static readonly ComponentType[] _targetUnitAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
        ComponentType.ReadOnly(Il2CppType.Of<Movement>())
    ];

    static readonly ComponentType[] _craftableItemAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ComponentType.ReadOnly(Il2CppType.Of<ItemData>()),
        ComponentType.ReadOnly(Il2CppType.Of<InventoryItem>())
    ];

    static readonly ComponentType[] _harvestableResourceAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ComponentType.ReadOnly(Il2CppType.Of<Health>()),
        ComponentType.ReadOnly(Il2CppType.Of<DurabilityTarget>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
        ComponentType.ReadOnly(Il2CppType.Of<DropTable>()),
        ComponentType.ReadOnly(Il2CppType.Of<DropTableBuffer>()),
        ComponentType.ReadOnly(Il2CppType.Of<YieldResourcesOnDamageTaken>())
    ];

    static readonly ComponentType[] _targetUnitNoneComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<SpawnTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<Minion>()),
        ComponentType.ReadOnly(Il2CppType.Of<DestroyOnSpawn>())
    ];

    static readonly ComponentType[] _craftableItemNoneComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<ShatteredItem>()),
        ComponentType.ReadOnly(Il2CppType.Of<UpgradeableLegendaryItem>())
    ];

    static EntityQuery _vBloodUnitQuery;
    static EntityQuery _targetUnitQuery;
    static EntityQuery _craftableItemQuery;
    static EntityQuery _harvestableResourceQuery;

    static readonly ConcurrentDictionary<PrefabGUID, HashSet<Entity>> _targetCache = [];
    public static IReadOnlyDictionary<PrefabGUID, HashSet<Entity>> TargetCache => _targetCache;

    static readonly List<PrefabGUID> _shardBearers = 
    [
        Prefabs.CHAR_Manticore_VBlood,
        Prefabs.CHAR_ChurchOfLight_Paladin_VBlood,
        Prefabs.CHAR_Gloomrot_Monster_VBlood,
        Prefabs.CHAR_Vampire_Dracula_VBlood
    ];

    static readonly HashSet<string> _filteredTargetUnits =
    [
        "Trader",
            "HostileVillager",
            "TombSummon",
            "StatueSpawn",
            "SmiteOrb",
            "CardinalAide",
            "GateBoss",
            "DraculaMinion",
            "Summon",
            "Minion",
            "Chieftain",
            "ConstrainingPole",
            "Horse",
            "EnchantedCross",
            "DivineAngel",
            "FallenAngel",
            "FarbaneSuprise",
            "Withered",
            "Servant",
            "Spider_Melee",
            "Spider_Range",
            "GroundSword",
            "FloatingWeapon",
            "Airborne"
    ];

    static readonly HashSet<string> _filteredCraftableItems =
    [
        "Item_Cloak",
        "BloodKey_T01",
        "NewBag",
        "Miners",
        "WoodCutter",
        "ShadowMatter",
        "T0X",
        "Heart_T",
        "Water_T",
        "FakeItem",
        "PrisonPotion",
        "Dracula",
        "Consumable_Empty",
        "Reaper_T02",
        "Slashers_T02",
        "FishingPole",
        "Disguise",
        "Canister",
        "Trippy",
        "Eat_Rat",
        "Irradiant",
        "Slashers_T01",
        "Slashers_T03",
        "Slashers_T04",
        "Reaper_T03",
        "Reaper_T04",
        "Reaper_T01",
        "GarlicResistance",
        "T01_Bone"
    ];

    static readonly HashSet<string> _filteredHarvestableResources =
    [
        "Item_Ingredient_Crystal",
        "Coal",
        "Thistle"
    ];

    static bool _shouldReset = ConfigService.EliteShardBearers;
    static bool _craftables = true;
    static bool _harvestables = true;
    public QuestService()
    {
        _vBloodUnitQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _vBloodUnitAllComponents,
            None = _targetUnitNoneComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        _targetUnitQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _targetUnitAllComponents,
            None = _targetUnitNoneComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        _craftableItemQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _craftableItemAllComponents,
            None = _craftableItemNoneComponents,
            Options = EntityQueryOptions.IncludeSpawnTag
        });

        _harvestableResourceQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _harvestableResourceAllComponents,
            Options = EntityQueryOptions.IncludeSpawnTag
        });

        Configuration.InitializeQuestRewardItems();
        QuestServiceRoutine().Start();
    }

    static readonly int[] _typeIndices = [0];
    static IEnumerator QuestServiceRoutine()
    {
        if (_shouldReset) ResetShardBearers().Start();
        if (_craftables) InitializeCraftables().Start();
        if (_harvestables) InitializeHarvestables().Start();

        while (true)
        {
            yield return QueryResultStreamAsync(
                _targetUnitQuery,
                _targetUnitAllComponents,
                _typeIndices,
                stream =>
                {
                    try
                    {
                        Dictionary<PrefabGUID, HashSet<Entity>> prefabGuidEntityGroups = [];

                        using (stream)
                        {
                            foreach (QueryResult result in stream.GetResults())
                            {
                                PrefabGUID prefabGuid = result.ResolveComponentData<PrefabGUID>();
                                string prefabName = prefabGuid.GetPrefabName();

                                if (_filteredTargetUnits.Any(unit => prefabName.Contains(unit, StringComparison.OrdinalIgnoreCase)))
                                    continue;

                                if (!prefabGuidEntityGroups.TryGetValue(prefabGuid, out var entities))
                                {
                                    entities = [];
                                    prefabGuidEntityGroups[prefabGuid] = entities;
                                }

                                entities.Add(result.Entity);
                            }

                            foreach (var keyValuePair in prefabGuidEntityGroups)
                            {
                                _targetCache.AddOrUpdate(
                                    keyValuePair.Key,
                                    _ => keyValuePair.Value,
                                    (_, existingSet) =>
                                    {
                                        existingSet.Clear();
                                        existingSet.UnionWith(keyValuePair.Value);
                                        return existingSet;
                                    }
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Core.Log.LogWarning($"[QuestService] QuestServiceRoutine() - {ex}");
                    }
                }
            );

            foreach (PrefabGUID prefabGuid in _shardBearers)
            {
                _targetCache.TryRemove(prefabGuid, out var _);
            }

            foreach (var playerInfoPair in SteamIdPlayerInfoCache)
            {
                ulong steamId = playerInfoPair.Key;
                PlayerInfo playerInfo = playerInfoPair.Value;

                Entity userEntity = playerInfo.CharEntity;
                User user = playerInfo.User;

                if (!_leveling)
                {
                    RefreshQuests(user, steamId, Progression.GetSimulatedLevel(userEntity));
                }
                else if (_leveling && steamId.TryGetPlayerExperience(out var xpData))
                {
                    RefreshQuests(user, steamId, xpData.Key);
                }

                yield return null;
            }

            _lastUpdate = DateTime.UtcNow;
            yield return _routineDelay;
        }
    }
    static IEnumerator ResetShardBearers()
    {
        yield return QueryResultStreamAsync(
            _vBloodUnitQuery,
            _vBloodUnitAllComponents,
            _typeIndices,
            stream =>
            {
                try
                {
                    using (stream)
                    {
                        foreach (QueryResult result in stream.GetResults())
                        {
                            PrefabGUID prefabGuid = result.ResolveComponentData<PrefabGUID>();

                            if (_shardBearers.Contains(prefabGuid)) result.Entity.TryDestroy();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"[QuestService] ResetShardBearers() - {ex}");
                }
            }
        );

        _shouldReset = false;
    }
    static IEnumerator InitializeCraftables()
    {
        yield return QueryResultStreamAsync(
            _craftableItemQuery,
            _craftableItemAllComponents,
            _typeIndices,
            stream =>
            {
                try
                {
                    using (stream)
                    {
                        foreach (QueryResult result in stream.GetResults())
                        {
                            Entity entity = result.Entity;
                            PrefabGUID prefabGuid = result.ResolveComponentData<PrefabGUID>();
                            string prefabName = prefabGuid.GetPrefabName();

                            if (_filteredCraftableItems.Any(item => prefabName.Contains(item, StringComparison.OrdinalIgnoreCase))) continue;

                            if (entity.Has<Equippable>() && entity.TryGetComponent(out Salvageable salvageable))
                            {
                                if (salvageable.RecipeGUID.HasValue()) CraftPrefabs.Add(prefabGuid);
                            }
                            else if (entity.Has<ConsumableCondition>())
                            {
                                CraftPrefabs.Add(prefabGuid);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"[QuestService] InitializeCraftables() - {ex}");
                }
            }
        );

        _craftables = false;
    }
    static IEnumerator InitializeHarvestables()
    {
        yield return QueryResultStreamAsync(
            _harvestableResourceQuery,
            _harvestableResourceAllComponents,
            _typeIndices,
            stream =>
            {
                try
                {
                    using (stream)
                    {
                        foreach (QueryResult result in stream.GetResults())
                        {
                            Entity entity = result.Entity;
                            PrefabGUID prefabGuid = result.ResolveComponentData<PrefabGUID>();
                            string prefabName = prefabGuid.GetPrefabName();

                            if (!entity.Has<DropTableBuffer>()) continue;
                            else if (_filteredHarvestableResources.Any(resource => prefabName.Contains(resource, StringComparison.OrdinalIgnoreCase))) continue;
                            else if (prefabGuid.HasValue()) ResourcePrefabs.Add(prefabGuid);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogWarning($"[QuestService] InitializeHarvestables() - {ex}");
                }
            }
        );

        _harvestables = false;
    }
}
