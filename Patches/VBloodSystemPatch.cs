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

    static readonly Dictionary<ulong, DateTime> LastUpdateCache = [];

    static readonly bool Leveling = ConfigService.LevelingSystem;
    static readonly bool Expertise = ConfigService.ExpertiseSystem;
    static readonly bool Legacies = ConfigService.BloodSystem;
    static readonly bool Familiars = ConfigService.FamiliarSystem;
    static readonly bool Quests = ConfigService.QuestSystem;

    [HarmonyPatch(typeof(VBloodSystem), nameof(VBloodSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(VBloodSystem __instance)
    {
        if (!Core._initialized) return;

        NativeList<VBloodConsumed> events = __instance.EventList;
        DateTime now = DateTime.UtcNow;
        try
        {
            foreach (VBloodConsumed vBloodConsumed in events)
            {
                Entity player = vBloodConsumed.Target;
                User user = player.Read<PlayerCharacter>().UserEntity.Read<User>();
                ulong steamId = user.PlatformId;

                if (LastUpdateCache.TryGetValue(steamId, out DateTime lastUpdate) && (now - lastUpdate).TotalSeconds < 5) continue;

                LastUpdateCache[steamId] = now;

                Entity vBlood = PrefabCollectionSystem._PrefabGuidToEntityMap[vBloodConsumed.Source];

                if (Leveling)
                {
                    int currentLevel = LevelingSystem.GetLevel(steamId);
                    LevelingSystem.ProcessExperienceGain(player, vBlood, steamId, currentLevel);
                }
                if (Expertise) WeaponSystem.ProcessExpertise(player, vBlood);
                if (Legacies) BloodSystem.ProcessLegacy(player, vBlood);
                if (Familiars)
                {
                    FamiliarLevelingSystem.ProcessFamiliarExperience(player, vBlood, steamId, 1f);
                    FamiliarUnlockSystem.ProcessUnlock(player, vBlood);
                }
                if (Quests && steamId.TryGetPlayerQuests(out var questData)) QuestSystem.ProcessQuestProgress(questData, vBloodConsumed.Source, 1, user);
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Error in VBloodSystemPatch: {e}");
        }
    }
}