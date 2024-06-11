using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared.WarEvents;
using Stunlock.Core;
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
                bool isStatChangeInvalid = deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11);
                bool hasVBloodConsumeSource = deathEvent.Died.Has<VBloodConsumeSource>();
                bool gateBoss = deathEvent.Died.Read<PrefabGUID>().GetPrefabName().ToLower().Contains("gateboss");


                if (Familiars && deathEvent.Died.Has<Follower>() && deathEvent.Died.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // update player familiar actives data
                {
                    ulong steamId = deathEvent.Died.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
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
                        if (!isStatChangeInvalid && (!hasVBloodConsumeSource || gateBoss)) // only process non-feed related deaths here
                        {
                            if (Leveling) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                            if (Expertise) ExpertiseSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                            if (Familiars) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                        }
                        if (Familiars) FamiliarUnlockSystem.HandleUnitUnlock(deathEvent.Killer, deathEvent.Died); // familiar unlocks
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
                    if (!hasVBloodConsumeSource || gateBoss)
                    {
                        if (Leveling) LevelingSystem.UpdateLeveling(followedPlayer, deathEvent.Died);
                        if (Familiars) FamiliarLevelingSystem.UpdateFamiliar(followedPlayer, deathEvent.Died);
                    }
                }
                else if (deathEvent.Killer.Has<EntityOwner>() && deathEvent.Killer.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) // player summon kills
                {
                    Entity killer = deathEvent.Killer.Read<EntityOwner>().Owner;
                    if (!hasVBloodConsumeSource || gateBoss)
                    {
                        if (Leveling) LevelingSystem.UpdateLeveling(killer, deathEvent.Died);
                        if (Expertise) ExpertiseSystem.UpdateExpertise(killer, deathEvent.Died);
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