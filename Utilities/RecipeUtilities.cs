using Bloodcraft.Services;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Utilities;
internal static class RecipeUtilities // would like to tie this into professions eventually, for now only useful as custom merchant currencies and such
{
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly PrefabGUID advancedGrinder = new(-178579946); // vampiric dust
    //static readonly PrefabGUID advancedLoom = new(1299929048);
    static readonly PrefabGUID advancedFurnace = new(-222851985);
    //static readonly PrefabGUID smallFurnace = new(-1150411622); // silver ingot
    static readonly PrefabGUID fabricator = new(-465055967); // copper wires
    //static readonly PrefabGUID artisanTable = new(-1718710437);

    //static readonly PrefabGUID shadowGreatSword = new(-1525227854);

    static readonly PrefabGUID ironBody = new(-1270503528);
    static readonly PrefabGUID vampiricDust = new(311920560);
    static readonly PrefabGUID copperWires = new(-2031309726);
    static readonly PrefabGUID silverIngot = new(-1633898285);

    public static void ExtraRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[advancedGrinder];
        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[vampiricDust];

        RecipeData recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);

        recipeMap[vampiricDust] = recipeData;

        GameDataSystem.RegisterRecipes();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = vampiricDust, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[fabricator];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[copperWires];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);

        recipeMap[copperWires] = recipeData;

        GameDataSystem.RegisterRecipes();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[ironBody];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);

        recipeMap[ironBody] = recipeData;

        GameDataSystem.RegisterRecipes();

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[silverIngot];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);

        recipeMap[silverIngot] = recipeData;

        GameDataSystem.RegisterRecipes();

        //recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[shadowGreatSword];

        //recipeData = recipeEntity.Read<RecipeData>();

        //recipeData.AlwaysUnlocked = true;
        //recipeData.HideInStation = false;
        //recipeData.HudSortingOrder = 0;

        //recipeEntity.Write(recipeData);

        //recipeMap[shadowGreatSword] = recipeData;

        GameDataSystem.RegisterRecipes();

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = copperWires, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = ironBody, Disabled = false, Unlocked = true });
        //refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = shadowGreatSword, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[advancedFurnace];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = silverIngot, Disabled = false, Unlocked = true });

        GameDataSystem.RegisterRecipes();
        GameDataSystem.RegisterItems();
        GameDataSystem.RegisterBlueprints();
    }
}

