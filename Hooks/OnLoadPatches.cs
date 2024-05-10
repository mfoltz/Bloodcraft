using Cobalt.Core;
using HarmonyLib;
using ProjectM;


namespace KindredCommands.Patches;

[HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
public static class InitializationPatch
{
	[HarmonyPostfix]
	public static void AfterLoad()
	{
		Plugin.StripLevelSources();
    }
}
