using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.Terrain;
using ProjectM.UI;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TerrainUtils;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class TestPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
 
    [HarmonyPatch(typeof(CharacterMenuOpenedSystem_Server), nameof(CharacterMenuOpenedSystem_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CharacterMenuOpenedSystem_Server __instance)
    {
        if (!Core.hasInitialized) return;
        
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities) // Prefab AB_Interact_Mount_Owner_Buff_Horse PrefabGuid(854656674)
            {
                if (entity.TryGetComponent(out FromCharacter fromCharacter))
                {
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(fromCharacter.Character);
                    if (familiar.Exists() && familiar.Has<Mountable>())
                    {
                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = new(854656674),
                            Who = fromCharacter.Character.Read<NetworkId>()
                        };
                        
                        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    }
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


