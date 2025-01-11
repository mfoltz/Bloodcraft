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

    static readonly Dictionary<ulong, DateTime> _lastUpdateCache = [];

    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _quests = ConfigService.QuestSystem;

    [HarmonyPatch(typeof(VBloodSystem), nameof(VBloodSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(VBloodSystem __instance)
    {
        if (!Core._initialized) return;

        DateTime now = DateTime.UtcNow;
        NativeList<VBloodConsumed> events = __instance.EventList;

        try
        {
            foreach (VBloodConsumed vBloodConsumed in events)
            {
                Entity player = vBloodConsumed.Target;
                User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
                ulong steamId = user.PlatformId;

                if (_lastUpdateCache.TryGetValue(steamId, out DateTime lastUpdate) && (now - lastUpdate).TotalSeconds < 5) continue;

                _lastUpdateCache[steamId] = now;

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];

                if (_leveling)
                {
                    int currentLevel = LevelingSystem.GetLevel(steamId);
                    LevelingSystem.ProcessExperienceGain(player, vBlood, steamId, currentLevel);
                }
                if (_expertise) WeaponSystem.ProcessExpertise(player, vBlood);
                if (_legacies) BloodSystem.ProcessLegacy(player, vBlood);
                if (_familiars)
                {
                    FamiliarLevelingSystem.ProcessFamiliarExperience(player, vBlood, steamId, 1f);
                    FamiliarUnlockSystem.ProcessUnlock(player, vBlood);
                }
                if (_quests && steamId.TryGetPlayerQuests(out var questData)) QuestSystem.ProcessQuestProgress(questData, vBloodConsumed.Source, 1, user);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Error in VBloodSystemPatch: {e}");
        }
    }
}