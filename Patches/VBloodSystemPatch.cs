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

                bool playerCheck = vBloodConsumed.Target.Has<PlayerCharacter>();

                if (Plugin.LevelingSystem.Value && playerCheck) LevelingSystem.UpdateLeveling(player, vBlood);
                if (Plugin.ExpertiseSystem.Value && playerCheck) ExpertiseSystem.UpdateExpertise(player, vBlood);
                if (Plugin.BloodSystem.Value && playerCheck) BloodSystem.UpdateLegacy(player, vBlood);
                if (Plugin.FamiliarSystem.Value && playerCheck) FamiliarLevelingSystem.UpdateFamiliar(player, vBlood);
                if (Plugin.FamiliarSystem.Value && playerCheck) FamiliarUnlockSystem.HandleUnitUnlock(player, vBlood);
            }
        }
        catch (System.Exception e)
        {
            Core.Log.LogError($"Error in VBloodSystemPatch: {e}");
        }
    }
    
}