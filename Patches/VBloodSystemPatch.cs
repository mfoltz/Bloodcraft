using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Quests;
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

    static readonly Dictionary<ulong, DateTime> LastUpdateCache = [];

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
                if (!Core.hasInitialized) return;

                Entity player = vBloodConsumed.Target;
                User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
                ulong steamId = user.PlatformId;

                if (LastUpdateCache.TryGetValue(steamId, out DateTime lastUpdate) && (now - lastUpdate).TotalSeconds < 5) continue;

                LastUpdateCache[steamId] = now;

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];

                if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(player, vBlood);
                if (ConfigService.ExpertiseSystem) WeaponSystem.UpdateExpertise(player, vBlood);
                if (ConfigService.BloodSystem) BloodSystem.UpdateLegacy(player, vBlood);
                if (ConfigService.FamiliarSystem)
                {
                    FamiliarLevelingSystem.UpdateFamiliar(player, vBlood);
                    FamiliarUnlockSystem.HandleUnitUnlock(player, vBlood);
                }
                if (ConfigService.QuestSystem && steamId.TryGetPlayerQuests(out var questData)) QuestSystem.ProcessQuestProgress(questData, vBloodConsumed.Source, 1, user);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Error in VBloodSystemPatch: {e}");
        }
    }
}