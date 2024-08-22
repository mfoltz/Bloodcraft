using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
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
    

    const int BaseFishingXP = 100;
    static readonly PrefabGUID fishingTravelToTarget = new(-1130746976);
    static readonly PrefabGUID feedComplete = new(-1106009274);

    [HarmonyPatch(typeof(CreateGameplayEventOnDestroySystem), nameof(CreateGameplayEventOnDestroySystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnDestroySystem __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1297357609_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<Buff>() || !entity.Has<PrefabGUID>()) continue;

                PrefabGUID PrefabGUID = entity.Read<PrefabGUID>();

                if (ConfigService.ProfessionSystem && PrefabGUID.Equals(fishingTravelToTarget)) // fishing travel to target, this indicates a succesful fishing event
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
                    int multiplier = ProfessionMappings.GetFishingModifier(toProcess);

                    if (handler != null)
                    {
                        ProfessionSystem.SetProfession(user, steamId, BaseFishingXP * multiplier, handler);
                        ProfessionSystem.GiveProfessionBonus(toProcess, character, user, steamId, handler);
                    }
                }

                if (PrefabGUID.Equals(feedComplete) && entity.GetOwner().HasPlayer(out Entity player)) // feed complete non-vblood kills
                {
                    Entity died = entity.GetSpellTarget();
                    Entity userEntity = player.Read<PlayerCharacter>().UserEntity;
                    User user = userEntity.Read<User>();

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
                        QuestSystem.UpdateQuests(player, userEntity, died.Read<PrefabGUID>());
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