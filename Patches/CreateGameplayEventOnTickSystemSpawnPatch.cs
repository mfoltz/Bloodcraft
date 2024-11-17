using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class CreateGameplayEventOnTickSpawnSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly PrefabGUID PvPProtectedBuff = new(1111481396);

    [HarmonyPatch(typeof(CreateGameplayEventOnTickSystem_Spawn), nameof(CreateGameplayEventOnTickSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnTickSystem_Spawn __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<SchoolDebuffData>() || !entity.TryGetComponent(out Buff buff)) continue;
                else if (entity.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.IsPlayer()) // prevent chaos ignite etc from being applied to players with pvp prot or if pve 
                {
                    Entity buffTarget = buff.Target;

                    if (buffTarget.IsPlayer())
                    {
                        if (GameMode.Equals(GameModeType.PvE)) DestroyUtility.Destroy(EntityManager, entity);
                        else if (GameMode.Equals(GameModeType.PvP) && buffTarget.HasBuff(PvPProtectedBuff)) DestroyUtility.Destroy(EntityManager, entity);
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
