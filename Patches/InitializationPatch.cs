using HarmonyLib;
using ProjectM;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class InitializationPatch
{
    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix()
    {
        Core.Initialize();
        if (Plugin.FamiliarSystem.Value) Core.FamiliarService.HandleFamiliarsOnSpawn();
    }

}