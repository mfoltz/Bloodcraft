using Cobalt.Systems.Expertise;
using Cobalt.Systems.Sanguimancy;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Hooks.BaseStats;
using static Cobalt.Hooks.UnitStatsOverride;
using static Cobalt.Systems.Experience.PrestigeSystem.PrestigeStatManager;
using static Cobalt.Systems.Expertise.WeaponStats;
using static Cobalt.Systems.Expertise.WeaponStats.WeaponStatManager;
using static Cobalt.Systems.Sanguimancy.BloodStats;
using static Cobalt.Systems.Sanguimancy.BloodStats.BloodStatManager;

namespace Cobalt.Hooks
{
    [HarmonyPatch]
    public class EquipmentPatch
    {
        //private static Dictionary<Entity, float> weaponLevels = [];

        [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
        [HarmonyPostfix]
        private static void EquipItemSystemPrefix(EquipItemSystem __instance)
        {
            if (!Plugin.ExpertiseSystem.Value) return;
            Core.Log.LogInfo("EquipItemSystem Prefix..."); //prefix here to properly catch previous weapon
            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
            HandleEquipmentEvent(entities);
        }

        [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
        [HarmonyPostfix]
        private static void UnEquipItemSystemPostix(UnEquipItemSystem __instance)
        {
            if (!Plugin.ExpertiseSystem.Value) return;
            Core.Log.LogInfo("UnEquipItemSystem Postfix..."); //should this be postfix?
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
            HandleEquipmentEvent(entities);
        }

        [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
        [HarmonyPostfix]
        private static void WeaponLevelPostfix(WeaponLevelSystem_Spawn __instance)
        {
            //Core.Log.LogInfo("WeaponLevelSystem_Spawn Postfix...");
            if (!Plugin.LevelingSystem.Value) return;
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
                Core.Log.LogError($"Exited LevelPrefix early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        [HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
        [HarmonyPostfix]
        private static void ArmorLevelSpawnPostfix(ArmorLevelSystem_Spawn __instance)
        {
            //Core.Log.LogInfo("ArmorLevelSystem_Spawn Postfix...");
            if (!Plugin.LevelingSystem.Value) return;
            NativeArray<Entity> entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);
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
                Core.Log.LogError($"Exited LevelPrefix early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        [HarmonyPatch(typeof(ArmorLevelSystem_Destroy), nameof(ArmorLevelSystem_Destroy.OnUpdate))]
        [HarmonyPostfix]
        private static void ArmorLevelDestroyPostfix(ArmorLevelSystem_Destroy __instance)
        {
            //Core.Log.LogInfo("ArmorLevelSystem_Destroy Postfix...");
            if (!Plugin.LevelingSystem.Value) return;
            NativeArray<Entity> entities = __instance.__query_663986292_0.ToEntityArray(Allocator.Temp);
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
                Core.Log.LogError($"Exited LevelPrefix early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
        [HarmonyPostfix]
        private static void OnUpdatePostix(ModifyUnitStatBuffSystem_Spawn __instance)
        {
            if (!Plugin.LevelingSystem.Value) return;
            NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
            try
            {
                foreach (var entity in entities)
                {
                    if (entity.Has<SpellLevel>())
                    {
                        if (!entity.Has<EntityOwner>() || !entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) continue;
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

        [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void OnUpdatePreix(ReactToInventoryChangedSystem __instance)
        {
            //Core.Log.LogInfo("ReactToInventoryChangedSystem Postfix...");
            if (!Plugin.ExpertiseSystem.Value) return;
            NativeArray<Entity> entities = __instance.__query_2096870024_0.ToEntityArray(Allocator.TempJob);
            try
            {
                foreach (var entity in entities)
                {
                    InventoryChangedEvent inventoryChangedEvent = entity.Read<InventoryChangedEvent>(); // pick up if going to servant inventory, otherwise make level match player weapon mastery?
                    if (!inventoryChangedEvent.ItemEntity.Has<WeaponLevelSource>()) continue;
                    Entity inventory = inventoryChangedEvent.InventoryEntity;
                    if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<PlayerCharacter>())
                    {
                        ulong steamId = inventory.Read<InventoryConnection>().InventoryOwner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        string weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(inventoryChangedEvent.ItemEntity.Read<PrefabGUID>()).ToString();
                        IWeaponExpertiseHandler handler = WeaponExpertiseHandlerFactory.GetWeaponExpertiseHandler(weaponType);
                        if (handler != null)
                        {
                            WeaponLevelSource weaponLevelSource = new()
                            {
                                Level = ExpertiseSystem.ConvertXpToLevel(handler.GetExperienceData(steamId).Value)
                            };
                            inventoryChangedEvent.ItemEntity.Write(weaponLevelSource);
                        }
                    }
                    else if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Removed) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<PlayerCharacter>())
                    {
                        PrefabCollectionSystem prefabCollectionSystem = Core.PrefabCollectionSystem;
                        WeaponLevelSource weaponLevelSource = prefabCollectionSystem._PrefabGuidToEntityMap[inventoryChangedEvent.Item].Read<WeaponLevelSource>();
                        inventoryChangedEvent.ItemEntity.Write(weaponLevelSource);
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

        private static void HandleEquipmentEvent(NativeArray<Entity> entities)
        {
            try
            {
                foreach (Entity entity in entities)
                {
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    Entity character = fromCharacter.Character;
                    Equipment equipment = character.Read<Equipment>();
                    UpdatePlayerStats(character);
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited HandleEquipment early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }

    public static class UnitStatsOverride
    {
        private static readonly PrefabGUID unarmed = new(-2075546002);

        public static void ApplyWeaponBonuses(Entity character, string weaponType)
        {
            UnitStats stats = character.Read<UnitStats>();
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            IWeaponExpertiseHandler handler = WeaponExpertiseHandlerFactory.GetWeaponExpertiseHandler(weaponType);
            Equipment equipment = character.Read<Equipment>();

            if (!weaponType.ToLower().Contains("unarmed")) GearOverride.SetWeaponItemLevel(equipment, handler.GetExperienceData(steamId).Key, Core.EntityManager);
            Core.Log.LogInfo($"Before adding: {stats.PhysicalPower._Value} | {stats.SpellPower._Value} | {stats.PhysicalCriticalStrikeChance._Value} | {stats.PhysicalCriticalStrikeDamage._Value} | {stats.SpellCriticalStrikeChance._Value} | {stats.SpellCriticalStrikeDamage._Value}");
            Entity buff = Entity.Null;
            if (weaponType.ToLower().Contains("unarmed"))
            {
                ServerGameManager serverGameManager = Core.ServerGameManager;
                if (serverGameManager.TryGetBuff(character, unarmed.ToIdentifier(), out Entity unarmedBuff))
                {
                    buff = unarmedBuff;
                    Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(buff);
                }
            }
            if (Core.DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
            {
                DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer = new();
                if (!buff.Equals(Entity.Null)) buffer = buff.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                else buffer = equipment.WeaponSlot.SlotEntity._Entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                foreach (var bonus in bonuses)
                {
                    WeaponStatType weaponStatType = ExpertiseSystem.GetWeaponStatTypeFromString(bonus);
                    float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponStatType);
                    if (!buff.Equals(Entity.Null))
                    {
                        ModifyUnitStatBuff_DOTS unarmedModifier = new()
                        {
                            StatType = (UnitStatType)weaponStatType,
                            ModificationType = ModificationType.AddToBase,
                            Value = scaledBonus,
                            Modifier = 1,
                            IncreaseByStacks = false,
                            ValueByStacks = 0,
                            Priority = 0,
                            Id = ModificationId.Empty
                        };
                        buffer.Add(unarmedModifier);
                        Core.Log.LogInfo($"{unarmedModifier.StatType} | {unarmedModifier.Value}");
                        continue;
                    }
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var statBuff = buffer[i];
                        if (statBuff.StatType == (UnitStatType)weaponStatType) // Assuming WeaponStatType can be cast to UnitStatType
                        {
                            statBuff.Value += scaledBonus; // Modify the value accordingly
                            buffer[i] = statBuff; // Assign the modified struct back to the buffer
                            Core.Log.LogInfo($"{statBuff.StatType} | {statBuff.Value}");
                            break;
                        }
                        // add to buffer if existing one not found to increase
                        statBuff.StatType = (UnitStatType)weaponStatType;
                        statBuff.Value = scaledBonus;
                        buffer.Add(statBuff);
                        Core.Log.LogInfo($"{statBuff.StatType} | {statBuff.Value}");
                    }
                }
                stats = character.Read<UnitStats>();
                Core.Log.LogInfo($"After adding: {stats.PhysicalPower._Value} | {stats.SpellPower._Value} | {stats.PhysicalCriticalStrikeChance._Value} | {stats.PhysicalCriticalStrikeDamage._Value} | {stats.SpellCriticalStrikeChance._Value} | {stats.SpellCriticalStrikeDamage._Value}");
            }
            else
            {
                Core.Log.LogInfo($"No bonuses found for {weaponType}...");
            }
        }

        public static void RemoveWeaponBonuses(Entity character, string weaponType)
        {
            var stats = character.Read<UnitStats>();
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            Equipment equipment = character.Read<Equipment>();
            IWeaponExpertiseHandler handler = WeaponExpertiseHandlerFactory.GetWeaponExpertiseHandler(weaponType);
            Core.Log.LogInfo($"Before removing: {stats.PhysicalPower._Value} | {stats.SpellPower._Value} | {stats.PhysicalCriticalStrikeChance._Value} | {stats.PhysicalCriticalStrikeDamage._Value} | {stats.SpellCriticalStrikeChance._Value} | {stats.SpellCriticalStrikeDamage._Value}");
            Entity buff = Entity.Null;
            if (weaponType.ToLower().Contains("unarmed"))
            {
                ServerGameManager serverGameManager = Core.ServerGameManager;
                if (serverGameManager.TryGetBuff(character, unarmed.ToIdentifier(), out Entity unarmedBuff))
                {
                    buff = unarmedBuff;
                }
            }
            if (Core.DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
            {
                DynamicBuffer<ModifyUnitStatBuff_DOTS> buffer = new();
                if (!buff.Equals(Entity.Null)) buffer = buff.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                else buffer = equipment.WeaponSlot.SlotEntity._Entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                foreach (var bonus in bonuses)
                {
                    WeaponStatType weaponStatType = ExpertiseSystem.GetWeaponStatTypeFromString(bonus);
                    float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponStatType);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var statBuff = buffer[i];
                        if (statBuff.StatType == (UnitStatType)weaponStatType) // Assuming WeaponStatType can be cast to UnitStatType
                        {
                            statBuff.Value -= scaledBonus; // Modify the value accordingly
                            if (statBuff.Value <= 0)
                            {
                                buffer.RemoveAt(i);
                            }
                            else
                            {
                                buffer[i] = statBuff;
                            } // remove from buffer if value is less than or equal to 0 since this means it was added and not modifying original
                            Core.Log.LogInfo($"{statBuff.StatType} | {statBuff.Value}");
                            break;
                        }
                    }
                }
                stats = character.Read<UnitStats>();
                Core.Log.LogInfo($"After removing: {stats.PhysicalPower._Value} | {stats.SpellPower._Value} | {stats.PhysicalCriticalStrikeChance._Value} | {stats.PhysicalCriticalStrikeDamage._Value} | {stats.SpellCriticalStrikeChance._Value} | {stats.SpellCriticalStrikeDamage._Value}");
            }
            else
            {
                Core.Log.LogInfo($"No bonuses found for {weaponType}...");
            }
        }

        public static void ApplyBloodBonuses(Entity character) // tie extra spell slots to sanguimancy
        {
            var stats = character.Read<UnitStats>();

            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (Core.DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var bonuses))
            {
                foreach (var bonus in bonuses)
                {
                    BloodStatType bloodStatType = BloodSystem.GetBloodStatTypeFromString(bonus);
                    float scaledBonus = CalculateScaledBloodBonus(steamId, bloodStatType);

                    Core.Log.LogInfo($"Applying blood stats: {bonus} | {scaledBonus}");
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

            if (Core.DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var bonuses))
            {
                foreach (var bonus in bonuses)
                {
                    BloodStatType bloodStatType = BloodSystem.GetBloodStatTypeFromString(bonus);
                    Core.Log.LogInfo($"Resetting {bloodStatType} to {BaseBloodStats[bloodStatType]}");
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

        public static float CalculateScaledWeaponBonus(IWeaponExpertiseHandler handler, ulong steamId, WeaponStatType statType)
        {
            if (handler != null)
            {
                var xpData = handler.GetExperienceData(steamId);
                int currentLevel = ExpertiseSystem.ConvertXpToLevel(xpData.Value);
                //Equipment equipment = character.Read<Equipment>();
                //GearOverride.SetWeaponItemLevel(equipment, currentLevel);
                float maxBonus = WeaponStatManager.BaseCaps[statType];
                float scaledBonus = maxBonus * (currentLevel / 99.0f); // Scale bonus up to 99%
                Core.Log.LogInfo($"{currentLevel} | {statType} | {scaledBonus}");
                return scaledBonus;
            }
            else
            {
                Core.Log.LogInfo("Handler is null...");
            }

            return 0; // Return 0 if no handler is found or other error
        }

        public static float CalculateScaledBloodBonus(ulong steamId, BloodStatType statType)
        {
            if (Core.DataStructures.PlayerSanguimancy.TryGetValue(steamId, out var sanguimancy))
            {
                int currentLevel = sanguimancy.Key;

                float maxBonus = BloodStatManager.BaseCaps[statType];
                float scaledBonus = maxBonus * (currentLevel / 99.0f); // Scale bonus up to 99%
                Core.Log.LogInfo($"{currentLevel} | {statType} | {scaledBonus}");
                return scaledBonus;
            }
            else
            {
                Core.Log.LogInfo("No blood stats found...");
            }

            return 0; // Return 0 if no blood stats are found
        }

        public static ExpertiseSystem.WeaponType GetCurrentWeaponType(Entity character)
        {
            Entity weapon = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;

            if (weapon.Equals(Entity.Null))
            {
                return ExpertiseSystem.WeaponType.Unarmed;
            }

            return ExpertiseSystem.GetWeaponTypeFromPrefab(weapon.Read<PrefabGUID>());
        }

        public static void UpdatePlayerStats(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            // Get the current weapon type
            string currentWeapon = GetCurrentWeaponType(character).ToString();

            // Initialize player's weapon dictionary if it doesn't exist
            if (!Core.DataStructures.PlayerEquippedWeapon.TryGetValue(steamId, out var equippedWeapons))
            {
                equippedWeapons = [];
                Core.DataStructures.PlayerEquippedWeapon[steamId] = equippedWeapons;
            }

            // Check if weapon has changed
            if (!equippedWeapons.TryGetValue(currentWeapon, out var isCurrentWeaponEquipped) || !isCurrentWeaponEquipped)
            {
                // Find the previous weapon (the one that is set to true)
                string previousWeapon = equippedWeapons.FirstOrDefault(w => w.Value).Key;

                Core.Log.LogInfo($"Previous: {previousWeapon} | Current: {currentWeapon}");

                // Apply and remove stat bonuses based on weapon change
                if (!string.IsNullOrEmpty(previousWeapon))
                {
                    Core.Log.LogInfo($"Removing bonuses for {previousWeapon}...");
                    RemoveWeaponBonuses(character, previousWeapon);  // Remove bonuses from the previous weapon
                    equippedWeapons[previousWeapon] = false;  // Set previous weapon as unequipped
                }

                Core.Log.LogInfo($"Applying bonuses for {currentWeapon}...");
                ApplyWeaponBonuses(character, currentWeapon);  // Apply bonuses from the new weapon
                equippedWeapons[currentWeapon] = true;  // Set current weapon as equipped

                // Save the player's weapon state
                Core.DataStructures.SavePlayerEquippedWeapon();
            }

            //ApplyBloodBonuses(character);
            //GearOverride.SetLevel(character, entityManager);
        }
    }

    public static class GearOverride
    {
        public static void SetLevel(Entity player)
        {
            //player.LogComponentTypes();
            ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
            {
                int playerLevel = xpData.Key;
                Equipment equipment = player.Read<Equipment>();
                equipment.ArmorLevel._Value = 0f;
                equipment.SpellLevel._Value = 0f;
                equipment.WeaponLevel._Value = playerLevel;

                // weapon level of the weapon mirrors player weapon level if higher?

                player.Write(equipment);

                Core.Log.LogInfo($"Set GearScore to {playerLevel}.");
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
                //Core.Log.LogInfo($"Set weapon level source to {level}.");
            }
        }
    }

    public static class BaseStats
    {
        private static void ApplyPrestigeBonuses(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (Core.DataStructures.PlayerPrestige.TryGetValue(steamId, out var prestigeData) && prestigeData.Key > 0)
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
            if (Core.DataStructures.PlayerPrestige.TryGetValue(steamId, out var prestigeData) && !prestigeData.Key.Equals(0))
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