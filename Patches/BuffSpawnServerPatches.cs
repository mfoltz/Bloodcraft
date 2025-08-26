using System;
using Bloodcraft.Patches.BuffSpawnServerPatches;
using Bloodcraft.Resources;
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

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _eliteShardBearers = ConfigService.EliteShardBearers;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _trueImmortal = ConfigService.TrueImmortal;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _potionStacking = ConfigService.PotionStacking;
    static readonly bool _professions = ConfigService.ProfessionSystem;

    static readonly EntityQuery _query = QueryService.BuffSpawnServerQuery;

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Core._initialized) return;

        using NativeAccessor<Entity> entities = _query.ToEntityArrayAccessor();
        using NativeAccessor<PrefabGUID> prefabGuids = _query.ToComponentDataArrayAccessor<PrefabGUID>();
        using NativeAccessor<Buff> buffs = _query.ToComponentDataArrayAccessor<Buff>();

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
                    GameMode = _gameMode,
                    EliteShardBearers = _eliteShardBearers,
                    Legacies = _legacies,
                    Expertise = _expertise,
                    TrueImmortal = _trueImmortal,
                    Familiars = _familiars,
                    FamiliarPvP = _familiarPvP,
                    PotionStacking = _potionStacking,
                    Professions = _professions,
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
