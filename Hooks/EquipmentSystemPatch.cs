using Cobalt.Systems.Expertise;
using Cobalt.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Hooks.ModifyUnitStatBuffUtils;
using static Cobalt.Systems.Expertise.WeaponStats;
using static Cobalt.Systems.Expertise.WeaponStats.WeaponStatManager;
using static Cobalt.Systems.Sanguimancy.BloodStats;
using static Cobalt.Systems.Sanguimancy.BloodStats.BloodStatManager;

namespace Cobalt.Hooks
{
    [HarmonyPatch]
    public class EquipmentPatch
    {
        /*
        [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
        [HarmonyPostfix]
        private static void EquipItemSystemPostix(EquipItemSystem __instance)
        {
            if (!Plugin.ExpertiseSystem.Value) return;
            Core.Log.LogInfo("EquipItemSystem Postfix..."); //prefix here to properly catch previous weapon
            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    Entity character = fromCharacter.Character;
                    Equipment equipment = character.Read<Equipment>();
                    ExpertiseSystem.WeaponType weaponType = GetCurrentWeaponType(character);
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    Entity weaponEntity = equipment.WeaponSlot.SlotEntity._Entity;
                    ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, weaponType, weaponEntity);
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited EquipItemSystem early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
        */

        [HarmonyPatch(typeof(EquipItemSystem), nameof(EquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void EquipItemSystemPrefix(EquipItemSystem __instance)
        {
            if (!Plugin.ExpertiseSystem.Value) return;
            Core.Log.LogInfo("EquipItemSystem Prefix..."); //prefix here to properly catch previous weapon
            NativeArray<Entity> entities = __instance._EventQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    Entity character = fromCharacter.Character;
                    Equipment equipment = character.Read<Equipment>();
                    ExpertiseSystem.WeaponType weaponType = GetCurrentWeaponType(character);
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed)) continue;
                    Entity weaponEntity = equipment.WeaponSlot.SlotEntity._Entity;
                    ModifyUnitStatBuffUtils.ResetWeaponModifications(weaponEntity);
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited EquipItemSystem early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        [HarmonyPatch(typeof(UnEquipItemSystem), nameof(UnEquipItemSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void UnEquipItemSystemPrefix(UnEquipItemSystem __instance)
        {
            if (!Plugin.ExpertiseSystem.Value) return;
            Core.Log.LogInfo("UnEquipItemSystem Prefix...");
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    FromCharacter fromCharacter = entity.Read<FromCharacter>();
                    Entity character = fromCharacter.Character;
                    Equipment equipment = character.Read<Equipment>();
                    ulong steamId = fromCharacter.User.Read<User>().PlatformId;
                    Entity weaponEntity = equipment.WeaponSlot.SlotEntity._Entity;
                    ModifyUnitStatBuffUtils.ResetWeaponModifications(weaponEntity);
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited UnEquipItemSystem early: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }

        [HarmonyPatch(typeof(Apply_BuffModificationsSystem_Server), nameof(Apply_BuffModificationsSystem_Server.OnUpdate))]
        [HarmonyPostfix]
        private static void BuffModificationPostfix(Apply_BuffModificationsSystem_Server __instance)
        {
            Core.Log.LogInfo("Apply_BuffModificationsSystem_Server Postfix...");
            if (!Plugin.LevelingSystem.Value) return;
            NativeArray<Entity> entities = __instance.__query_1912026727_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    entity.LogComponentTypes();
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

        [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.OnUpdate))]
        [HarmonyPrefix]
        private static void OnUpdatePrefix(DealDamageSystem __instance)
        {
            if (!Plugin.ExpertiseSystem.Value) return;
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.TempJob);
            try
            {
                foreach (var entity in entities)
                {
                    DealDamageEvent dealDamageEvent = entity.Read<DealDamageEvent>();
                    Entity entityOwner = dealDamageEvent.SpellSource.Read<EntityOwner>().Owner;
                    EntityCategory entityCategory = dealDamageEvent.Target.Read<EntityCategory>();
                    if (!entityCategory.MainCategory.Equals(MainEntityCategory.Resource) || !entityOwner.Has<Equipment>()) continue;
                    Entity weapon = entityOwner.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
                    if (weapon.Equals(Entity.Null) || !weapon.Has<WeaponLevelSource>()) continue;
                    if (weapon.Read<WeaponLevelSource>().Level >= entityCategory.ResourceLevel._Value) continue;
                    float originalSource = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[weapon.Read<PrefabGUID>()].Read<WeaponLevelSource>().Level;
                    if (originalSource >= entityCategory.ResourceLevel._Value)
                    {
                        entityCategory.ResourceLevel._Value = 0;
                        dealDamageEvent.Target.Write(entityCategory);
                        Core.Log.LogInfo($"Reduced Resource Level to 0.");
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

                    Entity inventory = inventoryChangedEvent.InventoryEntity;
                    if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<PlayerCharacter>())
                    {
                        if (!inventoryChangedEvent.ItemEntity.Has<WeaponLevelSource>()) continue;
                        ulong steamId = inventory.Read<InventoryConnection>().InventoryOwner.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(inventoryChangedEvent.ItemEntity.Read<PrefabGUID>());
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
                        if (!inventoryChangedEvent.ItemEntity.Has<WeaponLevelSource>()) continue;
                        PrefabCollectionSystem prefabCollectionSystem = Core.PrefabCollectionSystem;
                        WeaponLevelSource weaponLevelSource = prefabCollectionSystem._PrefabGuidToEntityMap[inventoryChangedEvent.Item].Read<WeaponLevelSource>();
                        inventoryChangedEvent.ItemEntity.Write(weaponLevelSource);
                    }
                    else if (inventoryChangedEvent.ChangeType.Equals(InventoryChangedEventType.Obtained) && inventory.Has<InventoryConnection>() && inventory.Read<InventoryConnection>().InventoryOwner.Has<CastleWorkstation>())
                    {
                        Entity userEntity = inventory.Read<InventoryConnection>().InventoryOwner.Read<UserOwner>().Owner._Entity;
                        NetworkId networkId = inventory.Read<InventoryConnection>().InventoryOwner.Read<NetworkId>();
                        ulong steamId = userEntity.Read<User>().PlatformId;
                        if (Core.DataStructures.PlayerCraftingJobs.TryGetValue(networkId, out var jobs) && jobs.TryGetValue(steamId, out var playerJobs))
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
                            Core.Log.LogInfo($"Processing Craft: {inventoryChangedEvent.Item.LookupName()}");
                            float ProfessionValue = 50f;
                            ProfessionValue *= ProfessionUtilities.GetTierMultiplier(inventoryChangedEvent.Item);
                            IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(inventoryChangedEvent.Item, "");
                            if (handler != null)
                            {
                                ProfessionSystem.SetProfession(inventoryChangedEvent.Item, userEntity.Read<User>(), steamId, ProfessionValue, handler);
                            }
                        }
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
    }

    public static class ModifyUnitStatBuffUtils
    {
        public static void ApplyWeaponBonuses(Entity character, ExpertiseSystem.WeaponType weaponType, Entity weaponEntity)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            IWeaponExpertiseHandler handler = WeaponExpertiseHandlerFactory.GetWeaponExpertiseHandler(weaponType);
            Equipment equipment = character.Read<Equipment>();

            if (!weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed)) GearOverride.SetWeaponItemLevel(equipment, handler.GetExperienceData(steamId).Key, Core.EntityManager);
            if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed) && !weaponEntity.Has<ModifyUnitStatBuff_DOTS>())
            {
                Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(weaponEntity);
            }
            if (Core.DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var bonuses))
            {
                var buffer = weaponEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                foreach (var weaponStatType in bonuses)
                {
                    float scaledBonus = CalculateScaledWeaponBonus(handler, steamId, weaponStatType);
                    bool found = false;
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                        if (statBuff.StatType.Equals(WeaponStatMap[weaponStatType])) // Assuming WeaponStatType can be cast to UnitStatType
                        {
                            statBuff.Value += scaledBonus; // Modify the value accordingly
                            buffer[i] = statBuff; // Assign the modified struct back to the buffer
                            Core.Log.LogInfo($"Modified {statBuff.StatType} | {statBuff.Value}");
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
                        Core.Log.LogInfo($"Added {newStatBuff.StatType} | {newStatBuff.Value}");
                    }
                }
            }
            else
            {
                Core.Log.LogInfo($"No bonuses found for {weaponType}...");
            }
        }

        public static void ResetWeaponModifications(Entity weapon)
        {
            if (weapon.Has<ModifyUnitStatBuff_DOTS>())
            {
                Core.Log.LogInfo("Resetting weapon modifications...");
                if (weapon.Read<PrefabGUID>().LookupName().ToLower().Contains("unarmed"))
                {
                    weapon.Remove<ModifyUnitStatBuff_DOTS>();
                    return;
                }

                Entity originalWeaponEntity = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[weapon.Read<PrefabGUID>()];
                var originalBuffer = originalWeaponEntity.ReadBuffer<ModifyUnitStatBuff_DOTS>();

                weapon.Remove<ModifyUnitStatBuff_DOTS>();
                var newBuffer = Core.EntityManager.AddBuffer<ModifyUnitStatBuff_DOTS>(weapon);

                foreach (var item in originalBuffer)
                {
                    newBuffer.Add(item);
                }
            }
            else
            {
                Core.Log.LogInfo("No modifications found to reset...");
            }
        }

        /*
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
        */

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

        /*
        public static void UpdatePlayerStats(Entity character)
        {
            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            // Get the current weapon type
            string currentWeapon = GetCurrentWeaponType(character).ToString();
            Entity currentWeaponEntity = Entity.Null;
            if (currentWeapon.ToLower().Contains("unarmed"))
            {
                Core.ServerGameManager.TryGetBuff(character, unarmed.ToIdentifier(), out currentWeaponEntity);
            }
            else
            {
                currentWeaponEntity = character.Read<Equipment>().WeaponSlot.SlotEntity._Entity;
            }

            var equippedWeapons = Core.DataStructures.PlayerEquippedWeapon[steamId];

            // Check if weapon has changed
            if (!equippedWeapons.TryGetValue(currentWeapon, out var currentWeaponInfo) || !currentWeaponInfo.Item1)
            {
                // Find the previous weapon (the one that is set to true)
                var previousWeaponEntry = equippedWeapons.FirstOrDefault(w => w.Value.Item1);
                string previousWeapon = previousWeaponEntry.Key;
                Entity previousWeaponEntity = previousWeaponEntry.Value.Item2;

                Core.Log.LogInfo($"Previous: {previousWeapon} | Current: {currentWeapon}");

                // Apply and remove stat bonuses based on weapon change
                if (!string.IsNullOrEmpty(previousWeapon))
                {
                    Core.Log.LogInfo($"Removing bonuses for {previousWeapon}...");
                    RemoveWeaponBonuses(character, previousWeapon, previousWeaponEntity);  // Remove bonuses from the previous weapon
                    //equippedWeapons[previousWeapon] = (false, previousWeaponEntity);  // Set previous weapon as unequipped
                }

                Core.Log.LogInfo($"Applying bonuses for {currentWeapon}...");
                ApplyWeaponBonuses(character, currentWeapon);  // Apply bonuses from the new weapon

                if (equippedWeapons.ContainsKey(currentWeapon))
                {
                    equippedWeapons[currentWeapon] = (true, equippedWeapons[currentWeapon].Item2);  // Set current weapon as equipped, keep entity reference
                }
                else
                {
                    equippedWeapons[currentWeapon] = (true, currentWeaponEntity);  // Assign new tuple with isEquipped true and new weapon entity reference
                }

                // Save the player's weapon state
                Core.DataStructures.SavePlayerEquippedWeapon();
            }
        }
        */
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

    public static class Prestige
    {
        /*
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
        */
    }
}