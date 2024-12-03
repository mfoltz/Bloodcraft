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
using static Bloodcraft.Systems.Leveling.LevelingSystem;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool Leveling = ConfigService.LevelingSystem;
    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool Familiars = ConfigService.FamiliarSystem;
    static readonly bool Legacies = ConfigService.BloodSystem;
    static readonly bool ExoPrestiging = ConfigService.ExoPrestiging;

    static readonly int MaxLevel = ConfigService.MaxLevel;

    static readonly PrefabGUID ExoFormBuff = new(-31099041);
    static readonly PrefabGUID InCombatBuff = new(581443919);
    static readonly PrefabGUID MutantFromBiteBloodBuff = new(-491525099);

    static readonly PrefabGUID FallenAngelDeathBuff = new(-1934189109);
    static readonly PrefabGUID FallenAngelDespawnBuff = new(1476380301);
    static readonly PrefabGUID PlayerFaction = new(1106458752);
    static readonly PrefabGUID FallenAngel = new(-76116724);

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_1231292176_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;

                //Core.Log.LogInfo($"ScriptSpawnServer: {prefabGUID.LookupName()}");

                if (ExoPrestiging && prefabGUID.Equals(ExoFormBuff) && entity.GetBuffTarget().TryGetPlayer(out Entity player))
                {
                    BuffUtilities.HandleExoFormBuff(entity, player);
                }
                
                if (Familiars && entity.GetBuffTarget().IsFollowingPlayer())
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
                else if (Familiars && entity.GetBuffTarget().IsPlayer() && entityOwner.Owner.TryGetFollowedPlayer(out player))
                {
                    Entity familiar = entityOwner.Owner;
                    Buff buff = entity.Read<Buff>();

                    if (buff.BuffEffectType == BuffEffectType.Debuff && ServerGameManager.IsAllies(player, familiar))
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }           
                }

                if (!entity.Has<BloodBuff>()) continue;
                else if (entityOwner.Owner.TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (Classes && entity.Has<BloodBuff_BiteToMutant_DataShared>() && ClassUtilities.HasClass(steamId))
                    {
                        PlayerClass playerClass = ClassUtilities.GetPlayerClass(steamId);

                        // testing
                        if (playerClass.Equals(PlayerClass.DeathMage) && entity.GetBuffTarget().TryGetPlayer(out player))
                        {
                            List<PrefabGUID> perks = ConfigUtilities.ParseConfigIntegerString(ClassBuffMap[playerClass]).Select(x => new PrefabGUID(x)).ToList();
                            int indexOfBuff = perks.IndexOf(MutantFromBiteBloodBuff);
                            
                            if (indexOfBuff != -1)
                            {
                                int step = MaxLevel / perks.Count;
                                int level = (Leveling && steamId.TryGetPlayerExperience(out var playerExperience)) ? playerExperience.Key : (int)player.Read<Equipment>().GetFullLevel();

                                if (level >= step * (indexOfBuff + 1))
                                {
                                    //Core.Log.LogInfo($"Modifying MutantFromBiteBuff...");

                                    /*
                                    entity.With((ref BloodBuff_BiteToMutant_DataShared bloodBuff_BiteToMutant_DataShared) =>
                                    {
                                        //bloodBuff_BiteToMutant_DataShared.DeathBuff = FallenAngelDespawnBuff;
                                        //bloodBuff_BiteToMutant_DataShared.MutantFaction = PlayerFaction;
                                    });
                                    */

                                    var buffer = entity.ReadBuffer<RandomMutant>();

                                    RandomMutant randomMutant = buffer[0];
                                    randomMutant.Mutant = FallenAngel;
                                    buffer[0] = randomMutant;

                                    buffer.RemoveAt(1);
                                }
                            }
                        }
                    }

                    if (Leveling && entity.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>()) // brute level bonus -snip-
                    {
                        BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared = entity.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                        bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        entity.Write(bloodBuff_Brute_ArmorLevelBonus_DataShared);
                    }

                    if (Legacies && BloodSystem.BuffToBloodTypeMap.TryGetValue(prefabGUID, out BloodType bloodType) && BloodManager.GetCurrentBloodType(player).Equals(bloodType)) // applies stat choices to blood types when changed
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