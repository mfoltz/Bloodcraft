using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Experience;
using Bloodcraft.SystemUtilities.Expertise;
using Bloodcraft.SystemUtilities.Familiars;
using Bloodcraft.SystemUtilities.Legacies;
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
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem; 
    static ConfigService ConfigService => Core.ConfigService;

    static Dictionary<ulong, DateTime> LastUpdateCache = [];

    [HarmonyPatch(typeof(VBloodSystem), nameof(VBloodSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(VBloodSystem __instance)
    {
        NativeList<VBloodConsumed> events = __instance.EventList;
        DateTime now = DateTime.UtcNow;
        try
        {
            foreach (VBloodConsumed vBloodConsumed in events)
            {
                if (!Core.hasInitialized) continue;

                Entity player = vBloodConsumed.Target;
                User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
                ulong steamId = user.PlatformId;

                if (LastUpdateCache.TryGetValue(steamId, out DateTime lastUpdate) && (now - lastUpdate).TotalSeconds < 5) continue;
                
                LastUpdateCache[steamId] = now;

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];

                if (ConfigService.LevelingSystem) PlayerLevelingUtilities.UpdateLeveling(player, vBlood);
                if (ConfigService.ExpertiseSystem) ExpertiseHandler.UpdateExpertise(player, vBlood);
                if (ConfigService.BloodSystem) LegacyUtilities.UpdateLegacy(player, vBlood);
                if (ConfigService.FamiliarSystem)
                {
                    FamiliarLevelingUtilities.UpdateFamiliar(player, vBlood);
                    FamiliarUnlockUtilities.HandleUnitUnlock(player, vBlood);
                }
                if (ConfigService.QuestSystem && Core.DataStructures.PlayerQuests.TryGetValue(steamId, out var questData)) QuestUtilities.ProcessQuestProgress(questData, vBloodConsumed.Source, 1, user);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Error in VBloodSystemPatch: {e}");
        }
    }
}