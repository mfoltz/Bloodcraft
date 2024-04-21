using Bloodstone.API;
using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks
{
    [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
    public static class EquipItemSystemPatches
    {
        private static EntityManager EntityManager { get; } = VWorld.Server.EntityManager;
        private static ServerGameManager ServerGameManager { get; } = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
        public static void Prefix(EquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("EquipItemSystem Prefix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (!entity.Has<FromCharacter>()) continue;
                    Entity character = entity.Read<FromCharacter>().Character;
                    GearOverride.SetLevel(ServerGameManager, EntityManager, character);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited EquipItemSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        public static void Postfix(EquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("EquipItemSystem Postfix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (!entity.Has<PlayerCharacter>()) continue;
                    GearOverride.SetLevel(ServerGameManager, EntityManager, entity);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited EquipItemSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
    /*
    [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
    public static class UnequipItemSystemPatch
    {
        public static void Prefix(UnEquipItemSystem __instance)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            Plugin.Log.LogInfo("UnEquipItemSystem Prefix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    Utilities.LogComponentTypes(entity);
                    if (entityManager.HasComponent<ArmorLevel>(entity))
                    {
                        var armorLevel = entityManager.GetComponentData<ArmorLevel>(entity);
                        armorLevel.Level = 0;
                        entityManager.SetComponentData(entity, armorLevel);
                        Plugin.Log.LogInfo("Set armorLevel to 0...");
                    }
                    else if (entityManager.HasComponent<WeaponLevel>(entity))
                    {
                        var weaponLevel = entityManager.GetComponentData<WeaponLevel>(entity);
                        weaponLevel.Level = 0;
                        entityManager.SetComponentData(entity, weaponLevel);
                        Plugin.Log.LogInfo("Set weaponLevel to 0...");
                    }
                    else if (entityManager.HasComponent<SpellLevel>(entity))
                    {
                        var spellLevel = entityManager.GetComponentData<SpellLevel>(entity);
                        spellLevel.Level = 0;
                        entityManager.SetComponentData(entity, spellLevel);
                        Plugin.Log.LogInfo("Set spellLevel to 0...");
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogInfo($"Exited UnEquipItemSystem hook early {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        public static void Postfix(UnEquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("UnEquipItemSystem Postfix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (!entity.Has<PlayerCharacter>()) continue;
                    entity.LogComponentTypes();
                    Entity userEntity = entity.Read<PlayerCharacter>().UserEntity;
                    GearOverride.SetLevel(userEntity);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited UnEquipItemSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
    */
    /*
    [HarmonyPatch(typeof(ItemPickupSystem), nameof(ItemPickupSystem.OnUpdate))]
    public static class EquipmentSystemPatch
    {
        public static void Prefix(EquipmentSystem __instance)
        {
            //Plugin.Log.LogInfo("EquipmentSystem Prefix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob1_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    //entity.LogComponentTypes();
                    Entity player = entity.Read<Equippable>().EquipTarget._Entity;
                    if (player.Equals(Entity.Null)) continue;
                    else
                    {
                        player.LogComponentTypes();
                        GearOverride.SetLevel(player);
                    }
                    
                    
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited EquipmentSystem hook early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
    */
    public static class GearOverride
    {
        public static void SetLevel(ServerGameManager serverGameManager, EntityManager entityManager, Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
            {
                Equipment equipment = character.Read<Equipment>();
                float playerLevel = xpData.Key;
                equipment.ArmorLevel._Value = 0f;
                equipment.WeaponLevel._Value = 0f;
                equipment.SpellLevel._Value = playerLevel;
                character.Write(equipment);
                Plugin.Log.LogInfo($"Set gearScore to {playerLevel}.");
            }
            
        }
    }
}