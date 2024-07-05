using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.SystemUtilities.Quests;
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
                if (!Core.hasInitialized) continue;

                Entity player = vBloodConsumed.Target;
                User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
                ulong steamId = user.PlatformId;

                if (lastUpdateCache.TryGetValue(steamId, out DateTime lastUpdate) && (now - lastUpdate).TotalSeconds < 5) continue;
                
                lastUpdateCache[steamId] = now;

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];

                if (Plugin.LevelingSystem.Value) PlayerLevelingUtilities.UpdateLeveling(player, vBlood);
                if (Plugin.ExpertiseSystem.Value) ExpertiseUtilities.UpdateExpertise(player, vBlood);
                if (Plugin.BloodSystem.Value) LegacyUtilities.UpdateLegacy(player, vBlood);
                if (Plugin.FamiliarSystem.Value) FamiliarLevelingUtilities.UpdateFamiliar(player, vBlood);
                if (Plugin.FamiliarSystem.Value) FamiliarUnlockUtilities.HandleUnitUnlock(player, vBlood);
                if (Plugin.QuestSystem.Value && Core.DataStructures.PlayerQuests.TryGetValue(steamId, out var questData)) QuestUtilities.UpdateQuestProgress(questData, vBloodConsumed.Source, 1, user);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Error in VBloodSystemPatch: {e}");
        }
    }
}