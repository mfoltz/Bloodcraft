using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BehaviourStateChangedSystemPatch // stops familiars from trying to return to where they spawned when leaving combat
{
    static EntityManager EntityManager => Core.EntityManager;

    [HarmonyPatch(typeof(CreateGameplayEventOnBehaviourStateChangedSystem), nameof(CreateGameplayEventOnBehaviourStateChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnBehaviourStateChangedSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_221632411_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out BehaviourTreeStateChangedEvent behaviourTreeStateChangedEvent)) continue;
                else if (behaviourTreeStateChangedEvent.Entity.TryGetFollowedPlayer(out Entity player))
                {
                    Entity familiar = behaviourTreeStateChangedEvent.Entity;
                    BehaviourTreeState behaviourTreeState = familiar.Read<BehaviourTreeState>();

                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {
                        behaviourTreeState.Value = GenericEnemyState.Follow;
                        behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;

                        entity.Write(behaviourTreeStateChangedEvent);
                        behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);



                        Familiars.HandleFamiliarMinions(familiar);
                    }
                    else if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Idle))
                    {
                        Familiars.TryReturnFamiliar(player, familiar);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
