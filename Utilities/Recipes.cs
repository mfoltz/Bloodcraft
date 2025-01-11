using Bloodcraft.Services;
using ProjectM;
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
    public static void AddExtraRecipes()
    {
        var recipeMap = GameDataSystem.RecipeHashLookupMap;

        Entity stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedGrinder];
        Entity recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_vampiricDust];

        RecipeData recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);

        recipeMap[_vampiricDust] = recipeData;

        GameDataSystem.RegisterRecipes();

        var refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _vampiricDust, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_fabricator];
        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_copperWires];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);
        recipeMap[_copperWires] = recipeData;

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_ironBody];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);
        recipeMap[_ironBody] = recipeData;

        recipeEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_silverIngot];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        recipeEntity.Write(recipeData);
        recipeMap[_silverIngot] = recipeData;

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _copperWires, Disabled = false, Unlocked = true });
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _ironBody, Disabled = false, Unlocked = true });

        stationEntity = PrefabCollectionSystem._PrefabGuidToEntityMap[_advancedFurnace];
        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = _silverIngot, Disabled = false, Unlocked = true });

        GameDataSystem.RegisterRecipes();
    }
}

