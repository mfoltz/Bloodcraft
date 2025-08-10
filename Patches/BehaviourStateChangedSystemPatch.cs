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
internal static class BehaviourStateChangedSystemPatch
{

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    [HarmonyPatch(typeof(CreateGameplayEventOnBehaviourStateChangedSystem), nameof(CreateGameplayEventOnBehaviourStateChangedSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnBehaviourStateChangedSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.__query_221632411_0.ToEntityArray(Allocator.Temp);
        NativeArray<BehaviourTreeStateChangedEvent> behaviourTreeStateChangedEvents = __instance.__query_221632411_0.ToComponentDataArray<BehaviourTreeStateChangedEvent>(Allocator.Temp);

        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);

        try
        {
            for (int i = 0; i < behaviourTreeStateChangedEvents.Length; i++)
            {
                Entity source = entities[i];
                BehaviourTreeStateChangedEvent behaviourTreeStateChangedEvent = behaviourTreeStateChangedEvents[i];
                Entity target = behaviourTreeStateChangedEvent.Entity;

                if (!blockFeedBuffLookup.HasComponent(target)) continue;
                else if (target.TryGetFollowedPlayer(out Entity playerCharacter))
                {
                    if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Return))
                    {
                        target.With((ref BehaviourTreeState behaviourTreeState) => behaviourTreeState.Value = GenericEnemyState.Follow);

                        source.With((ref BehaviourTreeStateChangedEvent behaviourTreeState) => behaviourTreeState.NewState = GenericEnemyState.Follow);

                        Familiars.HandleFamiliarMinions(target);
                    }
                    else if (behaviourTreeStateChangedEvent.NewState.Equals(GenericEnemyState.Idle))
                    {
                        Familiars.TryReturnFamiliar(playerCharacter, target);
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
