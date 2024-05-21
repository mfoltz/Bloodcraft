using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
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
            foreach (DeathEvent ev in deathEvents)
            {
                if (__instance.EntityManager.HasComponent<PlayerCharacter>(ev.Killer) && __instance.EntityManager.HasComponent<Movement>(ev.Died))
                {
                    if (Plugin.LevelingSystem.Value && !ev.Died.Has<VBloodConsumeSource>()) LevelingSystem.UpdateLeveling(ev.Killer, ev.Died);
                    if (Plugin.ExpertiseSystem.Value && !ev.Died.Has<VBloodConsumeSource>()) ExpertiseSystem.UpdateExpertise(ev.Killer, ev.Died);
                }
                else if (__instance.EntityManager.HasComponent<PlayerCharacter>(ev.Killer))
                {
                    //ev.Died.LogComponentTypes();
                    if (Plugin.ProfessionSystem.Value && !ev.Died.Has<VBloodConsumeSource>()) ProfessionSystem.UpdateProfessions(ev.Killer, ev.Died);
                }
                else if (__instance.EntityManager.HasComponent<Follower>(ev.Killer) && ev.Killer.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    if (Plugin.LevelingSystem.Value && !ev.Died.Has<VBloodConsumeSource>()) LevelingSystem.UpdateLeveling(ev.Killer.Read<Follower>().Followed._Value, ev.Died);
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