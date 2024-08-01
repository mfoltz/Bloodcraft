using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
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
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly bool Expertise = Plugin.ExpertiseSystem.Value;
    static readonly bool Familiars = Plugin.FamiliarSystem.Value;
    static readonly bool Professions = Plugin.ProfessionSystem.Value;
    static readonly bool Legacies = Plugin.BloodSystem.Value;
    static readonly bool Quests = Plugin.QuestSystem.Value;

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

                if (Familiars && deathEvent.Died.Has<Follower>() && deathEvent.Died.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // update player familiar actives data
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
                            if (Leveling) PlayerLevelingUtilities.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (Expertise) ExpertiseUtilities.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarLevelingUtilities.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarUnlockUtilities.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                            if (Quests) QuestUtilities.UpdateQuests(deathEvent.Killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                        else if (deathEvent.Died.Has<VBloodUnit>())
                        {
                            if (Leveling) PlayerLevelingUtilities.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (Expertise) ExpertiseUtilities.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarLevelingUtilities.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                            if (Legacies) LegacyUtilities.UpdateLegacy(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarUnlockUtilities.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                            if (Quests) QuestUtilities.UpdateQuests(deathEvent.Killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                        //if (Familiars) FamiliarUnlockUtilities.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                        //if (Quests) QuestUtilities.UpdateQuests(deathEvent.Killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                    }
                    else
                    {
                        if (Professions && !hasVBloodConsumeSource) // if no movement, handle resource harvest
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
                        if (Leveling) PlayerLevelingUtilities.UpdateLeveling(followedPlayer, deathEvent.Died);
                        if (Familiars) FamiliarLevelingUtilities.UpdateFamiliar(followedPlayer, deathEvent.Died);
                        if (Quests) QuestUtilities.UpdateQuests(followedPlayer, userEntity, deathEvent.Died.Read<PrefabGUID>());
                    }
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<PlayerCharacter>() && deathEvent.Died.Has<Movement>()) // player summon kills
                {
                    Entity killer = deathEvent.Killer.Read<EntityOwner>().Owner;
                    Entity userEntity = killer.Read<PlayerCharacter>().UserEntity;
                    if (!hasVBloodConsumeSource)
                    {
                        if (Leveling) PlayerLevelingUtilities.UpdateLeveling(killer, deathEvent.Died);
                        if (Expertise) ExpertiseUtilities.UpdateExpertise(killer, deathEvent.Died);
                        if (Quests) QuestUtilities.UpdateQuests(killer, userEntity, deathEvent.Died.Read<PrefabGUID>());
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
                            if (Leveling) PlayerLevelingUtilities.UpdateLeveling(follower.Followed._Value, deathEvent.Died);
                            if (Familiars) FamiliarLevelingUtilities.UpdateFamiliar(follower.Followed._Value, deathEvent.Died);
                            if (Quests) QuestUtilities.UpdateQuests(character, userEntity, deathEvent.Died.Read<PrefabGUID>());
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited DeathEventListenerSystem hook early: {e}");
        }
        finally
        {
            deathEvents.Dispose();
        }
    } 
}