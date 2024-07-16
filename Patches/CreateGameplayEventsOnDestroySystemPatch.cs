using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
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
    const int BaseFishingXP = 100;

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

                if (Plugin.ProfessionSystem.Value && PrefabGUID.GuidHash.Equals(-1130746976)) // fishing travel to target, this indicates a succesful fishing event
                {
                    Entity character = entity.Read<EntityOwner>().Owner;
                    User user = character.Read<PlayerCharacter>().UserEntity.Read<User>();
                    ulong steamId = user.PlatformId;
                    PrefabGUID toProcess = new(0);
                    Entity target = entity.Read<Buff>().Target;

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
                        ProfessionUtilities.SetProfession(user, steamId, BaseFishingXP * multiplier, handler);
                        ProfessionUtilities.GiveProfessionBonus(toProcess, character, user, steamId, handler);
                    }
                }

                if (PrefabGUID.GuidHash.Equals(-1106009274) && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) // feed complete non-vblood kills
                {
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    Entity killer = entity.Read<EntityOwner>().Owner;

                    if (Plugin.BloodSystem.Value) LegacyUtilities.UpdateLegacy(killer, died);
                    if (Plugin.ExpertiseSystem.Value) ExpertiseUtilities.UpdateExpertise(killer, died);
                    if (Plugin.LevelingSystem.Value) PlayerLevelingUtilities.UpdateLeveling(killer, died);
                    if (Plugin.FamiliarSystem.Value)
                    {
                        FamiliarLevelingUtilities.UpdateFamiliar(killer, died);
                        FamiliarUnlockUtilities.HandleUnitUnlock(killer, died);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited GameplayEventsSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
}