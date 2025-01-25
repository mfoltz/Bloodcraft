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
    static readonly PrefabGUID _gemCuttingTable = new(-21483617);  // extractor hates being alive so using this instead

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

    static readonly PrefabGUID _solarusShard = new(-21943750);
    static readonly PrefabGUID _monsterShard = new(-1581189572);
    static readonly PrefabGUID _manticoreShard = new(-1260254082);
    static readonly PrefabGUID _draculaShard = new(666638454);

    static readonly PrefabGUID _solarusShardContainer = new(-824445631);
    static readonly PrefabGUID _monsterShardContainer = new(-1996942061);
    static readonly PrefabGUID _manticoreShardContainer = new(653759442);
    static readonly PrefabGUID _draculaShardContainer = new(1495743889);

    static readonly List<PrefabGUID> _shardRecipes =
    [
        _solarusShardRecipe,
        _monsterShardRecipe,
        _manticoreShardRecipe,
        _draculaShardRecipe
    ];

    static readonly List<PrefabGUID> _soulShards = 
    [
        _solarusShard,
        _monsterShard,
        _manticoreShard,
        _draculaShard
    ];

    static readonly List<PrefabGUID> _shardContainers =
    [
        _solarusShardContainer,
        _monsterShardContainer,
        _manticoreShardContainer,
        _draculaShardContainer
    ];

    static readonly Dictionary<PrefabGUID, PrefabGUID> FamiliarSoulBoostItems = new()
    {
        { _solarusShard, _radiantFibre},
        { _monsterShard, _resonator},
        { _manticoreShard, _demonFragment},
        { _draculaShard, _pristineHeart}
    };
    public static void ModifyRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity itemEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_itemBuildingEMP];

        var recipeRequirementBuffer = EntityManager.AddBuffer<RecipeRequirementBuffer>(itemEntity);
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _depletedBattery, Amount = 5 });
        recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _techScrap, Amount = 25 });

        if (!itemEntity.Has<Salvageable>())
        {
            itemEntity.AddWith((ref Salvageable salvageable) =>
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
                salvageable.SalvageTimer = 5f;
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
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 10f;
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
                salvageable.RecipeGUID = PrefabGUID.Empty;
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
                salvageable.RecipeGUID = PrefabGUID.Empty;
                salvageable.SalvageFactor = 1f;
                salvageable.SalvageTimer = 20f;
            });

            if (!prefabEntity.Has<RecipeRequirementBuffer>())
            {
                prefabEntity.AddBuffer<RecipeRequirementBuffer>();
            }

            recipeRequirementBuffer = prefabEntity.ReadBuffer<RecipeRequirementBuffer>();
            recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = _goldJewelry, Amount = 2 });
        }

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

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_batteryCharge, out prefabEntity))
        {
            if (prefabEntity.Has<Salvageable>()) prefabEntity.Remove<Salvageable>();
            if (prefabEntity.Has<RecipeRequirementBuffer>()) prefabEntity.Remove<RecipeRequirementBuffer>();
        }

        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShardRecipe];
       
        if (!recipeEntity.Has<RecipeLinkBuffer>())
        {
            EntityManager.AddBuffer<RecipeLinkBuffer>(recipeEntity);
        }

        recipeRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();

        var recipeOutputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        recipeOutputBuffer.Add(new RecipeOutputBuffer { Guid = _itemJewelTemplate, Amount = 1 });  

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
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
            recipeData.IgnoreServerSettings = false;
            recipeData.CraftDuration = 10f;
        });

        recipeMap[_extractShardRecipe] = recipeEntity.Read<RecipeData>();
        
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
            if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(soulShard, out prefabEntity)
                && FamiliarSoulBoostItems.TryGetValue(soulShard, out PrefabGUID boostItem))
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
                recipeRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = boostItem, Amount = 1 });
            }
        }
        */

        foreach (PrefabGUID shardContainer in _shardContainers)
        {
            if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(shardContainer, out prefabEntity) 
                && prefabEntity.TryGetBuffer<InventoryInstanceElement>(out var instanceBuffer))
            {
                InventoryInstanceElement inventoryInstanceElement = instanceBuffer[0];

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

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_gemCuttingTable];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _extractShardRecipe, Disabled = false, Unlocked = true });

        GameDataSystem.RegisterRecipes();
        GameDataSystem.RegisterItems();
        PrefabCollectionSystem.RegisterGameData();
    }
}

