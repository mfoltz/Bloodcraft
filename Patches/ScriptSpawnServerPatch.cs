using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Scripting;
using ProjectM.Shared;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = System.Random;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem => SystemService.EndSimulationEntityCommandBufferSystem;

    static readonly Random Random = new();

    static readonly AssetGuid assetGuid = AssetGuid.FromString("98e5411c-d93f-43da-8366-b8bcc7172c66"); // percent
    static readonly float3 color = new(0.0f, 1.0f, 1.0f);

    static readonly WaitForSeconds CaptureTick = new(CaptureInterval);
    static readonly WaitForSeconds DestroyDelay = new(0.4f);

    const float CaptureTime = 6f; // 0.25f for timing on buffs though
    const float CaptureInterval = 1.5f;
    const int TicksRequired = (int)(CaptureTime / CaptureInterval);
    const float BreakChanceMax = 0.25f;
    const float BreakChanceMin = 0.05f;

    static readonly PrefabGUID CaptureBuff = new(1280015305);
    static readonly PrefabGUID ImmaterialBuff = new(-259674366);
    static readonly PrefabGUID BreakBuff = new(-1466712470);
    static readonly PrefabGUID SuccessBuff = new(-2124138742);

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
                if (!entity.Has<EntityOwner>()) continue;

                if (ConfigService.FamiliarSystem && entity.Has<Script_Castleman_AdaptLevel_DataShared>()) // handle simon familiars
                {
                    if (entity.GetBuffTarget().TryGetFollowedPlayer(out Entity _))
                    {
                        if (entity.Has<ScriptSpawn>()) entity.Remove<ScriptSpawn>();
                        if (entity.Has<ScriptUpdate>()) entity.Remove<ScriptUpdate>();
                        if (entity.Has<ScriptDestroy>()) entity.Remove<ScriptDestroy>();
                        if (entity.Has<Script_Buff_ModifyDynamicCollision_DataServer>()) entity.Remove<Script_Buff_ModifyDynamicCollision_DataServer>();
                        entity.Remove<Script_Castleman_AdaptLevel_DataShared>();
                    }
                }
#if DEV
                else if (ConfigService.FamiliarSystem && entity.GetOwner().TryGetPlayer(out Entity player) && entity.TryGetComponent(out PrefabGUID prefab) && prefab.Equals(CaptureBuff))
                {
                    Entity target = entity.GetBuffTarget();
                    if (target.GetOwner().IsPlayer()) continue;

                    Entity userEntity = player.Read<PlayerCharacter>().UserEntity;
                    float3 targetPosition = target.Read<Translation>().Value;

                    float healthFactor = target.Read<Health>().Value / target.Read<Health>().MaxHealth._Value;
                    float adjustedBreakChance = Mathf.Lerp(BreakChanceMin, BreakChanceMax, healthFactor);

                    Core.StartCoroutine(CaptureRoutine(target, player, userEntity, targetPosition, adjustedBreakChance));
                    continue;
                }
#endif

                if (!entity.Has<BloodBuff>()) continue;
                else if (entity.GetOwner().TryGetPlayer(out Entity player))
                {
                    ulong steamId = player.GetSteamId();

                    if (ConfigService.LevelingSystem && entity.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>()) // brute level bonus -snip-
                    {
                        BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared = entity.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                        bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        entity.Write(bloodBuff_Brute_ArmorLevelBonus_DataShared);
                    }

                    if (ConfigService.BloodSystem && BloodSystem.BuffToBloodTypeMap.TryGetValue(entity.Read<PrefabGUID>(), out BloodType bloodType)) // applies stat choices to blood types when changed
                    {
                        BloodManager.ApplyBloodStats(steamId, bloodType, entity);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static IEnumerator CaptureRoutine(Entity target, Entity player, Entity userEntity, float3 targetPosition, float breakChance)
    {
        PrefabGUID targetPrefab = target.Read<PrefabGUID>();
        float duration = 0f;
        int captureTicks = 0;

        while (target.Exists())
        {
            if (Random.NextDouble() < breakChance)
            {
                CaptureFailed(target, player);

                yield break;
            }
            else
            {
                duration += CaptureInterval;
                float percentCaptured = (duration / CaptureTime) * 100;

                Entity sctEntity = ScrollingCombatTextMessage.Create(EntityManager, EndSimulationEntityCommandBufferSystem.CreateCommandBuffer(), assetGuid, targetPosition, color, player, percentCaptured, default, userEntity);
                captureTicks++;
            }

            if (captureTicks >= TicksRequired)
            {
                CaptureSuccess(target);
                FamiliarUnlockSystem.HandleUnlock(targetPrefab, player, true); // Define this method as per your capture success logic

                yield break;
            }

            yield return CaptureTick;
        }
    }
    static IEnumerator DelayedDestroy(Entity target)
    {
        yield return DestroyDelay;

        DestroyUtility.Destroy(EntityManager, target, DestroyDebugReason.None);
    }
    static void CaptureSuccess(Entity target)
    {
        if (ServerGameManager.TryGetBuff(target, CaptureBuff, out Entity captureBuffEntity))
        {
            DestroyUtility.Destroy(EntityManager, captureBuffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        if (ServerGameManager.TryGetBuff(target, ImmaterialBuff, out Entity immaterialBuffEntity))
        {
            DestroyUtility.Destroy(EntityManager, immaterialBuffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        BuffUtilities.ApplyBuff(SuccessBuff, target);
        Core.StartCoroutine(DelayedDestroy(target));
    }
    static void CaptureFailed(Entity target, Entity player)
    {
        if (ServerGameManager.TryGetBuff(target, CaptureBuff, out Entity captureBuffEntity))
        {
            DestroyUtility.Destroy(EntityManager, captureBuffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        if (ServerGameManager.TryGetBuff(target, ImmaterialBuff, out Entity immaterialBuffEntity))
        {
            DestroyUtility.Destroy(EntityManager, immaterialBuffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        BuffUtilities.ApplyBuff(BreakBuff, target);
    }
}