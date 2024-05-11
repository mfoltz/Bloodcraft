using Cobalt.Core;
using Cobalt.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks;

public class FishingSystemPatch
{
    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    public static class GameplayEventsSystemPatch
    {
        private static readonly int BaseFishingXP = 100;

        public static void Prefix(CreateGameplayEventOnDestroySystem __instance)
        {
            NativeArray<Entity> entities = __instance.__query_1297357609_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Equals(Entity.Null)) continue;
                    if (!entity.Has<Buff>()) continue;
                    PrefabGUID prefabGUID = entity.Read<PrefabGUID>();

                    if (!prefabGUID.GuidHash.Equals(-1130746976)) // fishing travel to target, this indicates a succesful fishing event
                    {
                        continue;
                    }

                    Entity character = entity.Read<EntityOwner>().Owner;
                    User user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
                    ulong steamId = user.PlatformId;
                    PrefabGUID toProcess = new(0);
                    Entity target = entity.Read<Buff>().Target;

                    if (!target.Equals(Entity.Null))
                    {
                        //target.LogComponentTypes();
                        if (!target.Has<DropTableBuffer>())
                        {
                            Plugin.Log.LogInfo("No DropTableBuffer found on entity...");
                        }
                        else
                        {
                            var dropTableBuffer = target.ReadBuffer<DropTableBuffer>();
                            if (dropTableBuffer.IsEmpty || !dropTableBuffer.IsCreated)
                            {
                                Plugin.Log.LogInfo("DropTableBuffer is empty or not created...");
                            }
                            else
                            {
                                toProcess = dropTableBuffer[0].DropTableGuid;
                                Plugin.Log.LogInfo($"{toProcess.LookupName()}");
                            }
                        }
                    }
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(toProcess, "");
                    int multiplier = ProfessionUtilities.GetFishingModifier(toProcess);

                    if (handler != null)
                    {
                        ProfessionSystem.SetProfession(toProcess, user, steamId, BaseFishingXP * multiplier, handler);
                        ProfessionSystem.GiveProfessionBonus(toProcess, target, user, steamId, handler);
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