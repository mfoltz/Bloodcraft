using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
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
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystemSpawn => SystemService.ModifyUnitStatBuffSystem_Spawn;

    static readonly WaitForSeconds _delay = new(1f); // previous 0.2f

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly int _maxLevel = ConfigService.MaxLevel;

    const float BLOODBOLT_SWARM_COOLDOWN = 45f;

    static readonly PrefabGUID _exoFormBuff = new(-31099041);
    static readonly PrefabGUID _castleManCombatBuff = new(731266864);

    static readonly PrefabGUID _mutantFromBiteBloodBuff = new(-491525099);
    static readonly PrefabGUID _fallenAngel = new(-76116724);

    static readonly PrefabGUID _vBloodBloodBuff = new(20081801);

    static readonly PrefabGUID _werewolfStandardBuff = new(-1598161201);
    static readonly PrefabGUID _werewolfVBloodBuff = new(-622259665);

    static readonly PrefabGUID _bloodBoltBeamTrigger = new(1615225381);
    static readonly PrefabGUID _bloodBoltSwarmTrigger = new(832491730);
    static readonly PrefabGUID _bloodBoltSwarmTriggerDeadZone = new(-622814018);

    static readonly PrefabGUID _bloodBoltChannelBuff = new(136816739);
    static readonly PrefabGUID _bloodBoltSwarmGroup = new(797450963);

    static readonly PrefabGUID _bruteGearLevelBuff = new(-1596803256);

    static readonly HashSet<PrefabGUID> _bloodBoltSwarmTriggers =
    [
        _bloodBoltBeamTrigger,
        _bloodBoltSwarmTrigger,
        _bloodBoltSwarmTriggerDeadZone
    ];

    static readonly HashSet<PrefabGUID> _werewolfBuffs =
    [
        _werewolfStandardBuff,
        _werewolfVBloodBuff
    ];

    static readonly EntityQuery _query = QueryService.ScriptSpawnServerQuery;

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = _query.ToEntityArray(Allocator.Temp);
        NativeArray<PrefabGUID> prefabGuids = _query.ToComponentDataArray<PrefabGUID>(Allocator.Temp);
        NativeArray<Buff> buffs = _query.ToComponentDataArray<Buff>(Allocator.Temp);
        NativeArray<EntityOwner> entityOwners = _query.ToComponentDataArray<EntityOwner>(Allocator.Temp);

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);
        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BloodBuff> bloodBuffLookup = __instance.GetComponentLookup<BloodBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;
                Entity owner = entityOwners[i].Owner;

                PrefabGUID prefabGuid = prefabGuids[i];
                int buffType = GetBuffType(prefabGuid.GuidHash, buffEntity, buffs[i], buffTarget, owner, ref playerCharacterLookup, ref blockFeedBuffLookup, ref bloodBuffLookup);

                // Core.Log.LogInfo($"{prefabGuid.GetPrefabName()} | {buffType}");

                switch (buffType)
                {
                    case 1 when _exoForm:
                        Buffs.ModifyExoFormBuff(buffEntity, buffTarget);
                        break;
                    case 2 when _exoForm:
                        buffEntity.Remove<ScriptSpawn>();
                        buffEntity.Remove<Script_ApplyBuffOnAggroListTarget_DataServer>();
                        break;
                    case 3 when _exoForm:
                        ServerGameManager.SetAbilityGroupCooldown(buffEntity.GetOwner(), _bloodBoltSwarmGroup, BLOODBOLT_SWARM_COOLDOWN);
                        break;
                    case 4 when (_legacies || _expertise):
                        ApplyStats(buffEntity, buffTarget);
                        break;
                    /*
                    case 5 when _classes:
                        Classes.HandleBloodBuffMutant(buffEntity, buffTarget);
                        break;
                    */
                    case 6 when _leveling:
                        buffEntity.With((ref BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared) =>
                        {
                            bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        });
                        break;
                    case 7 when _legacies && BloodSystem.BuffToBloodTypeMap.ContainsKey(prefabGuid):
                        Buffs.RefreshStats(buffTarget);
                        break;
                    case 8 when _familiars && owner.IsAllied(buffTarget):
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
        finally
        {
            entities.Dispose();
            prefabGuids.Dispose();
            buffs.Dispose();
            entityOwners.Dispose();
        }
    }
    static int GetBuffType(
        int prefabGuid,
        Entity buffEntity,
        Buff buff,
        Entity buffTarget,
        Entity owner,
        ref ComponentLookup<PlayerCharacter> playerCharacterLookup,
        ref ComponentLookup<BlockFeedBuff> blockFeedBuffLookup,
        ref ComponentLookup<BloodBuff> bloodBuffLookup)
    {
        if (playerCharacterLookup.HasComponent(buffTarget))
        {
            return prefabGuid switch
            {
                -31099041 => 1,
                1615225381 or 832491730 or -622814018 => 2,
                136816739 => 3,
                20081801 => 4,
                // -491525099 => 5,
                -1596803256 => 6,
                _ when bloodBuffLookup.HasComponent(buffEntity) => 7,
                _ when blockFeedBuffLookup.HasComponent(owner) && buff.BuffEffectType.Equals(BuffEffectType.Debuff) => 8,
                _ => 0
            };
        }
        else
        {
            return prefabGuid switch
            {
                731266864 => 9,
                -1598161201 or -622259665 => 10,
                _ => 0
            };
        }      
    }
    static void ApplyStats(Entity buffEntity, Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        buffEntity.With((ref BloodBuff_VBlood_0_DataShared bloodBuff_VBlood_0_DataShared) =>
        {
            bloodBuff_VBlood_0_DataShared.DrainIncreaseFactor = 0f;
            bloodBuff_VBlood_0_DataShared.ModificationId = ModificationIDs.Create().NewModificationId();
        });

        BloodManager.UpdateBloodStats(buffEntity, playerCharacter, steamId);
        WeaponManager.UpdateWeaponStats(buffEntity, playerCharacter, steamId);

        ModifyUnitStatBuffSystemSpawn.OnUpdate();
    }
}