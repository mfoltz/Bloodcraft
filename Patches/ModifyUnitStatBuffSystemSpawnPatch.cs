using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ModifyUnitStatBuffSystemSpawnPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _legacy = ConfigService.LegacySystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    static readonly PrefabGUID _bonusStatsBuff = new(737485591);

    [HarmonyPatch(typeof(ModifyUnitStatBuffSystem_Spawn), nameof(ModifyUnitStatBuffSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ModifyUnitStatBuffSystem_Spawn __instance)
    {
        if (!Core._initialized) return;
        else if (!_expertise && !_legacy) return;

        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>();
        ComponentLookup<WeaponLevel> weaponLevelLookup = __instance.GetComponentLookup<WeaponLevel>();

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
                else if (_familiars && isBonusStatsBuff && blockFeedBuffLookup.HasComponent(buffTarget))
                {
                    playerCharacter = buffTarget.GetOwner().TryGetPlayer(out Entity player) ? player : Entity.Null;
                    Entity servant = Familiars.GetFamiliarServant(playerCharacter);

                    if (!servant.Exists()) continue;
                    // store servant entity ref in bonus stats buff?
                    ApplyFamiliarEquipmentStats(entity, buffTarget, servant);
                }
                else if (_expertise && weaponLevelLookup.HasComponent(entity) && entityOwner.Owner.TryGetPlayer(out playerCharacter))
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

        NativeArray<Entity> entities = __instance.__query_1735840491_0.ToEntityArray(Allocator.Temp);
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

                    /*
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
                    */
                }

                foreach (Entity equippableBuff in equippableBuffs)
                {
                    familiar.TryRemoveBuff(buffEntity: equippableBuff);
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
