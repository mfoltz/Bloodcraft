using System;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class Recipes
{
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    private static class Requirements
    {
        public static PrefabGUID PrimalJewel { get; } = new(ConfigService.PrimalJewelCost);
    }

    private static class Stations
    {
        public static PrefabGUID AdvancedGrinder { get; } = new(-178579946);
        public static PrefabGUID PrimitiveGrinder { get; } = new(-600683642);
        public static PrefabGUID AdvancedFurnace { get; } = new(-222851985);
        public static PrefabGUID Fabricator { get; } = new(-465055967);
        public static PrefabGUID ShardExtractor { get; } = new(1794206684);
        public static PrefabGUID GemCuttingTable { get; } = new(-21483617);
        public static PrefabGUID AdvancedBloodPress { get; } = new(-684391635);
    }

    private static class Inventories
    {
        public static PrefabGUID RefinementLarge { get; } = new(1436956144);
        public static PrefabGUID RefinementSmall { get; } = new(-534407618);
        public static PrefabGUID Extractor { get; } = new(-1814907421);
    }

    private static class RecipeIds
    {
        public static PrefabGUID IronBody { get; } = new(-1270503528);
        public static PrefabGUID VampiricDust { get; } = new(311920560);
        public static PrefabGUID CopperWires { get; } = new(-2031309726);
        public static PrefabGUID SilverIngot { get; } = new(-1633898285);
        public static PrefabGUID FakeFlower { get; } = new(-2095604835);
        public static PrefabGUID ChargedBattery { get; } = new(-40415372);
        public static PrefabGUID GoldIngot { get; } = new(-882942445);
        public static PrefabGUID ExtractShard { get; } = new(1743327679);
        public static PrefabGUID SolarusShard { get; } = new(-958598508);
        public static PrefabGUID MonsterShard { get; } = new(1791150988);
        public static PrefabGUID ManticoreShard { get; } = new(-111826090);
        public static PrefabGUID DraculaShard { get; } = new(-414358988);
        public static PrefabGUID MorganaShard { get; } = PrefabGUIDs.Recipe_MagicSource_General_T09_Morgana;
        public static PrefabGUID FakeGemdust { get; } = new(-1105418306);
        public static PrefabGUID BloodCrystal { get; } = new(-597461125);  // using perfect topaz gemdust recipe for this
        public static PrefabGUID PrimalStygian { get; } = new(-259193408); // using perfect amethyst gemdust recipe for this
    }

    private static class Items
    {
        public static PrefabGUID BatHide { get; } = new(1262845777);
        public static PrefabGUID LesserStygian { get; } = new(2103989354);
        public static PrefabGUID BloodEssence { get; } = new(862477668);
        public static PrefabGUID PlantThistle { get; } = new(-598100816);
        public static PrefabGUID BatteryCharge { get; } = new(-77555820);
        public static PrefabGUID TechScrap { get; } = new(834864259);
        public static PrefabGUID PrimalEssence { get; } = new(1566989408);
        public static PrefabGUID CopperWires { get; } = new(-456161884);
        public static PrefabGUID EmpBuilding { get; } = new(-1447213995);
        public static PrefabGUID DepletedBattery { get; } = new(1270271716);
        public static PrefabGUID JewelTemplate { get; } = new(1075994038);
        public static PrefabGUID GoldJewelry { get; } = new(-1749304196);
        public static PrefabGUID GoldIngot { get; } = new(-1027710236);
        public static PrefabGUID BloodCrystal { get; } = new(-1913156733);
        public static PrefabGUID GreaterEssence { get; } = new(271594022);
        public static PrefabGUID GreaterStygian { get; } = new(576389135);
        public static PrefabGUID PrimalStygian { get; } = new(28358550);
    }

    private static class Components
    {
        public static PrefabGUID PristineHeart { get; } = new(-1413694594);
        public static PrefabGUID RadiantFibre { get; } = new(-182923609);
        public static PrefabGUID Resonator { get; } = new(-1629804427);
        public static PrefabGUID Document { get; } = new(1334469825);
        public static PrefabGUID DemonFragment { get; } = new(-77477508);
        public static PrefabGUID MagicalComponent { get; } = new(1488205677);
        public static PrefabGUID TailoringComponent { get; } = new(828271620);
        public static PrefabGUID GemGrindstone { get; } = new(2115367516);
        public static PrefabGUID GoldOre { get; } = new(660533034);
        public static PrefabGUID ProcessedSulphur { get; } = new(880699252);
        public static PrefabGUID Crystal { get; } = new(-257494203);
    }

    private static class Gems
    {
        public static PrefabGUID PerfectAmethyst { get; } = new(-106283194);
        public static PrefabGUID PerfectEmerald { get; } = new(1354115931);
        public static PrefabGUID PerfectRuby { get; } = new(188653143);
        public static PrefabGUID PerfectSapphire { get; } = new(-2020212226);
        public static PrefabGUID PerfectTopaz { get; } = new(-1983566585);
        public static PrefabGUID PerfectMiststone { get; } = new(750542699);
    }

    private static class Shards
    {
        public static PrefabGUID Solarus { get; } = new(-21943750);
        public static PrefabGUID Monster { get; } = new(-1581189572);
        public static PrefabGUID Manticore { get; } = new(-1260254082);
        public static PrefabGUID Dracula { get; } = new(666638454);
        public static PrefabGUID Morgana { get; } = PrefabGUIDs.Item_MagicSource_SoulShard_Morgana;
    }

    private static class Containers
    {
        public static PrefabGUID SolarusShard { get; } = new(-824445631);
        public static PrefabGUID MonsterShard { get; } = new(-1996942061);
        public static PrefabGUID ManticoreShard { get; } = new(653759442);
        public static PrefabGUID DraculaShard { get; } = new(1495743889);
        public static PrefabGUID MorganaShard { get; } = PrefabGUIDs.TM_Castle_Container_Specialized_Soulshards_Morgana;
    }

    private static class Collections
    {
        public static IReadOnlyList<PrefabGUID> ShardRecipes { get; } = new List<PrefabGUID>
        {
            RecipeIds.SolarusShard,
            RecipeIds.MonsterShard,
            RecipeIds.ManticoreShard,
            RecipeIds.DraculaShard,
            RecipeIds.MorganaShard
        };

        public static IReadOnlyList<PrefabGUID> SoulShards { get; } = new List<PrefabGUID>
        {
            Shards.Solarus,
            Shards.Monster,
            Shards.Manticore,
            Shards.Dracula,
            Shards.Morgana
        };

        public static IReadOnlyList<PrefabGUID> ShardContainers { get; } = new List<PrefabGUID>
        {
            Containers.SolarusShard,
            Containers.MonsterShard,
            Containers.ManticoreShard,
            Containers.DraculaShard,
            Containers.MorganaShard
        };

        public static IReadOnlyDictionary<PrefabGUID, PrefabGUID> RecipesToShards { get; } =
            new Dictionary<PrefabGUID, PrefabGUID>
            {
                { RecipeIds.SolarusShard, Shards.Solarus },
                { RecipeIds.MonsterShard, Shards.Monster },
                { RecipeIds.ManticoreShard, Shards.Manticore },
                { RecipeIds.DraculaShard, Shards.Dracula },
                { RecipeIds.MorganaShard, Shards.Morgana }
            };
    }

    private sealed class MiscItemAdjustment
    {
        public PrefabGUID ItemGuid { get; }

        public PrefabGUID SalvageRecipeGuid { get; }

        public float SalvageTimer { get; }

        public (PrefabGUID guid, int amount)[] Requirements { get; }

        public MiscItemAdjustment(
            PrefabGUID itemGuid,
            PrefabGUID salvageRecipeGuid,
            float salvageTimer,
            params (PrefabGUID guid, int amount)[] requirements)
        {
            ItemGuid = itemGuid;
            SalvageRecipeGuid = salvageRecipeGuid;
            SalvageTimer = salvageTimer;
            Requirements = requirements ?? Array.Empty<(PrefabGUID guid, int amount)>();
        }
    }

    private sealed class StationAdjustment
    {
        public PrefabGUID StationGuid { get; }

        public PrefabGUID[] RecipesToAdd { get; }

        public PrefabGUID[] RecipesToRemove { get; }

        public StationAdjustment(
            PrefabGUID stationGuid,
            PrefabGUID[] recipesToAdd,
            PrefabGUID[] recipesToRemove)
        {
            StationGuid = stationGuid;
            RecipesToAdd = recipesToAdd ?? Array.Empty<PrefabGUID>();
            RecipesToRemove = recipesToRemove ?? Array.Empty<PrefabGUID>();
        }
    }

    private static readonly IReadOnlyList<MiscItemAdjustment> MiscItemAdjustments = new List<MiscItemAdjustment>
    {
        new MiscItemAdjustment(
            Items.EmpBuilding,
            PrefabGUIDs.Recipe_CastleUpkeep_T02,
            20f,
            (Items.DepletedBattery, 2),
            (Items.TechScrap, 15)),
        new MiscItemAdjustment(
            Items.PrimalEssence,
            PrefabGUIDs.Recipe_CastleUpkeep_T02,
            10f,
            (Items.BatteryCharge, 5)),
        new MiscItemAdjustment(
            Items.CopperWires,
            PrefabGUIDs.Recipe_CastleUpkeep_T02,
            15f,
            (Items.BatteryCharge, 1)),
        new MiscItemAdjustment(
            Items.BatHide,
            PrefabGUIDs.Recipe_CastleUpkeep_T02,
            15f,
            (Items.LesserStygian, 3),
            (Items.BloodEssence, 5)),
        new MiscItemAdjustment(
            Components.GoldOre,
            PrefabGUIDs.Recipe_CastleUpkeep_T02,
            10f,
            (Items.GoldJewelry, 2)),
        new MiscItemAdjustment(
            Components.RadiantFibre,
            PrefabGUIDs.Recipe_CastleUpkeep_T02,
            10f,
            (PrefabGUIDs.Item_Ingredient_Gemdust, 8),
            (PrefabGUIDs.Item_Ingredient_Plant_PlantFiber, 16),
            (PrefabGUIDs.Item_Ingredient_Pollen, 24))
    };

    private static readonly IReadOnlyList<StationAdjustment> StationAdjustments = new List<StationAdjustment>
    {
        new StationAdjustment(
            Stations.AdvancedGrinder,
            new[] { RecipeIds.VampiricDust },
            new[] { RecipeIds.PrimalStygian, RecipeIds.BloodCrystal }),
        new StationAdjustment(
            Stations.PrimitiveGrinder,
            Array.Empty<PrefabGUID>(),
            new[] { RecipeIds.PrimalStygian, RecipeIds.BloodCrystal }),
        new StationAdjustment(
            Stations.Fabricator,
            new[] { RecipeIds.CopperWires, RecipeIds.ChargedBattery },
            Array.Empty<PrefabGUID>()),
        new StationAdjustment(
            Stations.GemCuttingTable,
            new[] { RecipeIds.PrimalStygian, RecipeIds.ExtractShard },
            Array.Empty<PrefabGUID>()),
        new StationAdjustment(
            Stations.AdvancedBloodPress,
            new[] { RecipeIds.BloodCrystal },
            Array.Empty<PrefabGUID>())
    };

    public static void ModifyRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        ConfigurePrimalStygianRecipe(recipeMap);
        ConfigureBloodCrystalRecipe(recipeMap);
        RemoveRecipeLinks(
            PrefabCollectionSystem._PrefabGuidToEntityMap[RecipeIds.FakeGemdust],
            RecipeIds.PrimalStygian,
            RecipeIds.BloodCrystal);

        ApplyMiscItemAdjustments();
        RemoveSalvageableAndRequirements(Items.BatteryCharge);

        ConfigureExtractShardRecipeIfJewelValid();
        IncreaseShardContainerSlots();
        ConfigureAdvancedGrinder(recipeMap);
        ConfigureFabricator(recipeMap);
        FixFakeFlowerRecipe();
        ApplyStationAdjustments();

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
    /// Updates the Primal Stygian recipe requirements, outputs, and metadata.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigurePrimalStygianRecipe(NativeParallelHashMap<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[RecipeIds.PrimalStygian];

        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstRequirement = requirements[0];
        firstRequirement.Guid = Items.GreaterStygian;
        firstRequirement.Amount = 8;
        requirements[0] = firstRequirement;

        var outputs = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        var firstOutput = outputs[0];
        firstOutput.Guid = Items.PrimalStygian;
        firstOutput.Amount = 1;
        outputs[0] = firstOutput;

        UpdateRecipeData(recipeEntity, 10f, true, false);
        recipeMap[RecipeIds.PrimalStygian] = recipeEntity.Read<RecipeData>();
    }

    /// <summary>
    /// Updates the Blood Crystal recipe requirements, outputs, and metadata.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigureBloodCrystalRecipe(NativeParallelHashMap<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[RecipeIds.BloodCrystal];

        var requirements = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstRequirement = requirements[0];
        firstRequirement.Guid = Components.Crystal;
        firstRequirement.Amount = 100;
        requirements[0] = firstRequirement;

        requirements.Add(new RecipeRequirementBuffer { Guid = Items.GreaterEssence, Amount = 1 });

        var outputs = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        var firstOutput = outputs[0];
        firstOutput.Guid = Items.BloodCrystal;
        firstOutput.Amount = 100;
        outputs[0] = firstOutput;

        UpdateRecipeData(recipeEntity, 10f, true, false);
        recipeMap[RecipeIds.BloodCrystal] = recipeEntity.Read<RecipeData>();
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

    private static void ApplyMiscItemAdjustments()
    {
        foreach (var adjustment in MiscItemAdjustments)
        {
            ModifyMiscItem(
                adjustment.ItemGuid,
                adjustment.SalvageRecipeGuid,
                adjustment.SalvageTimer,
                adjustment.Requirements);
        }
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

    private static void ApplyStationAdjustments()
    {
        foreach (var adjustment in StationAdjustments)
        {
            if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(adjustment.StationGuid, out var stationEntity))
            {
                continue;
            }

            if (adjustment.RecipesToAdd.Length > 0)
            {
                AddRefinementRecipes(stationEntity, adjustment.RecipesToAdd);
            }

            if (adjustment.RecipesToRemove.Length > 0)
            {
                RemoveRefinementRecipes(stationEntity, adjustment.RecipesToRemove);
            }
        }
    }

    /// <summary>
    /// If the Primal Jewel requirement is valid, update the extract shard recipe and shard-specific recipes.
    /// </summary>
    private static void ConfigureExtractShardRecipeIfJewelValid()
    {
        if (!Requirements.PrimalJewel.HasValue())
        {
            return;
        }

        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(Requirements.PrimalJewel, out Entity itemPrefab)
            || !itemPrefab.Has<ItemData>())
        {
            Core.Log.LogWarning(
                "Primal Jewel requirement doesn't appear to be a valid item (missing itemData component), " +
                "correct this for the recipe to appear on gem cutting stations after placement!");
            return;
        }

        var extractRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[RecipeIds.ExtractShard];
        var requirementBuffer = extractRecipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        var firstRequirement = requirementBuffer[0];
        firstRequirement.Guid = Requirements.PrimalJewel;
        requirementBuffer[0] = firstRequirement;

        var outputBuffer = extractRecipeEntity.ReadBuffer<RecipeOutputBuffer>();
        outputBuffer.Add(new RecipeOutputBuffer { Guid = Items.JewelTemplate, Amount = 1 });

        foreach (PrefabGUID shardRecipe in Collections.ShardRecipes)
        {
            var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shardRecipe];
            var shardRequirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
            shardRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = Collections.RecipesToShards[shardRecipe], Amount = 1 });
            shardRequirementBuffer.Add(new RecipeRequirementBuffer { Guid = Requirements.PrimalJewel, Amount = 1 });
        }
    }

    /// <summary>
    /// Increases the inventory slot capacity for each shard container to 14.
    /// </summary>
    private static void IncreaseShardContainerSlots()
    {
        foreach (PrefabGUID shardContainer in Collections.ShardContainers)
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
    /// Configures the Vampiric Dust recipe metadata for the advanced grinder.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigureAdvancedGrinder(NativeParallelHashMap<PrefabGUID, RecipeData> recipeMap)
    {
        var recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[RecipeIds.VampiricDust];
        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });
        recipeMap[RecipeIds.VampiricDust] = recipeEntity.Read<RecipeData>();
    }

    /// <summary>
    /// Updates copper wires and charged battery recipe metadata for the fabricator.
    /// </summary>
    /// <param name="recipeMap">The recipe lookup map to update.</param>
    private static void ConfigureFabricator(NativeParallelHashMap<PrefabGUID, RecipeData> recipeMap)
    {
        var copperRecipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[RecipeIds.CopperWires];
        UpdateRecipeData(copperRecipeEntity, 10f, true, false);
        recipeMap[RecipeIds.CopperWires] = copperRecipeEntity.Read<RecipeData>();

        var chargedBatteryEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[RecipeIds.ChargedBattery];
        var requirementBuffer = chargedBatteryEntity.ReadBuffer<RecipeRequirementBuffer>();
        requirementBuffer.Add(new RecipeRequirementBuffer { Guid = Items.BatteryCharge, Amount = 1 });
        UpdateRecipeData(chargedBatteryEntity, 90f, true, false);
        recipeMap[RecipeIds.ChargedBattery] = chargedBatteryEntity.Read<RecipeData>();
    }

    /// <summary>
    /// Adjusts the fake flower recipe to use plant thistle.
    /// </summary>
    private static void FixFakeFlowerRecipe()
    {
        if (!PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(RecipeIds.FakeFlower, out Entity recipePrefab))
        {
            return;
        }

        if (!recipePrefab.TryGetBuffer<RecipeRequirementBuffer>(out var requirementBuffer) || requirementBuffer.IsEmpty)
        {
            return;
        }

        var firstRequirement = requirementBuffer[0];
        firstRequirement.Guid = Items.PlantThistle;
        requirementBuffer[0] = firstRequirement;
    }

}
