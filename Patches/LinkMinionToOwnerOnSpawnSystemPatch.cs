using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using ProjectM.Shared;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class LinkMinionToOwnerOnSpawnSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool Familiars = ConfigService.FamiliarSystem;

    const float MINION_LIFETIME = 30f;

    public static readonly Dictionary<Entity, HashSet<Entity>> FamiliarMinions = [];

    [HarmonyPatch(typeof(LinkMinionToOwnerOnSpawnSystem), nameof(LinkMinionToOwnerOnSpawnSystem.OnUpdate))] // familiar minion summons will hang around forever if not killed or otherwise explicitly dealt with
    [HarmonyPrefix]
    static void OnUpdatePrefix(LinkMinionToOwnerOnSpawnSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!Familiars) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Minion [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetFollowedPlayer(out Entity player))
                {
                    Entity familiar = Utilities.Familiars.FindPlayerFamiliar(player);

                    if (familiar.Exists())
                    {
                        HandleFamiliarMinionSpawn(familiar, entity);

                        /*
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
                        */

                        entity.Write(new EntityOwner { Owner = player });
                    }
                }
                else if (entityOwner.Owner.Has<BlockFeedBuff>()) // for familiar battles
                {
                    HandleFamiliarMinionSpawn(entityOwner.Owner, entity);
                }
                else if (entityOwner.Owner.TryGetComponent(out entityOwner) && entityOwner.Owner.IsPlayer())
                {
                    DestroyUtility.Destroy(EntityManager, entity); // kinda forgot what this is for but scared to touch it >_>
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void HandleFamiliarMinionSpawn(Entity familiar, Entity minion)
    {
        if (!FamiliarMinions.ContainsKey(familiar))
        {
            FamiliarMinions.TryAdd(familiar, [minion]);
        }
        else
        {
            FamiliarMinions[familiar].Add(minion);
        }

        Utilities.Familiars.NothingLivesForever(minion, MINION_LIFETIME);
    }
}
