using Cobalt.Core;
using Cobalt.Systems.Bloodline;
using Cobalt.Systems.Expertise;
using Cobalt.Systems.WeaponMastery;
using HarmonyLib;
using ProjectM;
using ProjectM.Hybrid;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Systems.Bloodline.BloodStatsSystem;
using static Cobalt.Systems.Bloodline.BloodStatsSystem.BloodStatManager;
using static Cobalt.Systems.Experience.PrestigeSystem.PrestigeStatManager;
using static Cobalt.Systems.Expertise.WeaponStatsSystem;
using static Cobalt.Systems.Expertise.WeaponStatsSystem.WeaponStatManager;
using static Cobalt.Hooks.BaseStats;
using static Cobalt.Hooks.UnitStatsOverride;
using ProjectM.Terrain;

namespace Cobalt.Hooks
{
    [HarmonyPatch]
    public class EquipmentPatch
    {
        [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void EquipItemSystemPrefix(EquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("EquipItemSystem Prefix...");
            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
            HandleEquipmentEvent(entities);
        }

        [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void UnEquipItemSystemPrefix(UnEquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("UnEquipItemSystem Prefix...");
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
            HandleEquipmentEvent(entities);
        }

        private static void HandleEquipmentEvent(NativeArray<Entity> entities)
        {
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<EquipItemEvent>())
                    {
                        entity.LogComponentTypes();
                        FromCharacter fromCharacter = entity.Read<FromCharacter>();
                        Entity character = fromCharacter.Character;

                        //string currentWeapon = GetCurrentWeaponType(character).ToString();
                        //Plugin.Log.LogInfo($"{currentWeapon}");
                        GearOverride.SetLevel(character);
                        UnitStatsOverride.UpdatePlayerStats(character);
                    }
                    else if (entity.Has<UnequipItemEvent>())
                    {
                        entity.LogComponentTypes();
                        FromCharacter fromCharacter = entity.Read<FromCharacter>();
                        Entity character = fromCharacter.Character;

                        //string currentWeapon = GetCurrentWeaponType(character).ToString();
                        //Plugin.Log.LogInfo($"{currentWeapon}");
                        GearOverride.SetLevel(character);
                        UnitStatsOverride.UpdatePlayerStats(character);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited HandleEquipment early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }

    public static class UnitStatsOverride
    {
        private static void ApplyWeaponBonuses(EntityManager entityManager, Entity character, string weaponType)
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
                            ModifyUnitStatBuff_DOTS modifyUnitStatBuff_DOTS = new()
                            {
                                StatType = UnitStatType.MaxHealth,
                                Value = scaledBonus + BaseWeaponStats[WeaponStatType.MaxHealth],
                                ModificationType = ModificationType.Set,
                                Modifier = 1,
                                Id = ModificationId.NewId(5)
                            };
                            if (!character.Has<ModifyUnitStatBuff_DOTS>())
                            {
                                var buffer = entityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(character);
                                buffer.Add(modifyUnitStatBuff_DOTS);
                            }
                            else
                            {
                                var buffer = character.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                                buffer.Add(modifyUnitStatBuff_DOTS);
                            }
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

        public static void RemoveWeaponBonuses(EntityManager entityManager, Entity character, string weaponType)
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
                            ModifyUnitStatBuff_DOTS modifyUnitStatBuff_DOTS = new()
                            {
                                StatType = UnitStatType.MaxHealth,
                                Value = BaseWeaponStats[WeaponStatType.MaxHealth],
                                ModificationType = ModificationType.Set,
                                Modifier = 1,
                                Id = ModificationId.NewId(5)
                            };
                            if (!character.Has<ModifyUnitStatBuff_DOTS>())
                            {
                                var buffer = entityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(character);
                                buffer.Add(modifyUnitStatBuff_DOTS);
                            }
                            else
                            {
                                var musb_dots_buffer = character.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                                musb_dots_buffer.Add(modifyUnitStatBuff_DOTS);
                            }
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
            if (!weapon.Has<WeaponLevelSource>()) return WeaponMasterySystem.WeaponType.Unarmed;
            else return WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon.Read<PrefabGUID>());
        }

        public static void UpdatePlayerStats(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            EntityManager entityManager = VWorld.Server.EntityManager;

            // Get the current weapon type
            string currentWeapon = GetCurrentWeaponType(character).ToString();

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
                    //Plugin.Log.LogInfo($"Applying bonuses for {currentWeapon}...");
                    //ApplyWeaponBonuses(entityManager, character, currentWeapon);  // Apply bonuses from the new weapon

                    Plugin.Log.LogInfo($"Removing bonuses for {previousWeapon}...");
                    RemoveWeaponBonuses(entityManager, character, previousWeapon);  // Remove bonuses from the previous weapon
                    Plugin.Log.LogInfo($"Applying bonuses for {currentWeapon}...");
                    ApplyWeaponBonuses(entityManager, character, currentWeapon);  // Apply bonuses from the new weapon
                    equippedWeapons[previousWeapon] = false;  // Set previous weapon as unequipped
                }
                

                equippedWeapons[currentWeapon] = true;  // Set current weapon as equipped
                DataStructures.SavePlayerEquippedWeapon();
            }

            ApplyBloodBonuses(character);
        }
    }

    public static class BaseStats
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
    }

    public static class GearOverride
    {
        public static void SetLevel(Entity player)
        {
            ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
            {
                Equipment equipment = player.Read<Equipment>();
                RemoveItemLevelSources(player, equipment);
                //RemoveItemLevels(player, equipment);
                if (equipment.WeaponLevel._Value.Equals(xpData.Key) && equipment.ArmorLevel._Value.Equals(0f) && equipment.SpellLevel._Value.Equals(0f))
                {
                    Plugin.Log.LogInfo($"GearScore already set to {xpData.Key}.");
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

        public static void RemoveItemLevelSources(Entity character, Equipment equipment)
        {
            // Reset level for Armor Chest Slot
            Plugin.Log.LogInfo($"Removing item levels...");
            equipment.ArmorChestSlot.SlotEntity._Entity.LogComponentTypes();
            if (!equipment.ArmorChestSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorChestSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource chestLevel = equipment.ArmorChestSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                chestLevel.Level = 0f;
                equipment.ArmorChestSlot.SlotEntity._Entity.Write(chestLevel);
                character.Write(equipment);
            }

            // Reset level for Armor Footgear Slot
            if (!equipment.ArmorFootgearSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorFootgearSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource footgearLevel = equipment.ArmorFootgearSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                footgearLevel.Level = 0f;
                equipment.ArmorFootgearSlot.SlotEntity._Entity.Write(footgearLevel);
                character.Write(equipment);
            }

            // Reset level for Armor Gloves Slot
            if (!equipment.ArmorGlovesSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorGlovesSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource glovesLevel = equipment.ArmorGlovesSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                glovesLevel.Level = 0f;
                equipment.ArmorGlovesSlot.SlotEntity._Entity.Write(glovesLevel);
                character.Write(equipment);
            }

            // Reset level for Armor Legs Slot
            if (!equipment.ArmorLegsSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.ArmorLegsSlot.SlotEntity._Entity.Read<ArmorLevelSource>().Level.Equals(0f))
            {
                ArmorLevelSource legsLevel = equipment.ArmorLegsSlot.SlotEntity._Entity.Read<ArmorLevelSource>();
                legsLevel.Level = 0f;
                equipment.ArmorLegsSlot.SlotEntity._Entity.Write(legsLevel);
                character.Write(equipment);
            }

            // Reset level for Grimoire Slot (Spell Level)
            if (!equipment.GrimoireSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.GrimoireSlot.SlotEntity._Entity.Read<SpellLevelSource>().Level.Equals(0f))
            {
                SpellLevelSource spellLevel = equipment.GrimoireSlot.SlotEntity._Entity.Read<SpellLevelSource>();
                spellLevel.Level = 0f;
                equipment.GrimoireSlot.SlotEntity._Entity.Write(spellLevel);
                character.Write(equipment);
            }
            if (!equipment.WeaponSlot.SlotEntity._Entity.Equals(Entity.Null) && !equipment.WeaponSlot.SlotEntity._Entity.Read<WeaponLevelSource>().Level.Equals(0f));
            {
                WeaponLevelSource weaponLevel = equipment.WeaponSlot.SlotEntity._Entity.Read<WeaponLevelSource>();
                weaponLevel.Level = 0f;
                equipment.WeaponSlot.SlotEntity._Entity.Write(weaponLevel);
                character.Write(equipment);
            }
        }
    }
}