using Cobalt.Core;
using Cobalt.Systems.Weapon;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;

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
                    Entity character = entity.Read<Equippable>().EquipTarget._Entity;
                    if (character.Equals(Entity.Null) || !character.Has<PlayerCharacter>()) continue;
                    else
                    {
                        GearOverride.SetLevel(character);
                        UnitStatsOverride.UpdatePlayerStats(character);
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

    public static class UnitStatsOverride
    {
        private static PlayerWeaponStats GetPlayerWeaponStats(Entity character, CombatMasterySystem.WeaponType weaponType)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponData))
            {
                if (weaponData.Weapons.TryGetValue(weaponType, out var stats))
                {
                    return stats;  // Return stats if available
                }
            }
            return new PlayerWeaponStats();  // Return a new instance with default stats if not found
        }

        private static void ApplyStatBonuses(Entity character, CombatMasterySystem.WeaponType weaponType)
        {
            var stats = character.Read<UnitStats>();
            var health = character.Read<Health>();  // Assuming there's a Health component
            var bonuses = GetPlayerWeaponStats(character, weaponType);
            if (bonuses == null) return;
            // Add the bonuses
            health.MaxHealth._Value += bonuses.MaxHealth;
            stats.AttackSpeed._Value += bonuses.AttackSpeed;
            stats.PrimaryAttackSpeed._Value += bonuses.AttackSpeed;
            stats.PhysicalPower._Value += bonuses.PhysicalPower;
            stats.SpellPower._Value += bonuses.SpellPower;
            stats.PhysicalCriticalStrikeChance._Value += bonuses.PhysicalCritChance;
            stats.PhysicalCriticalStrikeDamage._Value += bonuses.PhysicalCritDamage;
            stats.SpellCriticalStrikeChance._Value += bonuses.SpellCritChance;
            stats.SpellCriticalStrikeDamage._Value += bonuses.SpellCritDamage;

            character.Write(stats);
            character.Write(health);
        }

        public static void RemoveStatBonuses(Entity character, CombatMasterySystem.WeaponType weaponType)
        {
            var stats = character.Read<UnitStats>();
            var health = character.Read<Health>();
            var bonuses = GetPlayerWeaponStats(character, weaponType);
            // Subtract the bonuses
            health.MaxHealth._Value -= bonuses.MaxHealth;
            stats.AttackSpeed._Value -= bonuses.AttackSpeed;
            stats.PrimaryAttackSpeed._Value -= bonuses.AttackSpeed;
            stats.PhysicalPower._Value -= bonuses.PhysicalPower;
            stats.SpellPower._Value -= bonuses.SpellPower;
            stats.PhysicalCriticalStrikeChance._Value -= bonuses.PhysicalCritChance;
            stats.PhysicalCriticalStrikeDamage._Value -= bonuses.PhysicalCritDamage;
            stats.SpellCriticalStrikeChance._Value -= bonuses.SpellCritChance;
            stats.SpellCriticalStrikeDamage._Value -= bonuses.SpellCritDamage;

            character.Write(stats);
            character.Write(health);
        }

        public static CombatMasterySystem.WeaponType GetCurrentWeaponType(Entity character)
        {
            // Assuming an implementation to retrieve the current weapon type
            return CombatMasterySystem.GetWeaponTypeFromPrefab(character.Read<Equipment>().WeaponSlotEntity._Entity.Read<PrefabGUID>());
        }

        public static void UpdatePlayerStats(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (!DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponData))
            {
                return;  // No weapon data to update
            }

            // Get the current weapon type
            CombatMasterySystem.WeaponType currentWeapon = GetCurrentWeaponType(character);

            // Check if weapon has changed
            if (weaponData.CurrentWeapon != currentWeapon)
            {
                RemoveStatBonuses(character, weaponData.PreviousWeapon);  // Remove bonuses from the previous weapon
                ApplyStatBonuses(character, currentWeapon);  // Apply bonuses from the new weapon

                // Update weapon data
                weaponData.PreviousWeapon = weaponData.CurrentWeapon;
                weaponData.CurrentWeapon = currentWeapon;
            }
        }
    }

    public static class GearOverride
    {
        public static void SetLevel(Entity player)
        {
            ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
            {
                Equipment equipment = player.Read<Equipment>();
                RemoveItemLevels(equipment);

                if (equipment.SpellLevel._Value.Equals(xpData.Key) && equipment.ArmorLevel._Value.Equals(0f) && equipment.WeaponLevel._Value.Equals(0f))
                {
                    return;
                }

                float playerLevel = xpData.Key;
                equipment.ArmorLevel._Value = 0f;
                equipment.SpellLevel._Value = 0f;
                equipment.WeaponLevel._Value = playerLevel;

                player.Write(equipment);
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