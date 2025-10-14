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

    public static void ModifyRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        ConfigureEmpBuildingItem();
        ConfigurePrimalStygianRecipe(recipeMap);
        ConfigureBloodCrystalRecipe(recipeMap);
        RemoveRecipeLinks(
            PrefabCollectionSystem._PrefabGuidToEntityMap[_fakeGemdustRecipe],
            _primalStygianRecipe,
            _bloodCrystalRecipe);

        ModifyMiscItem(_primalEssence, PrefabGUIDs.Recipe_CastleUpkeep_T02, 10f, (_batteryCharge, 5));
        ModifyMiscItem(_copperWires, PrefabGUIDs.Recipe_CastleUpkeep_T02, 15f, (_batteryCharge, 1));
        ModifyMiscItem(_batHide, PrefabGUIDs.Recipe_CastleUpkeep_T02, 15f, (_lesserStygian, 3), (_bloodEssence, 5));
        ModifyMiscItem(_goldOre, PrefabGUIDs.Recipe_CastleUpkeep_T02, 10f, (_goldJewelry, 2));
        ModifyMiscItem(
            _radiantFibre,
            PrefabGUIDs.Recipe_CastleUpkeep_T02,
            10f,
            (PrefabGUIDs.Item_Ingredient_Gemdust, 8),
            (PrefabGUIDs.Item_Ingredient_Plant_PlantFiber, 16),
            (PrefabGUIDs.Item_Ingredient_Pollen, 24));

        RemoveSalvageableAndRequirements(_batteryCharge);

        ConfigureExtractShardRecipeIfJewelValid();
        IncreaseShardContainerSlots();
        ConfigureAdvancedGrinder(recipeMap);
        RemoveRefinementRecipes(
            PrefabCollectionSystem._PrefabGuidToEntityMap[_primitiveGrinder],
            _primalStygianRecipe,
            _bloodCrystalRecipe);
        ConfigureFabricator(recipeMap);
        FixFakeFlowerRecipe();
        ConfigureGemCuttingTable();
        ConfigureAdvancedBloodPress();

        GameDataSystem.RegisterRecipes();
        GameDataSystem.RegisterItems();
        PrefabCollectionSystem.RegisterGameData();
    }

    /// <summary>
    /// Adds the <see cref="Salvageable"/> component if missing and configures its fields.
    /// </summary>
    /// <param name="entity">The entity to modify.</param>
    /// <param name="recipeGuid">The recipe GUID to associate with salvaging.</param>
    /// <param name="salvageTimer">The salvage timer to apply.</param>
    private static void EnsureSalvageable(Entity entity, PrefabGUID recipeGuid, float salvageTimer)
    {
        if (!entity.Has<Salvageable>())
        {
            entity.Add<Salvageable>();
        }

        entity.With((ref Salvageable salvageable) =>
        {
            salvageable.RecipeGUID = recipeGuid;
            salvageable.SalvageFactor = 1f;
            salvageable.SalvageTimer = salvageTimer;
        });
    }

    /// <summary>
    /// Ensures the entity has a <see cref="RecipeRequirementBuffer"/> and appends the supplied requirements.
    /// </summary>
    /// <param name="entity">The entity whose buffer to update.</param>
    /// <param name="requirements">The requirements to add.</param>
    private static void AddRecipeRequirements(Entity entity, params (PrefabGUID guid, int amount)[] requirements)
    {
        if (requirements == null || requirements.Length == 0)
        {
            return;
        }

        var buffer = entity.Has<RecipeRequirementBuffer>()
            ? entity.ReadBuffer<RecipeRequirementBuffer>()
            : entity.AddBuffer<RecipeRequirementBuffer>();

        foreach (var (guid, amount) in requirements)
        {
            buffer.Add(new RecipeRequirementBuffer { Guid = guid, Amount = amount });
        }
    }

    /// <summary>
    /// Updates recipe metadata such as craft duration, unlock state, and visibility.
    /// </summary>
    /// <param name="recipeEntity">The entity representing the recipe.</param>
    /// <param name="craftDuration">The craft duration in seconds.</param>
    /// <param name="alwaysUnlocked">Whether the recipe is always unlocked.</param>
    /// <param name="hideInStation">Whether the recipe should be hidden in the station UI.</param>
    /// <param name="hudSortingOrder">The HUD sorting order to apply.</param>
    private static void UpdateRecipeData(
        Entity recipeEntity,
        float craftDuration,
        bool alwaysUnlocked,
        bool hideInStation,
        int hudSortingOrder = 0)
    {
        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.CraftDuration = craftDuration;
            recipeData.AlwaysUnlocked = alwaysUnlocked;
            recipeData.HideInStation = hideInStation;
            recipeData.HudSortingOrder = hudSortingOrder;
        });
    }

    /// <summary>
    /// Adds the specified recipes to a station's <see cref="RefinementstationRecipesBuffer"/>.
    /// </summary>
    /// <param name="stationEntity">The station entity.</param>
    /// <param name="recipeGuids">The recipes to add.</param>
    private static void AddRefinementRecipes(Entity stationEntity, params PrefabGUID[] recipeGuids)
    {
        var buffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        foreach (var guid in recipeGuids)
        {
            buffer.Add(new RefinementstationRecipesBuffer
            {
                RecipeGuid = guid,
                Disabled = false,
                Unlocked = true
            });
        }
    }

    /// <summary>
    /// Removes the specified recipes from a station's <see cref="RefinementstationRecipesBuffer"/> if present.
    /// </summary>
    /// <param name="stationEntity">The station entity.</param>
    /// <param name="recipeGuids">The recipes to remove.</param>
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
    /// Removes matching entries from a recipe's <see cref="RecipeLinkBuffer"/>.
    /// </summary>
    /// <param name="entity">The recipe entity.</param>
    /// <param name="guids">The GUIDs to remove.</param>
    private static void RemoveRecipeLinks(Entity entity, params PrefabGUID[] guids)
    {
        if (!entity.Has<RecipeLinkBuffer>())
        {
            return;
        }

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

    /// <summary>
    /// Configures the EMP building item with salvage data and recipe requirements.
    /// </summary>
    private static void ConfigureEmpBuildingItem()
    {
        Entity itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_itemBuildingEMP];

        var requirementBuffer = itemEntity.Has<RecipeRequirementBuffer>()
            ? itemEntity.ReadBuffer<RecipeRequirementBuffer>()
            : EntityManager.AddBuffer<RecipeRequirementBuffer>(itemEntity);

        requirementBuffer.Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 2 });
        requirementBuffer.Add(new RecipeRequirementBuffer { Guid = _techScrap, Amount = 15 });

        EnsureSalvageable(itemEntity, PrefabGUIDs.Recipe_CastleUpkeep_T02, 20f);
    }

    /// <summary>
    /// Updates the Primal Stygian recipe requirements, outputs, and metadata.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigurePrimalStygianRecipe(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_primalStygianRecipe];

        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstRequirement = requirements[0];
        firstRequirement.Guid = _greaterStygian;
        firstRequirement.Amount = 8;
        requirements[0] = firstRequirement;

        var outputs = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        var firstOutput = outputs[0];
        firstOutput.Guid = _primalStygian;
        firstOutput.Amount = 1;
        outputs[0] = firstOutput;

        UpdateRecipeData(recipeEntity, 10f, true, false);
        recipeMap[_primalStygianRecipe] = recipeEntity.Read<RecipeData>();
    }

    /// <summary>
    /// Updates the Blood Crystal recipe requirements, outputs, and metadata.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigureBloodCrystalRecipe(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_bloodCrystalRecipe];

        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstRequirement = requirements[0];
        firstRequirement.Guid = _crystal;
        firstRequirement.Amount = 100;
        requirements[0] = firstRequirement;

        requirements.Add(new RecipeRequirementBuffer { Guid = _greaterEssence, Amount = 1 });

        var outputs = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        var firstOutput = outputs[0];
        firstOutput.Guid = _bloodCrystal;
        firstOutput.Amount = 100;
        outputs[0] = firstOutput;

        UpdateRecipeData(recipeEntity, 10f, true, false);
        recipeMap[_bloodCrystalRecipe] = recipeEntity.Read<RecipeData>();
    }

    /// <summary>
    /// Ensures miscellaneous items are salvageable and have the provided requirements.
    /// </summary>
    /// <param name="itemGuid">The item prefab to modify.</param>
    /// <param name="salvageRecipeGuid">The salvage recipe GUID to assign.</param>
    /// <param name="salvageTimer">The salvage timer to apply.</param>
    /// <param name="requirements">The recipe requirements to add.</param>
    private static void ModifyMiscItem(
        PrefabGUID itemGuid,
        PrefabGUID salvageRecipeGuid,
        float salvageTimer,
        params (PrefabGUID guid, int amount)[] requirements)
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(itemGuid, out Entity prefabEntity))
        {
            return;
        }

        EnsureSalvageable(prefabEntity, salvageRecipeGuid, salvageTimer);
        AddRecipeRequirements(prefabEntity, requirements);
    }

    /// <summary>
    /// Removes salvageable and requirement data from the prefab if present.
    /// </summary>
    /// <param name="itemGuid">The item prefab GUID.</param>
    private static void RemoveSalvageableAndRequirements(PrefabGUID itemGuid)
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(itemGuid, out var entity))
        {
            return;
        }

        if (entity.Has<Salvageable>())
        {
            entity.Remove<Salvageable>();
        }

        if (entity.Has<RecipeRequirementBuffer>())
        {
            entity.Remove<RecipeRequirementBuffer>();
        }
    }

    /// <summary>
    /// If the Primal Jewel requirement is valid, update the extract shard recipe and shard-specific recipes.
    /// </summary>
    private static void ConfigureExtractShardRecipeIfJewelValid()
    {
        if (!_primalJewelRequirement.HasValue())
        {
            return;
        }

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalJewelRequirement, out Entity itemPrefab)
            || !itemPrefab.Has<ItemData>())
        {
            Core.Log.LogWarning(
                "Primal Jewel requirement doesn't appear to be a valid item (missing itemData component), " +
                "correct this for the recipe to appear on gem cutting stations after placement!");
            return;
        }

        var extractRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShardRecipe];
        var requirementBuffer = extractRecipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstRequirement = requirementBuffer[0];
        firstRequirement.Guid = _primalJewelRequirement;
        requirementBuffer[0] = firstRequirement;

        var outputBuffer = extractRecipeEntity.ReadBuffer<RecipeOutputBuffer>();
        outputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });

        foreach (PrefabGUID shardRecipe in _shardRecipes)
        {
            var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shardRecipe];
            var shardRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
            shardRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _recipesToShards[shardRecipe], Amount = 1 });
            shardRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _primalJewelRequirement, Amount = 1 });
        }
    }

    /// <summary>
    /// Increases the inventory slot capacity for each shard container to 14.
    /// </summary>
    private static void IncreaseShardContainerSlots()
    {
        foreach (PrefabGUID shardContainer in _shardContainers)
        {
            if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(shardContainer, out var prefabEntity))
            {
                continue;
            }

            if (!prefabEntity.TryGetBuffer<InventoryInstanceElement>(out var instanceBuffer) || instanceBuffer.Length == 0)
            {
                continue;
            }

            var inventoryElement = instanceBuffer[0];
            inventoryElement.RestrictedCategory = (long)ItemCategory.ALL;
            inventoryElement.Slots = 14;
            inventoryElement.MaxSlots = 14;
            instanceBuffer[0] = inventoryElement;
        }
    }

    /// <summary>
    /// Enables Vampiric Dust on the advanced grinder and removes conflicting recipes.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigureAdvancedGrinder(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDustRecipe];
        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });
        recipeMap[_vampiricDustRecipe] = recipeEntity.Read<RecipeData>();

        var stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        AddRefinementRecipes(stationEntity, _vampiricDustRecipe);
        RemoveRefinementRecipes(stationEntity, _primalStygianRecipe, _bloodCrystalRecipe);
    }

    /// <summary>
    /// Adds copper wires and charged battery recipes to the fabricator.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigureFabricator(Dictionary<PrefabGUID, RecipeData> recipeMap)
    {
        var stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];

        var copperRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWiresRecipe];
        UpdateRecipeData(copperRecipeEntity, 10f, true, false);
        recipeMap[_copperWiresRecipe] = copperRecipeEntity.Read<RecipeData>();

        var chargedBatteryEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_chargedBatteryRecipe];
        var requirementBuffer = chargedBatteryEntity.ReadBuffer<RecipeRequirementBuffer>();
        requirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
        UpdateRecipeData(chargedBatteryEntity, 90f, true, false);
        recipeMap[_chargedBatteryRecipe] = chargedBatteryEntity.Read<RecipeData>();

        AddRefinementRecipes(stationEntity, _copperWiresRecipe, _chargedBatteryRecipe);
    }

    /// <summary>
    /// Adjusts the fake flower recipe to use plant thistle.
    /// </summary>
    private static void FixFakeFlowerRecipe()
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_fakeFlowerRecipe, out Entity recipePrefab))
        {
            return;
        }

        if (!recipePrefab.TryGetBuffer<RecipeRequirementBuffer>(out var requirementBuffer) || requirementBuffer.IsEmpty)
        {
            return;
        }

        var firstRequirement = requirementBuffer[0];
        firstRequirement.Guid = _plantThistle;
        requirementBuffer[0] = firstRequirement;
    }

    /// <summary>
    /// Adds Primal Stygian and shard extraction recipes to the gem cutting table.
    /// </summary>
    private static void ConfigureGemCuttingTable()
    {
        var stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_gemCuttingTable];
        AddRefinementRecipes(stationEntity, _primalStygianRecipe, _extractShardRecipe);
    }

    /// <summary>
    /// Adds the Blood Crystal recipe to the advanced blood press.
    /// </summary>
    private static void ConfigureAdvancedBloodPress()
    {
        var stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedBloodPress];
        AddRefinementRecipes(stationEntity, _bloodCrystalRecipe);
    }
}
