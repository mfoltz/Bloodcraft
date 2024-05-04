using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Lapis.Hooks
{
    [HarmonyPatch]
    public class BuffPatch
    {
        private static readonly PrefabGUID unarmedBuff = new(-2075546002);
        [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
        [HarmonyPostfix]
        private static void OnUpdatePostfix(BuffSystem_Spawn_Server __instance)
        {
            NativeArray<Entity> entities = __instance.__query_401358634_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null) || !entity.Has<Buff>()) continue;
                    //entity.LogComponentTypes();
                    Buff buff = entity.Read<Buff>();
                    if (!buff.Target.Has<PlayerCharacter>()) continue;
                    else
                    {
                        // check for unarmed buff
                        if (entity.Read<PrefabGUID>().Equals(unarmedBuff))
                        {
                            Cobalt.Hooks.UnitStatsOverride.UpdatePlayerStats(buff.Target);
                        }
                        else
                        {
                            //Plugin.Log.LogInfo("Not unarmed...");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}