using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class SpawnTravelBuffSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    static readonly PrefabGUID _batLandingTravel = new(-371745443);
    static readonly PrefabGUID _draculaFlyToCenter = new(-1961466676);
    static readonly PrefabGUID _pvpProtectedBuff = new(1111481396);

    [HarmonyPatch(typeof(Spawn_TravelBuffSystem), nameof(Spawn_TravelBuffSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(Spawn_TravelBuffSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;
                else if (prefabGUID.Equals(_batLandingTravel) && entityOwner.Owner.TryGetPlayer(out Entity player))
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    if (Familiars.AutoCallMap.TryRemove(player, out Entity familiar) && familiar.Exists())
                    {
                        Familiars.CallFamiliar(player, familiar, user, steamId);
                    }
                }

                /*
                else if (ConfigService.FamiliarSystem && entity.TryGetComponent(out Buff buff) && buff.Target.IsPlayer())
                {
                    Entity buffTarget = buff.Target;
                    Entity owner = entityOwner.Owner;

                    if (GameMode.Equals(GameModeType.PvE))
                    {
                        if (owner.IsPlayer() && !owner.Equals(buffTarget))
                        {
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (ConfigService.FamiliarSystem)
                        {
                            if (owner.IsFollowingPlayer() || owner.GetOwner().IsFollowingPlayer())
                            {
                                if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                            }
                        }
                    }
                    else if (GameMode.Equals(GameModeType.PvP) && buffTarget.HasBuff(PvPProtectedBuff))
                    {
                        if (owner.IsPlayer() && !owner.Equals(buffTarget))
                        {
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (ConfigService.FamiliarSystem)
                        {
                            if (owner.IsFollowingPlayer() || owner.GetOwner().IsFollowingPlayer())
                            {
                                if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                            }
                        }
                    }
                }
                */
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
