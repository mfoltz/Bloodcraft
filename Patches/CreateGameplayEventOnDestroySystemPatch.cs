using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class CreateGameplayEventOnDestroySystemPatch
{
    static readonly bool _professions = ConfigService.ProfessionSystem;

    const int BASE_FISHING_XP = 100; // somewhat arbitrary constant that I need to revisit when looking at professions again soon

    static readonly PrefabGUID _fishingTravelToTarget = new(-1130746976);
    static readonly PrefabGUID _feedComplete = new(-1106009274);

    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnDestroySystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_professions) return;

        NativeArray<Entity> entities = __instance.__query_1297357609_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (prefabGUID.Equals(_fishingTravelToTarget)) // fishing travel to target, this indicates a succesful fishing event
                {
                    Entity character = entityOwner.Owner;
                    User user = character.ReadRO<PlayerCharacter>().UserEntity.ReadRO<User>();
                    ulong steamId = user.PlatformId;

                    PrefabGUID toProcess = PrefabGUID.Empty;
                    Entity target = entity.GetBuffTarget();

                    if (target.Has<DropTableBuffer>())
                    {
                        var dropTableBuffer = target.ReadBuffer<DropTableBuffer>();

                        if (!dropTableBuffer.IsEmpty)
                        {
                            toProcess = dropTableBuffer[0].DropTableGuid;
                        }
                    }

                    if (toProcess.IsEmpty()) continue;

                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(toProcess, "");
                    if (handler != null)
                    {
                        int multiplier = ProfessionMappings.GetFishingModifier(toProcess);

                        ProfessionSystem.SetProfession(target, character, steamId, BASE_FISHING_XP * multiplier, handler);
                        ProfessionSystem.GiveProfessionBonus(toProcess, character, user, steamId, handler);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}