using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BehaviourStateChangedSystemPatch // stops familiars from trying to return to where they spawned at when coming out of combat
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly PrefabGUID TailorFleeBuff = new(-530519474);
    static readonly PrefabGUID WandererFleeBuff = new(573463911);

    [HarmonyPatch(typeof(CreateGameplayEventOnBehaviourStateChangedSystem), nameof(CreateGameplayEventOnBehaviourStateChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnBehaviourStateChangedSystem __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_221632411_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out BehaviourTreeStateChangedEvent behaviourTreeStateChangedEvent)) continue;
                else if (behaviourTreeStateChangedEvent.Entity.TryGetFollowedPlayer(out Entity player))
                {
                    BehaviourTreeState behaviourTreeState = behaviourTreeStateChangedEvent.Entity.Read<BehaviourTreeState>();

                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {
                        Entity familiar = behaviourTreeStateChangedEvent.Entity;

                        behaviourTreeState.Value = GenericEnemyState.Follow;
                        behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;

                        entity.Write(behaviourTreeStateChangedEvent);
                        behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);

                        FamiliarUtilities.HandleFamiliarMinions(familiar); // destroy any minions of familiar if familiar tries to return
                    }
                    else if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Idle))
                    {
                        Entity familiar = behaviourTreeStateChangedEvent.Entity;

                        FamiliarUtilities.ReturnFamiliar(player, familiar);
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
