using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class VBloodSystemPatch
{
    static PrefabCollectionSystem PrefabCollectionSystem => Core.PrefabCollectionSystem;

    static Dictionary<ulong, DateTime> lastUpdateCache = [];

    [HarmonyPatch(typeof(VBloodSystem), nameof(VBloodSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(VBloodSystem __instance)
    {
        NativeList<VBloodConsumed> events = __instance.EventList;
        DateTime now = DateTime.Now;
        try
        {
            foreach (VBloodConsumed vBloodConsumed in events)
            {
                Entity player = vBloodConsumed.Target;
                ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                if (lastUpdateCache.TryGetValue(steamId, out DateTime lastUpdate) && (now - lastUpdate).TotalSeconds < 5) continue;
                
                lastUpdateCache[steamId] = now;

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];

                if (Plugin.LevelingSystem.Value) LevelingSystem.UpdateLeveling(player, vBlood);
                if (Plugin.ExpertiseSystem.Value) ExpertiseSystem.UpdateExpertise(player, vBlood);
                if (Plugin.BloodSystem.Value) BloodSystem.UpdateLegacy(player, vBlood);
                if (Plugin.FamiliarSystem.Value) FamiliarLevelingSystem.UpdateFamiliar(player, vBlood);
                if (Plugin.FamiliarSystem.Value) FamiliarUnlockSystem.HandleUnitUnlock(player, vBlood);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Error in VBloodSystemPatch: {e}");
        }
    }
}