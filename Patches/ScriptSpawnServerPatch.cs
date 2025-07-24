using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ModificationSystem ModificationSystem => SystemService.ModificationSystem;

    public static ServerGameBalanceSettings _serverGameBalanceSettings = ServerGameBalanceSettings.Get(SystemService.ServerGameSettingsSystem._ServerBalanceSettings);
    public static ModificationsRegistry ModificationsRegistry => ModificationSystem._Registry;

    const float DELAY = 1f;
    static readonly WaitForSeconds _delay = new(DELAY);

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly int _maxLevel = ConfigService.MaxLevel;
    static readonly bool _shouldApplyBonusStats = _legacies || _expertise || _classes || _familiars;

    static readonly float _bloodBoltSwarmCooldown = Shapeshifts.GetShapeshiftAbilityCooldown<EvolvedVampire>(_bloodBoltSwarmGroup);

    static readonly PrefabGUID _castleManCombatBuff = Buffs.CastleManCombatBuff;
    static readonly PrefabGUID _standardWerewolfBuff = Buffs.StandardWerewolfBuff;
    static readonly PrefabGUID _vBloodWerewolfBuff = Buffs.VBloodWerewolfBuff;

    static readonly PrefabGUID _bloodBoltBeamTrigger = PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_BeamTrigger;
    static readonly PrefabGUID _bloodBoltSwarmTrigger = PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_Trigger;
    static readonly PrefabGUID _bloodBoltSwarmTriggerDeadZone = PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_TriggerDeadZonePunish;

    static readonly PrefabGUID _bloodBoltChannelBuff = PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_ChannelBuff;
    static readonly PrefabGUID _bloodBoltSwarmGroup = PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_AbilityGroup;

    // static readonly PrefabGUID _bonusPlayerStatsBuff = Buffs.BonusPlayerStatsBuff;
    // static readonly PrefabGUID _bonusFamiliarStatsBuff = Buffs.BonusFamiliarStatsBuff;
    static readonly PrefabGUID _bonusStatsBuff = Buffs.BonusStatsBuff;

    /*
    static readonly HashSet<PrefabGUID> _bonusStatBuffs =
    [
        _bonusPlayerStatsBuff,
        _bonusFamiliarStatsBuff
    ];
    */

    static readonly HashSet<int> _shapeshiftBuffs =
    [
        Buffs.EvolvedVampireBuff.GuidHash,
        Buffs.CorruptedSerpentBuff.GuidHash,
        Buffs.AncientGuardianBuff.GuidHash
    ];

    static readonly HashSet<int> _bloodBoltSwarmTriggers =
    [
        _bloodBoltBeamTrigger.GuidHash,
        _bloodBoltSwarmTrigger.GuidHash,
        _bloodBoltSwarmTriggerDeadZone.GuidHash
    ];

    static readonly HashSet<int> _werewolfBuffs =
    [
        _standardWerewolfBuff.GuidHash,
        _vBloodWerewolfBuff.GuidHash
    ];

    static readonly EntityQuery _query = QueryService.ScriptSpawnServerQuery;

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        if (!Core._initialized) return;

        using NativeAccessor<Entity> entities = _query.ToEntityArrayAccessor();
        using NativeAccessor<PrefabGUID> prefabGuids = _query.ToComponentDataArrayAccessor<PrefabGUID>();
        using NativeAccessor<Buff> buffs = _query.ToComponentDataArrayAccessor<Buff>();
        using NativeAccessor<EntityOwner> entityOwners = _query.ToComponentDataArrayAccessor<EntityOwner>();

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);
        ComponentLookup<BloodBuff> bloodBuffLookup = __instance.GetComponentLookup<BloodBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;
                Entity owner = entityOwners[i].Owner;
                PrefabGUID prefabGuid = prefabGuids[i];

                // Core.Log.LogWarning($"[ScriptSpawnServer] - {buffTarget.GetPrefabGuid().GetPrefabName()} | {prefabGuid.GetPrefabName()}");

                if (!buffTarget.Exists()) continue;

                bool targetIsPlayer = playerCharacterLookup.HasComponent(buffTarget);
                bool targetIsFamiliar = blockFeedBuffLookup.HasComponent(buffTarget);
                bool ownerIsFamiliar = blockFeedBuffLookup.HasComponent(owner);
                bool isBloodBuff = bloodBuffLookup.HasComponent(buffEntity);
                bool isDebuff = buffs[i].BuffEffectType.Equals(BuffEffectType.Debuff);

                int buffType = GetBuffType(prefabGuid, isDebuff, targetIsPlayer, targetIsFamiliar, isBloodBuff);
                
                switch (buffType)
                {
                    case 1:
                        Shapeshifts.ModifyShapeshiftBuff(buffEntity, buffTarget, prefabGuid);
                        break;
                    case 2:
                        buffEntity.Remove<ScriptSpawn>();
                        buffEntity.Remove<Script_ApplyBuffOnAggroListTarget_DataServer>();
                        break;
                    case 3:
                        if (_bloodBoltSwarmCooldown != 0f) ServerGameManager.SetAbilityGroupCooldown(buffEntity.GetOwner(), _bloodBoltSwarmGroup, _bloodBoltSwarmCooldown);
                        break;
                    case 4 when _shouldApplyBonusStats:
                        if (targetIsPlayer) ApplyPlayerBonusStats(buffEntity, buffTarget);
                        if (targetIsFamiliar) ApplyFamiliarBonusStats(buffEntity, buffTarget);
                        break;
                    case 6 when _leveling && targetIsPlayer:
                        buffEntity.With((ref SpellLevel spellLevel) =>
                        {
                            spellLevel.Level = 0;
                        });
                        break;
                    case 7 when _legacies && BloodSystem.BloodBuffToBloodType.ContainsKey(prefabGuid):
                        Buffs.RefreshStats(buffTarget);
                        break;
                    case 8 when _familiars && owner.IsFamiliar() && owner.IsAllies(buffTarget):
                        buffEntity.Destroy();
                        break;
                    case 9 when _familiars:
                        if (buffTarget.TryGetFollowedPlayer(out Entity playerCharacter))
                        {
                            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
                            if (familiar.Exists()) Familiars.HandleFamiliarCastleMan(buffEntity);
                        }
                        break;
                    case 10 when _familiars:
                        if (buffTarget.TryGetFollowedPlayer(out playerCharacter))
                        {
                            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
                            if (familiar.Exists()) Familiars.HandleFamiliarShapeshiftRoutine(playerCharacter.GetUser(), playerCharacter, familiar).Start();
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[ScriptSpawnServer.OnUpdatePrefix] - {e}");
        }
    }
    static int GetBuffType(
        PrefabGUID prefabGuid,
        bool isDebuff,
        bool targetIsPlayer,
        bool targetIsFamiliar,
        bool isBloodBuff)
    {
        int guidHash = prefabGuid.GuidHash;

        if (_bloodBoltSwarmTriggers.Contains(guidHash) && targetIsPlayer)
        {
            return 2;
        }
        else if (targetIsPlayer)
        {
            if (_bonusStatsBuff.Equals(prefabGuid))
            {
                return 4;
            }
            else if (_shapeshiftBuffs.Contains(guidHash))
            {
                return 1;
            }

            return guidHash switch
            {
                136816739 => 3,
                _ when isBloodBuff => 7,
                _ when isDebuff => 8,
                _ => 0
            };
        }
        else if (targetIsFamiliar)
        {
            if (_bonusStatsBuff.Equals(prefabGuid))
            {
                return 4;
            }
            else if (_werewolfBuffs.Contains(guidHash))
            {
                return 10;
            }

            return guidHash switch
            {
                731266864 => 9,
                _ => 0
            };
        }

        return 0;
    }
    static void ApplyPlayerBonusStats(Entity buffEntity, Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        /*
        buffEntity.With((ref Buff buff) =>
        {
            buff.BuffType = BuffType.Parallel;
        });
        */

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

        /*
        if (playerCharacter.TryGetBuffer<EquipmentSetBuff>(out var equipmentSetBuffs))
        {
            EquipmentSetBuff equipmentSetBuff = new()
            {
                BuffGUID = _bonusStatsBuff,
                BuffInstance = buffEntity
            };

            equipmentSetBuffs.Add(equipmentSetBuff);
        }
        else
        {
            Core.Log.LogWarning($"[ApplyPlayerStats] - SyncToUserBuffer not found!");
        }
        */

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
                    equippableBuff.DestroyBuff();
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

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(ScriptSpawnServer __instance)
    {
        if (!Core._initialized) return;
        else if (!_leveling) return;

        using NativeAccessor<Entity> entities = _query.ToEntityArrayAccessor();
        using NativeAccessor<Buff> buffs = _query.ToComponentDataArrayAccessor<Buff>();

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;

                if (buffEntity.HasSpellLevel() && buffTarget.IsPlayer())
                {
                    LevelingSystem.SetLevel(buffTarget);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[ScriptSpawnServer.OnUpdatePostfix] - {e}");
        }
    }
}