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

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    public static readonly Dictionary<PrefabGUID, int> ClassSpells = [];

    static readonly PrefabGUID DominateBuff = new(-1447419822);
    static readonly PrefabGUID UseWaypointAbilityGroup = new(695067846);
    static readonly PrefabGUID UseCastleWaypointAbilityGroup = new(893332545);
    
    static readonly Dictionary<int, float> ExoFormCooldownMap = new() // not currently setting cooldowns, will consider later
    {
        { 1, 8f },
        { 2, 8f },
        { 3, 8f },
        { 4, 8f },
        { 5, 10f },
        { 6, 10f },
        { 7, 50f }
    };

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!Classes) return;

        NativeArray<Entity> entities = __instance._OnPostCastFinishedQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                AbilityPostCastFinishedEvent postCast = entity.Read<AbilityPostCastFinishedEvent>();
                PrefabGUID abilityGroupPrefab = postCast.AbilityGroup.Read<PrefabGUID>();

                if (postCast.AbilityGroup.Has<VBloodAbilityData>()) continue;
                else if (postCast.Character.IsPlayer())
                {
                    //Core.Log.LogInfo(postCast.AbilityGroup.GetPrefabGUID().LookupName());

                    if (ClassSpells.ContainsKey(abilityGroupPrefab))
                    {
                        float cooldown = ClassSpells[abilityGroupPrefab].Equals(0) ? 8f : (ClassSpells[abilityGroupPrefab] + 1) * 8f;
                        ServerGameManager.SetAbilityGroupCooldown(postCast.Character, abilityGroupPrefab, cooldown);
                    }
                    /*
                    else if (ConfigService.ExoPrestiging && BuffUtilities.ExoFormAbilityMap.ContainsValue(abilityGroupPrefab))
                    {
                        if (postCast.AbilityGroup.TryGetComponent(out AbilityGroupSlot abilityGroupSlot) && ExoFormCooldownMap.TryGetValue(abilityGroupSlot.SlotId, out float cooldown))
                        {
                            ServerGameManager.SetAbilityGroupCooldown(postCast.Character, abilityGroupPrefab, cooldown);
                        }
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

    [HarmonyPatch(typeof(AbilityCastStarted_SetupAbilityTargetSystem_Shared), nameof(AbilityCastStarted_SetupAbilityTargetSystem_Shared.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityCastStarted_SetupAbilityTargetSystem_Shared __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                AbilityCastStartedEvent castStartedEvent = entity.Read<AbilityCastStartedEvent>();
                PrefabGUID prefabGUID = castStartedEvent.AbilityGroup.Read<PrefabGUID>();

                //Core.Log.LogInfo($"AbilityCastStarted_SetupAbilityTargetSystem_Shared: {prefabGUID.LookupName()}");

                if ((prefabGUID.Equals(UseCastleWaypointAbilityGroup) || prefabGUID.Equals(UseWaypointAbilityGroup)) && castStartedEvent.Character.TryGetPlayer(out Entity player))
                {
                    //Core.Log.LogInfo("Waypoint cast detected, dismissing familiar if found...");
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
                    if (familiar.Exists() && !familiar.IsDisabled() && steamId.TryGetFamiliarActives(out var data))
                    {
                        //FamiliarUtilities.AutoCallMap.TryAdd(player, familiar);
                        
                        FamiliarUtilities.AutoCallMap[player] = familiar;
                        FamiliarUtilities.DismissFamiliar(player, familiar, user, steamId, data);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
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