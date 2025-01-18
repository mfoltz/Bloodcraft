using Bloodcraft.Services;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class Recipes // professions, familiars, same thing >_>
{
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly PrefabGUID _advancedGrinder = new(-178579946); // vampiric dust
    static readonly PrefabGUID _advancedFurnace = new(-222851985); // silver ingot
    static readonly PrefabGUID _fabricator = new(-465055967); // copper wires, iron body

    static readonly PrefabGUID _ironBodyRecipe = new(-1270503528);
    static readonly PrefabGUID _vampiricDustRecipe = new(311920560);
    static readonly PrefabGUID _copperWiresRecipe = new(-2031309726);
    static readonly PrefabGUID _silverIngotRecipe = new(-1633898285);
    static readonly PrefabGUID _fakeFlowerRecipe = new(-2095604835);

    static readonly PrefabGUID _plantThistle = new(-598100816);

    // static readonly PrefabGUID _demonFragment = new(-77477508);
    // static readonly PrefabGUID _extractShard = new(1743327679);

    public static void AddExtraRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDustRecipe];

        RecipeData recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_vampiricDustRecipe] = recipeData;

        GameDataSystem.RegisterRecipes();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _vampiricDustRecipe, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWiresRecipe];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_copperWiresRecipe] = recipeData;

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_ironBodyRecipe];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });
        recipeMap[_ironBodyRecipe] = recipeData;

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_silverIngotRecipe];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeEntity.With((ref RecipeData recipeData) =>
        {
            recipeData.AlwaysUnlocked = true;
            recipeData.HideInStation = false;
            recipeData.HudSortingOrder = 0;
        });

        recipeMap[_silverIngotRecipe] = recipeData;

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
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _copperWiresRecipe, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _ironBodyRecipe, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedFurnace];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _silverIngotRecipe, Disabled = false, Unlocked = true });

        if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_fakeFlowerRecipe, out Entity recipePrefab) 
            && recipePrefab.TryGetBuffer<RecipeRequirementBuffer>(out var refinementRecipeBuffer) && !refinementRecipeBuffer.IsEmpty)
        {
            RecipeRequirementBuffer recipeRequirement = refinementRecipeBuffer[0];
            recipeRequirement.Guid = _plantThistle;

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

