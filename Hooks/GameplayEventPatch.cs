using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using VCreate.Core;
using VCreate.Core.Commands;
using VCreate.Core.Toolbox;
using WorldBuild.Hooks;
/*
namespace VCreate.Hooks
{
    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    public class DealDamageSystemPatch
    {
        public static void Prefix(DealDamageSystem __instance)
        {
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
            EntityManager entityManager = __instance.EntityManager;

            PrefabGUID damageDone = serverGameManager.SCTTypes.DamageDone_Type;

            NativeArray<Entity> events = __instance._Query.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity damageDealt in events)
                {
                    if (!damageDealt.Has<DealDamageEvent>())
                    {
                        Plugin.Log.LogInfo("No damage event found...");
                        continue;
                    }
                    DealDamageEvent dealDamageEvent = damageDealt.Read<DealDamageEvent>();
                    bool charmCheck = BuffUtility.TryGetBuff(dealDamageEvent.SpellSource, VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out Entity _);
                    if (charmCheck) continue;
                    else
                    {
                        //dealDamageEvent.SpellSource.LogComponentTypes();
                    }
                    Entity familiar = dealDamageEvent.SpellSource.Read<EntityOwner>().Owner;
                    if (!familiar.Has<Follower>())
                    {
                        Plugin.Log.LogInfo("No follower on entity...");
                        continue;
                    }
                    else
                    {
                        Follower follower = familiar.Read<Follower>();
                        Entity followed = follower.Followed._Value;
                        if (!followed.Has<PlayerCharacter>())
                        {
                            Plugin.Log.LogInfo("No player character on followed from follower...");
                            continue;
                        }
                        else
                        {
                            Entity fam = PetCommands.FindPlayerFamiliar(followed);
                            if (fam.Equals(Entity.Null))
                            {
                                Plugin.Log.LogInfo("No familiar found for player character...");
                                continue;
                            }
                            else
                            {
                                
                                // if familiar, and damage event, then create SCT
                                Plugin.Log.LogInfo("Creating SCT for familiar...");
                                serverGameManager.CreateScrollingCombatText(dealDamageEvent.RawDamage, damageDone,dealDamageEvent.Target.Read<Height>().Value, familiar, dealDamageEvent.Target);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }
}
*/
