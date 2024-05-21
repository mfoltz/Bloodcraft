using Bloodcraft.Patches;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches
{
    [HarmonyPatch]
    public class BuffPatch
    {
        [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
        [HarmonyPostfix]
        private static void OnUpdatePostfix(BuffSystem_Spawn_Server __instance)
        {
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
}