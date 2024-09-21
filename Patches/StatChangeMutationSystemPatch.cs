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
        if (!Core.hasInitialized) return;
        if (!ConfigService.BloodSystem || !ConfigService.BloodQualityBonus) return;

        NativeArray<Entity> entities = __instance._StatChangeEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<StatChangeEvent>() && entity.Has<BloodQualityChange>())
                {
                    StatChangeEvent statChangeEvent = entity.Read<StatChangeEvent>();
                    BloodQualityChange bloodQualityChange = entity.Read<BloodQualityChange>();

                    if (!statChangeEvent.Entity.Has<Blood>()) continue;

                    BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(bloodQualityChange.BloodType);
                    ulong steamID = statChangeEvent.Entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                    if (bloodHandler == null) continue;

                    float quality = bloodHandler.GetLegacyData(steamID).Value;

                    if (ConfigService.PrestigeSystem && steamID.TryGetPlayerPrestiges(out var prestiges) && prestiges.TryGetValue(BloodSystem.BloodTypeToPrestigeMap[bloodType], out var bloodPrestige))
                    {
                        float qualityPercentBonus = ConfigService.PrestigeBloodQuality > 1f ? ConfigService.PrestigeBloodQuality : ConfigService.PrestigeBloodQuality * 100f;
                        Core.Log.LogInfo($"Prestige blood quality bonus in StatChangeMutationSystem: {qualityPercentBonus}");

                        quality = (float)bloodPrestige * qualityPercentBonus;
                        if (quality > 0)
                        {
                            bloodQualityChange.Quality += quality;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
                    }
                    else if (!ConfigService.PrestigeSystem)
                    {
                        if (quality > 0)
                        {
                            bloodQualityChange.Quality += quality;
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
