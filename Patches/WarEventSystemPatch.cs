using Bloodcraft;
using HarmonyLib;
using ProjectM.Gameplay.WarEvents;
using ProjectM.Shared.WarEvents;
using Unity.Collections;
using Unity.Entities;
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
                //entity.LogComponentTypes();
                WarEvent warEvent = entity.Read<WarEvent>();
                var buffer = warEvent.Entity.ReadBuffer<WarEvent_ChildReference>();
                for (int i = 0; i < buffer.Length; i++)
                {
                    Entity child = buffer[i].Entity;
                    var nestedBuffer = child.ReadBuffer<WarEvent_ChildReference>();
                    for (int j = 0; j < nestedBuffer.Length; j++)
                    {
                        nestedBuffer[j].Entity.LogComponentTypes();
                    }

                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex.Message);
        }
        finally
        {
            entities.Dispose();
        }
    }
}
*/
