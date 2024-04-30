using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using Unity.Entities;

namespace Cobalt.Hooks
{
    public delegate void OnGameDataInitializedEventHandler(World world);

    internal class ServerEventsPatches
    {
        internal static event OnGameDataInitializedEventHandler OnGameDataInitialized;

        [HarmonyPatch(typeof(LoadPersistenceSystemV2), nameof(LoadPersistenceSystemV2.SetLoadState))]
        [HarmonyPostfix]
        private static void ServerStartupStateChange_Postfix(ServerStartupState.State loadState, LoadPersistenceSystemV2 __instance)
        {
            try
            {
                if (loadState == ServerStartupState.State.SuccessfulStartup)
                {
                    OnGameDataInitialized?.Invoke(__instance.World);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }

        [HarmonyPatch(typeof(GameBootstrap), nameof(GameBootstrap.OnApplicationQuit))]
        public static class GameBootstrapQuit_Patch
        {
            public static void Prefix()
            {
                DataStructures.SavePlayerBools();
                DataStructures.SavePlayerExperience();
                DataStructures.SavePlayerFishing();
                DataStructures.SavePlayerMining();
                DataStructures.SavePlayerWoodcutting();
                DataStructures.SavePlayerBlacksmithing();
                DataStructures.SavePlayerTailoring();
                DataStructures.SavePlayerCombatMastery();
                DataStructures.SavePlayerBloodMastery();
                DataStructures.SavePlayerWeaponStats();
                DataStructures.SavePlayerBloodStats();
            }
        }
    }
}