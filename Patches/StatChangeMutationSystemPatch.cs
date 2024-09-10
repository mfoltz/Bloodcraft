using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class StatChangeMutationSystemPatch
{

    [HarmonyPatch(typeof(StatChangeMutationSystem), nameof(StatChangeMutationSystem.OnUpdate))] // increase blood quality if configured based on legacy/prestige
    [HarmonyPrefix]
    static void OnUpdatePrefix(StatChangeMutationSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StatChangeEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;

                if (!entity.Exists()) continue;

                if (ConfigService.BloodSystem && ConfigService.BloodQualityBonus && entity.Has<StatChangeEvent>() && entity.Has<BloodQualityChange>())
                {
                    StatChangeEvent statChangeEvent = entity.Read<StatChangeEvent>();
                    BloodQualityChange bloodQualityChange = entity.Read<BloodQualityChange>();

                    if (!statChangeEvent.Entity.Has<Blood>()) continue;

                    BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(bloodQualityChange.BloodType);
                    ulong steamID = statChangeEvent.Entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                    if (bloodHandler == null) continue;

                    float legacyKey = bloodHandler.GetLegacyData(steamID).Value;

                    if (ConfigService.PrestigeSystem && steamID.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(BloodSystem.BloodTypeToPrestigeMap[bloodType], out var bloodPrestige))
                    {
                        legacyKey = (float)bloodPrestige * ConfigService.PrestigeBloodQuality;
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
                    }
                    else if (!ConfigService.PrestigeSystem)
                    {
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
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
