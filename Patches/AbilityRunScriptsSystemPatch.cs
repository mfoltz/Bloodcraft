using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Shapeshifts;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    const float COOLDOWN_FACTOR = 8f;
    public static IReadOnlyDictionary<PrefabGUID, int> ClassSpells => _classSpells;
    static readonly Dictionary<PrefabGUID, int> _classSpells = [];

    static readonly PrefabGUID _useWaypointAbilityGroup = PrefabGUIDs.AB_Interact_UseWaypoint_AbilityGroup;
    static readonly PrefabGUID _useCastleWaypointAbilityGroup = PrefabGUIDs.AB_Interact_UseWaypoint_Castle_AbilityGroup;
    static readonly PrefabGUID _vanishBuff = Buffs.VanishBuff;

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_classes) return;

        // NativeArray<Entity> entities = __instance._OnCastEndedQuery.ToEntityArray(Allocator.Temp);
        NativeArray<AbilityPostCastEndedEvent> postCastEndedEvents = __instance._OnPostCastEndedQuery.ToComponentDataArray<AbilityPostCastEndedEvent>(Allocator.Temp);

        try
        {
            foreach (AbilityPostCastEndedEvent postCastEndedEvent in postCastEndedEvents)
            {
                if (postCastEndedEvent.AbilityGroup.Has<VBloodAbilityData>()) continue;
                else if (postCastEndedEvent.Character.TryGetPlayer(out Entity playerCharacter))
                {
                    PrefabGUID prefabGuid = postCastEndedEvent.AbilityGroup.GetPrefabGuid();
                    bool isExoForm = playerCharacter.IsExoForm();

                    if (isExoForm && ShapeshiftRegistry.TryGetByAbilityGroup(prefabGuid, out var shapeshift))
                    {
                        if (shapeshift.TryGetCooldown(prefabGuid, out var cooldown))
                        {
                            ServerGameManager.SetAbilityGroupCooldown(postCastEndedEvent.Character, prefabGuid, cooldown);
                        }
                    }
                    else if (ClassSpells.ContainsKey(prefabGuid))
                    {
                        float cooldown = ClassSpells[prefabGuid].Equals(0) ? COOLDOWN_FACTOR : (ClassSpells[prefabGuid] + 1) * COOLDOWN_FACTOR;
                        ServerGameManager.SetAbilityGroupCooldown(postCastEndedEvent.Character, prefabGuid, cooldown);
                    }
                }
            }
        }
        finally
        {
            postCastEndedEvents.Dispose();
        }
    }

    [HarmonyPatch(typeof(AbilityCastStarted_SetupAbilityTargetSystem_Shared), nameof(AbilityCastStarted_SetupAbilityTargetSystem_Shared.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityCastStarted_SetupAbilityTargetSystem_Shared __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        // NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        NativeArray<AbilityCastStartedEvent> castStartedEvents = __instance.EntityQueries[0].ToComponentDataArray<AbilityCastStartedEvent>(Allocator.Temp);

        try
        {
            foreach (AbilityCastStartedEvent castStartedEvent in castStartedEvents)
            {
                PrefabGUID prefabGuid = castStartedEvent.AbilityGroup.GetPrefabGuid();

                if ((prefabGuid.Equals(_useCastleWaypointAbilityGroup) || prefabGuid.Equals(_useWaypointAbilityGroup)) && castStartedEvent.Character.TryGetPlayer(out Entity playerCharacter))
                {
                    User user = playerCharacter.GetUser();
                    ulong steamId = user.PlatformId;

                    bool hasActive = steamId.HasActiveFamiliar();
                    bool isDismissed = steamId.HasDismissedFamiliar();

                    if (hasActive && !isDismissed)
                    {
                        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                        if (familiar.HasBuff(_vanishBuff))
                        {
                            continue;
                        }

                        Familiars.AutoCallMap[playerCharacter] = familiar;
                        Familiars.DismissFamiliar(playerCharacter, familiar, user, steamId);
                    }
                }
            }
        }
        finally
        {
            castStartedEvents.Dispose();
        }
    }
    public static void AddClassSpell(PrefabGUID prefabGuid, int spellIndex)
    {
        _classSpells.TryAdd(prefabGuid, spellIndex);
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