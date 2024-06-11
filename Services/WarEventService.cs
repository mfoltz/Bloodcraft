using BepInEx.Unity.IL2CPP.Utils.Collections;
using Il2CppSystem;
using ProjectM;
using ProjectM.Gameplay.WarEvents;
using ProjectM.Physics;
using ProjectM.Shared.WarEvents;
using ProjectM.Terrain;
using System.Collections;
using Unity.Collections;
using UnityEngine;

namespace Bloodcraft.Services;
/*
internal class WarEventService // needs work
{
    //static readonly int updateInterval = Plugin.PrimalInterval.Value;
    //readonly IgnorePhysicsDebugSystem warEventMonoBehaviour;
    public WarEventService()
    {
        if (Plugin.WarEventSystem.Value)
        {
            warEventMonoBehaviour = (new GameObject("WarEventService")).AddComponent<IgnorePhysicsDebugSystem>();
            warEventMonoBehaviour.StartCoroutine(UpdateLoop().WrapToIl2Cpp());
        }       
    }
    static IEnumerator UpdateLoop()
    {
        WaitForSeconds waitForSeconds = new(updateInterval * 60); // Convert minutes to seconds for update loop

        while (true)
        {
            yield return waitForSeconds;

            WarEventType eventType = WarEventType.Primal;
            long currentTicks = (long)Core.ServerTime;
            NativeList<WarEventTypeCoordinateKey> warEventKeys = Core.WarEventRegistrySystem.GateCoordinates;
            WarEventGameSettings.StructData warEventSpawns = Core.WarEventSystem.SpawnSettings;
            WarEventSystem.WarEventSchedulingData warEventSchedulingData = Core.WarEventSystem.SchedulingData;
            Nullable_Unboxed<TerrainChunk> initialCoordinate = default;
            WarEventSystem.ActivateNewEvent(Core.EntityManager, ref Core.Random, eventType, ref warEventKeys, currentTicks, warEventSpawns, ref warEventSchedulingData, initialCoordinate, true);
            Core.Log.LogInfo("Primal Event activated!");
        }
    }    
}

*/