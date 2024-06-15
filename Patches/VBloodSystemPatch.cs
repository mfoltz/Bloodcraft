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
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    [HarmonyPatch(typeof(VBloodSystem), nameof(VBloodSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(VBloodSystem __instance)
    {
        NativeList<VBloodConsumed> events = __instance.EventList;
        try
        {
            List<VBloodConsumed> uniqueEvents = [];

            foreach (VBloodConsumed vBloodConsumed in events)
            {
                // Check if the event is already in the list
                bool isDuplicate = false;
                foreach (VBloodConsumed uniqueEvent in uniqueEvents)
                {
                    if (uniqueEvent.Source.Equals(vBloodConsumed.Source) && uniqueEvent.Target.Equals(vBloodConsumed.Target))
                    {
                        isDuplicate = true;
                        break;
                    }
                }
                // Add the event to the list if it is not a duplicate
                if (!isDuplicate)
                {
                    uniqueEvents.Add(vBloodConsumed);
                }
            }

            foreach (VBloodConsumed vBloodConsumed in uniqueEvents)
            {
                //Core.Log.LogInfo($"VBloodConsumed events: {events.Length} | Unique events: {uniqueEvents.Count}");

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];
                Entity player = vBloodConsumed.Target;

                bool playerCheck = vBloodConsumed.Target.Has<PlayerCharacter>();

                if (Plugin.LevelingSystem.Value && playerCheck) LevelingSystem.UpdateLeveling(player, vBlood);
                if (Plugin.ExpertiseSystem.Value && playerCheck) ExpertiseSystem.UpdateExpertise(player, vBlood);
                if (Plugin.BloodSystem.Value && playerCheck) BloodSystem.UpdateLegacy(player, vBlood);
                if (Plugin.FamiliarSystem.Value && playerCheck) FamiliarLevelingSystem.UpdateFamiliar(player, vBlood);
                if (Plugin.FamiliarSystem.Value && playerCheck) FamiliarUnlockSystem.HandleUnitUnlock(player, vBlood);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Error in VBloodSystemPatch: {e}");
        }
    }
}