using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using ProfessionSystem = Bloodcraft.Systems.Professions.ProfessionSystem;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class DeathEventListenerSystemPatch
{
    [HarmonyPatch(typeof(DeathEventListenerSystem), nameof(DeathEventListenerSystem.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(DeathEventListenerSystem __instance)
    {
        NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);

        bool Leveling = Plugin.LevelingSystem.Value;
        bool Expertise = Plugin.ExpertiseSystem.Value;
        bool Familiars = Plugin.FamiliarSystem.Value;
        bool Professions = Plugin.ProfessionSystem.Value;

        try
        {
            foreach (DeathEvent deathEvent in deathEvents)
            {
                /*
                if (deathEvent.Killer.Has<PlayerCharacter>() && deathEvent.Died.Has<Movement>())
                {
                    if (Plugin.LevelingSystem.Value && !deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11) && !deathEvent.Died.Has<VBloodConsumeSource>()) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                    if (Plugin.ExpertiseSystem.Value && !deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11) && !deathEvent.Died.Has<VBloodConsumeSource>()) ExpertiseSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                    if (Plugin.FamiliarSystem.Value && !deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11) && !deathEvent.Died.Has<VBloodConsumeSource>()) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                    if (Plugin.FamiliarSystem.Value) FamiliarUnlockSystem.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died);
                }
                else if (deathEvent.Killer.Has<PlayerCharacter>())
                {
                    if (Plugin.ProfessionSystem.Value && !deathEvent.Died.Has<VBloodConsumeSource>()) ProfessionSystem.UpdateProfessions(deathEvent.Killer, deathEvent.Died);
                }
                else if (deathEvent.Killer.Has<Follower>() && deathEvent.Killer.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // player familiar kills
                {
                    if (Plugin.LevelingSystem.Value && !deathEvent.Died.Has<VBloodConsumeSource>()) LevelingSystem.UpdateLeveling(deathEvent.Killer.Read<Follower>().Followed._Value, deathEvent.Died);
                    if (Plugin.FamiliarSystem.Value && !deathEvent.Died.Has<VBloodConsumeSource>()) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer.Read<Follower>().Followed._Value, deathEvent.Died);
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) // player summon kills
                {
                    if (Plugin.LevelingSystem.Value && !deathEvent.Died.Has<VBloodConsumeSource>()) LevelingSystem.UpdateLeveling(deathEvent.Killer.Read<Follower>().Followed._Value, deathEvent.Died);
                }
                */
                bool isStatChangeInvalid = deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11);
                bool hasVBloodConsumeSource = deathEvent.Died.Has<VBloodConsumeSource>();

                if (deathEvent.Killer.Has<PlayerCharacter>())
                {
                    if (deathEvent.Died.Has<Movement>())
                    {
                        if (!isStatChangeInvalid && !hasVBloodConsumeSource) // only process non-feed related deaths here
                        {
                            if (Leveling) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (Expertise) ExpertiseSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
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
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) // player summon kills
                {
                    Entity killer = deathEvent.Killer.Read<EntityOwner>().Owner;
                    if (Leveling && !hasVBloodConsumeSource)
                    {
                        LevelingSystem.UpdateLeveling(killer, deathEvent.Died);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogError($"Exited DeathEventListenerSystem hook early: {e}");
        }
        finally
        {
            deathEvents.Dispose();
        }
    }
}