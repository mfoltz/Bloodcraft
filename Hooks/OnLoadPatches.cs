using HarmonyLib;
using ProjectM;

namespace Cobalt.Hooks;

[HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
public static class InitializationPatch
{
    [HarmonyPostfix]
    public static void AfterLoad()
    {
        Plugin.UpdateBaseStats();
    }
}