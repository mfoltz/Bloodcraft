using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class LinkMinionToOwnerOnSpawnSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly PrefabGUID InkCrawlerDeathBuff = new(1273155981);

    public static readonly Dictionary<Entity, HashSet<Entity>> FamiliarMinions = [];

    [HarmonyPatch(typeof(LinkMinionToOwnerOnSpawnSystem), nameof(LinkMinionToOwnerOnSpawnSystem.OnUpdate))] // for handling familiar minion summons as most will hang around forever if not killed or explicitly dealt with
    [HarmonyPrefix]
    static void OnUpdatePrefix(LinkMinionToOwnerOnSpawnSystem __instance)
    {
        if (!Core.hasInitialized) return;
        if (!ConfigService.FamiliarSystem) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Minion [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<EntityOwner>()) continue;
                else if (entity.GetOwner().TryGetFollowedPlayer(out Entity player))
                {
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                    if (familiar.Exists())
                    {
                        if (!FamiliarMinions.ContainsKey(familiar))
                        {
                            FamiliarMinions.TryAdd(familiar, [entity]);
                        }
                        else
                        {
                            FamiliarMinions[familiar].Add(entity);
                        }

                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = InkCrawlerDeathBuff
                        };

                        FromCharacter fromCharacter = new()
                        {
                            Character = entity,
                            User = familiar
                        };

                        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        if (ServerGameManager.TryGetBuff(entity, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
                        {
                            if (buff.Has<LifeTime>())
                            {
                                buff.Write(new LifeTime { Duration = 30, EndAction = LifeTimeEndAction.Destroy }); // mark for destruction to make sure familiar minions don't linger if other handling fails
                            }
                        }
                    }
                }
                else if (entity.GetOwner().GetOwner().IsPlayer())
                {
                    DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.None);
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
