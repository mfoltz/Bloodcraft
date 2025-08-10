using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
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

    const float DELAY_ADD = 1.25f;

    static readonly Dictionary<ulong, DateTime> _lastUpdateCache = [];

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _quests = ConfigService.QuestSystem;

    [HarmonyPatch(typeof(VBloodSystem), nameof(VBloodSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(VBloodSystem __instance)
    {
        if (!Core.IsReady) return;

        DateTime now = DateTime.UtcNow;
        NativeList<VBloodConsumed> events = __instance.EventList;

        try
        {
            foreach (VBloodConsumed vBloodConsumed in events)
            {
                Entity playerCharacter = vBloodConsumed.Target;
                User user = playerCharacter.GetUser();
                ulong steamId = user.PlatformId;

                if (_lastUpdateCache.TryGetValue(steamId, out DateTime lastUpdate) && (now - lastUpdate).TotalSeconds < 5) continue;

                _lastUpdateCache[steamId] = now;

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];

                // Core.Log.LogInfo($"Processing vBlood for {playerCharacter.GetSteamId()}"); // this patch is somehow triggering the double stats? odd, but need to either figure that out or move to deathEventSystem

                DeathEventListenerSystemPatch.DeathEventArgs deathEventArgs = new()
                {
                    Source = playerCharacter,
                    Target = vBlood,
                    ScrollingTextDelay = 0f,
                    // RefreshStats = false
                };

                if (_leveling)
                {
                    int currentLevel = LevelingSystem.GetLevel(steamId);
                    LevelingSystem.ProcessExperienceGain(playerCharacter, vBlood, steamId, currentLevel, deathEventArgs.ScrollingTextDelay);
                    deathEventArgs.ScrollingTextDelay += DELAY_ADD;
                }
                if (_expertise)
                {
                    WeaponSystem.ProcessExpertise(deathEventArgs);
                }
                if (_legacies)
                {
                    BloodSystem.ProcessLegacy(deathEventArgs);
                }
                if (_familiars)
                {
                    FamiliarLevelingSystem.ProcessFamiliarExperience(playerCharacter, vBlood, steamId, 1f);
                    FamiliarUnlockSystem.ProcessUnlock(playerCharacter, vBlood);
                }
                if (_quests && steamId.TryGetPlayerQuests(out var questData)) QuestSystem.ProcessQuestProgress(questData, vBloodConsumed.Source, 1, user);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"Error in VBloodSystemPatch Prefix: {e}");
        }
    }
}
