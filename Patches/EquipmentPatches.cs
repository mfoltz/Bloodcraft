using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using User = ProjectM.Network.User;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class EquipmentPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystem_Spawn => SystemService.ModifyUnitStatBuffSystem_Spawn;

    [HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void WeaponLevelDestroyPostfix(WeaponLevelSystem_Destroy __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().HasPlayer(out Entity player))
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
        NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<WeaponLevel>() || !entity.Has<EntityOwner>()) continue;

                Entity player;
                if (ConfigService.ExpertiseSystem && entity.GetOwner().HasPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    WeaponSystem.WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(entity);
                    if (weaponType.Equals(WeaponSystem.WeaponType.Unarmed) || weaponType.Equals(WeaponSystem.WeaponType.FishingPole)) // apply it here since it won't appear in the system naturally as they don't have the component till added
                    {
                        WeaponHandler.ApplyWeaponBonuses(steamId, weaponType, entity);
                        ModifyUnitStatBuffSystem_Spawn.OnUpdate();
                    }
                }

                if (ConfigService.LevelingSystem && entity.GetOwner().HasPlayer(out player))
                {
                    LevelingSystem.SetLevel(player);
                }
                else if (!ConfigService.LevelingSystem && ConfigService.ExpertiseSystem && entity.GetOwner().HasPlayer(out player))
                {
                    ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    int weaponLevel = (int)entity.Read<WeaponLevel>().Level;
                    int unarmedLevel = 0;

                    Equipment equipment = player.Read<Equipment>(); // fix weapon level on equipment if leveling turned off?
                    if (equipment.WeaponLevel._Value != 0)
                    {
                        equipment.WeaponLevel._Value = 0;
                        player.Write(equipment);
                    }

                    if (Core.DataStructures.PlayerMaxWeaponLevels.ContainsKey(steamId))
                    {
                        if (weaponLevel > Core.DataStructures.PlayerMaxWeaponLevels[steamId])
                        {
                            Core.DataStructures.PlayerMaxWeaponLevels[steamId] = weaponLevel;
                            unarmedLevel = weaponLevel;
                        }
                        else
                        {
                            unarmedLevel = Core.DataStructures.PlayerMaxWeaponLevels[steamId];
                        }
                    }
                    else
                    {
                        Core.DataStructures.PlayerMaxWeaponLevels.TryAdd(steamId, weaponLevel);
                        unarmedLevel = weaponLevel;
                    }

                    WeaponSystem.WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(entity);
                    if (weaponType.Equals(WeaponSystem.WeaponType.Unarmed))
                    {
                        entity.Write(new WeaponLevel { Level = unarmedLevel });
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
        NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().HasPlayer(out Entity player))
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
        NativeArray<Entity> entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().IsVampire())
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
        NativeArray<Entity> entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().HasPlayer(out Entity player))
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
        NativeArray<Entity> entities = __instance.__query_663986292_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().HasPlayer(out Entity player))
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

    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ProjectM.ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<WeaponLevel>() || !entity.Has<EntityOwner>()) continue;

                if (ConfigService.ExpertiseSystem && entity.GetOwner().HasPlayer(out Entity player))
                {
                    WeaponSystem.WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(entity);

                    if (weaponType.Equals(WeaponSystem.WeaponType.Unarmed) || weaponType.Equals(WeaponSystem.WeaponType.FishingPole)) continue;

                    ulong steamId = player.GetSteamId();
                    WeaponHandler.ApplyWeaponBonuses(steamId, weaponType, entity);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    
    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ProjectM.ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().HasPlayer(out Entity player))
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

    [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_2096870024_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!Core.hasInitialized) continue;

                InventoryChangedEvent inventoryChangedEvent = entity.Read<InventoryChangedEvent>();
                Entity inventory = inventoryChangedEvent.InventoryEntity;

                if (!inventory.Exists()) continue;
                if (ConfigService.ProfessionSystem && inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<UserOwner>())
                {
                    //Core.Log.LogInfo("entered job processing");
                    UserOwner userOwner = inventory.Read<InventoryConnection>().InventoryOwner.Read<UserOwner>();
                    if (!EntityManager.Exists(userOwner.Owner._Entity))
                    {
                        //Core.Log.LogInfo("user does not exist");
                        continue;
                    }

                    Entity userEntity = userOwner.Owner._Entity;
                    PrefabGUID itemPrefab = inventoryChangedEvent.Item;

                    if (inventoryChangedEvent.ItemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = inventoryChangedEvent.ItemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                        itemPrefab = inventoryChangedEvent.ItemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>()[tier].TierPrefab;
                    }

                    User user = userEntity.Read<User>();
                    ulong steamId = user.PlatformId;
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefab, "");
                    if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(userEntity, out var playerJobs) && playerJobs.TryGetValue(itemPrefab, out int credits) && credits > 0)
                    {
                        credits--;
                        if (credits == 0)
                        {
                            playerJobs.Remove(itemPrefab);
                        }
                        else
                        {
                            playerJobs[itemPrefab] = credits;
                        }
                        float ProfessionValue = 50f;
                        ProfessionValue *= ProfessionMappings.GetTierMultiplier(itemPrefab);
                        if (handler != null)
                        {
                            if (handler.GetProfessionName().ToLower().Contains("alchemy"))
                            {
                                ProfessionSystem.SetProfession(user, steamId, ProfessionValue * 3, handler);
                                continue;
                            }
                            ProfessionSystem.SetProfession(user, steamId, ProfessionValue, handler);
                            Entity itemEntity = inventoryChangedEvent.ItemEntity;
                            switch (handler)
                            {
                                case BlacksmithingHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetExperienceData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                                case AlchemyHandler:
                                    break;
                                case EnchantingHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetExperienceData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                                case TailoringHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetExperienceData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                            }
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
