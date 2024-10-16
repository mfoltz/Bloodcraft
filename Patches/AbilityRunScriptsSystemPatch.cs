﻿using Bloodcraft.Services;
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

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!Classes) return;

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
                    float cooldown = ClassSpells[abilityGroupPrefab].Equals(0) ? 8f : ClassSpells[abilityGroupPrefab] * 15f;
                    ServerGameManager.SetAbilityGroupCooldown(postCast.Character, abilityGroupPrefab, cooldown);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
