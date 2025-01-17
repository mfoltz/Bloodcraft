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

    const int BASE_FISHING_XP = 100;

    static readonly PrefabGUID _fishingTravelToTarget = new(-1130746976);

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
                    Entity playerCharacter = entityOwner.Owner;
                    Entity userEntity = playerCharacter.GetUserEntity();

                    User user = userEntity.GetUser();
                    ulong steamId = user.PlatformId;

                    PrefabGUID prefabGuid = PrefabGUID.Empty;
                    Entity target = entity.GetBuffTarget();

                    if (target.Has<DropTableBuffer>())
                    {
                        var dropTableBuffer = target.ReadBuffer<DropTableBuffer>();

                        if (!dropTableBuffer.IsEmpty)
                        {
                            prefabGuid = dropTableBuffer[0].DropTableGuid;
                        }
                    }

                    if (prefabGuid.IsEmpty()) continue;

                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGuid, "");
                    if (handler != null)
                    {
                        int multiplier = ProfessionMappings.GetFishingModifier(prefabGuid);

                        float delay = 0.75f; // make const or whatever later

                        ProfessionSystem.SetProfession(target, playerCharacter, steamId, BASE_FISHING_XP * multiplier, handler, ref delay);
                        ProfessionSystem.GiveProfessionBonus(target, prefabGuid, playerCharacter, userEntity, user, steamId, handler, delay);
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