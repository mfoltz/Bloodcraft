using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Systems.Expertise.WeaponStats.WeaponStatManager;
using static Bloodcraft.Systems.Legacies.BloodStats.BloodStatManager;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class EquipmentPatches
{
    static readonly bool Leveling = Plugin.LevelingSystem.Value;

    static readonly bool Expertise = Plugin.ExpertiseSystem.Value;

    static readonly bool Professions = Plugin.ProfessionSystem.Value;

    [HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void WeaponLevelDestroyPostfix(WeaponLevelSystem_Destroy __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Leveling && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited LevelPrefix early: {e}");
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
                if (Expertise && entity.Has<WeaponLevel>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    PrefabGUID prefab = entity.Read<PrefabGUID>();
                    ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());
                    if (weaponType.Equals(ExpertiseSystem.WeaponType.FishingPole)) continue;
                    if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed))
                    {
                        ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, weaponType, entity);
                        Core.ModifyUnitStatBuffSystem_Spawn.OnUpdate();
                    }
                }

                if (Leveling && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    GearOverride.SetLevel(character);
                }
                else if (!Leveling && entity.Has<WeaponLevel>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    ulong steamId = entity.Read<EntityOwner>().Owner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    int weaponLevel = (int)entity.Read<WeaponLevel>().Level;
                    int unarmedLevel = 0;
                    Entity character = entity.Read<EntityOwner>().Owner;

                    Equipment equipment = character.Read<Equipment>(); // fix weapon level on equipment if leveling turned off?
                    if (equipment.WeaponLevel._Value != 0)
                    {
                        equipment.WeaponLevel._Value = 0;
                        character.Write(equipment);
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

                    PrefabGUID prefab = entity.Read<PrefabGUID>();
                    ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());
                    if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed))
                    {
                        entity.Write(new WeaponLevel { Level = unarmedLevel });
                    }
                } 
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited WeaponLevelSystem_Spawn system early: (ignore this at first character spawn) {e}");
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
                if (Expertise && Leveling && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    PrefabGUID prefab = entity.Read<PrefabGUID>();

                    ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());
                    //if (!weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed) && !weaponType.Equals(ExpertiseSystem.WeaponType.FishingPole)) GearOverride.SetWeaponItemLevel(character.Read<Equipment>(), ExpertiseHandlerFactory.GetExpertiseHandler(weaponType).GetExpertiseData(steamId).Key, Core.EntityManager);
                    
                    Entity player = entity.Read<EntityOwner>().Owner;
                    GearOverride.SetLevel(player);
                }
                else if (Leveling && !Expertise && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    GearOverride.SetLevel(character);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited LevelPrefix early: {e}");
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
                if (Leveling && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    if (entity.Has<ArmorLevel>()) entity.Write(new ArmorLevel { Level = 0f });
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited LevelPrefix early: {e}");
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
                if (Leveling && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited LevelPrefix early: {e}");
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
                if (Leveling && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited LevelPrefix early: {e}");
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
                if (Expertise && entity.Has<WeaponLevel>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());

                    if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed) || weaponType.Equals(ExpertiseSystem.WeaponType.FishingPole)) continue;
                    ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, weaponType, entity);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
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
                if (Leveling && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
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
                if (Professions && inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<CastleWorkstation>())
                {
                    Entity userEntity = inventory.Read<InventoryConnection>().InventoryOwner.Read<UserOwner>().Owner._Entity;
                    // get ulong of online clanmates
                    ulong steamId = userEntity.Read<User>().PlatformId;
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(inventoryChangedEvent.Item, "");
                    if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(steamId, out var playerJobs))
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
                        if (handler != null)
                        {
                            ProfessionSystem.SetProfession(userEntity.Read<User>(), steamId, ProfessionValue, handler);
                            Entity itemEntity = inventoryChangedEvent.ItemEntity;
                            switch (handler)
                            {
                                case BlacksmithingHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetExperienceData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)Plugin.MaxProfessionLevel.Value);
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
                                        durability.MaxDurability *= (1 + (float)level / (float)Plugin.MaxProfessionLevel.Value);
                                        durability.Value = durability.MaxDurability;
                                        itemEntity.Write(durability);
                                    }
                                    break;
                                case TailoringHandler:
                                    if (itemEntity.Has<Durability>())
                                    {
                                        Durability durability = itemEntity.Read<Durability>();
                                        int level = handler.GetExperienceData(steamId).Key;
                                        durability.MaxDurability *= (1 + (float)level / (float)Plugin.MaxProfessionLevel.Value);
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
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
}

internal static class ModifyUnitStatBuffUtils // need to move this out of equipment patches
{
    static readonly float SynergyMultiplier = Plugin.StatSynergyMultiplier.Value;

    static readonly float PrestigeMultiplier = Plugin.PrestigeStatMultiplier.Value;

    static readonly int MaxExpertiseLevel = Plugin.MaxExpertiseLevel.Value;

    static readonly int MaxBloodLevel = Plugin.MaxBloodLevel.Value;
    public static void ApplyWeaponBonuses(Entity character, ExpertiseSystem.WeaponType weaponType, Entity weaponEntity)
    {
        ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

        //if (!weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed)) GearOverride.SetWeaponItemLevel(equipment, handler.GetExpertiseData(steamId).Key, Core.EntityManager);
        
        if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
        {
            if (!weaponEntity.Has<ModifyUnitStatBuff_DOTS>())
            {
                Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(weaponEntity);
            }
            var buffer = weaponEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
            foreach (var weaponStatType in bonuses)
            {
                float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponType, weaponStatType);
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
    public static void ApplyBloodBonuses(Entity character, BloodSystem.BloodType bloodType, Entity bloodBuff)
    {
        ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);

        if (Core.DataStructures.PlayerBloodStats.TryGetValue(steamId, out var bloodStats) && bloodStats.TryGetValue(bloodType, out var bonuses))
        {
            if (!bloodBuff.Has<ModifyUnitStatBuff_DOTS>())
            {
                Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(bloodBuff);
            }

            var buffer = bloodBuff.ReadBuffer<ModifyUnitStatBuff_DOTS>();
            foreach (var bloodStatType in bonuses)
            {
                float scaledBonus = CalculateScaledBloodBonus(handler, steamId, bloodType, bloodStatType);


                bool found = false;
                for (int i = 0; i < buffer.Length; i++)
                {
                    ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                    if (statBuff.StatType.Equals(BloodStatMap[bloodStatType])) 
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
                    UnitStatType statType = BloodStatMap[bloodStatType];
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
    public static float CalculateScaledWeaponBonus(IExpertiseHandler handler, ulong steamId, ExpertiseSystem.WeaponType weaponType, WeaponStats.WeaponStatManager.WeaponStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetExpertiseData(steamId);
            float maxBonus = WeaponStats.WeaponStatManager.BaseCaps[statType];

            if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) && classes.Count > 0)
            {
                List<int> playerClassStats = classes.First().Value.Item1;
                List<WeaponStats.WeaponStatManager.WeaponStatType> weaponStatTypes = playerClassStats.Select(value => (WeaponStats.WeaponStatManager.WeaponStatType)value).ToList();
                if (weaponStatTypes.Contains(statType))
                {
                    maxBonus *= SynergyMultiplier;
                }
                
            }
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(ExpertiseSystem.WeaponPrestigeMap[weaponType], out var PrestigeData) && PrestigeData > 0)
            {
                float gainFactor = 1 + (PrestigeMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }
            
            float scaledBonus = maxBonus * ((float)xpData.Key / MaxExpertiseLevel); // Scale bonus up to 99%
            return scaledBonus;
        }
        return 0; // Return 0 if no handler is found or other error
    }
    public static float CalculateScaledBloodBonus(IBloodHandler handler, ulong steamId, BloodSystem.BloodType bloodType, BloodStats.BloodStatManager.BloodStatType statType)
    {
        if (handler != null)
        {
            var xpData = handler.GetLegacyData(steamId);
            float maxBonus = BloodStats.BloodStatManager.BaseCaps[statType];

            if (Core.DataStructures.PlayerClasses.TryGetValue(steamId, out var classes) && classes.Count > 0)
            {
                List<int> playerClassStats = classes.First().Value.Item2;
                List<BloodStats.BloodStatManager.BloodStatType> bloodStatTypes = playerClassStats.Select(value => (BloodStats.BloodStatManager.BloodStatType)value).ToList();
                if (bloodStatTypes.Contains(statType))
                {
                    maxBonus *= SynergyMultiplier;
                }
                
            }
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestiges) && prestiges.TryGetValue(BloodSystem.BloodPrestigeMap[bloodType], out var PrestigeData) && PrestigeData > 0)
            {
                float gainFactor = 1 + (PrestigeMultiplier * PrestigeData);
                maxBonus *= gainFactor;
            }
            
            float scaledBonus = maxBonus * ((float)xpData.Key / MaxBloodLevel); // Scale bonus up to 99%
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
internal static class GearOverride // also this
{
    public static void SetLevel(Entity player)
    {
        ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
        {
            int playerLevel = xpData.Key;
            Equipment equipment = player.Read<Equipment>();

            equipment.ArmorLevel._Value = 0f;
            equipment.SpellLevel._Value = 0f;
            equipment.WeaponLevel._Value = playerLevel;
            player.Write(equipment);
        }
    }
}
