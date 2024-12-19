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

    static readonly PrefabGUID _beforeTheHunt = new(-122882616);
    static readonly PrefabGUID _gettingReadyForTheHunt = new(560247139);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ClaimAchievementSystem), nameof(ClaimAchievementSystem.HandleEvent))]
    static void ClaimAchievementSystemPostfix(
        ClaimAchievementSystem __instance,
        EntityCommandBuffer commandBuffer,
        PrefabGUID claimAchievementGUID,
        FromCharacter fromCharacter,
        bool forceClaim)
    {
        if (!Core._initialized) return;
        else if (!_leveling) return;

        if (claimAchievementGUID.Equals(_beforeTheHunt))
        {
            Entity characterEntity = fromCharacter.Character;
            Entity userEntity = fromCharacter.User;

            if (!userEntity.Has<AchievementOwner>()) return;

            Entity achievementOwnerEntity = userEntity.ReadRO<AchievementOwner>().Entity._Entity;
            EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

            __instance.CompleteAchievement(entityCommandBuffer, _gettingReadyForTheHunt, userEntity, characterEntity, achievementOwnerEntity, false, false);
        }
    }
}