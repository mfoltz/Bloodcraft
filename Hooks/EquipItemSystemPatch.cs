using Bloodstone.API;
using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks
{
    [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
    public static class EquipItemSystemPatch
    {
        public static void Prefix(EquipItemSystem __instance)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;
            Plugin.Log.LogInfo("EquipItemSystem Prefix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    // Assuming Utilities.LogComponentTypes logs components; you may want to do actual work here.
                    Utilities.LogComponentTypes(entity);

                    // Reset levels based on the type of item equipped.
                    if (entityManager.HasComponent<ArmorLevel>(entity))
                    {
                        var armorLevel = entityManager.GetComponentData<ArmorLevel>(entity);
                        armorLevel.Level = 0;
                        entityManager.SetComponentData(entity, armorLevel);
                    }
                    else if (entityManager.HasComponent<WeaponLevel>(entity))
                    {
                        var weaponLevel = entityManager.GetComponentData<WeaponLevel>(entity);
                        weaponLevel.Level = 0;
                        entityManager.SetComponentData(entity, weaponLevel);
                    }
                    else if (entityManager.HasComponent<SpellLevel>(entity))
                    {
                        var spellLevel = entityManager.GetComponentData<SpellLevel>(entity);
                        spellLevel.Level = 0;
                        entityManager.SetComponentData(entity, spellLevel);
                    }
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

    [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
    public static class UnequipItemSystemPatch
    {
        public static void Prefix(UnEquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("UnEquipItemSystem Prefix called...");
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    Utilities.LogComponentTypes(entity);
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
    }

    public static class GearOverride
    {
        public static void SetLevel(Entity user)
        {
            Entity character = user.Read<User>().LocalCharacter._Entity;
            ulong steamId = user.Read<User>().PlatformId;
            if (!DataStructures.PlayerExperience.TryGetValue(steamId, out var experience))
            {
                return;
            }
        }
    }
}