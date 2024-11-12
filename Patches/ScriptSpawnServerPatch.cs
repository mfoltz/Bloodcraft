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

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID ExoFormBuff = new(-31099041);
    static readonly PrefabGUID InCombatBuff = new(581443919);

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

                //Core.Log.LogInfo($"ScriptSpawnServer: {prefabGUID.LookupName()}");
                if (ConfigService.ExoPrestiging && prefabGUID.Equals(ExoFormBuff) && entity.GetBuffTarget().TryGetPlayer(out Entity player))
                {
                    BuffUtilities.HandleExoFormBuff(entity, player);
                }

                if (ConfigService.FamiliarSystem && entity.GetBuffTarget().IsFollowingPlayer())
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
                        if (!entity.Has<ModifyUnitStatBuff_DOTS>())
                        {
                            BloodManager.ApplyBloodStats(steamId, bloodType, entity);
                        }
                    }

                    /*
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