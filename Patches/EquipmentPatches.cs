using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Expertise.WeaponStats;
using static Bloodcraft.Systems.Expertise.WeaponStats.WeaponStatManager;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class EquipmentPatches
{
    [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(WeaponLevelSystem_Spawn __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.ExpertiseSystem.Value && !Plugin.LevelingSystem.Value && entity.Has<WeaponLevel>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());
                    GearOverride.SetWeaponItemLevel(character.Read<Equipment>(), ExpertiseHandlerFactory.GetExpertiseHandler(weaponType).GetExpertiseData(steamId).Key, Core.EntityManager);

                    Equipment equipment = character.Read<Equipment>();
                    equipment.WeaponLevel._Value = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[entity.Read<PrefabGUID>()].Read<WeaponLevelSource>().Level;
                }
                else if (Plugin.ExpertiseSystem.Value && Plugin.LevelingSystem.Value && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());
                    GearOverride.SetWeaponItemLevel(character.Read<Equipment>(), ExpertiseHandlerFactory.GetExpertiseHandler(weaponType).GetExpertiseData(steamId).Key, Core.EntityManager);

                    Entity player = entity.Read<EntityOwner>().Owner;
                    GearOverride.SetLevel(player);
                }
                else if (Plugin.LevelingSystem.Value && !Plugin.ExpertiseSystem.Value && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    GearOverride.SetLevel(character);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited LevelPrefix early: {e}");
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
                if (Plugin.LevelingSystem.Value)
                {
                    if (entity.Has<ArmorLevel>()) entity.Write(new ArmorLevel { Level = 0f });
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited LevelPrefix early: {e}");
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
                if (Plugin.LevelingSystem.Value && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited LevelPrefix early: {e}");
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
                if (Plugin.LevelingSystem.Value && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited LevelPrefix early: {e}");
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
                if (Plugin.ExpertiseSystem.Value && entity.Has<WeaponLevel>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());
                    if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed)) continue;
                    ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, weaponType, entity);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
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
        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                if (Plugin.LevelingSystem.Value && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DealDamageSystem __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                if (Plugin.ExpertiseSystem.Value)
                {
                    DealDamageEvent dealDamageEvent = entity.Read<DealDamageEvent>();
                    Entity entityOwner = dealDamageEvent.SpellSource.Read<EntityOwner>().Owner;
                    EntityCategory entityCategory = dealDamageEvent.Target.Read<EntityCategory>();
                    if (!entityCategory.MainCategory.Equals(MainEntityCategory.Resource) || !entityOwner.Has<Equipment>()) continue;
                    Entity weapon = entityOwner.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
                    if (weapon.Equals(Entity.Null) || !weapon.Has<WeaponLevelSource>()) continue;
                    if (weapon.Read<WeaponLevelSource>().Level >= entityCategory.ResourceLevel._Value) continue;
                    float originalSource = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[weapon.Read<PrefabGUID>()].Read<WeaponLevelSource>().Level;
                    if (originalSource >= entityCategory.ResourceLevel._Value)
                    {
                        entityCategory.ResourceLevel._Value = 0;
                        dealDamageEvent.Target.Write(entityCategory);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
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
        NativeArray<Entity> entities = __instance.__query_2096870024_0.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (var entity in entities)
            {
                InventoryChangedEvent inventoryChangedEvent = entity.Read<InventoryChangedEvent>();
                Entity inventory = inventoryChangedEvent.InventoryEntity;
                if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<PlayerCharacter>())
                {
                    if (Plugin.ExpertiseSystem.Value && inventoryChangedEvent.ItemEntity.Has<WeaponLevelSource>())
                    {
                        ulong steamId = inventory.Read<InventoryConnection>().InventoryOwner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(inventoryChangedEvent.ItemEntity.Read<PrefabGUID>());
                        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
                        if (handler != null)
                        {
                            WeaponLevelSource weaponLevelSource = new()
                            {
                                Level = handler.GetExpertiseData(steamId).Key
                            };
                            inventoryChangedEvent.ItemEntity.Write(weaponLevelSource);
                        }
                    }
                }
                else if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Removed) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<PlayerCharacter>())
                {
                    if (Plugin.ExpertiseSystem.Value && inventoryChangedEvent.ItemEntity.Has<WeaponLevelSource>() )
                    {
                        inventoryChangedEvent.ItemEntity.Write(new WeaponLevelSource { Level = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[inventoryChangedEvent.Item].Read<WeaponLevelSource>().Level });
                    }
                }
                else if (Plugin.ProfessionSystem.Value && inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<CastleWorkstation>())
                {
                    Entity userEntity = inventory.Read<InventoryConnection>().InventoryOwner.Read<UserOwner>().Owner._Entity;
                    NetworkId networkId = inventory.Read<InventoryConnection>().InventoryOwner.Read<NetworkId>();
                    ulong steamId = userEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(networkId, out var jobs) && jobs.TryGetValue(steamId, out var playerJobs))
                    {
                        bool jobExists = false;
                        for (int i = 0; i < playerJobs.Count; i++)
                        {
                            if (playerJobs[i].Item1 == inventoryChangedEvent.Item && playerJobs[i].Item2 > 0)
                            {
                                playerJobs[i] = (playerJobs[i].Item1, playerJobs[i].Item2 - 1);
                                if (playerJobs[i].Item2 == 0) playerJobs.RemoveAt(i);
                                jobExists = true;
                                break;
                            }
                        }
                        if (!jobExists) continue;
                        float ProfessionValue = 50f;
                        ProfessionValue *= ProfessionUtilities.GetTierMultiplier(inventoryChangedEvent.Item);
                        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(inventoryChangedEvent.Item, "");
                        if (handler != null)
                        {
                            ProfessionSystem.SetProfession(userEntity.Read<User>(), steamId, ProfessionValue, handler);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
}

public static class ModifyUnitStatBuffUtils
{
    public static void ApplyWeaponBonuses(Entity character, ExpertiseSystem.WeaponType weaponType, Entity weaponEntity)
    {
        ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
        Equipment equipment = character.Read<Equipment>();

        GearOverride.SetWeaponItemLevel(equipment, handler.GetExpertiseData(steamId).Key, Core.EntityManager);

        if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
        {
            var buffer = weaponEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
            foreach (var weaponStatType in bonuses)
            {
                float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponStatType);
                bool found = false;
                for (int i = 0; i < buffer.Length; i++)
                {
                    ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                    if (statBuff.StatType.Equals(WeaponStatMap[weaponStatType])) // Assuming WeaponStatType can be cast to UnitStatType
                    {
                        statBuff.Value += scaledBonus; // Modify the value accordingly
                        buffer[i] = statBuff; // Assign the modified struct back to the buffer
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // If not found, create a new stat modifier
                    UnitStatType statType = WeaponStatMap[weaponStatType];
                    ModifyUnitStatBuff_DOTS newStatBuff = new()
                    {
                        StatType = statType,
                        ModificationType = ModificationType.AddToBase,
                        Value = scaledBonus,
                        Modifier = 1,
                        IncreaseByStacks = false,
                        ValueByStacks = 0,
                        Priority = 0,
                        Id = ModificationId.Empty
                    };
                    buffer.Add(newStatBuff);
                }
            }
        }
    }
    public static float CalculateScaledWeaponBonus(IExpertiseHandler handler, ulong steamId, WeaponStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetExpertiseData(steamId);
            int currentLevel = ExpertiseSystem.ConvertXpToLevel(xpData.Value);
            float maxBonus = WeaponStatManager.BaseCaps[statType];
            float scaledBonus = maxBonus * (currentLevel / 99.0f); // Scale bonus up to 99%
            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    public static ExpertiseSystem.WeaponType GetCurrentWeaponType(Entity character)
    {
        Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
        if (weapon.Equals(Entity.Null)) return ExpertiseSystem.WeaponType.Unarmed;
        return ExpertiseSystem.GetWeaponTypeFromPrefab(weapon.Read<PrefabGUID>());
    }
    public static BloodSystem.BloodType GetCurrentBloodType(Entity character)
    {
        Blood blood = character.Read<Blood>();

        return BloodSystem.GetBloodTypeFromPrefab(blood.BloodType);
    }
}

public static class GearOverride
{
    public static void SetLevel(Entity player, int extra = 0)
    {
        ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
        {
            int playerLevel = xpData.Key + extra;
            Equipment equipment = player.Read<Equipment>();

            if (Plugin.LevelingSystem.Value && Plugin.ExpertiseSystem.Value)
            {
                equipment.ArmorLevel._Value = 0f;
                equipment.SpellLevel._Value = 0f;
                equipment.WeaponLevel._Value = playerLevel;
                player.Write(equipment);
            }
            else if (Plugin.LevelingSystem.Value && !Plugin.ExpertiseSystem.Value)
            {
                equipment.ArmorLevel._Value = 0f;
                equipment.SpellLevel._Value = playerLevel;
                equipment.WeaponLevel._Value = 0f;
                player.Write(equipment);
            }
        }
    }
    public static void SetWeaponItemLevel(Equipment equipment, int level, EntityManager entityManager)
    {
        Entity weaponEntity = equipment.WeaponSlot.SlotEntity._Entity;
        if (!weaponEntity.Equals(Entity.Null) && entityManager.HasComponent<WeaponLevelSource>(weaponEntity))
        {
            weaponEntity.Write(new WeaponLevelSource { Level = level });
        }
    }
}
