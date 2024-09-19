using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class TestPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    
    [HarmonyPatch(typeof(SetupNetworkIdSystem_PreSerialize), nameof(SetupNetworkIdSystem_PreSerialize.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(SetupNetworkIdSystem_PreSerialize __instance)
    {
        if (!Core.hasInitialized) return;
        
        NativeArray<Entity> entities = __instance.__query_1510972495_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<ScrollingCombatTextMessage>())
                {
                    entity.LogComponentTypes();
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
*/

