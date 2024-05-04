using Bloodstone.API;
using Cobalt.Core;
using Cobalt.Hooks;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Jobs;

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
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
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