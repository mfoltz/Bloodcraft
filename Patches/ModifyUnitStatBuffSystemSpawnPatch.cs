using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Leveling;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ModifyUnitStatBuffSystemSpawnPatch
{
    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.ExpertiseSystem) return;

        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                if (!entity.Has<WeaponLevel>() || !entity.Has<EntityOwner>()) continue;

                if (entity.GetOwner().TryGetPlayer(out Entity player))
                {
                    WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(entity);

                    if (weaponType.Equals(WeaponType.Unarmed) || weaponType.Equals(WeaponType.FishingPole)) continue; // handled in weapon level spawn since they shouldn't show up here but just incase

                    ulong steamId = player.GetSteamId();
                    WeaponManager.ApplyWeaponStats(steamId, weaponType, entity);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.LevelingSystem) return;

        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!entity.Has<EntityOwner>()) continue;

                if (entity.GetOwner().TryGetPlayer(out Entity player))
                {
                    LevelingSystem.SetLevel(player);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
