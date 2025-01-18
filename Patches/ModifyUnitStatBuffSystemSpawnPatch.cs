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
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;

    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Core._initialized) return;
        else if (!_expertise) return;

        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!entity.Has<WeaponLevel>() || !entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;

                if (entityOwner.Owner.TryGetPlayer(out Entity player))
                {
                    WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(entity);

                    if (weaponType.Equals(WeaponType.Unarmed) || weaponType.Equals(WeaponType.FishingPole)) continue; // handled in weapon level spawn since they shouldn't show up here but just incase

                    ulong steamId = player.GetSteamId();

                    //if (ConfigService.ProfessionSystem) EquipmentManager.ApplyEquipmentStats(steamId, weaponEntity);
                    WeaponManager.ApplyWeaponStats(steamId, weaponType, entity);
                }

                /*
                else if (ConfigService.ProfessionSystem && entity.Has<ArmorLevel>() && entity.GetOwner().TryGetPlayer(out player))
                {
                    Entity armorEntity = entity.Read<EquippableBuff>().ItemSource;
                    if (!armorEntity.Has<BlockFeedBuff>()) continue;
                    else EquipmentManager.ApplyEquipmentStats(player.GetSteamId(), armorEntity);
                }
                else if (ConfigService.ProfessionSystem && entity.Has<SpellLevel>() && entity.GetOwner().TryGetPlayer(out player))
                {
                    Entity sourceEntity = entity.Read<EquippableBuff>().ItemSource;
                    if (!sourceEntity.Has<BlockFeedBuff>()) continue;
                    else EquipmentManager.ApplyEquipmentStats(player.GetSteamId(), sourceEntity);
                }
                */
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
        if (!Core._initialized) return;
        else if (!_leveling) return;

        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;

                if (entityOwner.Owner.TryGetPlayer(out Entity player))
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
