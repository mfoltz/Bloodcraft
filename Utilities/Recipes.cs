using Bloodcraft.Services;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.UI;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class Recipes // would like to tie this into professions eventuallyyyyy - for now only useful as custom merchant currencies and such
{
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly PrefabGUID _advancedGrinder = new(-178579946); // vampiric dust
    static readonly PrefabGUID _advancedFurnace = new(-222851985); // silver ingot
    static readonly PrefabGUID _fabricator = new(-465055967); // copper wires, iron body

    static readonly PrefabGUID _ironBody = new(-1270503528);
    static readonly PrefabGUID _vampiricDust = new(311920560);
    static readonly PrefabGUID _copperWires = new(-2031309726);
    static readonly PrefabGUID _silverIngot = new(-1633898285);
    // static readonly PrefabGUID _extractShard = new(1743327679);
    static readonly PrefabGUID _fakeFlower = new(-2095604835);

    // static readonly PrefabGUID _demonFragment = new(-77477508);
    public static void AddExtraRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDust];

        RecipeData recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_vampiricDust] = recipeData;

        GameDataSystem.RegisterRecipes();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _vampiricDust, Disabled = false, Unlocked = true });
        // refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _extractShard, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWires];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_copperWires] = recipeData;

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_ironBody];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });
        recipeMap[_ironBody] = recipeData;

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_silverIngot];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_silverIngot] = recipeData;

        /*
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_extractShard];

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        if (recipeEntity.TryGetBuffer<RecipeOutputBuffer>(out var buffer))
        {
            RecipeOutputBuffer recipeOutputBuffer = new()
            {
                Amount = 1,
                Guid = _demonFragment
            };

            buffer.Add(recipeOutputBuffer);
        }

        recipeMap[_extractShard] = recipeData;
        */

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _copperWires, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _ironBody, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedFurnace];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _silverIngot, Disabled = false, Unlocked = true });

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_fakeFlower, out Entity recipePrefab) 
            && recipePrefab.TryGetBuffer<RecipeRequirementBuffer>(out var refinementRecipeBuffer) && !refinementRecipeBuffer.IsEmpty)
        {
            RecipeRequirementBuffer recipeRequirement = refinementRecipeBuffer[0];
            recipeRequirement.Guid = new(-598100816); // Item_Ingredient_Plant_Thistle

            refinementRecipeBuffer[0] = recipeRequirement;
        }

        /*
        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new(-64110296), out Entity altarPrefab)
            && altarPrefab.TryGetBuffer<WorkstationRecipesBuffer>(out var craftingRecipeBuffer))
        {
            WorkstationRecipesBuffer craftingRecipe = new()
            {
                RecipeGuid = new(-1525227854) // crafting recipe
            };

            craftingRecipeBuffer.Add(craftingRecipe);

            craftingRecipe = new()
            {
                RecipeGuid = new(1743327679) // refinement recipe
            };
            
            craftingRecipeBuffer.Add(craftingRecipe);
        }
        */

        GameDataSystem.RegisterRecipes();
    }
}

