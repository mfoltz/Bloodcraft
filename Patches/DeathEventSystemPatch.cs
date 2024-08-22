using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
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
                    ulong steamId = userEntity.Read<User>().PlatformId;

                    if (deathEvent.Died.Has<Movement>() && !hasVBloodConsumeSource)
                    {
                        if (!isStatChangeInvalid ) // only process non-feed related deaths here except for gatebosses
                        {
                            if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.ExpertiseSystem) WeaponSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.FamiliarSystem)
                            {
                                FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                                FamiliarUnlockSystem.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                            }
                            if (ConfigService.QuestSystem) QuestSystem.UpdateQuests(deathEvent.Killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                        else if (deathEvent.Died.Has<VBloodUnit>())
                        {
                            if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.ExpertiseSystem) WeaponSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.FamiliarSystem)
                            {
                                FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                                FamiliarUnlockSystem.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                            }
                            if (ConfigService.BloodSystem) BloodSystem.UpdateLegacy(deathEvent.Killer, deathEvent.Died);
                            if (ConfigService.QuestSystem) QuestSystem.UpdateQuests(deathEvent.Killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                    }
                    else
                    {
                        if (ConfigService.ProfessionSystem && !hasVBloodConsumeSource) // if no movement, handle resource harvest
                        {
                            ProfessionSystem.UpdateProfessions(deathEvent.Killer, deathEvent.Died);
                        }
                    }

                    if (ConfigService.ClientCompanion && EclipseService.RegisteredUsers.Contains(steamId))
                    {
                        EclipseService.SendClientProgress(deathEvent.Killer, steamId);
                    }
                }
                else if (deathEvent.Killer.Has<Follower>() && deathEvent.Killer.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // player familiar kills
                {
                    Entity followedPlayer = deathEvent.Killer.Read<Follower>().Followed._Value;
                    Entity userEntity = followedPlayer.Read<PlayerCharacter>().UserEntity;
                    ulong steamId = userEntity.Read<User>().PlatformId;

                    if (deathEvent.Died.Has<Movement>() && !hasVBloodConsumeSource)
                    {
                        if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(followedPlayer, deathEvent.Died);
                        if (ConfigService.FamiliarSystem) FamiliarLevelingSystem.UpdateFamiliar(followedPlayer, deathEvent.Died);
                        if (ConfigService.QuestSystem) QuestSystem.UpdateQuests(followedPlayer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                    }

                    if (ConfigService.ClientCompanion && EclipseService.RegisteredUsers.Contains(steamId))
                    {
                        EclipseService.SendClientProgress(deathEvent.Killer, steamId);
                    }
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<PlayerCharacter>() && deathEvent.Died.Has<Movement>()) // player summon kills
                {
                    Entity killer = deathEvent.Killer.Read<EntityOwner>().Owner;
                    Entity userEntity = killer.Read<PlayerCharacter>().UserEntity;
                    ulong steamId = userEntity.Read<User>().PlatformId;

                    if (!hasVBloodConsumeSource)
                    {
                        if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(killer, deathEvent.Died);
                        if (ConfigService.ExpertiseSystem) WeaponSystem.UpdateExpertise(killer, deathEvent.Died);
                        if (ConfigService.QuestSystem) QuestSystem.UpdateQuests(killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                    }

                    if (ConfigService.ClientCompanion && EclipseService.RegisteredUsers.Contains(steamId))
                    {
                        EclipseService.SendClientProgress(deathEvent.Killer, steamId);
                    }
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<Follower>() && deathEvent.Killer.Read<EntityOwner>().Owner.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // familiar summon kills
                {
                    Follower follower = deathEvent.Killer.Read<EntityOwner>().Owner.Read<Follower>();
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                    ulong steamId = follower.Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    if (familiar != Entity.Null)
                    {
                        Entity character = follower.Followed._Value;
                        Entity userEntity = character.Read<PlayerCharacter>().UserEntity;
                        if (deathEvent.Died.Has<Movement>() && !hasVBloodConsumeSource)
                        {
                            if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(follower.Followed._Value, deathEvent.Died);
                            if (ConfigService.FamiliarSystem) FamiliarLevelingSystem.UpdateFamiliar(follower.Followed._Value, deathEvent.Died);
                            if (ConfigService.QuestSystem) QuestSystem.UpdateQuests(character, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                    }

                    if (ConfigService.ClientCompanion && EclipseService.RegisteredUsers.Contains(steamId))
                    {
                        EclipseService.SendClientProgress(deathEvent.Killer, steamId);
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