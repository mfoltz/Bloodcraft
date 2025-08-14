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

    static readonly PrefabGUID _bonusStatsBuff = Buffs.BonusStatsBuff;

    static readonly bool _eliteShardBearers = ConfigService.EliteShardBearers;
    static readonly int _shardBearerLevel = ConfigService.ShardBearerLevel;
    static readonly PrefabGUID _dracula = new(-327335305);
    static readonly PrefabGUID _draculaVisual = PrefabGUIDs.AB_Shapeshift_BloodHunger_BloodSight_Buff;

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
        if (!Core.IsReady) return;

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
                        buffEntity.With((ref SpellLevel spellLevel) => spellLevel.Level = 0);
                        break;
                    case 7 when _legacies && BloodSystem.BloodBuffToBloodType.ContainsKey(prefabGuid):
                        Buffs.RefreshStats(buffTarget);
                        break;
                    case 8 when _familiars && owner.IsFamiliar() && owner.IsAllied(buffTarget):
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

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(ScriptSpawnServer __instance)
    {
        if (!Core.IsReady) return;

        bool handleLevel = _leveling;

        using NativeAccessor<Entity> entities = _query.ToEntityArrayAccessor();
        using NativeAccessor<Buff> buffs = _query.ToComponentDataArrayAccessor<Buff>();
        
        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;

                /*
                if (_eliteShardBearers && buffEntity.GetPrefabGuid().Equals(Buffs.EvolvedVampireBuff) && !buffTarget.IsPlayer())
                {
                    if (buffTarget.TryGetComponent(out PrefabGUID targetPrefab) && targetPrefab.Equals(_dracula))
                    {
                        ApplyEliteDraculaModifiers(buffTarget);
                    }
                }
                */

                if (handleLevel && buffEntity.HasSpellLevel() && buffTarget.IsPlayer())
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
    static void ApplyEliteDraculaModifiers(Entity entity)
    {
        entity.Remove<DynamicallyWeakenAttackers>();

        if (_shardBearerLevel > 0)
        {
            entity.With((ref UnitLevel unitLevel) => unitLevel.Level._Value = _shardBearerLevel);
        }

        entity.With((ref AbilityBar_Shared abilityBarShared) =>
        {
            abilityBarShared.AbilityAttackSpeed._Value = 2f;
            abilityBarShared.PrimaryAttackSpeed._Value = 2f;
        });

        entity.With((ref Health health) =>
        {
            health.MaxHealth._Value *= 5;
            health.Value = health.MaxHealth._Value;
        });

        entity.With((ref UnitStats unitStats) =>
        {
            unitStats.PhysicalPower._Value *= 1.5f;
            unitStats.SpellPower._Value *= 1.5f;
        });

        entity.With((ref AiMoveSpeeds aiMoveSpeeds) =>
        {
            aiMoveSpeeds.Walk._Value = 2.5f;
            aiMoveSpeeds.Run._Value = 3.5f;
        });

        Buffs.HandleShinyBuff(entity, _draculaVisual);
    }
}