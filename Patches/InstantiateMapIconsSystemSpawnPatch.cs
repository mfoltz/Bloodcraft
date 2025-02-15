using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Patches;


[HarmonyPatch]
internal class InstantiateMapIconsSystemSpawnPatch
{
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    /*
    [HarmonyPatch(typeof(InstantiateMapIconsSystem_Spawn), nameof(InstantiateMapIconsSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(InstantiateMapIconsSystem_Spawn __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                Core.Log.LogInfo($"InstantiateMapIconsSystem_Spawn - {entity} | {entity.GetPrefabGuid().GetPrefabName()}");
                
                if (entity.TryGetComponent(out Buff buff) && Familiars.FamiliarHorseMap.Values.Contains(buff.Target)
                    && entity.TryGetBuffer<AttachMapIconsToEntity>(out var buffer) && !buffer.IsEmpty)
                {
                    AttachMapIconsToEntity mapIcon = buffer[0];
                    mapIcon.Prefab = Core.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.ContainsKey(new(-1491648886)) ? new(-1491648886) : new(-892362184); // MapIcon_CharmedUnit or MapIcon_Player
                    
                    buffer[0] = mapIcon;
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    */

    const string MAP_ICON_ERROR = "PlayerMapIcon requires the creator to have the PlayerCharacter component.";

    [HarmonyPatch(typeof(Debug), nameof(Debug.LogError), [typeof(Il2CppSystem.Object)])]
    [HarmonyPrefix]
    static bool LogErrorPrefix(Il2CppSystem.Object message)
    {
        if (!Core._initialized) return true;
        else if (!_familiars) return true;

        string stringMessage = message.ToString();
        if (stringMessage.Contains(MAP_ICON_ERROR))
        {
            return false;
        }

        return true;
    }
}