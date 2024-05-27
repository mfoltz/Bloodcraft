using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
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
        try
        {
            foreach (DeathEvent deathEvent in deathEvents)
            {
                if (__instance.EntityManager.HasComponent<PlayerCharacter>(deathEvent.Killer) && __instance.EntityManager.HasComponent<Movement>(deathEvent.Died))
                {
                    if (Plugin.LevelingSystem.Value && !deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11) && !deathEvent.Died.Has<VBloodConsumeSource>()) LevelingSystem.UpdateLeveling(deathEvent.Killer, deathEvent.Died);
                    if (Plugin.ExpertiseSystem.Value && !deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11) && !deathEvent.Died.Has<VBloodConsumeSource>()) ExpertiseSystem.UpdateExpertise(deathEvent.Killer, deathEvent.Died);
                    //if (Plugin.FamiliarSystem.Value && !deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11) && !deathEvent.Died.Has<VBloodConsumeSource>()) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer, deathEvent.Died);
                }
                else if (__instance.EntityManager.HasComponent<PlayerCharacter>(deathEvent.Killer))
                {
                    if (Plugin.ProfessionSystem.Value && !deathEvent.Died.Has<VBloodConsumeSource>()) ProfessionSystem.UpdateProfessions(deathEvent.Killer, deathEvent.Died);
                }
                else if (__instance.EntityManager.HasComponent<Follower>(deathEvent.Killer) && deathEvent.Killer.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    if (Plugin.LevelingSystem.Value && !deathEvent.Died.Has<VBloodConsumeSource>()) LevelingSystem.UpdateLeveling(deathEvent.Killer.Read<Follower>().Followed._Value, deathEvent.Died);
                    //if (Plugin.FamiliarSystem.Value && !deathEvent.Died.Has<VBloodConsumeSource>()) FamiliarLevelingSystem.UpdateFamiliar(deathEvent.Killer.Read<Follower>().Followed._Value, deathEvent.Died);
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