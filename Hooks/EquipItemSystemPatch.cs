using Cobalt.Core;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Cobalt.Hooks
{
    [HarmonyPatch(typeof(EquipmentSystem), nameof(EquipmentSystem.OnUpdate))]
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

    public static class GearOverride
    {
        public static void SetLevel(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
            {
                Equipment equipment = character.Read<Equipment>();
                RemoveItemLevels(equipment);

                if (equipment.SpellLevel._Value.Equals(xpData.Key) && equipment.ArmorLevel._Value.Equals(0f) && equipment.WeaponLevel._Value.Equals(0f))
                {
                    return;
                }

                float playerLevel = xpData.Key;
                equipment.ArmorLevel._Value = 0f;
                equipment.SpellLevel._Value = 0f;
                equipment.WeaponLevel._Value = playerLevel;
                
                character.Write(equipment);
                //Plugin.Log.LogInfo($"Set gearScore to {playerLevel}.");
            }
        }
        public static void RemoveItemLevels(Equipment equipment)
        {
            // Reset level for Armor Chest Slot
            if (!equipment.ArmorChestSlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorChestSlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource chestLevel = equipment.ArmorChestSlotEntity._Entity.Read<ArmorLevelSource>();
                chestLevel.Level = 0f;
                equipment.ArmorChestSlotEntity._Entity.Write(chestLevel);
            }

            // Reset level for Armor Footgear Slot
            if (!equipment.ArmorFootgearSlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorFootgearSlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource footgearLevel = equipment.ArmorFootgearSlotEntity._Entity.Read<ArmorLevelSource>();
                footgearLevel.Level = 0f;
                equipment.ArmorFootgearSlotEntity._Entity.Write(footgearLevel);
            }

            // Reset level for Armor Gloves Slot
            if (!equipment.ArmorGlovesSlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorGlovesSlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource glovesLevel = equipment.ArmorGlovesSlotEntity._Entity.Read<ArmorLevelSource>();
                glovesLevel.Level = 0f;
                equipment.ArmorGlovesSlotEntity._Entity.Write(glovesLevel);
            }

            // Reset level for Armor Legs Slot
            if (!equipment.ArmorLegsSlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorLegsSlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource legsLevel = equipment.ArmorLegsSlotEntity._Entity.Read<ArmorLevelSource>();
                legsLevel.Level = 0f;
                equipment.ArmorLegsSlotEntity._Entity.Write(legsLevel);
            }

            // Reset level for Weapon Slot (actually don't do this it makes gathering resources... difficult)
            /*
            if (!equipment.WeaponSlotEntity._Entity.Equals(Entity.Null) && !equipment.WeaponSlotEntity._Entity.Read<WeaponLevelSource>().Level.Equals(0f))
            {
                WeaponLevelSource weaponLevel = equipment.WeaponSlotEntity._Entity.Read<WeaponLevelSource>();
                weaponLevel.Level = 0f;
                equipment.WeaponSlotEntity._Entity.Write(weaponLevel);
            }
            */
            // Reset level for Grimoire Slot (Spell Level)
            if (!equipment.GrimoireSlotEntity._Entity.Equals(Entity.Null) && !equipment.GrimoireSlotEntity._Entity.Read<SpellLevelSource>().Level.Equals(0f))
            {
                SpellLevelSource spellLevel = equipment.GrimoireSlotEntity._Entity.Read<SpellLevelSource>();
                spellLevel.Level = 0f;
                equipment.GrimoireSlotEntity._Entity.Write(spellLevel);
            }
        }
    }
}