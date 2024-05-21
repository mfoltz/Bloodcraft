using HarmonyLib;
using ProjectM;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class InitializationPatch
{
    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix()
    {
        Core.Initialize();
    }
}