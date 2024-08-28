using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Shared;
using Steamworks;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using User = ProjectM.Network.User;
using WeaponType = Bloodcraft.Systems.Expertise.WeaponType;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class EquipmentPatches
{
    static SystemService SystemService => Core.SystemService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystem => SystemService.ModifyUnitStatBuffSystem_Spawn;

    [HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void WeaponLevelDestroyPostfix(WeaponLevelSystem_Destroy __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;

                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().TryGetPlayer(out Entity player))
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
                if (!Core.hasInitialized) return;

                if (!entity.Has<WeaponLevel>() || !entity.Has<EntityOwner>()) continue;

                Entity player;
                if (ConfigService.ExpertiseSystem && entity.GetOwner().TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(entity);
                    if (weaponType.Equals(WeaponType.Unarmed) || weaponType.Equals(WeaponType.FishingPole)) // apply it here since it won't appear in the system naturally as they don't have the component till added
                    {
                        WeaponManager.ApplyWeaponBonuses(steamId, weaponType, entity);
                        ModifyUnitStatBuffSystem.OnUpdate();
                    }
                }

                if (ConfigService.LevelingSystem && entity.GetOwner().TryGetPlayer(out player))
                {
                    LevelingSystem.SetLevel(player);
                }
                else if (!ConfigService.LevelingSystem && ConfigService.ExpertiseSystem && entity.GetOwner().TryGetPlayer(out player))
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

                    if (steamId.TryGetPlayerMaxWeaponLevels(out var maxWeaponLevel))
                    {
                        if (weaponLevel > maxWeaponLevel)
                        {
                            steamId.SetPlayerMaxWeaponLevels(weaponLevel);
                            unarmedLevel = weaponLevel;
                        }
                        else
                        {
                            unarmedLevel = maxWeaponLevel;
                        }
                    }
                    else
                    {
                        steamId.SetPlayerMaxWeaponLevels(weaponLevel);
                        unarmedLevel = weaponLevel;
                    }

                    WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(entity);
                    if (weaponType.Equals(WeaponType.Unarmed))
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
                if (!Core.hasInitialized) return;

                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().TryGetPlayer(out Entity player))
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
                if (!Core.hasInitialized) return;

                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().IsPlayer())
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
                if (!Core.hasInitialized) return;

                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().TryGetPlayer(out Entity player))
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
                if (!Core.hasInitialized) return;

                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().TryGetPlayer(out Entity player))
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

    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                if (!Core.hasInitialized) return;

                if (!entity.Has<WeaponLevel>() || !entity.Has<EntityOwner>()) continue;

                if (ConfigService.ExpertiseSystem && entity.GetOwner().TryGetPlayer(out Entity player))
                {
                    WeaponType weaponType = WeaponSystem.GetWeaponTypeFromSlotEntity(entity);

                    if (weaponType.Equals(WeaponType.Unarmed) || weaponType.Equals(WeaponType.FishingPole)) continue; // handled in weapon level spawn since they shouldn't show up here but just incase

                    ulong steamId = player.GetSteamId();
                    WeaponManager.ApplyWeaponBonuses(steamId, weaponType, entity);
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
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var entity in entities)
            {
                if (!Core.hasInitialized) return;

                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.LevelingSystem && entity.GetOwner().TryGetPlayer(out Entity player))
                {
                    LevelingSystem.SetLevel(player);
                    ulong steamId = player.GetSteamId();

                    if (ConfigService.ClientCompanion && EclipseService.RegisteredUsers.Contains(steamId))
                    {
                        EclipseService.SendClientProgress(player, steamId);
                    }
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
                if (!Core.hasInitialized) return;

                InventoryChangedEvent inventoryChangedEvent = entity.Read<InventoryChangedEvent>();
                Entity inventory = inventoryChangedEvent.InventoryEntity;

                if (!inventory.Exists()) continue;
                if (ConfigService.ProfessionSystem && inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<UserOwner>())
                {
                    UserOwner userOwner = inventory.Read<InventoryConnection>().InventoryOwner.Read<UserOwner>();
                    Entity userEntity = userOwner.Owner._Entity;

                    if (!userEntity.Exists())
                    {
                        continue;
                    }

                    PrefabGUID itemPrefab = inventoryChangedEvent.Item;

                    if (inventoryChangedEvent.ItemEntity.Has<UpgradeableLegendaryItem>())
                    {
                        int tier = inventoryChangedEvent.ItemEntity.Read<UpgradeableLegendaryItem>().CurrentTier;
                        itemPrefab = inventoryChangedEvent.ItemEntity.ReadBuffer<UpgradeableLegendaryItemTiers>()[tier].TierPrefab;
                    }

                    User user = userEntity.Read<User>();
                    ulong steamId = user.PlatformId;

                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(itemPrefab, "");
                    if (steamId.TryGetPlayerCraftingJobs(out var playerJobs) && playerJobs.TryGetValue(itemPrefab, out int credits) && credits > 0)
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
                                        int level = handler.GetProfessionData(steamId).Key;
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
                                        int level = handler.GetProfessionData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                                case TailoringHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetProfessionData(steamId).Key;
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
