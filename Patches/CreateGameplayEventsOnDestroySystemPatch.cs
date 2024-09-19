using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
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
    const int BaseFishingXP = 100; // somewhat arbitrary constant that I need to revisit when looking at professions again soon

    static readonly PrefabGUID fishingTravelToTarget = new(-1130746976);
    static readonly PrefabGUID feedComplete = new(-1106009274);

    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnDestroySystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.ProfessionSystem) return;

        NativeArray<Entity> entities = __instance.__query_1297357609_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<Buff>() || !entity.Has<PrefabGUID>()) continue;

                PrefabGUID PrefabGUID = entity.Read<PrefabGUID>();
                if (PrefabGUID.Equals(fishingTravelToTarget)) // fishing travel to target, this indicates a succesful fishing event
                {
                    Entity character = entity.GetOwner();
                    User user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
                    ulong steamId = user.PlatformId;

                    PrefabGUID toProcess = new(0);
                    Entity target = entity.GetBuffTarget();

                    if (target.Has<DropTableBuffer>())
                    {
                        var dropTableBuffer = target.ReadBuffer<DropTableBuffer>();
                        if (!dropTableBuffer.IsEmpty)
                        {
                            toProcess = dropTableBuffer[0].DropTableGuid;
                        }
                    }

                    if (toProcess.GuidHash == 0)
                    {
                        continue;
                    }

                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(toProcess, "");
                    if (handler != null)
                    {
                        int multiplier = ProfessionMappings.GetFishingModifier(toProcess);
                        ProfessionSystem.SetProfession(target, character, steamId, BaseFishingXP * multiplier, handler);
                        ProfessionSystem.GiveProfessionBonus(toProcess, character, user, steamId, handler);
                    }
                }
                /* there might be some reason I'm forgetting I don't just process these over in deathEventListerSystem but let's try and find out
                if (PrefabGUID.Equals(feedComplete) && entity.GetOwner().TryGetPlayer(out Entity player)) // feed complete non-vblood kills
                {
                    Entity died = entity.GetSpellTarget();
                    Entity userEntity = player.Read<PlayerCharacter>().UserEntity;

                    if (ConfigService.BloodSystem) BloodSystem.UpdateLegacy(player, died);
                    if (ConfigService.ExpertiseSystem) WeaponSystem.UpdateExpertise(player, died);
                    if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(player, died);
                    if (ConfigService.FamiliarSystem)
                    {
                        FamiliarLevelingSystem.UpdateFamiliar(player, died);
                        FamiliarUnlockSystem.HandleUnitUnlock(player, died);
                    }
                    if (ConfigService.QuestSystem)
                    {
                        QuestSystem.UpdateQuests(player, died);
                    }
                }
                */
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}