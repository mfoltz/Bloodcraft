using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    const float COOLDOWN_FACTOR = 8f;

    public static readonly Dictionary<PrefabGUID, int> ClassSpells = [];

    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _useWaypointAbilityGroup = new(695067846);
    static readonly PrefabGUID _useCastleWaypointAbilityGroup = new(893332545);

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_classes) return;

        // NativeArray<Entity> entities = __instance._OnCastEndedQuery.ToEntityArray(Allocator.Temp);
        NativeArray<AbilityPostCastEndedEvent> castEndedEvents = __instance._OnPostCastEndedQuery.ToComponentDataArray<AbilityPostCastEndedEvent>(Allocator.Temp);

        try
        {
            foreach (AbilityPostCastEndedEvent postCastEnded in castEndedEvents)
            {
                if (!postCastEnded.AbilityGroup.TryGetComponent(out PrefabGUID prefabGuid)) continue;
                else if (postCastEnded.AbilityGroup.Has<VBloodAbilityData>()) continue;
                else if (postCastEnded.Character.IsPlayer())
                {
                    if (_exoForm && Buffs.ExoFormAbilityMap.ContainsValue(prefabGuid))
                    {
                        if (postCastEnded.AbilityGroup.TryGetComponent(out AbilityGroupSlot abilityGroupSlot) && Buffs.ExoFormCooldownMap.TryGetValue(abilityGroupSlot.SlotId, out float cooldown))
                        {
                            ServerGameManager.SetAbilityGroupCooldown(postCastEnded.Character, prefabGuid, cooldown);
                        }
                    }
                    else if (ClassSpells.ContainsKey(prefabGuid))
                    {
                        float cooldown = ClassSpells[prefabGuid].Equals(0) ? COOLDOWN_FACTOR : (ClassSpells[prefabGuid] + 1) * COOLDOWN_FACTOR;
                        ServerGameManager.SetAbilityGroupCooldown(postCastEnded.Character, prefabGuid, cooldown);
                    }
                }
            }
        }
        finally
        {
            castEndedEvents.Dispose();
        }
    }

    [HarmonyPatch(typeof(AbilityCastStarted_SetupAbilityTargetSystem_Shared), nameof(AbilityCastStarted_SetupAbilityTargetSystem_Shared.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityCastStarted_SetupAbilityTargetSystem_Shared __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        // NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        NativeArray<AbilityCastStartedEvent> castStartedEvents = __instance.EntityQueries[0].ToComponentDataArray<AbilityCastStartedEvent>(Allocator.Temp);

        try
        {
            foreach (AbilityCastStartedEvent castStartedEvent in castStartedEvents)
            {
                if (!castStartedEvent.AbilityGroup.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if ((prefabGUID.Equals(_useCastleWaypointAbilityGroup) || prefabGUID.Equals(_useWaypointAbilityGroup)) && castStartedEvent.Character.TryGetPlayer(out Entity player))
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    Entity familiar = Familiars.GetActiveFamiliar(player);
                    if (familiar.Exists() && !familiar.IsDisabled() && steamId.TryGetFamiliarActives(out var data))
                    {
                        Familiars.AutoCallMap[player] = familiar;
                        Familiars.DismissFamiliar(player, familiar, user, steamId, data);
                    }
                }
            }
        }
        finally
        {
            castStartedEvents.Dispose();
        }
    }

    /*
    [HarmonyPatch(typeof(EvaluateCastOptionsSystem), nameof(EvaluateCastOptionsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(EvaluateCastOptionsSystem __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Exists() && entity.TryGetComponent(out EvaluateCastOptionsRequest castOptionsRequest))
                {
                    Core.Log.LogInfo("EvaluateCastOptionEntities[");
                    //entity.LogComponentTypes();

                    if (castOptionsRequest.InternalState.CandidateEntity.Exists()) castOptionsRequest.InternalState.CandidateEntity.LogComponentTypes();
                    if (castOptionsRequest.InternalState.CandidateGroupEntity.Exists()) castOptionsRequest.InternalState.CandidateGroupEntity.LogComponentTypes();
                    if (castOptionsRequest.InternalState.CastOptionsEntity.Exists()) castOptionsRequest.InternalState.CastOptionsEntity.LogComponentTypes();
                    
                    Core.Log.LogInfo("...]");
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    */
}