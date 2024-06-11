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
                if (!entity.Has<Buff>() || !entity.Has<PrefabGUID>()) continue;
                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();

                if (Plugin.FamiliarSystem.Value && prefabGUID.LookupName().ToLower().Contains("combat"))
                {
                    if (entity.Read<Buff>().Target.Has<PlayerCharacter>())
                    {
                        //Core.Log.LogInfo(prefabGUID.GetPrefabName());
                        Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(entity.Read<Buff>().Target);
                        if (familiar != Entity.Null && Core.EntityManager.Exists(familiar))
                        {
                            Follower follower = familiar.Read<Follower>();
                            follower.ModeModifiable._Value = 0;
                            familiar.Write(follower);
                        }
                    }
                }

                if (Plugin.ProfessionSystem.Value && prefabGUID.GuidHash.Equals(-1130746976)) // fishing travel to target, this indicates a succesful fishing event
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
                    int multiplier = ProfessionUtilities.GetFishingModifier(toProcess);
                    if (handler != null)
                    {
                        ProfessionSystem.SetProfession(user, steamId, BaseFishingXP * multiplier, handler);
                        ProfessionSystem.GiveProfessionBonus(toProcess, character, user, steamId, handler);
                    }
                }

                if (prefabGUID.GuidHash.Equals(-1106009274) && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) // feed complete kills
                {
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    Entity killer = entity.Read<EntityOwner>().Owner;

                    if (Plugin.BloodSystem.Value) BloodSystem.UpdateLegacy(killer, died);
                    if (Plugin.ExpertiseSystem.Value) ExpertiseSystem.UpdateExpertise(killer, died);
                    if (Plugin.LevelingSystem.Value) LevelingSystem.UpdateLeveling(killer, died);
                    if (Plugin.FamiliarSystem.Value)
                    {
                        FamiliarLevelingSystem.UpdateFamiliar(killer, died);
                        FamiliarUnlockSystem.HandleUnitUnlock(killer, died);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited GameplayEventsSystem hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
}