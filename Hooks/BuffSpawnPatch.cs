using Bloodstone.API;
using HarmonyLib;
using ProjectM.Network;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using VCreate.Core.Toolbox;
using VCreate.Core;
using VCreate.Data;

namespace VCreate.Hooks
{
    [HarmonyPatch]
    public class BuffPatch
    {
        [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
        [HarmonyPostfix]
        private static void OnUpdatePostfix(BuffSystem_Spawn_Server __instance)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null)) continue;
                    //entity.LogComponentTypes();
                    Entity character = entity.Read<Buff>().Target;
                    Entity familiar = VCreate.Core.Commands.PetCommands.FindPlayerFamiliar(character);
                    var buffer = entityManager.GetBuffer<BuffBuffer>(character);
                    if (familiar.Equals(Entity.Null) || BuffUtility.TryGetBuff(familiar, VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out Entity _))
                    {
                        //Plugin.Log.LogInfo("Familiar not found for buff sharing.");
                        continue;
                    }
                    else
                    {
                        foreach (var buff in buffer)
                        {
                            //Plugin.Log.LogInfo("Processing buffInstanceData...");
                            PrefabGUID prefab = buff.Entity.Read<PrefabGUID>();
                            if (!prefab.LookupName().ToLower().Contains("consumable")) continue;
                            //Plugin.Log.LogInfo($"Found consumable buff to share with familiar. {prefab.LookupName()}");
                            bool buffCheck = BuffUtility.TryGetBuff(familiar, prefab, entityManager.GetBufferFromEntity<BuffBuffer>(true), out Entity _);
                            if (buffCheck)
                            {
                                continue;
                            }
                            else
                            {
                                Helper.BuffCharacter(familiar, prefab);
                            }

                            //Plugin.Log.LogInfo("Shared consumable buffs with familiar.");
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