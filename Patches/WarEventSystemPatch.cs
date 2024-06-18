using Bloodcraft;
using HarmonyLib;
using ProjectM.Gameplay.WarEvents;
using ProjectM.Shared.WarEvents;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal class WarEventSystemPatch
{
    [HarmonyPatch(typeof(WarEventSystem), nameof(WarEventSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(WarEventSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_303314001_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<WarEvent>())
                {
                    continue;
                }
                //entity.LogComponentTypes(); so I want to add a chance for majors to be primals, and I want to modify the stats of the unit compositions?
                
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited WarEventSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
   
}
*/