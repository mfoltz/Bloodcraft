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
using static Bloodcraft.Utilities.Classes;

namespace Bloodcraft.Patches;

/*
[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystemSpawn => SystemService.ModifyUnitStatBuffSystem_Spawn;

    static readonly WaitForSeconds _delay = new(0.2f);

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly int _maxLevel = ConfigService.MaxLevel;

    const float BLOODBOLT_SWARM_COOLDOWN = 45f;

    static readonly PrefabGUID _exoFormBuff = new(-31099041);
    static readonly PrefabGUID _inCombatBuff = new(581443919);
    static readonly PrefabGUID _castleManCombatBuff = new(731266864);
    static readonly PrefabGUID _mutantFromBiteBloodBuff = new(-491525099);
    static readonly PrefabGUID _fallenAngelDeathBuff = new(-1934189109);
    static readonly PrefabGUID _fallenAngelDespawnBuff = new(1476380301);
    static readonly PrefabGUID _clearAggroBuff = new(1793107442);
    static readonly PrefabGUID _vBloodBloodBuff = new(20081801);
    static readonly PrefabGUID _servantMissionBuff = new(-1100464221);
    static readonly PrefabGUID _werewolfStandardBuff = new(-1598161201);
    static readonly PrefabGUID _werewolfVBloodBuff = new(-622259665);
    static readonly PrefabGUID _bloodBoltBeamTrigger = new(1615225381);
    static readonly PrefabGUID _bloodBoltSwarmTrigger = new(832491730);
    static readonly PrefabGUID _bloodBoltSwarmTriggerDeadZone = new(-622814018);
    static readonly PrefabGUID _bloodBoltChannelBuff = new(136816739);
    static readonly PrefabGUID _bloodBoltSwarmGroup = new(797450963);

    static readonly PrefabGUID _playerFaction = new(1106458752);

    static readonly PrefabGUID _fallenAngel = new(-76116724);

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

        ComponentLookup<BloodBuff> bloodBuffLookup = __instance.GetComponentLookup<BloodBuff>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);
        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGuid)) continue;

                if (_exoForm && prefabGuid.Equals(_exoFormBuff) && entity.GetBuffTarget().TryGetPlayer(out Entity playerCharacter))
                {
                    Buffs.ModifyExoFormBuff(entity, playerCharacter);
                }
                else if (_exoForm && _bloodBoltSwarmTriggers.Contains(prefabGuid) && entityOwner.Owner.IsPlayer())
                {
                    if (entity.Has<ScriptSpawn>()) entity.Remove<ScriptSpawn>();
                    if (entity.Has<Script_ApplyBuffOnAggroListTarget_DataServer>()) entity.Remove<Script_ApplyBuffOnAggroListTarget_DataServer>();
                }
                else if (_exoForm && prefabGuid.Equals(_bloodBoltChannelBuff) && entityOwner.Owner.IsPlayer())
                {
                    ServerGameManager.SetAbilityGroupCooldown(entityOwner.Owner, _bloodBoltSwarmGroup, BLOODBOLT_SWARM_COOLDOWN);
                }
                else if (_familiars && prefabGuid.Equals(_castleManCombatBuff) && entity.GetBuffTarget().IsFollowingPlayer())
                {
                    if (entity.Has<Script_Castleman_AdaptLevel_DataShared>())
                    {
                        if (entity.Has<ScriptSpawn>()) entity.Remove<ScriptSpawn>();
                        if (entity.Has<ScriptUpdate>()) entity.Remove<ScriptUpdate>();
                        if (entity.Has<ScriptDestroy>()) entity.Remove<ScriptDestroy>();
                        if (entity.Has<Script_Buff_ModifyDynamicCollision_DataServer>()) entity.Remove<Script_Buff_ModifyDynamicCollision_DataServer>();
                        if (entity.Has<Script_Castleman_AdaptLevel_DataShared>()) entity.Remove<Script_Castleman_AdaptLevel_DataShared>(); // need to remove script spawn, update etc first or throws
                    }
                }
                else if (_familiars && _werewolfBuffs.Contains(prefabGuid) && entity.GetBuffTarget().TryGetFollowedPlayer(out playerCharacter))
                {
                    Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
                    if (familiar.Exists()) Familiars.HandleFamiliarShapeshiftRoutine(playerCharacter.GetUser(), playerCharacter, familiar).Start();
                }
                else if (_familiars && entity.GetBuffTarget().IsPlayer() && entityOwner.Owner.TryGetFollowedPlayer(out playerCharacter))
                {
                    Entity familiar = entityOwner.Owner;
                    Buff buff = entity.Read<Buff>();

                    if (buff.BuffEffectType.Equals(BuffEffectType.Debuff) && ServerGameManager.IsAllies(playerCharacter, familiar))
                    {
                        entity.Destroy();
                    }
                }

                if (!entity.Has<BloodBuff>()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out playerCharacter))
                {
                    ulong steamId = playerCharacter.GetSteamId();

                    if (_classes && entity.Has<BloodBuff_BiteToMutant_DataShared>() && HasClass(steamId))
                    {
                        PlayerClass playerClass = GetPlayerClass(steamId);

                        if (playerClass.Equals(PlayerClass.DeathMage) && entity.GetBuffTarget().TryGetPlayer(out playerCharacter))
                        {
                            List<PrefabGUID> perks = Configuration.ParseConfigIntegerString(ClassBuffMap[playerClass]).Select(x => new PrefabGUID(x)).ToList();
                            int indexOfBuff = perks.IndexOf(_mutantFromBiteBloodBuff);

                            if (indexOfBuff != -1)
                            {
                                int step = _maxLevel / perks.Count;
                                int level = (_leveling && steamId.TryGetPlayerExperience(out var playerExperience)) ? playerExperience.Key : (int)playerCharacter.Read<Equipment>().GetFullLevel();

                                if (level >= step * (indexOfBuff + 1))
                                {
                                    var buffer = entity.ReadBuffer<RandomMutant>();

                                    RandomMutant randomMutant = buffer[0];
                                    randomMutant.Mutant = _fallenAngel;
                                    buffer[0] = randomMutant;

                                    buffer.RemoveAt(1);

                                    entity.With((ref BloodBuff_BiteToMutant_DataShared bloodBuff_BiteToMutant_DataShared) =>
                                    {
                                        bloodBuff_BiteToMutant_DataShared.MaxBonus = 1;
                                        bloodBuff_BiteToMutant_DataShared.MinBonus = 1;
                                    });
                                }
                            }
                        }
                    }

                    if (_leveling && entity.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>()) // brute level bonus -snip-
                    {
                        entity.With((ref BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared) =>
                        {
                            bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        });
                    }

                    if (_legacies && prefabGuid.Equals(_vBloodBloodBuff))
                    {
                        HandleStats(entity, playerCharacter);
                    }
                    else if (_legacies && BloodSystem.BuffToBloodTypeMap.TryGetValue(prefabGuid, out BloodType bloodType))
                    {
                        Buffs.RefreshStats(playerCharacter);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            prefabGuids.Dispose();
        }
    }
    static void HandleStats(Entity buffEntity, Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        buffEntity.With((ref BloodBuff_VBlood_0_DataShared bloodBuff_VBlood_0_DataShared) =>
        {
            bloodBuff_VBlood_0_DataShared.DrainIncreaseFactor = 0f;
            bloodBuff_VBlood_0_DataShared.ModificationId = ModificationIDs.Create().NewModificationId();
        });

        UpdateBloodStats(buffEntity, playerCharacter, steamId);
        UpdateWeaponStats(buffEntity, playerCharacter, steamId);

        ModifyUnitStatBuffSystemSpawn.OnUpdate();
    }
    static void UpdateBloodStats(Entity buffEntity, Entity playerCharacter, ulong steamId)
    {
        BloodType bloodType = BloodManager.GetCurrentBloodType(playerCharacter);
        BloodManager.ApplyBloodStats(buffEntity, bloodType, steamId);
    }
    static void UpdateWeaponStats(Entity buffEntity, Entity playerCharacter, ulong steamId)
    {
        Systems.Expertise.WeaponType weaponType = WeaponManager.GetCurrentWeaponType(playerCharacter);
        WeaponManager.ApplyWeaponStats(buffEntity, weaponType, steamId);
    }
}
*/

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystemSpawn => SystemService.ModifyUnitStatBuffSystem_Spawn;

    static readonly WaitForSeconds _delay = new(0.2f);

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
                        HandleStats(buffEntity, buffTarget);
                        break;
                    case 5 when _classes:
                        HandleBloodBuffMutant(buffEntity, buffTarget);
                        break;
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
                        HandleCastleMan(buffEntity);
                        break;
                    case 10 when _familiars:
                        Entity familiar = Familiars.GetActiveFamiliar(buffTarget);
                        if (familiar.Exists()) Familiars.HandleFamiliarShapeshiftRoutine(buffTarget.GetUser(), buffTarget, familiar).Start();
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
                -491525099 => 5,
                -1596803256 => 6,
                _ when bloodBuffLookup.HasComponent(buffEntity) => 7,
                _ when blockFeedBuffLookup.HasComponent(owner) && buff.BuffEffectType.Equals(BuffEffectType.Debuff) => 8,
                _ => 0
            };
        }
        else if (blockFeedBuffLookup.HasComponent(buffTarget))
        {
            return prefabGuid switch
            {
                731266864 => 9,
                -1598161201 or -622259665 => 10,
                _ => 0
            };
        }
        else return 0;
    }
    static void HandleStats(Entity buffEntity, Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        buffEntity.With((ref BloodBuff_VBlood_0_DataShared bloodBuff_VBlood_0_DataShared) =>
        {
            bloodBuff_VBlood_0_DataShared.DrainIncreaseFactor = 0f;
            bloodBuff_VBlood_0_DataShared.ModificationId = ModificationIDs.Create().NewModificationId();
        });

        UpdateBloodStats(buffEntity, playerCharacter, steamId);
        UpdateWeaponStats(buffEntity, playerCharacter, steamId);

        ModifyUnitStatBuffSystemSpawn.OnUpdate();
    }
    static void UpdateBloodStats(Entity buffEntity, Entity playerCharacter, ulong steamId)
    {
        BloodType bloodType = BloodManager.GetCurrentBloodType(playerCharacter);
        BloodManager.ApplyBloodStats(buffEntity, bloodType, steamId);
    }
    static void UpdateWeaponStats(Entity buffEntity, Entity playerCharacter, ulong steamId)
    {
        Systems.Expertise.WeaponType weaponType = WeaponManager.GetCurrentWeaponType(playerCharacter);
        WeaponManager.ApplyWeaponStats(buffEntity, weaponType, steamId);
    }
    static void HandleBloodBuffMutant(Entity buffEntity, Entity playerCharacter)
    {
        ulong steamId = playerCharacter.GetSteamId();

        if (!HasClass(steamId)) return;
        PlayerClass playerClass = GetPlayerClass(steamId);

        if (playerClass.Equals(PlayerClass.DeathMage) && UpdateBuffsBufferDestroyPatch.ClassBuffsSet[playerClass].Contains(_mutantFromBiteBloodBuff))
        {
            // List<PrefabGUID> perks = Configuration.ParseConfigIntegerString(ClassBuffMap[playerClass])
            //    .Select(x => new PrefabGUID(x)).ToList();

            List<PrefabGUID> perks = UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered[playerClass];
            int indexOfBuff = perks.IndexOf(_mutantFromBiteBloodBuff);

            if (indexOfBuff != -1)
            {
                int step = _maxLevel / perks.Count;
                int level = (_leveling && steamId.TryGetPlayerExperience(out var playerExperience))
                    ? playerExperience.Key
                    : (int)playerCharacter.Read<Equipment>().GetFullLevel();

                if (level >= step * (indexOfBuff + 1))
                {
                    var buffer = buffEntity.ReadBuffer<RandomMutant>();

                    RandomMutant randomMutant = buffer[0];
                    randomMutant.Mutant = _fallenAngel;
                    buffer[0] = randomMutant;

                    buffer.RemoveAt(1);

                    buffEntity.With((ref BloodBuff_BiteToMutant_DataShared bloodBuff) =>
                    {
                        bloodBuff.MaxBonus = 1;
                        bloodBuff.MinBonus = 1;
                    });
                }
            }
        }
    }
    static void HandleCastleMan(Entity buffEntity)
    {
        buffEntity.Remove<ScriptSpawn>();
        buffEntity.Remove<ScriptUpdate>();
        buffEntity.Remove<ScriptDestroy>();
        buffEntity.Remove<Script_Buff_ModifyDynamicCollision_DataServer>();
        buffEntity.Remove<Script_Castleman_AdaptLevel_DataShared>();
    }
}