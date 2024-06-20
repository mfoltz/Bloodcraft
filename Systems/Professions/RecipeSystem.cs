using ProjectM;
using ProjectM.Gameplay.Systems;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Systems.Professions;
public class RecipeSystem
{
    static readonly PrefabGUID advancedGrinder = new(-178579946);
    static readonly PrefabGUID advancedLoom = new(1299929048);
    static readonly PrefabGUID advancedFurnace = new(-222851985);
    static readonly PrefabGUID smallFurnace = new(-1150411622);
    static readonly PrefabGUID fabricator = new(-465055967);
    static readonly PrefabGUID artisanTable = new(-1718710437);
    static readonly PrefabGUID anvil = new(-437790980);

    static readonly PrefabGUID vampiricDust = new(311920560);
    static readonly PrefabGUID copperWires = new(-2031309726);
    static readonly PrefabGUID silverIngot = new(-1633898285);
    static readonly PrefabGUID draculaShard = new(-414358988);
    static readonly PrefabGUID extractShard = new(1743327679); // appeared
    static readonly PrefabGUID shadowSlashers = new(501702204);

    static readonly PrefabGUID exceptionalMagical = new(1488205677);
    static readonly PrefabGUID onyxTear = new(-651878258);
    static readonly PrefabGUID primalShard = new(28358550);
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
        //Entity extractEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[extractShard];

        recipeData = recipeEntity.Read<RecipeData>();
        //RecipeData extractData = extractEntity.Read<RecipeData>();

        //extractData.AlwaysUnlocked = true;
        //extractData.HideInStation = false;
        //extractData.HudSortingOrder = 0;

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;

        //extractEntity.Write(extractData);
        recipeEntity.Write(recipeData);

        recipeMap[copperWires] = recipeData;
        //recipeMap[extractShard] = extractData;

        Core.GameDataSystem.RegisterRecipes();

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = copperWires, Disabled = false, Unlocked = true });
        //refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = extractShard, Disabled = false, Unlocked = true });

        stationEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[smallFurnace];
        recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[silverIngot];
        var requirementBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        requirementBuffer.Add(new RecipeRequirementBuffer { Guid = copperWires, Amount = 5 });

        var outputBuffer = recipeEntity.ReadBuffer<RecipeOutputBuffer>();
        var item = outputBuffer[0];
        item.Guid = primalShard;
        item.Amount = 10;
        outputBuffer[0] = item;

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 0;
        recipeEntity.Write(recipeData);

        recipeMap[silverIngot] = recipeData;

        Core.GameDataSystem.RegisterRecipes();

        refinementBuffer = stationEntity.ReadBuffer<RefinementstationRecipesBuffer>();
        refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = silverIngot, Disabled = false, Unlocked = true });

        /*
        stationEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[artisanTable];
        recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[draculaShard];

        recipeData = recipeEntity.Read<RecipeData>();

        recipeData.AlwaysUnlocked = true;
        recipeData.HideInStation = false;
        recipeData.HudSortingOrder = 1;
        recipeData.IgnoreServerSettings = true;
        recipeEntity.Write(recipeData);

        //Core.EntityManager.AddBuffer<RecipeRequirementBuffer>(recipeEntity);
        var reqBuffer = recipeEntity.ReadBuffer<RecipeRequirementBuffer>();
        reqBuffer.Add(new RecipeRequirementBuffer { Guid = exceptionalMagical, Amount = 5 });
        reqBuffer.Add(new RecipeRequirementBuffer { Guid = vampiricDust, Amount = 20 });
        reqBuffer.Add(new RecipeRequirementBuffer { Guid = onyxTear, Amount = 10 });
        reqBuffer.Add(new RecipeRequirementBuffer { Guid = primalShard, Amount = 100 });
        
        //recipeMap[draculaShard] = recipeData;

        Core.GameDataSystem.RegisterRecipes();

        var workBuffer = stationEntity.ReadBuffer<WorkstationRecipesBuffer>();
        //workBuffer.Add(new WorkstationRecipesBuffer { RecipeGuid = draculaShard});

        //stationEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[anvil];
        //recipeEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[shadowSlashers];

        //recipeData = recipeEntity.Read<RecipeData>();
        
        //recipeData.AlwaysUnlocked = true;
        //recipeData.HideInStation = false;
        //recipeData.HudSortingOrder = 1;
        //recipeData.IgnoreServerSettings = true;
        //recipeEntity.Write(recipeData);

        var outputBuffer = Core.EntityManager.AddBuffer<RecipeOutputBuffer>(recipeEntity);
        outputBuffer.Add(new RecipeOutputBuffer { Guid = primalShard, Amount = 250 });

        //recipeMap[shadowSlashers] = recipeData;
        //stationEntity.Remove<WorkstationRecipesBuffer>();
        //workBuffer = Core.EntityManager.AddBuffer<WorkstationRecipesBuffer>(stationEntity);
        //workBuffer.Add(new WorkstationRecipesBuffer { RecipeGuid = shadowSlashers });
        //refinementBuffer.Add(new RefinementstationRecipesBuffer { RecipeGuid = shadowSlashers, Disabled = false, Unlocked = true });
        */
        Core.GameDataSystem.RegisterRecipes();
        Core.GameDataSystem.RegisterItems();
        Core.GameDataSystem.RegisterBlueprints();
    }
}
