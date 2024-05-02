using Cobalt.Core;
using Cobalt.Systems.Weapon;
using Cobalt.Systems.WeaponMastery;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;
using static Cobalt.Systems.Weapon.WeaponStatsSystem.WeaponStatManager;

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
                        try
                        {
                            UnitStatsOverride.UpdatePlayerStats(character);
                        }
                        catch (System.Exception e)
                        {
                            Plugin.Log.LogError($"Error updating player stats: {e}");
                        }
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
        private static void ApplyStatBonuses(Entity character, string weaponType)
        {
            var stats = character.Read<UnitStats>();
            var health = character.Read<Health>();  // Assuming there's a Health component

            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponData))
            {
                IWeaponMasteryHandler handler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
                if (weaponData.TryGetValue(weaponType, out var bonuses))
                {
                    foreach (var bonus in bonuses)
                    {
                        WeaponStatType weaponStatType = WeaponMasterySystem.GetWeaponStatTypeFromString(bonus.Key);
                        float scaledBonus = CalculateScaledBonus(handler, steamId, weaponStatType);
                        switch (weaponStatType)
                        {
                            case WeaponStatType.MaxHealth:
                                health.MaxHealth._Value += scaledBonus;
                                break;

                            case WeaponStatType.CastSpeed:
                                stats.AttackSpeed._Value += scaledBonus;
                                break;

                            case WeaponStatType.AttackSpeed:
                                stats.PrimaryAttackSpeed._Value += scaledBonus;
                                break;

                            case WeaponStatType.PhysicalPower:
                                stats.PhysicalPower._Value += scaledBonus;
                                break;

                            case WeaponStatType.SpellPower:
                                stats.SpellPower._Value += scaledBonus;
                                break;

                            case WeaponStatType.PhysicalCritChance:
                                stats.PhysicalCriticalStrikeChance._Value += scaledBonus;
                                break;

                            case WeaponStatType.PhysicalCritDamage:
                                stats.PhysicalCriticalStrikeDamage._Value += scaledBonus;
                                break;

                            case WeaponStatType.SpellCritChance:
                                stats.SpellCriticalStrikeChance._Value += scaledBonus;
                                break;

                            case WeaponStatType.SpellCritDamage:
                                stats.SpellCriticalStrikeDamage._Value += scaledBonus;
                                break;
                        }
                    }

                    character.Write(stats);
                    character.Write(health);
                }
            }

            // Add the bonuses
        }

        public static void RemoveStatBonuses(Entity character, string weaponType)
        {
            var stats = character.Read<UnitStats>();
            var health = character.Read<Health>();
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var weaponData))
            {
                IWeaponMasteryHandler handler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
                if (weaponData.TryGetValue(weaponType, out var bonuses))
                {
                    foreach (var bonus in bonuses)
                    {
                        WeaponStatType weaponStatType = WeaponMasterySystem.GetWeaponStatTypeFromString(bonus.Key);
                        float scaledBonus = CalculateScaledBonus(handler, steamId, weaponStatType);
                        switch (weaponStatType)
                        {
                            case WeaponStatType.MaxHealth:
                                health.MaxHealth._Value -= scaledBonus;
                                break;

                            case WeaponStatType.CastSpeed:
                                stats.AttackSpeed._Value -= scaledBonus;
                                break;

                            case WeaponStatType.AttackSpeed:
                                stats.PrimaryAttackSpeed._Value -= scaledBonus;
                                break;

                            case WeaponStatType.PhysicalPower:
                                stats.PhysicalPower._Value -= scaledBonus;
                                break;

                            case WeaponStatType.SpellPower:
                                stats.SpellPower._Value -= scaledBonus;
                                break;

                            case WeaponStatType.PhysicalCritChance:
                                stats.PhysicalCriticalStrikeChance._Value -= scaledBonus;
                                break;

                            case WeaponStatType.PhysicalCritDamage:
                                stats.PhysicalCriticalStrikeDamage._Value -= scaledBonus;
                                break;

                            case WeaponStatType.SpellCritChance:
                                stats.SpellCriticalStrikeChance._Value -= scaledBonus;
                                break;

                            case WeaponStatType.SpellCritDamage:
                                stats.SpellCriticalStrikeDamage._Value -= scaledBonus;
                                break;
                        }
                    }

                    character.Write(stats);
                    character.Write(health);
                }
                else
                {
                    return;  // No bonuses to subtract
                }
            }
            else
            {
                return;  // No weapon data to apply
            }
            // Subtract the bonuses
        }

        public static float CalculateScaledBonus(IWeaponMasteryHandler handler, ulong steamId, WeaponStatType statType)
        {
            if (handler != null)
            {
                var xpData = handler.GetExperienceData(steamId);
                int currentLevel = WeaponMasterySystem.ConvertXpToLevel(xpData.Value);

                float maxBonus = WeaponStatManager.BaseCaps[statType];
                float scaledBonus = maxBonus * (currentLevel / 99.0f); // Scale bonus up to 99%

                return scaledBonus;
            }

            return 0; // Return 0 if no handler is found or other error
        }

        public static WeaponMasterySystem.WeaponType GetCurrentWeaponType(Entity character)
        {
            // Assuming an implementation to retrieve the current weapon type
            Entity weapon = character.Read<Equipment>().WeaponSlotEntity._Entity;
            if (weapon.Equals(Entity.Null)) return WeaponMasterySystem.WeaponType.Unarmed;
            return WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon.Read<PrefabGUID>());
        }

        public static void UpdatePlayerStats(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (!DataStructures.PlayerWeaponStats.TryGetValue(steamId, out var _))
            {
                return;  // No weapon data to update
            }

            // Get the current weapon type
            WeaponMasterySystem.WeaponType currentWeapon = GetCurrentWeaponType(character);

            // Check if weapon has changed
            if (DataStructures.PlayerWeapons.TryGetValue(steamId, out var weaponsTuple))
            {
                string newCurrentWeapon = currentWeapon.ToString();  // Assuming `currentWeapon` is an enum or similar and needs to be converted to string for comparison.
                string previousWeapon = weaponsTuple.Item2;

                // Check if the current weapon has changed.
                if (weaponsTuple.Item1 != newCurrentWeapon)
                {
                    // Apply and remove stat bonuses based on weapon change.
                    RemoveStatBonuses(character, previousWeapon);  // Remove bonuses from the previous weapon
                    ApplyStatBonuses(character, newCurrentWeapon);  // Apply bonuses from the new weapon

                    // Update the tuple to reflect the new current and previous weapons.
                    DataStructures.PlayerWeapons[steamId] = new(newCurrentWeapon, weaponsTuple.Item1);  // Here weaponsTuple.Item1 becomes the new previous weapon
                }
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