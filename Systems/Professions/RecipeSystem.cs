using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Systems.Professions;

internal static class RecipeSystem
{
    static readonly PrefabGUID advancedGrinder = new(-178579946); // vampiric dust
    //static readonly PrefabGUID advancedLoom = new(1299929048);
    //static readonly PrefabGUID advancedFurnace = new(-222851985);
    static readonly PrefabGUID smallFurnace = new(-1150411622); // silver ingot
    static readonly PrefabGUID fabricator = new(-465055967); // copper wires
    //static readonly PrefabGUID artisanTable = new(-1718710437);

    static readonly PrefabGUID vampiricDust = new(311920560);
    static readonly PrefabGUID copperWires = new(-2031309726);
    static readonly PrefabGUID silverIngot = new(-1633898285);

    public static void HandleRecipes()
    {
        var recipeMap = Core.GameDataSystem.RecipeHashLookupMap;

        Entity stationEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[advancedGrinder];
        Entity recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[vampiricDust];

        RecipeData recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);
        
        recipeMap[vampiricDust] = recipeData;

        Core.GameDataSystem.RegisterRecipes();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = vampiricDust, Disabled = false, Unlocked = true });

        stationEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[fabricator];
        recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[copperWires];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);

        recipeMap[copperWires] = recipeData;

        Core.GameDataSystem.RegisterRecipes();

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = copperWires, Disabled = false, Unlocked = true });

        stationEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[smallFurnace];
        recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[silverIngot];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);

        recipeMap[silverIngot] = recipeData;

        Core.GameDataSystem.RegisterRecipes();

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = silverIngot, Disabled = false, Unlocked = true });
        
        Core.GameDataSystem.RegisterRecipes();
        Core.GameDataSystem.RegisterItems();
        Core.GameDataSystem.RegisterBlueprints();
    }
}

