using System;
using Bloodcraft.Patches.BuffSpawnServerPatches;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BuffSystemSpawnPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static ModificationsRegistry ModificationsRegistry => SystemService.ModificationSystem.Registry;


    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Core._initialized) return;

        EntityQuery query = QueryService.BuffSpawnServerQuery;

        using NativeAccessor<Entity> entities = query.ToEntityArrayAccessor();
        using NativeAccessor<PrefabGUID> prefabGuids = query.ToComponentDataArrayAccessor<PrefabGUID>();
        using NativeAccessor<Buff> buffs = query.ToComponentDataArrayAccessor<Buff>();

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; ++i)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;
                PrefabGUID buffPrefabGuid = prefabGuids[i];

                if (!buffTarget.Exists())
                    continue;

                bool isPlayerTarget = playerCharacterLookup.HasComponent(buffTarget);
                ulong steamId = isPlayerTarget ? buffTarget.GetSteamId() : 0;
                string prefabName = buffPrefabGuid.GetPrefabName();

                var ctx = new BuffSpawnContext
                {
                    BuffEntity = buffEntity,
                    Target = buffTarget,
                    PrefabGuid = buffPrefabGuid,
                    PrefabName = prefabName,
                    IsPlayer = isPlayerTarget,
                    SteamId = steamId,
                    BlockFeedLookup = blockFeedBuffLookup
                };

                foreach (IBuffSpawnHandler handler in BuffSpawnHandlerRegistry.Handlers)
                {
                    if (handler.CanHandle(ctx))
                    {
                        handler.Handle(ctx);
                        break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[BuffSystem_Spawn_Server] - Exception: {e}");
        }
    }
}
