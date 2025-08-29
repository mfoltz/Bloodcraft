using System.Collections.Generic;
using System.Linq;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Bloodcraft;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;

class BonusStatBuffHandler : IScriptSpawnHandler
{
    static readonly bool ShouldApply = ConfigService.LegacySystem || ConfigService.ExpertiseSystem || ConfigService.ClassSystem || ConfigService.FamiliarSystem;

    static EntityManager EntityManager => Core.EntityManager;

    public bool CanHandle(ScriptSpawnContext ctx) => ShouldApply && (ctx.TargetIsPlayer || ctx.TargetIsFamiliar);

    public void Handle(ScriptSpawnContext ctx)
    {
        if (ctx.TargetIsPlayer) ApplyPlayerBonusStats(ctx.BuffEntity, ctx.Target);
        if (ctx.TargetIsFamiliar) ApplyFamiliarBonusStats(ctx.BuffEntity, ctx.Target);
    }

    static void ApplyPlayerBonusStats(Entity buffEntity, Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        if (buffEntity.TryGetBuffer<SyncToUserBuffer>(out var syncToUsers))
        {
            if (syncToUsers.IsEmpty)
            {
                SyncToUserBuffer syncToUserBuffer = new()
                {
                    UserEntity = playerCharacter.GetUserEntity()
                };

                syncToUsers.Add(syncToUserBuffer);
            }
        }

        BloodManager.UpdateBloodStats(buffEntity, playerCharacter, steamId);
        WeaponManager.UpdateWeaponStats(buffEntity, playerCharacter, steamId);
    }

    static void ApplyFamiliarBonusStats(Entity buffEntity, Entity familiar)
    {
        if (!familiar.TryGetFollowedPlayer(out Entity playerCharacter)) return;
        Entity servant = Familiars.GetFamiliarServant(playerCharacter);

        buffEntity.Remove<Buff_Persists_Through_Death>();

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
                    equippableBuff.Destroy();
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
