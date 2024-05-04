using Cobalt.Core;
using Cobalt.Systems.Bloodline;
using Cobalt.Systems.Expertise;
using Cobalt.Systems.WeaponMastery;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Hooks.EquipmentSystemPatch;
using static Cobalt.Systems.Bloodline.BloodStatsSystem;
using static Cobalt.Systems.Bloodline.BloodStatsSystem.BloodStatManager;
using static Cobalt.Systems.Experience.PrestigeSystem.PrestigeStatManager;
using static Cobalt.Systems.Expertise.WeaponStatsSystem;
using static Cobalt.Systems.Expertise.WeaponStatsSystem.WeaponStatManager;

namespace Cobalt.Hooks
{
    [HarmonyPatch(typeof(EquipmentSystem), nameof(EquipmentSystem.OnUpdate))]
    public static class EquipmentSystemPatch
    {
        private static Dictionary<WeaponStatType, float> baseWeaponStats = new()
        {
            { WeaponStatType.MaxHealth, 125f },
            { WeaponStatType.AttackSpeed, 0f },
            { WeaponStatType.PhysicalPower, 10f },
            { WeaponStatType.SpellPower, 10f },
            { WeaponStatType.PhysicalCritChance, 0.05f },
            { WeaponStatType.PhysicalCritDamage, 1.5f },
            { WeaponStatType.SpellCritChance, 0.05f },
            { WeaponStatType.SpellCritDamage, 1.5f }
        };

        public static Dictionary<WeaponStatType, float> BaseWeaponStats
        {
            get => baseWeaponStats;
            set => baseWeaponStats = value;
        }

        private static Dictionary<BloodStatType, float> baseBloodStats = new()
        {
            { BloodStatType.SunResistance, 0f },
            { BloodStatType.FireResistance, 0f },
            { BloodStatType.HolyResistance, 0f },
            { BloodStatType.SilverResistance, 0f },
            { BloodStatType.PassiveHealthRegen, 0.01f }
        };

        public static Dictionary<BloodStatType, float> BaseBloodStats
        {
            get => baseBloodStats;
            set => baseBloodStats = value;
        }

        private static Dictionary<PrestigeStatType, float> basePrestigeStats = new()
        {
            { PrestigeStatType.PhysicalResistance, 0f },
            { PrestigeStatType.SpellResistance, 0f },
            { PrestigeStatType.MovementSpeed, 0f }
        };

        public static Dictionary<PrestigeStatType, float> BasePrestigeStats
        {
            get => basePrestigeStats;
            set => basePrestigeStats = value;
        }

        public static void Prefix(EquipmentSystem __instance)
        {
            Plugin.Log.LogInfo("EquipmentSystem Prefix called...");
            NativeArray<Entity> entities = __instance.__query_1269768774_0.ToEntityArray(Unity.Collections.Allocator.Temp);
            UnitStatsOverride.HandleUpdates(entities);
            entities = __instance.__query_1269768774_1.ToEntityArray(Unity.Collections.Allocator.Temp);
            UnitStatsOverride.HandleUpdates(entities);
            entities = __instance.__query_1269768774_2.ToEntityArray(Unity.Collections.Allocator.Temp);
            UnitStatsOverride.HandleUpdates(entities);
            entities = __instance.__query_1269768774_3.ToEntityArray(Unity.Collections.Allocator.Temp);
            UnitStatsOverride.HandleUpdates(entities);
            entities = __instance.__query_1269768774_4.ToEntityArray(Unity.Collections.Allocator.Temp);
            UnitStatsOverride.HandleUpdates(entities);
        }
    }

    public static class UnitStatsOverride
    {
        public static void HandleUpdates(NativeArray<Entity> entities)
        {
            try
            {
                foreach (var entity in entities)
                {
                    entity.LogComponentTypes();
                    Entity character = entity.Read<Equippable>().EquipTarget._Entity;
                    if (character.Equals(Entity.Null) || !character.Has<PlayerCharacter>()) continue;
                    else
                    {
                        Plugin.Log.LogInfo("Updating player level...");
                        GearOverride.SetLevel(character);
                        try
                        {
                            Plugin.Log.LogInfo("Updating player stats...");
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
        private static void ApplyWeaponBonuses(Entity character, string weaponType)
        {
            var stats = character.Read<UnitStats>();
            var health = character.Read<Health>();  // Assuming there's a Health component

            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            IWeaponMasteryHandler handler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
            if (DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
            {
                foreach (var bonus in bonuses)
                {
                    WeaponStatType weaponStatType = WeaponMasterySystem.GetWeaponStatTypeFromString(bonus);
                    float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponStatType);
                    Plugin.Log.LogInfo($"Applying {bonus} | {scaledBonus}");
                    switch (weaponStatType)
                    {
                        case WeaponStatType.MaxHealth:
                            health.MaxHealth._Value += scaledBonus;
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
            else
            {
                Plugin.Log.LogInfo($"No bonuses found for {weaponType}...");
            }

            // Add the bonuses
        }

        public static void RemoveWeaponBonuses(Entity character, string weaponType)
        {
            var stats = character.Read<UnitStats>();
            var health = character.Read<Health>();
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            //IWeaponMasteryHandler handler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
            if (DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
            {
                foreach (var bonus in bonuses)
                {
                    WeaponStatType weaponStatType = WeaponMasterySystem.GetWeaponStatTypeFromString(bonus);
                    Plugin.Log.LogInfo($"Resetting {weaponStatType} to {BaseWeaponStats[weaponStatType]}");
                    switch (weaponStatType)
                    {
                        case WeaponStatType.MaxHealth:
                            health.MaxHealth._Value = BaseWeaponStats[WeaponStatType.MaxHealth];
                            break;

                        case WeaponStatType.PhysicalPower:
                            stats.PhysicalPower._Value = BaseWeaponStats[WeaponStatType.PhysicalPower];
                            break;

                        case WeaponStatType.SpellPower:
                            stats.SpellPower._Value = BaseWeaponStats[WeaponStatType.SpellPower];
                            break;

                        case WeaponStatType.PhysicalCritChance:
                            stats.PhysicalCriticalStrikeChance._Value = BaseWeaponStats[WeaponStatType.PhysicalCritChance];
                            break;

                        case WeaponStatType.PhysicalCritDamage:
                            stats.PhysicalCriticalStrikeDamage._Value = BaseWeaponStats[WeaponStatType.PhysicalCritDamage];
                            break;

                        case WeaponStatType.SpellCritChance:
                            stats.SpellCriticalStrikeChance._Value = BaseWeaponStats[WeaponStatType.SpellCritChance];
                            break;

                        case WeaponStatType.SpellCritDamage:
                            stats.SpellCriticalStrikeDamage._Value = BaseWeaponStats[WeaponStatType.SpellCritDamage];
                            break;
                    }
                }
                character.Write(stats);
                character.Write(health);
            }
            else
            {
                Plugin.Log.LogInfo($"No bonuses found for {weaponType}...");
            }

            // Subtract the bonuses
        }

        public static void ApplyBloodBonuses(Entity character)
        {
            var stats = character.Read<UnitStats>();

            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var bonuses))
            {
                foreach (var bonus in bonuses)
                {
                    BloodStatType bloodStatType = BloodMasterySystem.GetBloodStatTypeFromString(bonus);
                    float scaledBonus = CalculateScaledBloodBonus(steamId, bloodStatType);

                    Plugin.Log.LogInfo($"Applying blood stats: {bonus} | {scaledBonus}");
                    switch (bloodStatType)
                    {
                        case BloodStatType.SunResistance:
                            stats.SunResistance._Value = (int)(Math.Round(scaledBonus) + BaseBloodStats[BloodStatType.SunResistance]);
                            break;

                        case BloodStatType.FireResistance:
                            stats.FireResistance._Value = (int)(Math.Round(scaledBonus) + BaseBloodStats[BloodStatType.FireResistance]);
                            break;

                        case BloodStatType.HolyResistance:
                            stats.HolyResistance._Value = (int)(Math.Round(scaledBonus) + BaseBloodStats[BloodStatType.HolyResistance]);
                            break;

                        case BloodStatType.SilverResistance:
                            stats.SilverResistance._Value = (int)(Math.Round(scaledBonus) + BaseBloodStats[BloodStatType.SilverResistance]);
                            break;

                        case BloodStatType.PassiveHealthRegen:
                            stats.PassiveHealthRegen._Value = scaledBonus + BaseBloodStats[BloodStatType.PassiveHealthRegen];
                            break;
                    }
                }
                character.Write(stats);
            }
        }

        public static void RemoveBloodBonuses(Entity character)
        {
            var stats = character.Read<UnitStats>();

            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var bonuses))
            {
                foreach (var bonus in bonuses)
                {
                    BloodStatType bloodStatType = BloodMasterySystem.GetBloodStatTypeFromString(bonus);
                    Plugin.Log.LogInfo($"Resetting {bloodStatType} to {BaseBloodStats[bloodStatType]}");
                    switch (bloodStatType)
                    {
                        case BloodStatType.SunResistance:
                            stats.SunResistance._Value = (int)BaseBloodStats[BloodStatType.SunResistance];
                            break;

                        case BloodStatType.FireResistance:
                            stats.FireResistance._Value = (int)BaseBloodStats[BloodStatType.FireResistance];
                            break;

                        case BloodStatType.HolyResistance:
                            stats.HolyResistance._Value = (int)BaseBloodStats[BloodStatType.HolyResistance];
                            break;

                        case BloodStatType.SilverResistance:
                            stats.SilverResistance._Value = (int)BaseBloodStats[BloodStatType.SilverResistance];
                            break;

                        case BloodStatType.PassiveHealthRegen:
                            stats.PassiveHealthRegen._Value = BaseBloodStats[BloodStatType.PassiveHealthRegen];
                            break;
                    }
                }
                character.Write(stats);
            }
        }

        public static float CalculateScaledWeaponBonus(IWeaponMasteryHandler handler, ulong steamId, WeaponStatType statType)
        {
            if (handler != null)
            {
                var xpData = handler.GetExperienceData(steamId);
                int currentLevel = WeaponMasterySystem.ConvertXpToLevel(xpData.Value);

                float maxBonus = WeaponStatManager.BaseCaps[statType];
                float scaledBonus = maxBonus * (currentLevel / 99.0f); // Scale bonus up to 99%
                Plugin.Log.LogInfo($"{currentLevel} | {statType} | {scaledBonus}");
                return scaledBonus;
            }
            else
            {
                Plugin.Log.LogInfo("Handler is null...");
            }

            return 0; // Return 0 if no handler is found or other error
        }

        public static float CalculateScaledBloodBonus(ulong steamId, BloodStatType statType)
        {
            if (DataStructures.PlayerSanguimancy.TryGetValue(steamId, out var sanguimancy))
            {
                int currentLevel = sanguimancy.Key;

                float maxBonus = BloodStatManager.BaseCaps[statType];
                float scaledBonus = maxBonus * (currentLevel / 99.0f); // Scale bonus up to 99%
                Plugin.Log.LogInfo($"{currentLevel} | {statType} | {scaledBonus}");
                return scaledBonus;
            }
            else
            {
                Plugin.Log.LogInfo("No blood stats found...");
            }

            return 0; // Return 0 if no blood stats are found
        }

        private static void ApplyPrestigeBonuses(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerPrestige.TryGetValue(steamId, out var prestigeData) && prestigeData.Key > 0)
            {
                UnitStats stats = character.Read<UnitStats>();
                Movement movement = character.Read<Movement>();
                foreach (var stat in BasePrestigeStats)
                {
                    float scaledBonus = CalculateScaledPrestigeBonus(prestigeData.Key, stat.Key);
                    switch (stat.Key)
                    {
                        case PrestigeStatType.PhysicalResistance:
                            stats.PhysicalResistance._Value += scaledBonus;
                            break;

                        case PrestigeStatType.SpellResistance:
                            stats.SpellResistance._Value += scaledBonus;
                            break;

                        case PrestigeStatType.MovementSpeed:
                            movement.Speed._Value += scaledBonus;
                            break;
                    }
                }
                character.Write(stats);
                character.Write(movement);
            }
        }

        private static void RemovePrestigeBonuses(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerPrestige.TryGetValue(steamId, out var prestigeData) && !prestigeData.Key.Equals(0))
            {
                UnitStats stats = character.Read<UnitStats>();
                Movement movement = character.Read<Movement>();
                foreach (var stat in BasePrestigeStats)
                {
                    switch (stat.Key)
                    {
                        case PrestigeStatType.PhysicalResistance:
                            stats.PhysicalResistance._Value = BasePrestigeStats[PrestigeStatType.PhysicalResistance];
                            break;

                        case PrestigeStatType.SpellResistance:
                            stats.SpellResistance._Value = BasePrestigeStats[PrestigeStatType.SpellResistance];
                            break;

                        case PrestigeStatType.MovementSpeed:
                            movement.Speed._Value = BasePrestigeStats[PrestigeStatType.MovementSpeed];
                            break;
                    }
                }
                character.Write(stats);
                character.Write(movement);
            }
        }

        private static float CalculateScaledPrestigeBonus(int prestigeLevel, PrestigeStatType statType)
        {
            // Scaling factor of 0.01f per level of prestige
            if (statType.Equals(PrestigeStatType.MovementSpeed))
            {
                return 0.1f; // Movement speed is a flat bonus, current intention is 15 levels of prestige
            }
            return 0.01f;
        }

        public static WeaponMasterySystem.WeaponType GetCurrentWeaponType(Entity character)
        {
            // Assuming an implementation to retrieve the current weapon type
            Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
            if (weapon.Equals(Entity.Null)) return WeaponMasterySystem.WeaponType.Unarmed;
            return WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon.Read<PrefabGUID>());
        }

        public static void UpdatePlayerStats(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            // Get the current weapon type
            string currentWeapon = GetCurrentWeaponType(character).ToString();
            //Plugin.Log.LogInfo($"{currentWeapon}");
            // Initialize player's weapon dictionary if it doesn't exist

            var equippedWeapons = DataStructures.PlayerEquippedWeapon[steamId];

            // Check if weapon has changed
            if (!equippedWeapons.TryGetValue(currentWeapon, out var isCurrentWeaponEquipped) || !isCurrentWeaponEquipped)
            {
                // Find the previous weapon (the one that is set to true)
                string previousWeapon = equippedWeapons.FirstOrDefault(w => w.Value).Key;
                Plugin.Log.LogInfo($"Previous: {previousWeapon} | Current: {currentWeapon}");
                // Apply and remove stat bonuses based on weapon change.
                if (previousWeapon != null)
                {
                    Plugin.Log.LogInfo($"Removing bonuses for {previousWeapon}...");
                    RemoveWeaponBonuses(character, previousWeapon);  // Remove bonuses from the previous weapon
                    equippedWeapons[previousWeapon] = false;  // Set previous weapon as unequipped
                }
                Plugin.Log.LogInfo($"Applying bonuses for {currentWeapon}...");
                ApplyWeaponBonuses(character, currentWeapon);  // Apply bonuses from the new weapon
                equippedWeapons[currentWeapon] = true;  // Set current weapon as equipped
                DataStructures.SavePlayerEquippedWeapon();
            }
            ApplyBloodBonuses(character);
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
            if (!equipment.ArmorChestSlot.SlotEntity.Equals(Entity.Null) && !equipment.ArmorChestSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource chestLevel = equipment.ArmorChestSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                chestLevel.Level = 0f;
                equipment.ArmorChestSlot.SlotEntity._Entity.Write(chestLevel);
            }

            // Reset level for Armor Footgear Slot
            if (!equipment.ArmorFootgearSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorFootgearSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource footgearLevel = equipment.ArmorFootgearSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                footgearLevel.Level = 0f;
                equipment.ArmorFootgearSlot.SlotEntity._Entity.Write(footgearLevel);
            }

            // Reset level for Armor Gloves Slot
            if (!equipment.ArmorGlovesSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorGlovesSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource glovesLevel = equipment.ArmorGlovesSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                glovesLevel.Level = 0f;
                equipment.ArmorGlovesSlot.SlotEntity._Entity.Write(glovesLevel);
            }

            // Reset level for Armor Legs Slot
            if (!equipment.ArmorLegsSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorLegsSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource legsLevel = equipment.ArmorLegsSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                legsLevel.Level = 0f;
                equipment.ArmorLegsSlot.SlotEntity._Entity.Write(legsLevel);
            }

            // Reset level for Grimoire Slot (Spell Level)
            if (!equipment.GrimoireSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.GrimoireSlot.SlotEntity._Entity.Read<SpellLevelSource>().Level.Equals(0f))
            {
                SpellLevelSource spellLevel = equipment.GrimoireSlot.SlotEntity._Entity.Read<SpellLevelSource>();
                spellLevel.Level = 0f;
                equipment.GrimoireSlot.SlotEntity._Entity.Write(spellLevel);
            }
        }
    }
}