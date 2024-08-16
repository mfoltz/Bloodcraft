using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Experience;
using Bloodcraft.SystemUtilities.Expertise;
using Bloodcraft.SystemUtilities.Familiars;
using Bloodcraft.SystemUtilities.Legacies;
using Bloodcraft.SystemUtilities.Professions;
using Bloodcraft.SystemUtilities.Quests;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DeathEventListenerSystemPatch
{
    static ConfigService ConfigService => Core.ConfigService;

    [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(DeathEventListenerSystem __instance)
    {
        NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);
        try
        {
            foreach (DeathEvent deathEvent in deathEvents)
            {
                if (!Core.hasInitialized) continue;

                bool isStatChangeInvalid = deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11);
                bool hasVBloodConsumeSource = deathEvent.Died.Has<VBloodConsumeSource>();

                if (ConfigService.FamiliarSystem && deathEvent.Died.Has<Follower>() && deathEvent.Died.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // update player familiar actives data
                {
                    ulong steamId = deathEvent.Died.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    //if (FamiliarPatches.familiarMinions.ContainsKey(deathEvent.Died)) Core.FamiliarService.HandleFamiliarMinions(deathEvent.Died);
                    if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives) && actives.FamKey.Equals(deathEvent.Died.Read<PrefabGUID>().GuidHash))
                    {
                        actives = new(Entity.Null, 0);
                        Core.DataStructures.FamiliarActives[steamId] = actives;
                        Core.DataStructures.SavePlayerFamiliarActives();
                    }
                }
                if (deathEvent.Killer.Has<PlayerCharacter>())
                {
                    Entity userEntity = deathEvent.Killer.Read<PlayerCharacter>().UserEntity;
                    if (deathEvent.Died.Has<Movement>() && !hasVBloodConsumeSource)
                    {
                        if (!isStatChangeInvalid ) // only process non-feed related deaths here except for gatebosses
                        {
                            if (ConfigService.LevelingSystem) PlayerLevelingUtilities.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.ExpertiseSystem) ExpertiseHandler.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.FamiliarSystem)
                            {
                                FamiliarLevelingUtilities.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                                FamiliarUnlockUtilities.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                            }
                            if (ConfigService.QuestSystem) QuestUtilities.UpdateQuests(deathEvent.Killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                        else if (deathEvent.Died.Has<VBloodUnit>())
                        {
                            if (ConfigService.LevelingSystem) PlayerLevelingUtilities.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.ExpertiseSystem) ExpertiseHandler.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.FamiliarSystem)
                            {
                                FamiliarLevelingUtilities.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                                FamiliarUnlockUtilities.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                            }
                            if (ConfigService.BloodSystem) LegacyUtilities.UpdateLegacy(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.QuestSystem) QuestUtilities.UpdateQuests(deathEvent.Killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                    }
                    else
                    {
                        if (ConfigService.ProfessionSystem && !hasVBloodConsumeSource) // if no movement, handle resource harvest
                        {
                            ProfessionUtilities.UpdateProfessions(deathEvent.Killer, deathEvent.Died);
                        }
                    }
                }
                else if (deathEvent.Killer.Has<Follower>() && deathEvent.Killer.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // player familiar kills
                {
                    Entity followedPlayer = deathEvent.Killer.Read<Follower>().Followed._Value;
                    Entity userEntity = followedPlayer.Read<PlayerCharacter>().UserEntity;
                    if (deathEvent.Died.Has<Movement>() && !hasVBloodConsumeSource)
                    {
                        if (ConfigService.LevelingSystem) PlayerLevelingUtilities.UpdateLeveling(followedPlayer, deathEvent.Died);
                        if (ConfigService.FamiliarSystem) FamiliarLevelingUtilities.UpdateFamiliar(followedPlayer, deathEvent.Died);
                        if (ConfigService.QuestSystem) QuestUtilities.UpdateQuests(followedPlayer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                    }
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<PlayerCharacter>() && deathEvent.Died.Has<Movement>()) // player summon kills
                {
                    Entity killer = deathEvent.Killer.Read<EntityOwner>().Owner;
                    Entity userEntity = killer.Read<PlayerCharacter>().UserEntity;
                    if (!hasVBloodConsumeSource)
                    {
                        if (ConfigService.LevelingSystem) PlayerLevelingUtilities.UpdateLeveling(killer, deathEvent.Died);
                        if (ConfigService.ExpertiseSystem) ExpertiseHandler.UpdateExpertise(killer, deathEvent.Died);
                        if (ConfigService.QuestSystem) QuestUtilities.UpdateQuests(killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                    }
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<Follower>() && deathEvent.Killer.Read<EntityOwner>().Owner.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // familiar summon kills
                {
                    Follower follower = deathEvent.Killer.Read<EntityOwner>().Owner.Read<Follower>();
                    Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                    if (familiar != Entity.Null)
                    {
                        Entity character = follower.Followed._Value;
                        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
                        if (deathEvent.Died.Has<Movement>() && !hasVBloodConsumeSource)
                        {
                            if (ConfigService.LevelingSystem) PlayerLevelingUtilities.UpdateLeveling(follower.Followed._Value, deathEvent.Died);
                            if (ConfigService.FamiliarSystem) FamiliarLevelingUtilities.UpdateFamiliar(follower.Followed._Value, deathEvent.Died);
                            if (ConfigService.QuestSystem) QuestUtilities.UpdateQuests(character, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                    }
                }
            }
        }
        finally
        {
            deathEvents.Dispose();
        }
    } 
}