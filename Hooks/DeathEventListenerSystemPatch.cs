using Cobalt.Systems;
using HarmonyLib;
using ProjectM;
using Unity.Collections;

namespace OpenRPG.Hooks;

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
                    ExperienceSystem.EXPMonitor(ev.Killer, ev.Died);
                    ArmsMasterySystem.UpdateMastery(ev.Killer, ev.Died);
                }
            }
        }
        finally
        {
            deathEvents.Dispose();
        }
    }
}