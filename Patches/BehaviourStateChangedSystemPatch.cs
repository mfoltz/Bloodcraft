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
internal static class BehaviourStateChangedSystemPatch // stops familiars from trying to return to where they spawned at when coming out of combat
{
    [HarmonyPatch(typeof(CreateGameplayEventOnBehaviourStateChangedSystem), nameof(CreateGameplayEventOnBehaviourStateChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnBehaviourStateChangedSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance.__query_221632411_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                BehaviourTreeStateChangedEvent behaviourTreeStateChangedEvent = entity.Read<BehaviourTreeStateChangedEvent>();
                if (behaviourTreeStateChangedEvent.Entity.Has<Follower>() && behaviourTreeStateChangedEvent.Entity.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    BehaviourTreeState behaviourTreeState = behaviourTreeStateChangedEvent.Entity.Read<BehaviourTreeState>();
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {
                        Entity familiar = behaviourTreeStateChangedEvent.Entity;
                        behaviourTreeState.Value = GenericEnemyState.Follow;
                        behaviourTreeStateChangedEvent.NewState = GenericEnemyState.Follow;

                        entity.Write(behaviourTreeStateChangedEvent);
                        behaviourTreeStateChangedEvent.Entity.Write(behaviourTreeState);
                        FamiliarUtilities.
                                                HandleFamiliarMinions(familiar); // destroy any minions of familiar if familiar tries to return
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
