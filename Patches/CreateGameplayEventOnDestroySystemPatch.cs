using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class CreateGameplayEventOnDestroySystemPatch
{
    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _quests = ConfigService.QuestSystem;

    const int BASE_PROFESSION_XP = 100;
    const float SCT_DELAY = 0.75f;

    static readonly PrefabGUID _fishingTravelToTarget = PrefabGUIDs.AB_Fishing_Draw_TravelToTarget;
    static readonly PrefabGUID _fishingQuestGoal = PrefabGUIDs.FakeItem_AnyFish;

    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnDestroySystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_professions && !_quests) return;

        NativeArray<Entity> entities = __instance.__query_1297357609_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (prefabGUID.Equals(_fishingTravelToTarget)) // succesful fishing event
                {
                    Entity playerCharacter = entityOwner.Owner;
                    Entity userEntity = playerCharacter.GetUserEntity();

                    User user = userEntity.GetUser();
                    ulong steamId = user.PlatformId;

                    PrefabGUID prefabGuid = PrefabGUID.Empty;
                    Entity target = entity.GetBuffTarget();

                    if (_quests && steamId.TryGetPlayerQuests(out var quests)) QuestSystem.ProcessQuestProgress(quests, _fishingQuestGoal, 1, user);

                    if (target.TryGetBuffer<DropTableBuffer>(out var buffer)
                        && !buffer.IsEmpty)
                    {
                        prefabGuid = buffer[0].DropTableGuid;
                    }

                    if (prefabGuid.IsEmpty()) continue;

                    IProfession handler = ProfessionFactory.GetProfession(prefabGuid);
                    if (handler != null)
                    {
                        Profession profession = handler.GetProfessionEnum();
                        if (profession.IsDisabled()) continue;

                        int multiplier = ProfessionMappings.GetFishingModifier(prefabGuid);
                        float delay = SCT_DELAY;

                        ProfessionSystem.SetProfession(target, playerCharacter, steamId, BASE_PROFESSION_XP * multiplier, handler, ref delay);
                        ProfessionSystem.GiveProfessionBonus(target, prefabGuid, playerCharacter, userEntity, user, steamId, handler, delay);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}