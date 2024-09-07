using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Patches.ReplaceAbilityOnGroupSlotSystemPatch;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        NativeArray<Entity> entities = __instance._OnPostCastFinishedQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;
                if (!Classes) return;

                AbilityPostCastFinishedEvent postCast = entity.Read<AbilityPostCastFinishedEvent>();
                PrefabGUID abilityGroupPrefab = postCast.AbilityGroup.Read<PrefabGUID>();

                if (postCast.Character.IsPlayer() && ClassSpells.ContainsKey(abilityGroupPrefab.GuidHash))
                {
                    float cooldown = ClassSpells[abilityGroupPrefab.GuidHash].Equals(0) ? 8f : ClassSpells[abilityGroupPrefab.GuidHash] * 15f;
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
