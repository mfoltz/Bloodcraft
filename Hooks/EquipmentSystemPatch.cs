using Cobalt.Systems.Expertise;
using Cobalt.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Cobalt.Systems.Expertise.WeaponStats;
using static Cobalt.Systems.Expertise.WeaponStats.WeaponStatManager;
using static Cobalt.Systems.Sanguimancy.BloodStats;
using static Cobalt.Systems.Sanguimancy.BloodStats.BloodStatManager;

namespace Cobalt.Hooks
{
    [HarmonyPatch]
    public class EquipmentPatch
    {
        [HarmonyPatch(typeof(PreviewPlacementBuffSystem), nameof(PreviewPlacementBuffSystem.OnUpdate))]
        [HarmonyPostfix]
        private static void PreviewPlacementBuffSystemPostfix(PreviewPlacementBuffSystem __instance)
        {
            Core.Log.LogInfo("PreviewPlacementBuffSystem Postfix...");
            NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    entity.LogComponentTypes();
                }
            }
            catch (Exception e)
            {
                Core.Log.LogError($"Exited PreviewPlacementBuffSystem early: {e}");
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
        [HarmonyPrefix]
        private static void OnUpdatePrefix(ModifyUnitStatBuffSystem_Spawn __instance)
        {
            if (!Plugin.LevelingSystem.Value) return;
            NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
            try
            {
                foreach (var entity in entities)
                {
                    if (entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                    {
                        Entity character = entity.Read<EntityOwner>().Owner;
                        ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                        ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(entity.Read<PrefabGUID>());
                        ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, weaponType, entity);
                        /*
                        if (character.Has<BloodQualityBuff>())
                        {
                            var buffer = character.ReadBuffer<BloodQualityBuff>();
                            foreach (var buff in buffer)
                            {
                                if (buff.BloodQualityBuffPrefabGuid.GuidHash.Equals(-1596803256) && buff.BloodQualityBuffEntity.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>())
                                {
                                    buff.BloodQualityBuffEntity.Remove<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                                    GearOverride.SetLevel(character);
                                    Core.Log.LogInfo("Overrided brute blood.");
                                }
                            }
                        }
                        */
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

        [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
        [HarmonyPostfix]
        private static void OnUpdatePostix(ModifyUnitStatBuffSystem_Spawn __instance)
        {
            if (!Plugin.LevelingSystem.Value) return;
            Core.Log.LogInfo("ModifyUnitStatBuffSystem_Spawn Postfix...");
            NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.TempJob);
            try
            {
                foreach (var entity in entities)
                {
                    if (entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                    {
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

        [HarmonyPatch(typeof(ProjectM.Shared.Systems.ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
        [HarmonyPostfix]
        private static void OnUpdatePostix(ScriptSpawnServer __instance)
        {
            Core.Log.LogInfo("ScriptSpawnServer Postfix...");
            NativeArray<Entity> entities = __instance.__query_1231292176_0.ToEntityArray(Allocator.TempJob);
            try
            {
                foreach (var entity in entities)
                {
                    entity.LogComponentTypes();
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
                        //Core.Log.LogInfo($"Reduced Resource Level to 0.");
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
        private static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
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

            GearOverride.SetWeaponItemLevel(equipment, handler.GetExperienceData(steamId).Key, Core.EntityManager);

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

        /*
        public static void ApplyBloodBonuses(Entity character) // tie extra spell slots to sanguimancy
        {
            var stats = character.Read<UnitStats>();

            ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

            if (Core.DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var bonuses))
            {
                var buffer = character.ReadBuffer<BloodQualityBuff>();
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
        }
        */
        /*
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

            return ExpertiseSystem.GetWeaponTypeFromPrefab(weapon.Read<PrefabGUID>());
        }
    }

    public static class GearOverride
    {
        public static void SetLevel(Entity player, bool addLevel = false)
        {
            //player.LogComponentTypes();
            ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
            {
                int playerLevel = xpData.Key;
                if (addLevel) playerLevel++;
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