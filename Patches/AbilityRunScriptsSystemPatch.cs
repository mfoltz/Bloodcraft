using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
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
                else if (postCast.Character.IsPlayer() && ClassSpells.ContainsKey(abilityGroupPrefab))
                {
                    float cooldown = ClassSpells[abilityGroupPrefab].Equals(0) ? 8f : (ClassSpells[abilityGroupPrefab] + 1) * 8f;
                    ServerGameManager.SetAbilityGroupCooldown(postCast.Character, abilityGroupPrefab, cooldown);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
        /*
        if (ConfigService.FamiliarSystem)
        {
            NativeArray<Entity> entities = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (Entity entity in entities)
                {
                    AbilityCastStartedEvent preCast = entity.Read<AbilityCastStartedEvent>();
                    PrefabGUID prefabGUID = preCast.AbilityGroup.Read<PrefabGUID>();

                    if ((prefabGUID.Equals(UseCastleWaypointCast) || prefabGUID.Equals(UseWaypointCast)) && entity.GetOwner().TryGetPlayer(out Entity player) && !ServerGameManager.HasBuff(player, DominateBuff.ToIdentifier()))
                    {
                        Core.Log.LogInfo("Waypoint cast detected, dismissing familiar if found...");

                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                        if (familiar.Exists() && !familiar.IsDisabled())
                        {
                            FamiliarUtilities.AutoDismiss(player, familiar);
                        }
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

                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                    if (familiar.Exists() && !familiar.IsDisabled())
                    {
                        FamiliarUtilities.AutoDismiss(player, familiar);
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