using Cobalt.Systems.Experience;
using Cobalt.Systems.Expertise;
using Cobalt.Systems.Sanguimancy;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using ProfessionSystem = Cobalt.Systems.Professions.ProfessionSystem;

namespace Cobalt.Hooks;

[HarmonyPatch]
public class DeathEventListenerSystem_Patch
{
    [HarmonyPatch(typeof(DeathEventListenerSystem), "OnUpdate")]
    [HarmonyPostfix]
    public static void Postfix(DeathEventListenerSystem __instance)
    {
        NativeArray<DeathEvent> deathEvents = __instance._DeathEventQuery.ToComponentDataArray<DeathEvent>(Allocator.Temp);
        try
        {
            foreach (DeathEvent ev in deathEvents)
            {
                if (__instance.EntityManager.HasComponent<PlayerCharacter>(ev.Killer) && __instance.EntityManager.HasComponent<Movement>(ev.Died))
                {
                    if (Plugin.LevelingSystem.Value) LevelingSystem.UpdateExperience(ev.Killer, ev.Died);
                    if (Plugin.ExpertiseSystem.Value) ExpertiseSystem.UpdateWeaponExpertise(__instance.EntityManager, ev.Killer, ev.Died);
                    if (Plugin.BloodSystem.Value) BloodSystem.UpdateSanguimancy(ev.Killer, ev.Died);
                }
                else if (__instance.EntityManager.HasComponent<PlayerCharacter>(ev.Killer))
                {
                    //ev.Died.LogComponentTypes();
                    if (Plugin.ProfessionSystem.Value) ProfessionSystem.UpdateProfessions(ev.Killer, ev.Died);
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