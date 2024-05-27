using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class VBloodSystemPatch
{
    [HarmonyPatch(typeof(VBloodSystem), nameof(VBloodSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(VBloodSystem __instance)
    {
        NativeList<VBloodConsumed> events = __instance.EventList;
        try
        {
            foreach (VBloodConsumed vBloodConsumed in events)
            {
                Entity vBlood = Core.PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];
                Entity player = vBloodConsumed.Target;
                if (Plugin.LevelingSystem.Value && vBloodConsumed.Target.Has<PlayerCharacter>()) LevelingSystem.UpdateLeveling(player, vBlood);
                if (Plugin.ExpertiseSystem.Value && vBloodConsumed.Target.Has<PlayerCharacter>()) ExpertiseSystem.UpdateExpertise(player, vBlood);
                if (Plugin.ProfessionSystem.Value && vBloodConsumed.Target.Has<PlayerCharacter>()) BloodSystem.UpdateLegacy(player, vBlood);
                //if (Plugin.FamiliarSystem.Value && vBloodConsumed.Target.Has<PlayerCharacter>()) FamiliarLevelingSystem.UpdateFamiliar(player, vBlood);
            }
        }
        catch (System.Exception e)
        {
            Core.Log.LogError($"Error in VBloodSystemPatch: {e}");
        }
    }
}