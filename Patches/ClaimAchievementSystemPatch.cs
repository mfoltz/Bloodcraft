using Bloodcraft.Resources;
using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ClaimAchievementSystemPatch
{
    static SystemService SystemService => Core.SystemService;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static readonly bool _leveling = ConfigService.LevelingSystem;

    static readonly PrefabGUID _shelter = PrefabGUIDs.Journal_Shelter;
    static readonly PrefabGUID _gettingReadyForTheHunt = PrefabGUIDs.Journal_GettingReadyForTheHunt;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClaimAchievementSystem), nameof(ClaimAchievementSystem.HandleEvent))]
    static void ClaimAchievementSystemPostfix(
        ClaimAchievementSystem __instance,
        EntityCommandBuffer commandBuffer,
        PrefabGUID claimAchievementGUID,
        FromCharacter fromCharacter,
        bool forceClaim)
    {
        if (!Core.IsReady) return;
        else if (!_leveling) return;

        if (claimAchievementGUID.Equals(_shelter))
        {
            Entity characterEntity = fromCharacter.Character;
            Entity userEntity = fromCharacter.User;

            if (!userEntity.TryGetComponent(out AchievementOwner achievementOwner)) return;

            Entity achievementOwnerEntity = achievementOwner.Entity.GetEntityOnServer();
            EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

            __instance.CompleteAchievement(entityCommandBuffer, _gettingReadyForTheHunt, userEntity, characterEntity, achievementOwnerEntity, false, false);
        }
    }
}