using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Classes;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _exoPrestiging = ConfigService.ExoPrestiging;

    static readonly int _maxLevel = ConfigService.MaxLevel;

    static readonly PrefabGUID _exoFormBuff = new(-31099041);
    static readonly PrefabGUID _inCombatBuff = new(581443919);
    static readonly PrefabGUID _mutantFromBiteBloodBuff = new(-491525099);
    static readonly PrefabGUID _fallenAngelDeathBuff = new(-1934189109);
    static readonly PrefabGUID _fallenAngelDespawnBuff = new(1476380301);

    static readonly PrefabGUID _playerFaction = new(1106458752);

    static readonly PrefabGUID _fallenAngel = new(-76116724);

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance.__query_1231292176_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGuid)) continue;

                if (_exoPrestiging && prefabGuid.Equals(_exoFormBuff) && entity.GetBuffTarget().TryGetPlayer(out Entity player))
                {
                    Buffs.HandleExoFormBuff(entity, player);
                }

                if (_familiars && entity.GetBuffTarget().IsFollowingPlayer())
                {
                    if (entity.Has<Script_Castleman_AdaptLevel_DataShared>()) // handle simon familiars
                    {
                        if (entity.Has<ScriptSpawn>()) entity.Remove<ScriptSpawn>();
                        if (entity.Has<ScriptUpdate>()) entity.Remove<ScriptUpdate>();
                        if (entity.Has<ScriptDestroy>()) entity.Remove<ScriptDestroy>();
                        if (entity.Has<Script_Buff_ModifyDynamicCollision_DataServer>()) entity.Remove<Script_Buff_ModifyDynamicCollision_DataServer>();

                        entity.Remove<Script_Castleman_AdaptLevel_DataShared>(); // need to remove script spawn, update etc first or throws
                    }
                }
                else if (_familiars && entity.GetBuffTarget().IsPlayer() && entityOwner.Owner.TryGetFollowedPlayer(out player))
                {
                    Entity familiar = entityOwner.Owner;
                    Buff buff = entity.Read<Buff>();

                    if (buff.BuffEffectType == BuffEffectType.Debuff && ServerGameManager.IsAllies(player, familiar))
                    {
                        entity.Destroy();
                    }
                }

                if (!entity.Has<BloodBuff>()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (_classes && entity.Has<BloodBuff_BiteToMutant_DataShared>() && HasClass(steamId))
                    {
                        PlayerClass playerClass = GetPlayerClass(steamId);

                        if (playerClass.Equals(PlayerClass.DeathMage) && entity.GetBuffTarget().TryGetPlayer(out player))
                        {
                            List<PrefabGUID> perks = Configuration.ParseConfigIntegerString(ClassBuffMap[playerClass]).Select(x => new PrefabGUID(x)).ToList();
                            int indexOfBuff = perks.IndexOf(_mutantFromBiteBloodBuff);

                            if (indexOfBuff != -1)
                            {
                                int step = _maxLevel / perks.Count;
                                int level = (_leveling && steamId.TryGetPlayerExperience(out var playerExperience)) ? playerExperience.Key : (int)player.Read<Equipment>().GetFullLevel();

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
                        BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared = entity.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                        bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        entity.Write(bloodBuff_Brute_ArmorLevelBonus_DataShared);
                    }

                    if (_legacies && BloodSystem.BuffToBloodTypeMap.TryGetValue(prefabGuid, out BloodType bloodType) && BloodManager.GetCurrentBloodType(player).Equals(bloodType)) // applies stat choices to blood types when changed
                    {
                        if (!entity.Has<ModifyUnitStatBuff_DOTS>())
                        {
                            BloodManager.ApplyBloodStats(steamId, bloodType, entity);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}