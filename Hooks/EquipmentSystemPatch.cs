using Cobalt.Core;
using Cobalt.Systems.Bloodline;
using Cobalt.Systems.Expertise;
using Cobalt.Systems.WeaponMastery;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TextCore.Text;
using static Cobalt.Hooks.BaseStats;
using static Cobalt.Hooks.UnitStatsOverride;
using static Cobalt.Systems.Bloodline.BloodStatsSystem;
using static Cobalt.Systems.Bloodline.BloodStatsSystem.BloodStatManager;
using static Cobalt.Systems.Experience.PrestigeSystem.PrestigeStatManager;
using static Cobalt.Systems.Expertise.WeaponStatsSystem;
using static Cobalt.Systems.Expertise.WeaponStatsSystem.WeaponStatManager;

namespace Cobalt.Hooks
{
    [HarmonyPatch]
    public class EquipmentPatch
    {
        [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void EquipItemSystemPrefix(EquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("EquipItemSystem Prefix..."); //prefix here to properly catch previous weapon
            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
            HandleEquipmentEvent(entities);

        }

        [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void UnEquipItemSystemPrefix(UnEquipItemSystem __instance)
        {
            Plugin.Log.LogInfo("UnEquipItemSystem Postfix..."); //should this be postfix?
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
            HandleEquipmentEvent(entities);
        }
        
        [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
        [HarmonyPostfix]
        private static void WeaponLevelPostfix(WeaponLevelSystem_Spawn __instance)
        {
            Plugin.Log.LogInfo("WeaponLevelSystem_Spawn Postfix...");
            NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (!entity.Has<EntityOwner>() || !entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) continue;
                    EntityOwner entityOwner = entity.Read<EntityOwner>();
                    GearOverride.SetLevel(entityOwner.Owner);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Exited LevelPrefix early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }


        private static void HandleEquipmentEvent(NativeArray<Entity> entities)
        {
            try
            {
                foreach (Entity entity in entities)
                {
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    Entity character = fromCharacter.Character;
                    UpdatePlayerStats(character);
                    //GearOverride.SetLevel(character, VWorld.Server.EntityManager);
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
        public static readonly PrefabGUID unarmed = new(-2075546002);

        private static void ApplyWeaponBonuses(Entity character, string weaponType)
        {
            UnitStats stats = character.Read<UnitStats>();
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
            }
            else
            {
                Plugin.Log.LogInfo($"No bonuses found for {weaponType}...");
            }
        }

        public static void RemoveWeaponBonuses(Entity character, string weaponType)
        {
            var stats = character.Read<UnitStats>();
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
            }
            else
            {
                Plugin.Log.LogInfo($"No bonuses found for {weaponType}...");
            }

            // Subtract the bonuses
        }

        public static void ApplyBloodBonuses(Entity character) // tie extra spell slots to sanguimancy
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
                //Equipment equipment = character.Read<Equipment>();
                //GearOverride.SetWeaponItemLevel(equipment, currentLevel);
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

        public static WeaponMasterySystem.WeaponType GetCurrentWeaponType(Entity character)
        {
            Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
            if (!VWorld.Server.EntityManager.Exists(weapon))
            {
                return WeaponMasterySystem.WeaponType.Unarmed;
            }
            return WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon.Read<PrefabGUID>());
        }

        public static void UpdatePlayerStats(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            //EntityManager entityManager = VWorld.Server.EntityManager;

            // Get the current weapon type
            string currentWeapon = GetCurrentWeaponType(character).ToString();

            // Initialize player's weapon dictionary if it doesn't exist
            if (!DataStructures.PlayerEquippedWeapon.TryGetValue(steamId, out var equippedWeapons))
            {
                equippedWeapons = [];
                DataStructures.PlayerEquippedWeapon[steamId] = equippedWeapons;
            }

            // Check if weapon has changed
            if (!equippedWeapons.TryGetValue(currentWeapon, out var isCurrentWeaponEquipped) || !isCurrentWeaponEquipped)
            {
                // Find the previous weapon (the one that is set to true)
                string previousWeapon = equippedWeapons.FirstOrDefault(w => w.Value).Key;

                Plugin.Log.LogInfo($"Previous: {previousWeapon} | Current: {currentWeapon}");

                // Apply and remove stat bonuses based on weapon change
                if (!string.IsNullOrEmpty(previousWeapon))
                {
                    Plugin.Log.LogInfo($"Removing bonuses for {previousWeapon}...");
                    RemoveWeaponBonuses(character, previousWeapon);  // Remove bonuses from the previous weapon
                    equippedWeapons[previousWeapon] = false;  // Set previous weapon as unequipped
                }

                Plugin.Log.LogInfo($"Applying bonuses for {currentWeapon}...");
                ApplyWeaponBonuses(character, currentWeapon);  // Apply bonuses from the new weapon
                equippedWeapons[currentWeapon] = true;  // Set current weapon as equipped

                // Save the player's weapon state
                DataStructures.SavePlayerEquippedWeapon();
            }

            ApplyBloodBonuses(character);
            //GearOverride.SetLevel(character, entityManager);
        }
    }

    public static class GearOverride
    {
        public static void SetLevel(Entity player)
        {
            //player.LogComponentTypes();
            ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
            {
                int playerLevel = xpData.Key;
                Equipment equipment = player.Read<Equipment>();
                //equipment.ArmorLevel._Value = 0f;
                //equipment.SpellLevel._Value = 0f;
                equipment.WeaponLevel._Value = playerLevel;
                

                // weapon level of the weapon mirrors player weapon level if higher?

                player.Write(equipment);

                Plugin.Log.LogInfo($"Set GearScore to {playerLevel}.");
            }
        }
       

        public static void SetWeaponItemLevel(Equipment equipment, int level, EntityManager entityManager)
        {
            Entity weaponEntity = equipment.WeaponSlot.SlotEntity._Entity;
            if (!weaponEntity.Equals(Entity.Null) && entityManager.HasComponent<WeaponLevelSource>(weaponEntity))
            {
                WeaponLevelSource weaponLevel = entityManager.GetComponentData<WeaponLevelSource>(weaponEntity);
                weaponLevel.Level = level;
                entityManager.SetComponentData(weaponEntity, weaponLevel);
                Plugin.Log.LogInfo($"Set weapon level source to {level}.");
            }
        }
    }

    public static class BaseStats
    {
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

        private static Dictionary<WeaponStatType, float> baseWeaponStats = new()
        {
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
}