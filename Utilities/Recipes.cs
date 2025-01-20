using Bloodcraft.Services;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class Recipes
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly PrefabGUID _advancedGrinder = new(-178579946); // vampiric dust
    static readonly PrefabGUID _advancedFurnace = new(-222851985); // silver ingot
    static readonly PrefabGUID _fabricator = new(-465055967);      // copper wires, iron body
    static readonly PrefabGUID _shardExtractor = new(1794206684);  // shards, probably :p

    static readonly PrefabGUID _refinementInventoryLarge = new(1436956144);

    static readonly PrefabGUID _ironBodyRecipe = new(-1270503528);
    static readonly PrefabGUID _vampiricDustRecipe = new(311920560);
    static readonly PrefabGUID _copperWiresRecipe = new(-2031309726);
    static readonly PrefabGUID _silverIngotRecipe = new(-1633898285);
    static readonly PrefabGUID _fakeFlowerRecipe = new(-2095604835);
    static readonly PrefabGUID _chargedBatteryRecipe = new(-40415372);

    static readonly PrefabGUID _plantThistle = new(-598100816);
    static readonly PrefabGUID _batteryCharge = new(-77555820);
    static readonly PrefabGUID _primalEssence = new(1566989408);
    static readonly PrefabGUID _copperWires = new(-456161884);
    static readonly PrefabGUID _itemBuildingEMP = new(-1447213995);
    static readonly PrefabGUID _depletedBattery = new(1270271716);

    static readonly PrefabGUID _demonFragment = new(-77477508);

    static readonly PrefabGUID _extractorInventory = new(-1814907421);
    static readonly PrefabGUID _extractShardRecipe = new(1743327679);
    static readonly PrefabGUID _itemJewelTemplate = new(1075994038);
    static readonly PrefabGUID _manticoreRelic = new(-222860772);

    static readonly PrefabGUID _solarusShardRecipe = new(-958598508);
    static readonly PrefabGUID _monsterShardRecipe = new(1791150988);
    static readonly PrefabGUID _manticoreShardRecipe = new(-111826090);
    static readonly PrefabGUID _draculaShardRecipe = new(-414358988);

    static readonly List<PrefabGUID> _shardRecipes =
    [
        _solarusShardRecipe,
        _monsterShardRecipe,
        _manticoreShardRecipe,
        _draculaShardRecipe
    ];
    public static void AddExtraRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_itemBuildingEMP];

        var recipeRequirementBuffer = EntityManager.AddBuffer<RecipeRequirementBuffer>(itemEntity);
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 5 });
        
        if (!itemEntity.Has<Salvageable>())
        {
            itemEntity.Add<Salvageable>();

            itemEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 60f;
            });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_primalEssence, out Entity prefabEntity))
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 30f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            var primalSalvageBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            primalSalvageBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 5 });
        }

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_copperWires, out prefabEntity)) // check on live and see if they've always done this or not with the half duration thing
        {
            if (!prefabEntity.Has<Salvageable>())
            {
                prefabEntity.Add<Salvageable>();
            }

            prefabEntity.With((ref Salvageable salvageable) =>
            {
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            var wiresSalvageBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            wiresSalvageBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
        }

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

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_shardExtractor, out prefabEntity))
        {
            prefabEntity.With((ref Refinementstation refinementstation) =>
            {
                refinementstation.InventoryPrefabGuid = _refinementInventoryLarge;
            });
        }

        /*
        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShardRecipe];

        if (!recipeEntity.Has<RecipeLinkBuffer>())
        {
            // EntityManager.AddBuffer<RecipeLinkBuffer>(recipeEntity);
        }

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

        var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _demonFragment, Amount = 1 }); // if fragment works can try jewel again but need to reduce uncertainty, then link buffer as well
        */

        /*
        var recipeLinkBuffer = recipeEntity.ReadBuffer<RecipeLinkBuffer>();
        foreach (PrefabGUID shardRecipe in _shardRecipes)
        {
            recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shardRecipe];

            recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
            recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

            PrefabGUID shardPrefabGuid = recipeOutputBuffer[0].Guid;

            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = recipeOutputBuffer[0].Guid, Amount = 1 });
            recipeOutputBuffer.RemoveAt(0);     
            
            recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });
            recipeLinkBuffer.Add(new RecipeLinkBuffer { Guid = shardRecipe });
        }
        

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            // recipeData.CraftDuration = 180f;
            // recipeData.AlwaysUnlocked = true;
            // recipeData.HideInStation = false;
            // recipeData.HudSortingOrder = 0;
            recipeData.IgnoreServerSettings = false;
        });

        recipeMap[_extractShardRecipe] = recipeEntity.Read<RecipeData>();
        */

        Entity stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDustRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_vampiricDustRecipe] = recipeEntity.Read<RecipeData>();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _vampiricDustRecipe, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWiresRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
            // recipeData.CraftDuration = 20f;
        });

        recipeMap[_copperWiresRecipe] = recipeEntity.Read<RecipeData>();

        /*
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_ironBodyRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_ironBodyRecipe] = recipeEntity.Read<RecipeData>();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_silverIngotRecipe];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_silverIngotRecipe] = recipeEntity.Read<RecipeData>();
        */

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_chargedBatteryRecipe];

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _batteryCharge, Amount = 1 });
        
        recipeEntity.With((ref RecipeData recipeData) =>
        {
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
            var recipeRequirement = refinementRecipeBuffer[0];
            recipeRequirement.Guid = _plantThistle;

            refinementRecipeBuffer[0] = recipeRequirement;
        }

        GameDataSystem.RegisterRecipes();
        GameDataSystem.RegisterItems();
        PrefabCollectionSystem.RegisterGameData();
    }
}

