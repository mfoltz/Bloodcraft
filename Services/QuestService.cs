using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Quests.QuestSystem;
using static Bloodcraft.Utilities.EntityQueries;

namespace Bloodcraft.Services;
internal class QuestService
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

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
        ComponentType.ReadOnly(Il2CppType.Of<Health>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitStats>()),
        ComponentType.ReadOnly(Il2CppType.Of<Movement>()),
        ComponentType.ReadOnly(Il2CppType.Of<AbilityBar_Server>()),
        ComponentType.ReadOnly(Il2CppType.Of<AbilityBar_Shared>()),
        ComponentType.ReadOnly(Il2CppType.Of<AggroConsumer>())

    ];

    /*
    static readonly ComponentType[] _craftableItemAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ComponentType.ReadOnly(Il2CppType.Of<RecipeData>()),
        ComponentType.ReadOnly(Il2CppType.Of<RecipeRequirementBuffer>()),
        ComponentType.ReadOnly(Il2CppType.Of<RecipeOutputBuffer>())
    ];

    static readonly ComponentType[] _craftableItemAnyComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Equippable>()),
        ComponentType.ReadOnly(Il2CppType.Of<ConsumableCondition>()),
        ComponentType.ReadOnly(Il2CppType.Of<Salvageable>())
    ];
    */

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
        new(Il2CppType.Of<SpawnTag>()),
        new(Il2CppType.Of<Minion>()),
        new(Il2CppType.Of<DestroyOnSpawn>())
    ];

    /*
    static readonly ComponentType[] _craftableItemNoneComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<ShatteredItem>()),
        ComponentType.ReadOnly(Il2CppType.Of<UpgradeableLegendaryItem>())
    ];
    */

    static QueryDesc _vBloodUnitQueryDesc;
    static QueryDesc _targetUnitQueryDesc;
    // static QueryDesc _craftableItemQueryDesc;
    static QueryDesc _harvestableResourceQueryDesc;

    static readonly ConcurrentDictionary<PrefabGUID, HashSet<Entity>> _targetCache = [];
    public static IReadOnlyDictionary<PrefabGUID, HashSet<Entity>> TargetCache => _targetCache;

    static readonly List<PrefabGUID> _shardBearers = 
    [
        PrefabGUIDs.CHAR_Manticore_VBlood,
        PrefabGUIDs.CHAR_ChurchOfLight_Paladin_VBlood,
        PrefabGUIDs.CHAR_Gloomrot_Monster_VBlood,
        // PrefabGUIDs.CHAR_Vampire_Dracula_VBlood,
        PrefabGUIDs.CHAR_Blackfang_Morgana_VBlood
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
        /*
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
        */

        _vBloodUnitQueryDesc = EntityManager.CreateQueryDesc(_vBloodUnitAllComponents, typeIndices: _typeIndices, options: EntityQueryOptions.IncludeDisabled);
        _targetUnitQueryDesc = EntityManager.CreateQueryDesc(_targetUnitAllComponents, typeIndices: _typeIndices, options: EntityQueryOptions.IncludeDisabled);
        // _craftableItemQueryDesc = EntityManager.CreateQueryDesc(_craftableItemAllComponents, typeIndices: [0, 1], options: EntityQueryOptions.IncludeSpawnTag);
        _harvestableResourceQueryDesc = EntityManager.CreateQueryDesc(allTypes: _harvestableResourceAllComponents, typeIndices: _typeIndices, options: EntityQueryOptions.IncludeSpawnTag);

        Configuration.GetQuestRewardItems();
        QuestServiceRoutine().Start();
    }

    static readonly int[] _typeIndices = [0];
    static IEnumerator QuestServiceRoutine()
    {
        if (_shouldReset) ResetShardBearers().Start();
        if (_craftables) InitializeCraftables();
        if (_harvestables) InitializeHarvestables().Start();

        while (true)
        {
            yield return QueryResultStreamAsync(
                _targetUnitQueryDesc,
                stream =>
                {
                    try
                    {
                        Dictionary<PrefabGUID, HashSet<Entity>> prefabGuidEntityGroups = [];
                        using (stream)
                        {
                            ComponentLookup<Minion> minionLookup = EntityManager.GetComponentLookup<Minion>(true);
                            ComponentLookup<DestroyOnSpawn> destroyOnSpawnLookup = EntityManager.GetComponentLookup<DestroyOnSpawn>(true);
                            ComponentLookup<Trader> traderLookup = EntityManager.GetComponentLookup<Trader>(true);

                            foreach (QueryResult result in stream.GetResults())
                            {
                                if (minionLookup.HasComponent(result.Entity) 
                                    || destroyOnSpawnLookup.HasComponent(result.Entity) 
                                    || traderLookup.HasComponent(result.Entity)) continue;

                                PrefabGUID prefabGuid = result.ResolveComponentData<PrefabGUID>();
                                string prefabName = prefabGuid.GetPrefabName();

                                // Core.Log.LogWarning($"[QuestService] QuestServiceRoutine() - {prefabName}");

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

            Core.Log.LogWarning($"[QuestService] TargetCache - {TargetCache.Count}");

            _lastUpdate = DateTime.UtcNow;
            yield return _routineDelay;
        }
    }
    static IEnumerator ResetShardBearers()
    {
        yield return QueryResultStreamAsync(
            _vBloodUnitQueryDesc,
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
    static void InitializeCraftables()
    {
        var prefabGuidEntities = PrefabCollectionSystem._PrefabGuidToEntityMap;
        var recipeDataMap = GameDataSystem.RecipeHashLookupMap;

        var prefabGuids = recipeDataMap.GetKeyArray(Allocator.Temp);
        var recipeDatas = recipeDataMap.GetValueArray(Allocator.Temp);

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                PrefabGUID prefabGuid = prefabGuids[i];
                RecipeData recipeData = recipeDatas[i];
                Entity recipeEntity = recipeData.Entity;

                // Core.Log.LogWarning($"[QuestService] InitializeCraftables() - {prefabGuid.GetPrefabName()}");

                if (!recipeEntity.TryGetBuffer<RecipeOutputBuffer>(out var buffer) || buffer.IsEmpty)
                {
                    // Core.Log.LogWarning($"[QuestService] InitializeCraftables() - Empty buffer: {prefabGuid.GetPrefabName()}");
                    continue;
                }

                if (!prefabGuidEntities.TryGetValue(buffer[0].Guid, out Entity prefabEntity))
                {
                    // Core.Log.LogWarning($"[QuestService] InitializeCraftables() - Couldn't get item prefab from RecipeOutputBuffer: {prefabGuid.GetPrefabName()} | {(buffer.IsEmpty ? buffer.get_Item(0).Guid.GetPrefabName() : string.Empty)}");
                    continue;
                }

                prefabGuid = prefabEntity.GetPrefabGuid();
                string prefabName = prefabGuid.GetPrefabName();

                // Core.Log.LogWarning($"[QuestService] InitializeCraftables() - {prefabName}");

                if (_filteredCraftableItems.Any(item => prefabName.Contains(item, StringComparison.OrdinalIgnoreCase))) continue;

                if (prefabEntity.Has<Equippable>() && prefabEntity.TryGetComponent(out Salvageable salvageable))
                {
                    if (salvageable.RecipeGUID.HasValue())
                    {
                        // Core.Log.LogWarning($"[QuestService] Added to CraftPrefabs - {prefabName}");
                        CraftPrefabs.Add(prefabGuid);
                    }
                }
                else if (prefabEntity.Has<ConsumableCondition>())
                {
                    // Core.Log.LogWarning($"[QuestService] Added to CraftPrefabs - {prefabName}");
                    CraftPrefabs.Add(prefabGuid);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"[QuestService] InitializeCraftables() - {ex}");
        }
        finally
        {
            prefabGuids.Dispose();
            recipeDatas.Dispose();
        }

        /*
        yield return QueryResultStreamAsync(
            _craftableItemQueryDesc,
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
                            RecipeData recipeData = result.ResolveComponentData<RecipeData>();

                            Core.Log.LogWarning($"[QuestService] InitializeCraftables() - {prefabGuid.GetPrefabName()}");

                            if (!entity.TryGetBufferAccessor<RecipeOutputBuffer>(out var buffer) 
                            || buffer.IsEmpty || !prefabGuidEntities.TryGetValue(buffer[0].Guid, out Entity prefabEntity)) continue;

                            string prefabName = prefabEntity.GetPrefabGuid().GetPrefabName();

                            Core.Log.LogWarning($"[QuestService] InitializeCraftables() - {prefabName}");

                            if (_filteredCraftableItems.Any(item => prefabName.Contains(item, StringComparison.OrdinalIgnoreCase))) continue;

                            if (entity.Has<Equippable>() && entity.TryGetComponent(out Salvageable salvageable))
                            {
                                if (salvageable.RecipeGUID.HasValue())
                                {
                                    Core.Log.LogWarning($"[QuestService] Added to CraftPrefabs - {prefabName}");
                                    CraftPrefabs.Add(prefabGuid);
                                }
                            }
                            else if (entity.Has<ConsumableCondition>())
                            {
                                Core.Log.LogWarning($"[QuestService] Added to CraftPrefabs - {prefabName}");
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
        */

        Core.Log.LogWarning($"[QuestService] InitializeCraftables() - {CraftPrefabs.Count}");
        _craftables = false;
    }
    static IEnumerator InitializeHarvestables()
    {
        yield return QueryResultStreamAsync(
            _harvestableResourceQueryDesc,
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

        // Core.Log.LogWarning($"[QuestService] InitializeHarvestables() - {ResourcePrefabs.Count}");
        _harvestables = false;
    }
}
