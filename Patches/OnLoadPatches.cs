using HarmonyLib;
using ProjectM;

namespace Bloodcraft.Patches;

[HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
public static class InitializationPatch
{
    [HarmonyPostfix]
    public static void AfterLoad()
    {
        Core.Initialize();
    }
}