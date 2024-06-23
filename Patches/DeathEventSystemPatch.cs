using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Il2CppSystem.Data.Common.ObjectStorage;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DeathEventListenerSystemPatch
{
    static readonly PrefabGUID siegeGolem = new(914043867);
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly bool Expertise = Plugin.ExpertiseSystem.Value;
    static readonly bool Familiars = Plugin.FamiliarSystem.Value;
    static readonly bool Professions = Plugin.ProfessionSystem.Value;
    static readonly bool raidWatcher = Plugin.RaidMonitor.Value;
    static readonly bool Legacies = Plugin.BloodSystem.Value;

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
                
                if (raidWatcher && deathEvent.Died.Has<AnnounceCastleBreached>() && deathEvent.StatChangeReason.Equals(StatChangeReason.StatChangeSystem_0))
                {
                    if (Core.ServerGameManager.TryGetBuff(deathEvent.Killer, siegeGolem.ToIdentifier(), out Entity buff)) // if this was done by a player with a siege golem buff, start raid service
                    {
                        RaidService.StartRaidMonitor(deathEvent.Killer, deathEvent.Died);
                    }
                }

                if (Familiars && deathEvent.Died.Has<Follower>() && deathEvent.Died.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // update player familiar actives data
                {
                    ulong steamId = deathEvent.Died.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    //if (FamiliarPatches.familiarMinions.ContainsKey(deathEvent.Died)) Core.FamiliarService.HandleFamiliarMinions(deathEvent.Died);
                    if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives) && actives.Item2.Equals(deathEvent.Died.Read<PrefabGUID>().GuidHash))
                    {
                        actives = new(Entity.Null, 0);
                        Core.DataStructures.FamiliarActives[steamId] = actives;
                        Core.DataStructures.SavePlayerFamiliarActives();
                    }
                }

                if (deathEvent.Killer.Has<PlayerCharacter>())
                {
                    if (deathEvent.Died.Has<Movement>())
                    {
                        if (!isStatChangeInvalid && !hasVBloodConsumeSource) // only process non-feed related deaths here except for gatebosses
                        {
                            if (Leveling) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (Expertise) ExpertiseSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                        }
                        else if (deathEvent.Died.Has<VBloodUnit>() && !hasVBloodConsumeSource)
                        {
                            if (Leveling) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (Expertise) ExpertiseSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                            if (Legacies) BloodSystem.UpdateLegacy(deathEvent.Killer, deathEvent.Died);
                        }

                        if (Familiars && !hasVBloodConsumeSource) FamiliarUnlockSystem.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
                    }
                    else
                    {
                        if (Professions && !hasVBloodConsumeSource) // if no movement, handle resource harvest
                        {
                            ProfessionSystem.UpdateProfessions(deathEvent.Killer, deathEvent.Died);
                        }
                    }
                }
                else if (deathEvent.Killer.Has<Follower>() && deathEvent.Killer.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // player familiar kills
                {
                    var followedPlayer = deathEvent.Killer.Read<Follower>().Followed._Value;
                    if (!hasVBloodConsumeSource)
                    {
                        if (Leveling) LevelingSystem.UpdateLeveling(followedPlayer, deathEvent.Died);
                        if (Familiars) FamiliarLevelingSystem.UpdateFamiliar(followedPlayer, deathEvent.Died);
                    }
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<PlayerCharacter>() && deathEvent.Died.Has<Movement>()) // player summon kills
                {
                    Entity killer = deathEvent.Killer.Read<EntityOwner>().Owner;
                    if (!hasVBloodConsumeSource)
                    {
                        if (Leveling) LevelingSystem.UpdateLeveling(killer, deathEvent.Died);
                        if (Expertise) ExpertiseSystem.UpdateExpertise(killer, deathEvent.Died);
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