using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class InitializationPatch
{
    //static readonly PrefabGUID primalBosses = new(-1264407246);
    //static readonly PrefabGUID primalWaves = new(-1264407246);
    //static readonly int primalWaveMultiplier = Plugin.PrimalWaveMultiplier.Value;

    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix()
    {
        Core.Initialize();
        //if (Plugin.WarEventSystem.Value) ModifyPrimalUC();
        if (Plugin.FamiliarSystem.Value) Core.FamiliarService.HandleFamiliarsOnSpawn();
        //RecipeSystem.HandleRecipes();
    }

    /*
    static void ModifyPrimalUC()
    {
        //Entity baseEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[primalBosses];

        //var groupBuffer = baseEntity.ReadBuffer<UnitCompositionGroupEntry>();
        var bossBuffer = baseEntity.ReadBuffer<UnitCompositionGroupUnitEntry>();

        //groupBuffer.RemoveRange(1, groupBuffer.Length - 1);
        
        for (int i = 0; i < bossBuffer.Length; i++)
        {
            UnitCompositionGroupUnitEntry entry = bossBuffer[i];
            //Core.Log.LogInfo($"{entry.Unit.GetPrefabName()} | {entry.UnitBaseStatsType.ToString()}");
            entry.UnitBaseStatsType = UnitBaseStatsType.Boss;
            entry.CustomVBloodUnit = new(-740796338);
            entry.IsVBloodUnit = true;
            //entry.Unit = new(-740796338);
            //UnitCompositionGroupEntry groupEntry = groupBuffer[i];
            //groupEntry.UnitsStartIndex = 0;
            //groupEntry.UnitsCount = bossBuffer.Length;
            //groupBuffer[i] = groupEntry;
            bossBuffer[i] = entry;
        }
        
        //baseEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[primalWaves];
        //baseEntity.LogComponentTypes();
        //var groupBuffer = baseEntity.ReadBuffer<UnitCompositionGroupEntry>();

        var unitBuffer = baseEntity.ReadBuffer<UnitCompositionGroupUnitEntry>();

        for (int i = 0; i < unitBuffer.Length; i++)
        {
            UnitCompositionGroupUnitEntry entry = unitBuffer[i];
            //Core.Log.LogInfo($"{entry.Unit.GetPrefabName()} | {entry.UnitBaseStatsType.ToString()}");
            entry.IsVBloodUnit = true;
            entry.Unit = new(282791819);
            unitBuffer[i] = entry;
        }
    }
    */
}