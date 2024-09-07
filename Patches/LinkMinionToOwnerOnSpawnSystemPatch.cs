using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class LinkMinionToOwnerOnSpawnSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly PrefabGUID InkCrawlerDeathBuff = new(1273155981);

    public static readonly Dictionary<Entity, HashSet<Entity>> FamiliarMinions = [];

    [HarmonyPatch(typeof(LinkMinionToOwnerOnSpawnSystem), nameof(LinkMinionToOwnerOnSpawnSystem.OnUpdate))] // for handling familiar minion summons as most will hang around forever if not killed or explicitly dealt with
    [HarmonyPrefix]
    static void OnUpdatePrefix(LinkMinionToOwnerOnSpawnSystem __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Minion [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;
                if (!ConfigService.FamiliarSystem) return;

                //Core.Log.LogInfo($"LinkMinionToOwnerOnSpawnSystem: {entity.Read<PrefabGUID>().LookupName()}");

                if (entity.GetOwner().TryGetFollowedPlayer(out Entity player)) // if following player most likely a familiar minion summon
                {
                    Entity familiar = FindPlayerFamiliar(player);
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

                        if (entity.Read<PrefabGUID>().LookupName().ToLower().Contains("vblood")) continue; // this only applies for Voltatia as both the main one and the minion one pass through this system for some reason and want to leave the clone alone here

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
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
