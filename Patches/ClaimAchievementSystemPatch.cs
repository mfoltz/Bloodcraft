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

    static readonly PrefabGUID BeforeTheHunt = new(-122882616);
    static readonly PrefabGUID GettingReadyForTheHunt = new(560247139);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClaimAchievementSystem), nameof(ClaimAchievementSystem.HandleEvent))]
    static void ClaimAchievementSystemPostfix(
        ClaimAchievementSystem __instance,
        EntityCommandBuffer commandBuffer,
        PrefabGUID claimAchievementGUID,
        FromCharacter fromCharacter,
        bool forceClaim)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.LevelingSystem) return;

        if (claimAchievementGUID.Equals(BeforeTheHunt)) // achievement prior to GettingReadyForTheHunt
        {
            Entity characterEntity = fromCharacter.Character;
            Entity userEntity = fromCharacter.User;

            if (!userEntity.Has<AchievementOwner>()) return;

            Entity achievementOwnerEntity = userEntity.Read<AchievementOwner>().Entity._Entity;
            EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

            __instance.CompleteAchievement(entityCommandBuffer, GettingReadyForTheHunt, userEntity, characterEntity, achievementOwnerEntity, false, false);
        }
    }
}