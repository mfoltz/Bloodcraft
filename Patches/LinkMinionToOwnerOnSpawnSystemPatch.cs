using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class LinkMinionToOwnerOnSpawnSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _familiars = ConfigService.FamiliarSystem;

    const float MINION_LIFETIME = 30f;

    public static readonly ConcurrentDictionary<Entity, HashSet<Entity>> FamiliarMinions = [];

    [HarmonyPatch(typeof(LinkMinionToOwnerOnSpawnSystem), nameof(LinkMinionToOwnerOnSpawnSystem.OnUpdate))] // familiar minion summons will hang around forever if not killed or otherwise explicitly dealt with
    [HarmonyPrefix]
    static void OnUpdatePrefix(LinkMinionToOwnerOnSpawnSystem __instance)
    {
        if (!Core.IsReady) return;
        else if (!_familiars) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp); // All Components: ProjectM.EntityOwner [ReadOnly], ProjectM.Minion [ReadOnly], Unity.Entities.SpawnTag [ReadOnly]

        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists()) continue;
                else if (entityOwner.Owner.TryGetFollowedPlayer(out Entity player))
                {
                    Entity familiar = Familiars.GetActiveFamiliar(player);

                    if (familiar.Exists())
                    {
                        HandleFamiliarMinionSpawn(familiar, entity);

                        entity.Write(new EntityOwner { Owner = player });
                    }
                }
                else if (entityOwner.Owner.Has<BlockFeedBuff>()) // for familiar battles
                {
                    HandleFamiliarMinionSpawn(entityOwner.Owner, entity);
                }
                else if (entityOwner.Owner.IsDisabled())
                {
                    entity.Destroy(); // kinda forgot what this is for but scared to touch it >_>
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

        Familiars.NothingLivesForever(minion, MINION_LIFETIME);
    }
}
