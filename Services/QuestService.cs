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

    const float START_DELAY = 10f;
    const float ROUTINE_DELAY = 60f;

    static readonly WaitForSeconds _startDelay = new(START_DELAY);
    static readonly WaitForSeconds _routineDelay = new(ROUTINE_DELAY);

    public static DateTime _lastUpdate;

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

    static QueryDesc _targetUnitQueryDesc;
    static QueryDesc _harvestableResourceQueryDesc;
    public static IReadOnlyDictionary<PrefabGUID, HashSet<Entity>> TargetCache => _targetCache;
    static readonly ConcurrentDictionary<PrefabGUID, HashSet<Entity>> _targetCache = [];

    public static readonly List<PrefabGUID> ShardBearers =
    [
        PrefabGUIDs.CHAR_Manticore_VBlood,
        PrefabGUIDs.CHAR_ChurchOfLight_Paladin_VBlood,
        PrefabGUIDs.CHAR_Gloomrot_Monster_VBlood,
        PrefabGUIDs.CHAR_Vampire_Dracula_VBlood,
        PrefabGUIDs.CHAR_Blackfang_Morgana_VBlood
    ];

    public static readonly HashSet<string> FilteredTargetUnits =
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
        "Airborne",
        "SpiritDouble",
        "ValyrCauldron",
        "EmeryGolem"
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

    static bool _craftables = true;
    static bool _harvestables = true;
    public QuestService()
    {
        _targetUnitQueryDesc = EntityManager.CreateQueryDesc(_targetUnitAllComponents, typeIndices: [0], options: EntityQueryOptions.IncludeDisabled);
        _harvestableResourceQueryDesc = EntityManager.CreateQueryDesc(allTypes: _harvestableResourceAllComponents, typeIndices: [0], options: EntityQueryOptions.IncludeSpawnTag);

        Configuration.GetQuestRewardItems();
        QuestServiceRoutine().Run();
    }
    static IEnumerator QuestServiceRoutine()
    {
        if (_craftables) InitializeCraftables();
        if (_harvestables) InitializeHarvestables().Run();

        while (true)
        {
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

                if (!recipeEntity.TryGetBuffer<RecipeOutputBuffer>(out var buffer) || buffer.IsEmpty)
                    continue;

                if (!prefabGuidEntities.TryGetValue(buffer[0].Guid, out Entity prefabEntity))
                    continue;

                prefabGuid = prefabEntity.GetPrefabGuid();
                string prefabName = prefabGuid.GetPrefabName();

                if (_filteredCraftableItems.Any(item => prefabName.Contains(item, StringComparison.CurrentCultureIgnoreCase)))
                    continue;

                if (prefabEntity.Has<Equippable>()
                    && prefabEntity.TryGetComponent(out Salvageable salvageable))
                {
                    if (salvageable.RecipeGUID.HasValue())
                    {
                        CraftPrefabs.Add(prefabGuid);
                    }
                }
                else if (prefabEntity.Has<ConsumableCondition>())
                {
                    CraftPrefabs.Add(prefabGuid);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError($"[QuestService] InitializeCraftables() - {ex}");
        }
        finally
        {
            prefabGuids.Dispose();
            recipeDatas.Dispose();
        }

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
                            else if (_filteredHarvestableResources.Any(resource => prefabName.Contains(resource, StringComparison.CurrentCultureIgnoreCase))) continue;
                            else if (prefabGuid.HasValue()) ResourcePrefabs.Add(prefabGuid);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Core.Log.LogError($"[QuestService] InitializeHarvestables() - {ex}");
                }
            }
        );

        _harvestables = false;
    }
}
