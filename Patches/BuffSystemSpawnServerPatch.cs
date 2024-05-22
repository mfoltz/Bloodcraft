using HarmonyLib;
using ProjectM;
using ProjectM.Shared.Systems;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BuffSystem_Spawn_ServerPatch
{
    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(BuffSystem_Spawn_Server __instance)
    {
        Core.Log.LogInfo("BuffSystem_Spawn_Server.OnUpdate");
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.LevelingSystem.Value && entity.Has<Buff>() && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    GearOverride.SetLevel(entity.Read<Buff>().Target);
                }
            }
        }
        catch (System.Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
}