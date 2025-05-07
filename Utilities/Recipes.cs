using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class Recipes // pending organization and refactoring, should also make able to be copy and pasted into Eclipse (make sure client does the parts needed on client but not server parts and vice versa)
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly PrefabGUID _primalJewelRequirement = new(ConfigService.PrimalJewelCost);

    static readonly PrefabGUID _advancedGrinder = new(-178579946);
    static readonly PrefabGUID _primitiveGrinder = new(-600683642);
    static readonly PrefabGUID _advancedFurnace = new(-222851985);
    static readonly PrefabGUID _fabricator = new(-465055967);      
    static readonly PrefabGUID _shardExtractor = new(1794206684);
    static readonly PrefabGUID _gemCuttingTable = new(-21483617);
    static readonly PrefabGUID _advancedBloodPress = new(-684391635);

    static readonly PrefabGUID _refinementInventoryLarge = new(1436956144);
    static readonly PrefabGUID _refinementInventorySmall = new(-534407618);
    static readonly PrefabGUID _extractorInventory = new(-1814907421);

    static readonly PrefabGUID _ironBodyRecipe = new(-1270503528);
    static readonly PrefabGUID _vampiricDustRecipe = new(311920560);
    static readonly PrefabGUID _copperWiresRecipe = new(-2031309726);
    static readonly PrefabGUID _silverIngotRecipe = new(-1633898285);
    static readonly PrefabGUID _fakeFlowerRecipe = new(-2095604835);
    static readonly PrefabGUID _chargedBatteryRecipe = new(-40415372);

    static readonly PrefabGUID _batHide = new(1262845777);
    static readonly PrefabGUID _lesserStygian = new(2103989354);
    static readonly PrefabGUID _bloodEssence = new(862477668);
    static readonly PrefabGUID _plantThistle = new(-598100816);
    static readonly PrefabGUID _batteryCharge = new(-77555820);
    static readonly PrefabGUID _techScrap = new(834864259);
    static readonly PrefabGUID _primalEssence = new(1566989408);
    static readonly PrefabGUID _copperWires = new(-456161884);
    static readonly PrefabGUID _itemBuildingEMP = new(-1447213995);
    static readonly PrefabGUID _depletedBattery = new(1270271716);
    static readonly PrefabGUID _itemJewelTemplate = new(1075994038);

    static readonly PrefabGUID _pristineHeart = new(-1413694594);
    static readonly PrefabGUID _radiantFibre = new(-182923609);
    static readonly PrefabGUID _resonator = new(-1629804427);
    static readonly PrefabGUID _document = new(1334469825);
    static readonly PrefabGUID _demonFragment = new(-77477508);
    static readonly PrefabGUID _magicalComponent = new(1488205677);
    static readonly PrefabGUID _tailoringComponent = new(828271620);
    static readonly PrefabGUID _gemGrindStone = new(2115367516);

    static readonly PrefabGUID _perfectAmethyst = new(-106283194);
    static readonly PrefabGUID _perfectEmerald = new(1354115931);
    static readonly PrefabGUID _perfectRuby = new(188653143);
    static readonly PrefabGUID _perfectSapphire = new(-2020212226);
    static readonly PrefabGUID _perfectTopaz = new(-1983566585);
    static readonly PrefabGUID _perfectMiststone = new(750542699);

    static readonly PrefabGUID _goldJewelry = new(-1749304196);
    static readonly PrefabGUID _goldIngotRecipe = new(-882942445);
    static readonly PrefabGUID _goldIngot = new(-1027710236);
    static readonly PrefabGUID _goldOre = new(660533034);
    static readonly PrefabGUID _processedSulphur = new(880699252);

    static readonly PrefabGUID _extractShardRecipe = new(1743327679);
    static readonly PrefabGUID _solarusShardRecipe = new(-958598508);
    static readonly PrefabGUID _monsterShardRecipe = new(1791150988);
    static readonly PrefabGUID _manticoreShardRecipe = new(-111826090);
    static readonly PrefabGUID _draculaShardRecipe = new(-414358988);
    static readonly PrefabGUID _morganaShardRecipe = PrefabGUIDs.Recipe_MagicSource_General_T09_Morgana;

    static readonly PrefabGUID _solarusShard = new(-21943750);
    static readonly PrefabGUID _monsterShard = new(-1581189572);
    static readonly PrefabGUID _manticoreShard = new(-1260254082);
    static readonly PrefabGUID _draculaShard = new(666638454);
    static readonly PrefabGUID _morganaShard = PrefabGUIDs.Item_MagicSource_SoulShard_Morgana;

    static readonly PrefabGUID _solarusShardContainer = new(-824445631);
    static readonly PrefabGUID _monsterShardContainer = new(-1996942061);
    static readonly PrefabGUID _manticoreShardContainer = new(653759442);
    static readonly PrefabGUID _draculaShardContainer = new(1495743889);
    static readonly PrefabGUID _morganaShardContainer = PrefabGUIDs.TM_Castle_Container_Specialized_Soulshards_Morgana;

    static readonly PrefabGUID _fakeGemdustRecipe = new(-1105418306);

    static readonly PrefabGUID _bloodCrystalRecipe = new(-597461125);  // using perfect topaz gemdust recipe for this
    static readonly PrefabGUID _crystal = new(-257494203);
    static readonly PrefabGUID _bloodCrystal = new(-1913156733);
    static readonly PrefabGUID _greaterEssence = new(271594022);

    static readonly PrefabGUID _primalStygianRecipe = new(-259193408); // using perfect amethyst gemdust recipe for this
    static readonly PrefabGUID _greaterStygian = new(576389135);
    static readonly PrefabGUID _primalStygian = new(28358550);

    static readonly List<PrefabGUID> _shardRecipes =
    [
        _solarusShardRecipe,
        _monsterShardRecipe,
        _manticoreShardRecipe,
        _draculaShardRecipe,
        _morganaShardRecipe
    ];

    static readonly List<PrefabGUID> _soulShards = 
    [
        _solarusShard,
        _monsterShard,
        _manticoreShard,
        _draculaShard,
        _morganaShard
    ];

    static readonly List<PrefabGUID> _shardContainers =
    [
        _solarusShardContainer,
        _monsterShardContainer,
        _manticoreShardContainer,
        _draculaShardContainer,
        _morganaShardContainer
    ];
    
    static readonly Dictionary<PrefabGUID, PrefabGUID> _recipesToShards = new()
    {
        { _solarusShardRecipe, _solarusShard },
        { _monsterShardRecipe, _monsterShard },
        { _manticoreShardRecipe, _manticoreShard },
        { _draculaShardRecipe, _draculaShard },
        { _morganaShardRecipe, _morganaShard }
    };
    public static void ModifyRecipes() // this is already difficult to keep track of, definitely merits refactoring before doing more here
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_itemBuildingEMP];

        var recipeRequirementBuffer = EntityManager.AddBuffer<RecipeRequirementBuffer>(itemEntity);
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 2 });
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _techScrap, Amount = 15 });

        if (!itemEntity.Has<Salvageable>())
        {
            itemEntity.AddWith((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 20f;
            });
        }

        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_primalStygianRecipe];

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

        RecipeRequirementBuffer recipeRequirement = recipeRequirementBuffer[0];
        recipeRequirement.Guid = _greaterStygian;
        recipeRequirement.Amount = 8;

        recipeRequirementBuffer[0] = recipeRequirement;

        var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();

        RecipeOutputBuffer recipeOutput = recipeOutputBuffer[0];
        recipeOutput.Guid = _primalStygian;
        recipeOutput.Amount = 1;

        recipeOutputBuffer[0] = recipeOutput;

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration = 10f;
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_primalStygianRecipe] = recipeEntity.Read<RecipeData>();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_bloodCrystalRecipe];

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

        recipeRequirement = recipeRequirementBuffer[0];
        recipeRequirement.Guid = _crystal;
        recipeRequirement.Amount = 100;

        recipeRequirementBuffer[0] = recipeRequirement;
        recipeRequirement.Guid = _greaterEssence;
        recipeRequirement.Amount = 1;
        recipeRequirementBuffer.Add(recipeRequirement);

        recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();

        recipeOutput = recipeOutputBuffer[0];
        recipeOutput.Guid = _bloodCrystal;
        recipeOutput.Amount = 100;

        recipeOutputBuffer[0] = recipeOutput;

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration = 10f;
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_bloodCrystalRecipe] = recipeEntity.Read<RecipeData>();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fakeGemdustRecipe];

        var recipeLinkBuffer = recipeEntity.ReadBuffer<RecipeLinkBuffer>();

        for (int i = recipeLinkBuffer.Length - 1; i >= 0; i--)
        {
            RecipeLinkBuffer entry = recipeLinkBuffer[i];

            if (entry.Guid.Equals(_primalStygianRecipe) || entry.Guid.Equals(_bloodCrystalRecipe))
            {
                recipeLinkBuffer.RemoveAt(i);
            }
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalEssence, out Entity prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 5 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_copperWires, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 15f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_batHide, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 15f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _lesserStygian, Amount = 3 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _bloodEssence, Amount = 5 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_goldOre, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02; // just needs a valid prefabGuid
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _goldJewelry, Amount = 2 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_radiantFibre, out prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUIDs.Recipe_CastleUpkeep_T02;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = PrefabGUIDs.Item_Ingredient_Gemdust, Amount = 8 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = PrefabGUIDs.Item_Ingredient_Plant_PlantFiber, Amount = 16 });
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = PrefabGUIDs.Item_Ingredient_Pollen, Amount = 24 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_batteryCharge, out prefabEntity))
        {
            if (prefabEntity.Has<Salvageable>()) prefabEntity.Remove<Salvageable>();
            if (prefabEntity.Has<RecipeRequirementBuffer>()) prefabEntity.Remove<RecipeRequirementBuffer>();
        }

        if (_primalJewelRequirement.HasValue() && PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalJewelRequirement, out Entity itemPrefab) && itemPrefab.Has<ItemData>())
        {
            Entity extractRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShardRecipe];
            recipeRequirementBuffer = extractRecipeEntity.ReadBuffer<RecipeRequirementBuffer>();

            RecipeRequirementBuffer extractRequirement = recipeRequirementBuffer[0];
            extractRequirement.Guid = _primalJewelRequirement;

            recipeRequirementBuffer[0] = extractRequirement;

            recipeOutputBuffer = extractRecipeEntity.ReadBuffer<RecipeOutputBuffer>();
            recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });

            foreach (PrefabGUID shardRecipe in _shardRecipes)
            {
                recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shardRecipe];
                PrefabGUID soulShard = _recipesToShards[shardRecipe];

                recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = soulShard, Amount = 1 });
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _primalJewelRequirement, Amount = 1 });
            }
        }
        else if (_primalJewelRequirement.HasValue())
        {
            Core.Log.LogWarning($"Primal Jewel requirement doesn't appear to be a valid item (missing itemData component), correct this for the recipe to appear on gem cutting stations after placement!");
        }

        foreach (PrefabGUID shardContainer in _shardContainers)
        {
            if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(shardContainer, out prefabEntity) 
                && prefabEntity.TryGetBuffer<InventoryInstanceElement>(out var instanceBuffer))
            {
                InventoryInstanceElement inventoryInstanceElement = instanceBuffer[0];

                inventoryInstanceElement.RestrictedCategory = (long)ItemCategory.ALL; // for modified shards
                inventoryInstanceElement.Slots = 14;
                inventoryInstanceElement.MaxSlots = 14;

                instanceBuffer[0] = inventoryInstanceElement;
            }
        }

        Entity stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDustRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_vampiricDustRecipe] = recipeEntity.Read<RecipeData>();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _vampiricDustRecipe, Disabled = false, Unlocked = true });

        for (int i = refinementBuffer.Length - 1; i >= 0; i--)
        {
            var entry = refinementBuffer[i];

            if (entry.RecipeGuid.Equals(_primalStygianRecipe) || entry.RecipeGuid.Equals(_bloodCrystalRecipe))
            {
                refinementBuffer.RemoveAt(i);
            }
        }

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_primitiveGrinder];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();

        for (int i = refinementBuffer.Length - 1; i >= 0; i--)
        {
            var entry = refinementBuffer[i];

            if (entry.RecipeGuid.Equals(_primalStygianRecipe) || entry.RecipeGuid.Equals(_bloodCrystalRecipe))
            {
                refinementBuffer.RemoveAt(i);
            }
        }

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWiresRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
            recipeData.CraftDuration = 10f;
        });

        recipeMap[_copperWiresRecipe] = recipeEntity.Read<RecipeData>();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_chargedBatteryRecipe];

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
        
        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration = 90f;
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_chargedBatteryRecipe] = recipeEntity.Read<RecipeData>();

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _copperWiresRecipe, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _chargedBatteryRecipe, Disabled = false, Unlocked = true });

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_fakeFlowerRecipe, out Entity recipePrefab) 
            && recipePrefab.TryGetBuffer<RecipeRequirementBuffer>(out var refinementRecipeBuffer) && !refinementRecipeBuffer.IsEmpty)
        {
            recipeRequirement = refinementRecipeBuffer[0];
            recipeRequirement.Guid = _plantThistle;

            refinementRecipeBuffer[0] = recipeRequirement;
        }

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_gemCuttingTable];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _primalStygianRecipe, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _extractShardRecipe, Disabled = false, Unlocked = true });

        /*
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _manticoreShardRecipe, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _solarusShardRecipe, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _monsterShardRecipe, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _draculaShardRecipe, Disabled = false, Unlocked = true });
        */

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedBloodPress];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _bloodCrystalRecipe, Disabled = false, Unlocked = true });

        GameDataSystem.RegisterRecipes();
        GameDataSystem.RegisterItems();
        PrefabCollectionSystem.RegisterGameData();
    }
}

/*
    /// <summary>
    /// Main entry point where we modify recipes and items.
    /// </summary>
    public static void ModifyRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        // 1) Configure the EMP-building item with new recipe requirements.
        ConfigureEMPBuildingItem();

        // 2) Adjust the Primal Stygian recipe data.
        ConfigurePrimalStygianRecipe(recipeMap);

        // 3) Adjust the Blood Crystal recipe data.
        ConfigureBloodCrystalRecipe(recipeMap);

        // 4) Remove any references to primal stygian/blood crystal from the fake gemdust recipe.
        RemoveRecipeLinks(
            PrefabCollectionSystem._PrefabGuidToEntityMap[_fakeGemdustRecipe],
            _primalStygianRecipe,
            _bloodCrystalRecipe
        );

        // 5) Misc items: add salvageable data and recipe requirements.
        ModifyMiscItem(_primalEssence, 5, (_batteryCharge, 5));
        ModifyMiscItem(_copperWires, 20, (_batteryCharge, 1));
        ModifyMiscItem(_batHide, 15, (_lesserStygian, 3), (_bloodEssence, 5));
        ModifyMiscItem(_goldOre, 20, (_goldJewelry, 2));

        // 6) Remove salvageable/requirement data from battery charge if it exists.
        RemoveSalvageableAndRequirements(_batteryCharge);

        // 7) If a valid Primal Jewel requirement item is found, update the extract shard recipe accordingly.
        ConfigureExtractShardRecipeIfJewelValid();

        // 8) Increase capacity on shard containers.
        IncreaseShardContainerSlots();

        // 9) Advanced Grinder: enable Vampiric Dust, remove primal stygian & blood crystal from this station.
        ConfigureAdvancedGrinder(recipeMap);

        // 10) Primitive Grinder: remove primal stygian & blood crystal from this station as well.
        RemoveRefinementRecipes(
            PrefabCollectionSystem._PrefabGuidToEntityMap[_primitiveGrinder],
            _primalStygianRecipe,
            _bloodCrystalRecipe
        );

        // 11) Fabricator: add copper wires & charged battery recipes.
        ConfigureFabricator(recipeMap);

        // 12) Adjust the “fake flower” recipe’s requirement to use plant thistle.
        FixFakeFlowerRecipe();

        // 13) Gem cutting table: add primal stygian recipe, shard extraction, etc.
        ConfigureGemCuttingTable();

        // 14) Advanced Blood Press: add the blood crystal recipe.
        ConfigureAdvancedBloodPress();

        // 15) Final registration calls.
        GameDataSystem.RegisterRecipes();
        GameDataSystem.RegisterItems();
        PrefabCollectionSystem.RegisterGameData();
    }

    #region Private Helpers

    /// <summary>
    /// Adds the Salvageable component if missing, then sets its fields.
    /// </summary>
    private static void EnsureSalvageable(Entity entity, float salvageTimer)
    {
        if (!entity.Has<Salvageable>())
        {
            entity.Add<Salvageable>();
        }

        entity.With((ref Salvageable salvageable) =>
        {
            salvageable.RecipeGUID = PrefabGUID.Empty;
            salvageable.SalvageFactor = 1f;
            salvageable.SalvageTimer = salvageTimer;
        });
    }

    /// <summary>
    /// Ensures the entity has a RecipeRequirementBuffer, then adds each (guid, amount) pair.
    /// </summary>
    private static void AddRecipeRequirements(Entity entity, params (PrefabGUID guid, int amount)[] requirements)
    {
        if (!entity.Has<RecipeRequirementBuffer>())
        {
            entity.AddBuffer<RecipeRequirementBuffer>();
        }

        var buffer = entity.ReadBuffer<RecipeRequirementBuffer>();
        foreach (var (guid, amount) in requirements)
        {
            buffer.Add(new RecipeRequirementBuffer { Guid = guid, Amount = amount });
        }
    }

    /// <summary>
    /// Updates a recipe's base data: craft time, unlock state, station visibility, etc.
    /// </summary>
    private static void UpdateRecipeData(
        Entity recipeEntity,
        float craftDuration,
        bool alwaysUnlocked,
        bool hideInStation,
        int hudSortingOrder = 0)
    {
        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration   = craftDuration;
            recipeData.AlwaysUnlocked  = alwaysUnlocked;
            recipeData.HideInStation   = hideInStation;
            recipeData.HudSortingOrder = hudSortingOrder;
        });
    }

    /// <summary>
    /// Adds the specified recipes to the station's RefinementstationRecipesBuffer.
    /// </summary>
    private static void AddRefinementRecipes(Entity stationEntity, params PrefabGUID[] recipeGuids)
    {
        var buffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        foreach (var guid in recipeGuids)
        {
            buffer.Add(new RefinementstationRecipesBuffer
            {
                RecipeGuid = guid,
                Disabled   = false,
                Unlocked   = true
            });
        }
    }

    /// <summary>
    /// Removes the specified recipes from the station's RefinementstationRecipesBuffer, if present.
    /// </summary>
    private static void RemoveRefinementRecipes(Entity stationEntity, params PrefabGUID[] recipeGuids)
    {
        var buffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            var entry = buffer[i];
            foreach (PrefabGUID guid in recipeGuids)
            {
                if (entry.RecipeGuid.Equals(guid))
                {
                    buffer.RemoveAt(i);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Removes matching entries in RecipeLinkBuffer for the given guids.
    /// </summary>
    private static void RemoveRecipeLinks(Entity entity, params PrefabGUID[] guids)
    {
        if (!entity.Has<RecipeLinkBuffer>())
            return;

        var recipeLinkBuffer = entity.ReadBuffer<RecipeLinkBuffer>();
        for (int i = recipeLinkBuffer.Length - 1; i >= 0; i--)
        {
            var entry = recipeLinkBuffer[i];
            foreach (var guid in guids)
            {
                if (entry.Guid.Equals(guid))
                {
                    recipeLinkBuffer.RemoveAt(i);
                    break;
                }
            }
        }
    }

    #endregion

    #region Specific Configuration Steps

    /// <summary>
    /// Example: modifies the “EMP Building” item recipe requirements and salvageable data.
    /// </summary>
    private static void ConfigureEMPBuildingItem()
    {
        Entity itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_itemBuildingEMP];

        // Adds brand-new recipe requirement buffer with the needed entries.
        EntityManager.AddBuffer<RecipeRequirementBuffer>(itemEntity)
                     .Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 2 })
                     .Add(new RecipeRequirementBuffer { Guid = _techScrap,       Amount = 15 });

        // Ensure Salvageable info is present.
        if (!itemEntity.Has<Salvageable>())
        {
            itemEntity.AddWith((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID    = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer  = 10f;
            });
        }
    }

    /// <summary>
    /// Updates the Primal Stygian recipe requirements, outputs, and main RecipeData.
    /// </summary>
    private static void ConfigurePrimalStygianRecipe(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_primalStygianRecipe];

        // Modify the first requirement (index 0).
        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var requirement  = requirements[0];
        requirement.Guid    = _greaterStygian;
        requirement.Amount  = 8;
        requirements[0]     = requirement;

        // Modify the first output (index 0).
        var outputs = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        var output  = outputs[0];
        output.Guid   = _primalStygian;
        output.Amount = 1;
        outputs[0]    = output;

        // Update the recipe data and store in the map.
        UpdateRecipeData(recipeEntity, 10f, true, false, 0);
        recipeMap[_primalStygianRecipe] = recipeEntity.Read<RecipeData>();
    }

    /// <summary>
    /// Updates the Blood Crystal recipe requirements, outputs, and main RecipeData.
    /// </summary>
    private static void ConfigureBloodCrystalRecipe(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_bloodCrystalRecipe];

        // Modify index 0 requirement.
        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstReq     = requirements[0];
        firstReq.Guid    = _crystal;
        firstReq.Amount  = 100;
        requirements[0]  = firstReq;

        // Add an additional requirement for greater essence.
        requirements.Add(new RecipeRequirementBuffer { Guid = _greaterEssence, Amount = 1 });

        // Modify the output (index 0).
        var outputs  = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        var firstOut = outputs[0];
        firstOut.Guid   = _bloodCrystal;
        firstOut.Amount = 100;
        outputs[0]      = firstOut;

        // Update the recipe data and store in the map.
        UpdateRecipeData(recipeEntity, 10f, true, false, 0);
        recipeMap[_bloodCrystalRecipe] = recipeEntity.Read<RecipeData>();
    }

    /// <summary>
    /// For a given prefab, ensure it is salvageable (with a given timer), then add any new requirements.
    /// </summary>
    private static void ModifyMiscItem(PrefabGUID itemGuid, float salvageTimer, params (PrefabGUID guid, int amount)[] newRequirements)
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(itemGuid, out Entity prefabEntity))
            return;

        // Ensure salvageable.
        EnsureSalvageable(prefabEntity, salvageTimer);

        // Ensure it has a requirement buffer, then add new requirements.
        AddRecipeRequirements(prefabEntity, newRequirements);
    }

    /// <summary>
    /// Removes salvageable and requirement data from the prefab if present.
    /// </summary>
    private static void RemoveSalvageableAndRequirements(PrefabGUID itemGuid)
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(itemGuid, out var entity))
            return;

        if (entity.Has<Salvageable>())
            entity.Remove<Salvageable>();

        if (entity.Has<RecipeRequirementBuffer>())
            entity.Remove<RecipeRequirementBuffer>();
    }

    /// <summary>
    /// If the Primal Jewel Requirement is valid, update the extract shard recipe with it.
    /// </summary>
    private static void ConfigureExtractShardRecipeIfJewelValid()
    {
        if (!_primalJewelRequirement.HasValue())
            return;

        // Ensure the primal jewel requirement is a valid item with ItemData.
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalJewelRequirement, out Entity itemPrefab) 
            || !itemPrefab.Has<ItemData>())
        {
            Core.Log.LogWarning(
                "Primal Jewel requirement doesn't appear to be a valid item (missing ItemData), " +
                "correct this for the recipe to appear on gem cutting stations after placement!"
            );
            return;
        }

        // Update the extract shard recipe’s requirements and outputs.
        var extractRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShardRecipe];
        var reqBuffer           = extractRecipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstReq            = reqBuffer[0];

        firstReq.Guid   = _primalJewelRequirement;
        reqBuffer[0]    = firstReq;

        var outputBuffer = extractRecipeEntity.ReadBuffer<RecipeOutputBuffer>();
        outputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });
    }

    /// <summary>
    /// Increases the inventory slot capacity for each shard container to 14.
    /// </summary>
    private static void IncreaseShardContainerSlots()
    {
        foreach (var shardContainer in _shardContainers)
        {
            if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(shardContainer, out var prefabEntity))
                continue;

            if (!prefabEntity.TryGetBuffer<InventoryInstanceElement>(out var instanceBuffer) || instanceBuffer.Length == 0)
                continue;

            var inventoryElement = instanceBuffer[0];
            inventoryElement.Slots    = 14;
            inventoryElement.MaxSlots = 14;
            instanceBuffer[0]         = inventoryElement;
        }
    }

    /// <summary>
    /// Enables and configures Vampiric Dust on the advanced grinder, removes primal stygian & blood crystal from it.
    /// </summary>
    private static void ConfigureAdvancedGrinder(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        // 1) Update Vampiric Dust.
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDustRecipe];
        UpdateRecipeData(recipeEntity, craftDuration: 0f, alwaysUnlocked: true, hideInStation: false);
        recipeMap[_vampiricDustRecipe] = recipeEntity.Read<RecipeData>();

        // 2) Add it to advanced grinder.
        var stationEntity    = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(
            new RefinementstationRecipesBuffer
            {
                RecipeGuid = _vampiricDustRecipe,
                Disabled   = false,
                Unlocked   = true
            }
        );

        // 3) Remove primal stygian & blood crystal from advanced grinder.
        RemoveRefinementRecipes(stationEntity, _primalStygianRecipe, _bloodCrystalRecipe);
    }

    /// <summary>
    /// Adds copper wires and charged battery recipes to the fabricator.
    /// </summary>
    private static void ConfigureFabricator(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        var stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];

        // 1) Copper Wires recipe
        var copperRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWiresRecipe];
        UpdateRecipeData(copperRecipeEntity, 10f, true, false);
        recipeMap[_copperWiresRecipe] = copperRecipeEntity.Read<RecipeData>();

        // 2) Charged Battery recipe (add new requirement + update data)
        var chargedBatteryEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_chargedBatteryRecipe];
        var reqBuffer            = chargedBatteryEntity.ReadBuffer<RecipeRequirementBuffer>();
        reqBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });

        UpdateRecipeData(chargedBatteryEntity, 90f, true, false);
        recipeMap[_chargedBatteryRecipe] = chargedBatteryEntity.Read<RecipeData>();

        // 3) Register them both with the Fabricator station
        AddRefinementRecipes(
            stationEntity,
            _copperWiresRecipe,
            _chargedBatteryRecipe
        );
    }

    /// <summary>
    /// If we have a valid “fake flower” recipe, tweak its requirement to use thistle.
    /// </summary>
    private static void FixFakeFlowerRecipe()
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_fakeFlowerRecipe, out Entity recipePrefab))
            return;

        if (!recipePrefab.TryGetBuffer<RecipeRequirementBuffer>(out var refinementRecipeBuffer) || refinementRecipeBuffer.IsEmpty)
            return;

        var firstReq = refinementRecipeBuffer[0];
        firstReq.Guid = _plantThistle;
        refinementRecipeBuffer[0] = firstReq;
    }

    /// <summary>
    /// Adds several shard-related recipes to the gem cutting table station.
    /// </summary>
    private static void ConfigureGemCuttingTable()
    {
        var stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_gemCuttingTable];
        AddRefinementRecipes(
            stationEntity,
            _primalStygianRecipe,
            _extractShardRecipe,
            _manticoreShardRecipe,
            _solarusShardRecipe,
            _monsterShardRecipe,
            _draculaShardRecipe
        );
    }

    /// <summary>
    /// Adds Blood Crystal recipe to the advanced blood press station.
    /// </summary>
    private static void ConfigureAdvancedBloodPress()
    {
        var stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedBloodPress];
        AddRefinementRecipes(stationEntity, _bloodCrystalRecipe);
    }
*/

/*
Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShardRecipe];

if (!recipeEntity.Has<RecipeLinkBuffer>())
{
    EntityManager.AddBuffer<RecipeLinkBuffer>(recipeEntity);
}

recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

RecipeRequirementBuffer extractRequirement = recipeRequirementBuffer[0];
extractRequirement.Guid = _itemBuildingManticore;

recipeRequirementBuffer[0] = extractRequirement;

var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });  

var recipeLinkBuffer = recipeEntity.ReadBuffer<RecipeLinkBuffer>();

foreach (PrefabGUID shardRecipe in _shardRecipes)
{
    Entity shardRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shardRecipe];

    shardRecipeEntity.With((ref RecipeData recipeData) =>
    {
        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = true;
        recipeData.HudSortingOrder = 0;
        recipeData.IgnoreServerSettings = false;
        // recipeData.CraftDuration = 10f;
    });

    recipeMap[shardRecipe] = shardRecipeEntity.Read<RecipeData>();

    recipeOutputBuffer = shardRecipeEntity.ReadBuffer<RecipeOutputBuffer>();
    recipeRequirementBuffer = shardRecipeEntity.ReadBuffer<RecipeRequirementBuffer>();

    PrefabGUID shardPrefabGuid = recipeOutputBuffer[0].Guid;
    recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = recipeOutputBuffer[0].Guid, Amount = 1 });

    recipeOutputBuffer.Clear();     
    recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });
    recipeLinkBuffer.Add(new RecipeLinkBuffer { Guid = shardRecipe });
}

recipeEntity.With((ref RecipeData recipeData) =>
{
    recipeData.AlwaysUnlocked = true;
    recipeData.HideInStation = false;
    recipeData.HudSortingOrder = 0;
    recipeData.IgnoreServerSettings = false;
    recipeData.CraftDuration = 25f;
});

recipeMap[_extractShardRecipe] = recipeEntity.Read<RecipeData>();
*/

/*
recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_goldIngotRecipe];

if (!recipeEntity.Has<RecipeLinkBuffer>())
{
    recipeLinkBuffer = EntityManager.AddBuffer<RecipeLinkBuffer>(recipeEntity);

    if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_silverIngotRecipe, out prefabEntity)
        && prefabEntity.TryGetBuffer(out recipeRequirementBuffer) 
        && prefabEntity.TryGetBuffer(out recipeOutputBuffer))
    {
        RecipeRequirementBuffer recipeRequirement = recipeRequirementBuffer[0];

        recipeRequirement.Amount = 4;
        recipeRequirement.Guid = _goldOre;

        recipeRequirementBuffer[0] = recipeRequirement;
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _processedSulphur, Amount = 1 });

        RecipeOutputBuffer recipeOutput = recipeOutputBuffer[0];

        recipeOutput.Guid = _goldIngot;

        recipeOutputBuffer[0] = recipeOutput;
    }
}
*/

/*
foreach (PrefabGUID soulShard in _soulShards)
{
    if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(soulShard, out prefabEntity))
    {
        if (!prefabEntity.Has<Salvageable>())
        {
            prefabEntity.Add<Salvageable>();
        }

        prefabEntity.With((ref Salvageable salvageable) =>
        {
            salvageable.RecipeGUID = PrefabGUID.Empty;
            salvageable.SalvageFactor = 1f;
            salvageable.SalvageTimer = 15f;
        });           

        if (!prefabEntity.Has<RecipeRequirementBuffer>())
        {
            prefabEntity.AddBuffer<RecipeRequirementBuffer>();
        }

        recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _itemJewelTemplate, Amount = 1 });
    }
}
*/

/*
if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_refinementInventoryLarge, out prefabEntity))
{
    prefabEntity.With((ref RestrictedInventory restrictedInventory) =>
    {
        restrictedInventory.RestrictedItemCategory = ItemCategory.ALL;
    });

    var inventoryBuffer = prefabEntity.ReadBuffer<InventoryInstanceElement>();

    InventoryInstanceElement inventoryInstanceElement = inventoryBuffer[0];
    inventoryInstanceElement.RestrictedCategory = (long)ItemCategory.ALL;

    inventoryBuffer[0] = inventoryInstanceElement;
}
*/

/*
if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_refinementInventorySmall, out prefabEntity))
{
    prefabEntity.With((ref RestrictedInventory restrictedInventory) =>
    {
        restrictedInventory.RestrictedItemCategory = ItemCategory.ALL;
    });

    var inventoryBuffer = prefabEntity.ReadBuffer<InventoryInstanceElement>();

    InventoryInstanceElement inventoryInstanceElement = inventoryBuffer[0];
    inventoryInstanceElement.RestrictedCategory = (long)ItemCategory.ALL;

    inventoryBuffer[0] = inventoryInstanceElement;
}
*/