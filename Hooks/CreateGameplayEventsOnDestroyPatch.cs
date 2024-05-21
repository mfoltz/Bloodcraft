using Cobalt.Systems.Legacy;
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

public class CreateGameplayEventsOnDestroyPatch
{
    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    public static class GameplayEventsSystemPatch
    {
        private const int BaseFishingXP = 100;

        public static void Prefix(CreateGameplayEventOnDestroySystem __instance)
        {
            NativeArray<Entity> entities = __instance.__query_1297357609_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (Plugin.BloodSystem.Value && entity.Has<SpellTarget>() && entity.Read<PrefabGUID>().GuidHash.Equals(-1106009274))
                    {
                        Entity died = entity.Read<SpellTarget>().Target._Entity;
                        Entity killer = entity.Read<EntityOwner>().Owner;
                        BloodSystem.UpdateLegacy(killer, died);
                    }

                    PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                    if (Plugin.ProfessionSystem.Value && prefabGUID.GuidHash.Equals(-1130746976)) // fishing travel to target, this indicates a succesful fishing event
                    {
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
                                Core.Log.LogInfo("No DropTableBuffer found on entity...");
                            }
                            else
                            {
                                var dropTableBuffer = target.ReadBuffer<DropTableBuffer>();
                                if (dropTableBuffer.IsEmpty || !dropTableBuffer.IsCreated)
                                {
                                    Core.Log.LogInfo("DropTableBuffer is empty or not created...");
                                }
                                else
                                {
                                    toProcess = dropTableBuffer[0].DropTableGuid;
                                    Core.Log.LogInfo($"{toProcess.LookupName()}");
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
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited GameplayEventsSystem hook early: {e}");
            }
        }
    }
}