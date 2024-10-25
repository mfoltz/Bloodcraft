using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID SwitchTargetBuff = new(1489461671);
    static readonly PrefabGUID InCombatBuff = new(581443919);
    static readonly PrefabGUID ClearAggroBuff = new(1793107442);

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
                if (!entity.Has<EntityOwner>() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;

                if (ConfigService.FamiliarSystem && entity.GetBuffTarget().TryGetFollowedPlayer(out Entity player))
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
                else if (ConfigService.FamiliarSystem && entity.GetBuffTarget().IsPlayer() && entity.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.TryGetFollowedPlayer(out player))
                {
                    Entity familiar = entityOwner.Owner;
                    Buff buff = entity.Read<Buff>();

                    if (buff.BuffEffectType == BuffEffectType.Debuff && ServerGameManager.IsAllies(player, familiar))
                    {
                        //Core.Log.LogInfo($"Preventing friendly fire from familiar in ServerScriptSpawn...");
                        DestroyUtility.Destroy(EntityManager, entity);
                    }           
                }

                if (!entity.Has<BloodBuff>()) continue;
                else if (entity.GetOwner().TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (ConfigService.LevelingSystem && entity.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>()) // brute level bonus -snip-
                    {
                        BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared = entity.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                        bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        entity.Write(bloodBuff_Brute_ArmorLevelBonus_DataShared);
                    }

                    if (ConfigService.BloodSystem && BloodSystem.BuffToBloodTypeMap.TryGetValue(prefabGUID, out BloodType bloodType) && BloodManager.GetCurrentBloodType(player).Equals(bloodType)) // applies stat choices to blood types when changed
                    {
                        // only do this when matching blood type to ignore class perma buffs, skip if already has a stat buffer since that must be stacking here somehow?
                        // if in buffMap and matches blood type this should be the buff entity the game is replacing the class perma buff with

                        if (!entity.Has<ModifyUnitStatBuff_DOTS>())
                        {
                            //Core.Log.LogInfo($"Applying blood stats for {steamId} | {bloodType} | {prefabGUID.LookupName()}, no stats buffer...");

                            BloodManager.ApplyBloodStats(steamId, bloodType, entity);
                        }
                        else
                        {
                            //Core.Log.LogInfo($"Skipping blood stats application for {steamId} | {bloodType} | {prefabGUID.LookupName()}, pre-existing stats buffer found...");
                        }
                    }

                    /*
                    if (ConfigService.PrestigeSystem)
                    {
                        if (UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(prefabGUID)) // check if the buff is for prestige and reapply if so
                        {
                            if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                            {
                                if (prestigeLevel > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(prefabGUID)) BuffUtilities.ModifyBloodBuff(entity); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
                            }
                        }
                    }

                    if (Classes && ClassUtilities.HasClass(steamId))
                    {
                        LevelingSystem.PlayerClass playerClass = ClassUtilities.GetPlayerClass(steamId);
                        List<PrefabGUID> classBuffs = UpdateBuffsBufferDestroyPatch.ClassBuffs.ContainsKey(playerClass) ? UpdateBuffsBufferDestroyPatch.ClassBuffs[playerClass] : [];

                        if (classBuffs.Contains(prefabGUID)) BuffUtilities.ModifyBloodBuff(entity);
                    }
                    */
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}