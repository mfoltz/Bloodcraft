using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Leveling;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using Unity.Collections;
using Unity.Entities;
using User = ProjectM.Network.User;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class GearLevelPatches // WeaponLevelSystem_Spawn, WeaponLevelSystem_Destroy, ArmorLevelSystem_Spawn, ArmorLevelSystem_Destroy
{
    static SystemService SystemService => Core.SystemService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystemSpawn => SystemService.ModifyUnitStatBuffSystem_Spawn;

    static readonly Dictionary<ulong, int> PlayerMaxWeaponLevels = [];

    [HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void WeaponLevelDestroyPostfix(WeaponLevelSystem_Destroy __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.LevelingSystem) return;

        NativeArray<Entity> entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out Entity player))
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

    [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(WeaponLevelSystem_Spawn __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<WeaponLevel>() || !entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;

                if (ConfigService.ExpertiseSystem && entityOwner.Owner.TryGetPlayer(out Entity player))
                {
                    ulong steamId = player.GetSteamId();

                    WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(entity);
                    if (weaponType.Equals(WeaponType.Unarmed) || weaponType.Equals(WeaponType.FishingPole)) // apply it here since it won't appear in the system naturally as they don't have the component till added
                    {
                        WeaponManager.ApplyWeaponStats(steamId, weaponType, entity);
                        ModifyUnitStatBuffSystemSpawn.OnUpdate();
                    }
                }

                if (ConfigService.LevelingSystem && entityOwner.Owner.TryGetPlayer(out player))
                {
                    LevelingSystem.SetLevel(player);
                }
                else if (!ConfigService.LevelingSystem && ConfigService.ExpertiseSystem && entityOwner.Owner.TryGetPlayer(out player))
                {
                    ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    int weaponLevel = (int)entity.Read<WeaponLevel>().Level;
                    int unarmedWeaponLevel = 0;

                    Equipment equipment = player.Read<Equipment>(); // fix weapon level on equipment if leveling turned off?
                    if (equipment.WeaponLevel._Value != 0)
                    {
                        equipment.WeaponLevel._Value = 0;
                        player.Write(equipment);
                    }

                    if (!PlayerMaxWeaponLevels.ContainsKey(steamId))
                    {
                        PlayerMaxWeaponLevels[steamId] = weaponLevel;
                    }
                    else
                    {
                        int maxWeaponLevel = PlayerMaxWeaponLevels[steamId];

                        if (weaponLevel > maxWeaponLevel)
                        {
                            PlayerMaxWeaponLevels[steamId] = weaponLevel;
                            unarmedWeaponLevel = weaponLevel;
                        }
                    }

                    WeaponType weaponType = WeaponSystem.GetWeaponTypeFromWeaponEntity(entity);
                    if (weaponType.Equals(WeaponType.Unarmed))
                    {
                        entity.Write(new WeaponLevel { Level = unarmedWeaponLevel });
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(WeaponLevelSystem_Spawn __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.LevelingSystem) return;

        NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out Entity player))
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

    [HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void ArmorLevelSpawnPrefix(ArmorLevelSystem_Spawn __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.LevelingSystem) return;

        NativeArray<Entity> entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.IsPlayer())
                {
                    if (entity.Has<ArmorLevel>()) entity.Write(new ArmorLevel { Level = 0f });
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void ArmorLevelSpawnPostfix(ArmorLevelSystem_Spawn __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.LevelingSystem) return;

        NativeArray<Entity> entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out Entity player))
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

    [HarmonyPatch(typeof(ArmorLevelSystem_Destroy), nameof(ArmorLevelSystem_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void ArmorLevelDestroyPostfix(ArmorLevelSystem_Destroy __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.LevelingSystem) return;

        NativeArray<Entity> entities = __instance.__query_663986292_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out Entity player))
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
