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

    const float COOLDOWN_FACTOR = 8f; // actual cooldown will be divided by 2 after index * COOLDOWN_FACTOR, idk why

    public static readonly Dictionary<PrefabGUID, int> ClassSpells = [];

    static readonly PrefabGUID _dominateBuff = new(-1447419822);
    static readonly PrefabGUID _useWaypointAbilityGroup = new(695067846);
    static readonly PrefabGUID _useCastleWaypointAbilityGroup = new(893332545);

    static readonly Dictionary<int, float> _exoFormCooldownMap = new() // not currently setting cooldowns, will consider later
    {
        { 0, 8f },
        { 1, 8f },
        { 2, 8f },
        { 3, 8f },
        { 4, 10f },
        { 5, 20f },
        { 6, 40f }
    };

    /*
    public static readonly Dictionary<int, PrefabGUID> ExoFormAbilityMap = new()
    {
        { 0, new(-1473399128) }, // primary attack fast shockwaveslash
        { 1, new(841757706) }, // first weapon skill downswing detonate
        { 2, new(-1940289109) }, // space dash skill teleport behind target
        { 3, new(1270706044) }, // shift dash skill veil of bats
        { 4, new(532210332) }, // second weapon skill sword throw
        { 5, new(-1161896955) }, // first spell skill etherial sword
        { 6, new(-7407393) }, // second spell skill ring of blood
        { 7, new(797450963) } //  ultimate AB_Vampire_Dracula_BloodBoltSwarm_AbilityGroup
    };
    */

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_classes) return;

        NativeArray<Entity> entities = __instance._OnPostCastFinishedQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out AbilityPostCastFinishedEvent postCast) || !postCast.AbilityGroup.TryGetComponent(out PrefabGUID prefabGuid)) continue;
                else if (postCast.AbilityGroup.Has<VBloodAbilityData>()) continue;
                else if (postCast.Character.IsPlayer())
                {
                    // Core.Log.LogInfo(postCast.AbilityGroup.GetPrefabGUID().LookupName());

                    if (ClassSpells.ContainsKey(prefabGuid))
                    {
                        float cooldown = ClassSpells[prefabGuid].Equals(0) ? COOLDOWN_FACTOR : (ClassSpells[prefabGuid] + 1) * COOLDOWN_FACTOR;
                        ServerGameManager.SetAbilityGroupCooldown(postCast.Character, prefabGuid, cooldown);
                    }
                    else if (_exoForm && Buffs.ExoFormAbilityMap.ContainsValue(prefabGuid))
                    {
                        if (postCast.AbilityGroup.TryGetComponent(out AbilityGroupSlot abilityGroupSlot) && _exoFormCooldownMap.TryGetValue(abilityGroupSlot.SlotId, out float cooldown))
                        {
                            ServerGameManager.SetAbilityGroupCooldown(postCast.Character, prefabGuid, cooldown);
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

    [HarmonyPatch(typeof(AbilityCastStarted_SetupAbilityTargetSystem_Shared), nameof(AbilityCastStarted_SetupAbilityTargetSystem_Shared.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityCastStarted_SetupAbilityTargetSystem_Shared __instance)
    {
        if (!Core._initialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out AbilityCastStartedEvent castStartedEvent) || !castStartedEvent.AbilityGroup.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if ((prefabGUID.Equals(_useCastleWaypointAbilityGroup) || prefabGUID.Equals(_useWaypointAbilityGroup)) && castStartedEvent.Character.TryGetPlayer(out Entity player))
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    Entity familiar = Familiars.FindPlayerFamiliar(player);
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