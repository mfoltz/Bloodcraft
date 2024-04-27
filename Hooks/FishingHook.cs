using Bloodstone.API;
using Cobalt.Core;
using Cobalt.Systems;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Systems.ProfessionUtilities;

namespace Cobalt.Hooks;

public class FishingSystemPatch
{
    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    public static class GameplayEventsSystemPatch
    {
        private static readonly int BaseFishingXP = 10;

        public static void Prefix(CreateGameplayEventOnDestroySystem __instance)
        {
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null)) continue;
                    if (!entity.Has<Buff>()) continue;
                    if (!entity.Read<PrefabGUID>().GuidHash.Equals(1753229314)) continue; // AB_Fishing_Target_ReadyBuff
                    //entity.LogComponentTypes();
                    Entity character = entity.Read<EntityOwner>().Owner;
                    //character.LogComponentTypes();
                    User user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
                    ulong steamId = user.PlatformId;
                    PrefabGUID toProcess = new(0);
                    Entity target = entity.Read<Buff>().Target;
                    
                    if (!target.Equals(Entity.Null))
                    {
                        //target.LogComponentTypes();
                        if (!target.Has<DropTableBuffer>())
                        {
                            //Plugin.Log.LogInfo("No DropTableBuffer found on parent entity...");
                        }
                        else
                        {
                            var dropTableBuffer = target.ReadBuffer<DropTableBuffer>();
                            if (dropTableBuffer.IsEmpty || !dropTableBuffer.IsCreated)
                            {
                                //Plugin.Log.LogInfo("DropTableBuffer is empty or not created...");
                            }
                            else
                            {
                                toProcess = dropTableBuffer[0].DropTableGuid;
                                //Plugin.Log.LogInfo($"{toProcess.LookupName()}");
                            }
                        }
                    }
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(toProcess, "fishing");
                    int multiplier = ProfessionUtilities.GetFishingModifier(toProcess);

                    if (handler != null)
                    {
                        ProfessionSystem.SetProfession(user, steamId, BaseFishingXP * multiplier, toProcess, handler);
                    }
                    
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited GameplayEventsSystem hook early: {e}");
            }
        }
    }
}