using BepInEx.Unity.IL2CPP.Hook;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Utilities;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class ModifyUnitStatBuffSystemSpawnPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _legacy = ConfigService.LegacySystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    // static readonly PrefabGUID _bonusStatsBuff = new(737485591);
    static readonly PrefabGUID _bonusStatsBuff = Buffs.BonusPlayerStatsBuff;

    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Core._initialized) return;
        else if (!_expertise && !_legacy) return;
        
        NativeArray<Entity> entities = __instance.__query_35557666_0.ToEntityArray(Allocator.Temp);
        // ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();
        // ComponentLookup<WeaponLevel> weaponLevelLookup = __instance.GetComponentLookup<WeaponLevel>();

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;

                bool isBonusStatsBuff = entity.GetPrefabGuid().Equals(_bonusStatsBuff);
                Entity buffTarget = entity.GetBuffTarget();

                if ((_expertise || _legacy) && isBonusStatsBuff && entityOwner.Owner.TryGetPlayer(out Entity playerCharacter))
                {
                    ApplyPlayerStats(entity, playerCharacter);
                }
                // else if (_familiars && isBonusStatsBuff && blockFeedBuffLookup.HasComponent(buffTarget))
                else if (_familiars && isBonusStatsBuff && buffTarget.Has<BlockFeedBuff>())
                {
                    playerCharacter = buffTarget.GetOwner().TryGetPlayer(out Entity player) ? player : Entity.Null;
                    Entity servant = Familiars.GetFamiliarServant(playerCharacter);

                    if (!servant.Exists()) continue;
                    // store servant entity ref in bonus stats buff?
                    ApplyFamiliarEquipmentStats(entity, buffTarget, servant);
                }
                // else if (_expertise && weaponLevelLookup.HasComponent(entity) && entityOwner.Owner.TryGetPlayer(out playerCharacter))
                else if (_expertise && entity.Has<WeaponLevel>() && entityOwner.Owner.TryGetPlayer(out playerCharacter))
                {
                    Buffs.RefreshStats(playerCharacter);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Core._initialized) return;
        else if (!_leveling) return;

        NativeArray<Entity> entities = __instance.__query_35557666_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out Entity playerCharacter))
                {
                    LevelingSystem.SetLevel(playerCharacter);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void ApplyPlayerStats(Entity buffEntity, Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        BloodManager.UpdateBloodStats(buffEntity, playerCharacter, steamId);
        WeaponManager.UpdateWeaponStats(buffEntity, playerCharacter, steamId);
    }

    const int SHINY_TIER = 2;
    static void ApplyFamiliarEquipmentStats(Entity buffEntity, Entity familiar, Entity servant)
    {
        if (servant.TryGetComponent(out ServantEquipment servantEquipment))
        {
            List<ModifyUnitStatBuff_DOTS> modifyUnitStatBuffs = [];

            NativeList<Entity> equipment = new(Allocator.Temp);
            NativeList<Entity> equippableBuffs = new(Allocator.Temp);

            try
            {
                BuffUtility.TryGetBuffs<EquippableBuff>(EntityManager, familiar, equippableBuffs);
                servantEquipment.GetAllEquipmentEntities(equipment);

                PrefabGUID magicSourceBuff = servantEquipment.GetEquipmentEntity(EquipmentType.MagicSource).GetEntityOnServer()
                    .TryGetComponent(out EquippableData equippableData) ? equippableData.BuffGuid : PrefabGUID.Empty;
                
                foreach (Entity equipmentEntity in equipment)
                {
                    if (equipmentEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var sourceBuffer) && !sourceBuffer.IsEmpty)
                    {
                        foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in sourceBuffer)
                        {
                            modifyUnitStatBuffs.Add(modifyUnitStatBuff);
                        }
                    }

                    if (equipmentEntity.IsAncestralWeapon() && equipmentEntity.TryGetComponent(out LegendaryItemSpellModSetComponent spellModSetComponent))
                    {
                        SpellModSet statModSet = spellModSetComponent.StatMods;
                        int statModCount = statModSet.Count;

                        PrefabGUID spellSchoolInfusion = spellModSetComponent.AbilityMods0.Mod0.Id;
                        PrefabGUID secondSpellSchoolInfusion = spellModSetComponent.AbilityMods1.Mod0.Id;
                        PrefabGUID shinyPrefabGuid = Misc.InfusionShinyBuffs.TryGetValue(spellSchoolInfusion, out shinyPrefabGuid) ? shinyPrefabGuid : PrefabGUID.Empty;

                        Core.Log.LogWarning($"ApplyFamiliarEquipmentStats() - {spellSchoolInfusion.GetPrefabName()}|{secondSpellSchoolInfusion.GetPrefabName()}|{shinyPrefabGuid.GetPrefabName()}");

                        if (shinyPrefabGuid.HasValue() && familiar.TryGetBuff(shinyPrefabGuid, out Entity shinyBuff) && shinyBuff.Has<Buff>())
                        {
                            shinyBuff.With((ref Buff buff) =>
                            {
                                buff.Stacks = SHINY_TIER;
                            });

                            Buff buff = shinyBuff.Read<Buff>();
                            Core.Log.LogWarning($"ApplyFamiliarEquipmentStats() - {shinyPrefabGuid.GetPrefabName()}|{buff.Stacks}|{buff.MaxStacks}|{buff.IncreaseStacks}");
                        }

                        for (int i = 0; i < statModCount; i++)
                        {
                            SpellMod statMod = statModSet[i];
                            PrefabGUID prefabGuid = statMod.Id;
                            float value = statMod.Power;

                            Core.Log.LogWarning($"ApplyFamiliarEquipmentStats() - {prefabGuid.GetPrefabName()}|{statMod.Power}"); // ah, need the power from 0-1 lol kill me

                            if (!Misc.TryGetStatTypeFromPrefabName(prefabGuid, value, out UnitStatType unitStatType, out value)) continue;

                            // [Warning:Bloodcraft] [ApplyFamiliarEquipmentStats] - StatMod_Unique_CriticalStrikeSpell_Mid PrefabGuid(-1466424600)|1
                            // so need to make a map of those to the real values
                            // if (unitStatType.Equals(UnitStatType.DamageReduction)) value /= 10f;

                            ModifyUnitStatBuff_DOTS modifyUnitStatBuff_DOTS = new()
                            {
                                StatType = unitStatType,
                                // ModificationType = !unitStatType.Equals(UnitStatType.MovementSpeed) ? ModificationType.AddToBase : ModificationType.MultiplyBaseAdd,
                                ModificationType = !unitStatType.Equals(UnitStatType.MovementSpeed) ? ModificationType.Add : ModificationType.MultiplyBaseAdd,
                                Value = value,
                                Modifier = 1,
                                IncreaseByStacks = false,
                                ValueByStacks = 0,
                                Priority = 0,
                                Id = ModificationIDs.Create().NewModificationId()
                            };

                            modifyUnitStatBuffs.Add(modifyUnitStatBuff_DOTS);
                        }
                    }
                    else if (equipmentEntity.IsShardNecklace() && equipmentEntity.TryGetComponent(out spellModSetComponent))
                    {
                        SpellModSet statModSet = spellModSetComponent.StatMods;
                        int statModCount = statModSet.Count;

                        for (int i = 0; i < statModCount; i++)
                        {
                            SpellMod statMod = statModSet[i];
                            PrefabGUID prefabGuid = statMod.Id;
                            float value = statMod.Power;

                            Core.Log.LogWarning($"ApplyFamiliarEquipmentStats() - {prefabGuid.GetPrefabName()}|{statMod.Power}");

                            if (!Misc.TryGetStatTypeFromPrefabName(prefabGuid, value, out UnitStatType unitStatType, out value)) continue;

                            ModifyUnitStatBuff_DOTS modifyUnitStatBuff_DOTS = new()
                            {
                                StatType = unitStatType,
                                // ModificationType = !unitStatType.Equals(UnitStatType.MovementSpeed) ? ModificationType.AddToBase : ModificationType.MultiplyBaseAdd,
                                ModificationType = !unitStatType.Equals(UnitStatType.MovementSpeed) ? ModificationType.Add : ModificationType.MultiplyBaseAdd,
                                Value = value,
                                Modifier = 1,
                                IncreaseByStacks = false,
                                ValueByStacks = 0,
                                Priority = 0,
                                Id = ModificationIDs.Create().NewModificationId()
                            };

                            modifyUnitStatBuffs.Add(modifyUnitStatBuff_DOTS);
                        }
                    }
                }

                foreach (Entity equippableBuff in equippableBuffs)
                {
                    equippableBuff.TryDestroyBuff();
                }

                if (modifyUnitStatBuffs.Any() && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var targetBuffer))
                {
                    targetBuffer.Clear();

                    foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in modifyUnitStatBuffs)
                    {
                        targetBuffer.Add(modifyUnitStatBuff);
                    }
                }

                if (magicSourceBuff.HasValue())
                {
                    familiar.TryApplyBuff(magicSourceBuff);
                }
            }
            finally
            {
                equipment.Dispose();
                equippableBuffs.Dispose();
            }

            Familiars.FamiliarSyncDelayRoutine(familiar, servant).Start();
        }
    }
}
*/

/*

#nullable enable
internal static class ModifyUnitStatsDetour
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate ModifyUnitStats ModifyUnitStatsCreateHandler(IntPtr _this, ref SystemState state);
    static ModifyUnitStatsCreateHandler? _modifyUnitStatsCreate;
    static INativeDetour? _modifyUnitStatsCreateDetour;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    unsafe delegate void ModifyUnitStatsUpdateLookupsHandler(IntPtr _this, ref SystemState state);
    static ModifyUnitStatsUpdateLookupsHandler? _modifyUnitStatsUpdateLookups;
    static INativeDetour? _modifyUnitStatsUpdateLookupsDetour;

    static ModifyUnitStats? _modifyUnitStats;
    public static unsafe void InitializeCreate()
    {
        try
        {
            _modifyUnitStatsCreateDetour = NativeDetour.Create(
            typeof(ModifyUnitStats),
            "Create",
            ModifyUnitStats_Create, 
            out _modifyUnitStatsCreate);
        }
        catch (Exception e)
        {
            Core.Log.LogError($"[ModifyUnitStats_Create] failed to initialize detour: {e}");
        }
    }
    public static unsafe void InitializeUpdateLookups()
    {
        try
        {
            _modifyUnitStatsUpdateLookupsDetour = NativeDetour.Create(
            typeof(ModifyUnitStats),
            "UpdateLookups",
            ModifyUnitStats_UpdateLookups,
            out _modifyUnitStatsUpdateLookups);
        }
        catch (Exception e)
        {
            Core.Log.LogError($"[ModifyUnitStats_UpdateLookups] failed to initialize detour: {e}");
        }
    }
    public static unsafe ModifyUnitStats ModifyUnitStats_Create(IntPtr _this, ref SystemState state)
    {
        Core.Log.LogWarning($"[ModifyUnitStats.Create]");

        var result = _modifyUnitStatsCreate!(_this, ref state);
        _modifyUnitStats = result;

        return result;
    }
    public static unsafe void ModifyUnitStats_UpdateLookups(IntPtr _this, ref SystemState state)
    {
        Core.Log.LogWarning($"[ModifyUnitStats.UpdateLookups]");

        _modifyUnitStats = *(ModifyUnitStats*)_this;
        _modifyUnitStatsUpdateLookups!(_this, ref state);
    }
    public static void ApplyUnitStats(ref ModificationsRegistry modifications, ref ModifyUnitStatBuff_DOTS modifyUnitStatBuff, Entity source, Entity target, byte stacks = 0)
    {
        _modifyUnitStats?.Apply(ref modifications, ref modifyUnitStatBuff, source, target, stacks);
        // _modifyUnitStats?.Modify(ref modifications, ref modifyUnitStatBuff, target, stacks);
    }
    public static void RemoveUnitStats(ref ModificationsRegistry modifications, ref ModifyUnitStatBuff_DOTS modifyUnitStatBuff, Entity target)
    {
        _modifyUnitStats?.Remove(ref modifications, ref modifyUnitStatBuff, target);
    }
}
*/

/*
NativeArray<ArchetypeChunk> chunks = state.EntityQueries.get_Item(0).ToArchetypeChunkArray(Allocator.Temp);
    static void ApplyFamiliarEquipmentStats(Entity buffEntity, Entity familiar, Entity servant)
    {
        if (servant.TryGetComponent(out ServantEquipment servantEquipment))
        {
            List<ModifyUnitStatBuff_DOTS> modifyUnitStatBuffs = [];

            NativeList<Entity> equipment = new(Allocator.Temp);
            NativeList<Entity> equippableBuffs = new(Allocator.Temp);

            try
            {
                BuffUtility.TryGetBuffs<EquippableBuff>(EntityManager, familiar, equippableBuffs);
                servantEquipment.GetAllEquipmentEntities(equipment);

                PrefabGUID magicSourceBuff = servantEquipment.GetEquipmentEntity(EquipmentType.MagicSource).GetEntityOnServer()
                    .TryGetComponent(out EquippableData equippableData) ? equippableData.BuffGuid : PrefabGUID.Empty;

                foreach (Entity equipmentEntity in equipment)
                {
                    if (equipmentEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var sourceBuffer) && !sourceBuffer.IsEmpty)
                    {
                        foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in sourceBuffer)
                        {
                            modifyUnitStatBuffs.Add(modifyUnitStatBuff);
                        }
                    }
                }

                foreach (Entity equippableBuff in equippableBuffs)
                {
                    equippableBuff.TryDestroyBuff();
                }

                if (modifyUnitStatBuffs.Any() && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var targetBuffer))
                {
                    targetBuffer.Clear();

                    foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in modifyUnitStatBuffs)
                    {
                        targetBuffer.Add(modifyUnitStatBuff);
                    }
                }

                if (magicSourceBuff.HasValue())
                {
                    familiar.TryApplyBuff(magicSourceBuff);
                }
            }
            finally
            {
                equipment.Dispose();
                equippableBuffs.Dispose();
            }

            Familiars.FamiliarSyncDelayRoutine(familiar, servant).Start();
        }
    }
    static void ApplyFamiliarEquipmentStatsChunks(EntityManager entityManager, Entity buffEntity, Entity familiar, Entity servant)
    {
        if (servant.TryGetComponent(out ServantEquipment servantEquipment))
        {
            List<ModifyUnitStatBuff_DOTS> modifyUnitStatBuffs = [];

            NativeList<Entity> equipment = new(Allocator.Temp);
            NativeList<Entity> equippableBuffs = new(Allocator.Temp);

            try
            {
                BuffUtility.TryGetBuffs<EquippableBuff>(entityManager, familiar, equippableBuffs);
                servantEquipment.GetAllEquipmentEntities(equipment);

                PrefabGUID magicSourceBuff = servantEquipment.GetEquipmentEntity(EquipmentType.MagicSource).GetEntityOnServer()
                    .TryGetComponent(out EquippableData equippableData) ? equippableData.BuffGuid : PrefabGUID.Empty;

                foreach (Entity equipmentEntity in equipment)
                {
                    if (equipmentEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var sourceBuffer) && !sourceBuffer.IsEmpty)
                    {
                        foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in sourceBuffer)
                        {
                            modifyUnitStatBuffs.Add(modifyUnitStatBuff);
                        }
                    }
                }

                foreach (Entity equippableBuff in equippableBuffs)
                {
                    equippableBuff.TryDestroyBuff();
                }

                if (modifyUnitStatBuffs.Any() && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var targetBuffer))
                {
                    targetBuffer.Clear();

                    foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in modifyUnitStatBuffs)
                    {
                        targetBuffer.Add(modifyUnitStatBuff);
                    }
                }

                if (magicSourceBuff.HasValue())
                {
                    familiar.TryApplyBuff(magicSourceBuff);
                }
            }
            finally
            {
                equipment.Dispose();
                equippableBuffs.Dispose();
            }

            Familiars.FamiliarSyncDelayRoutine(familiar, servant).Start();
        }
    }
EntityTypeHandle entityTypeHandle = state.GetEntityTypeHandle();
EntityStorageInfoLookup entityStorageInfoLookup = state.GetEntityStorageInfoLookup();

ComponentTypeHandle<EntityOwner> entityOwnerTypeHandle = state.GetComponentTypeHandle<EntityOwner>(true);

ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = state.GetComponentLookup<BlockFeedBuff>(true);
ComponentLookup<PlayerCharacter> playerCharacterLookup = state.GetComponentLookup<PlayerCharacter>(true);
ComponentLookup<WeaponLevel> weaponLevelLookup = state.GetComponentLookup<WeaponLevel>(true);

try
{
    foreach (ArchetypeChunk chunk in chunks)
    {
        NativeArray<Entity> entities = chunk.GetNativeArray(entityTypeHandle);
        NativeArray<EntityOwner> entityOwners = chunk.GetNativeArray(entityOwnerTypeHandle);

        for (int i = 0; i < chunk.Count; i++)
        {
            Entity entity = entities.get_Item(i);
            EntityOwner entityOwner = entityOwners.get_Item(i);

            if (!entityStorageInfoLookup.Exists(entity)) continue;

            bool isBonusStatsBuff = entity.GetPrefabGuid().Equals(_bonusStatsBuff);
            Entity buffTarget = entity.GetBuffTarget();

            bool isPlayer = (_expertise || _legacy) && isBonusStatsBuff && playerCharacterLookup.HasComponent(entityOwner.Owner);
            bool isFamiliar = _familiars && isBonusStatsBuff && blockFeedBuffLookup.HasComponent(buffTarget);
            bool isExpertiseRefresh = _expertise && weaponLevelLookup.HasComponent(entity) && playerCharacterLookup.HasComponent(entityOwner.Owner);

            if (isPlayer)
            {
                ApplyPlayerStats(entity, entityOwner.Owner);
            }
            else if (isFamiliar)
            {
                Entity playerCharacter = buffTarget.GetOwner().TryGetPlayer(out Entity player) ? player : Entity.Null;
                Entity servant = Familiars.GetFamiliarServant(playerCharacter);

                if (!entityStorageInfoLookup.Exists(servant))
                    return;

                ApplyFamiliarEquipmentStats(state.EntityManager, entity, buffTarget, servant);
            }
            else if (isExpertiseRefresh)
            {
                Buffs.RefreshStats(entityOwner.Owner);
            }
        }
    }
}
finally
{
    chunks.Dispose();
}
*/

/*
    try
    {
        foreach (Entity entity in entities)
        {
            if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;

            bool isBonusStatsBuff = entity.GetPrefabGuid().Equals(_bonusStatsBuff);
            Entity buffTarget = entity.GetBuffTarget();

            if ((_expertise || _legacy) && isBonusStatsBuff && entityOwner.Owner.TryGetPlayer(out Entity playerCharacter))
            {
                ApplyPlayerStats(entity, playerCharacter);
            }
            // else if (_familiars && isBonusStatsBuff && blockFeedBuffLookup.HasComponent(buffTarget))
            else if (_familiars && isBonusStatsBuff && buffTarget.Has<BlockFeedBuff>())
            {
                playerCharacter = buffTarget.GetOwner().TryGetPlayer(out Entity player) ? player : Entity.Null;
                Entity servant = Familiars.GetFamiliarServant(playerCharacter);

                if (!servant.Exists()) continue;
                // store servant entity ref in bonus stats buff?
                ApplyFamiliarEquipmentStats(entity, buffTarget, servant);
            }
            // else if (_expertise && weaponLevelLookup.HasComponent(entity) && entityOwner.Owner.TryGetPlayer(out playerCharacter))
            else if (_expertise && entity.Has<WeaponLevel>() && entityOwner.Owner.TryGetPlayer(out playerCharacter))
            {
                Buffs.RefreshStats(playerCharacter);
            }
        }
    }
    finally
    {
        entities.Dispose();
    }
    */
/*
 *     static readonly ComponentType[] _modifyUnitStatBuffComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Buff>()),
        ComponentType.ReadOnly(Il2CppType.Of<ModifyUnitStatBuff_DOTS>()),
    ];
try
{
    if (_systemQuery.Equals(default(EntityQuery)))
    {
        _systemQuery = EntityQueries.BuildEntityQuery(state.EntityManager, _modifyUnitStatBuffComponents);
    }

    NativeArray<Entity> entities = _systemQuery.ToEntityArray(Allocator.Temp);

    try
    {
        foreach (Entity entity in entities)
        {
            if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;

            bool isBonusStatsBuff = entity.GetPrefabGuid().Equals(_bonusStatsBuff);
            Entity buffTarget = entity.GetBuffTarget();

            Core.Log.LogWarning($"{entity.GetPrefabGuid().GetPrefabName()} | {buffTarget.GetPrefabGuid().GetPrefabName()}");

            if ((_expertise || _legacy) && isBonusStatsBuff && entityOwner.Owner.TryGetPlayer(out Entity playerCharacter))
            {
                ApplyPlayerStats(entity, playerCharacter);
            }
            // else if (_familiars && isBonusStatsBuff && blockFeedBuffLookup.HasComponent(buffTarget))
            else if (_familiars && isBonusStatsBuff && buffTarget.Has<BlockFeedBuff>())
            {
                playerCharacter = buffTarget.GetOwner().TryGetPlayer(out Entity player) ? player : Entity.Null;
                Entity servant = Familiars.GetFamiliarServant(playerCharacter);

                if (!servant.Exists()) continue;
                // store servant entity ref in bonus stats buff?
                ApplyFamiliarEquipmentStats(entity, buffTarget, servant);
            }
            // else if (_expertise && weaponLevelLookup.HasComponent(entity) && entityOwner.Owner.TryGetPlayer(out playerCharacter))
            else if (_expertise && entity.Has<WeaponLevel>() && entityOwner.Owner.TryGetPlayer(out playerCharacter))
            {
                Buffs.RefreshStats(playerCharacter);
            }
        }
    }
    finally
    {
        entities.Dispose();
    }

}
catch (Exception e)
{
    Core.Log.LogWarning($"Failed to HandleModifyUnitStatBuff: {e}");
}
*/