using Bloodcraft.Services;
using Bloodcraft.Utilities;
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

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly PrefabGUID _pvpProtectedBuff = new(1111481396);

    [HarmonyPatch(typeof(CreateGameplayEventOnTickSystem_Spawn), nameof(CreateGameplayEventOnTickSystem_Spawn.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(CreateGameplayEventOnTickSystem_Spawn __instance)
    {
        if (!Core.IsReady) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<SchoolDebuffData>() || !entity.TryGetComponent(out Buff buff)) continue;
                else if (entity.TryGetComponent(out EntityOwner entityOwner) && entityOwner.Owner.IsPlayer())
                {
                    Entity buffTarget = buff.Target;

                    if (buffTarget.IsPlayer() && !buffTarget.IsDueling())
                    {
                        if (_gameMode.Equals(GameModeType.PvE)) DestroyUtility.Destroy(EntityManager, entity);
                        else if (_gameMode.Equals(GameModeType.PvP) && buffTarget.HasBuff(_pvpProtectedBuff)) DestroyUtility.Destroy(EntityManager, entity);
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
