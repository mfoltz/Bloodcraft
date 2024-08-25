using Epic.OnlineServices.Achievements;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Quests.QuestSystem;
using static Bloodcraft.Utilities;

namespace Bloodcraft.Services;
internal class QuestService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds updateDelay = new(60);
    static readonly WaitForSeconds startDelay = new(30);

    static readonly ComponentType[] UnitComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<AiMoveSpeeds>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
        ComponentType.ReadOnly(Il2CppType.Of<TilePosition>()),
        ComponentType.ReadOnly(Il2CppType.Of<Team>()),
        ComponentType.ReadOnly(Il2CppType.Of<BuffBuffer>()),
        ComponentType.ReadOnly(Il2CppType.Of<Translation>())
    ];

    static EntityQuery UnitQuery;

    public static Dictionary<PrefabGUID, HashSet<Entity>> TargetCache = [];
    public static DateTime LastUpdate;

    static readonly PrefabGUID enchantedCross = new(-1449314709);
    static readonly PrefabGUID divineAngel = new(-1737346940);
    static readonly PrefabGUID fallenAngel = new(-76116724);
    static readonly PrefabGUID manticore = new(-393555055);
    static readonly PrefabGUID dracula = new(-327335305);
    static readonly PrefabGUID monster = new(1233988687);
    static readonly PrefabGUID solarus = new(-740796338);

    static bool ShardBearersReset = false;
    public QuestService()
    {
        UnitQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = UnitComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });
        QuestRewards();
        Core.StartCoroutine(QuestUpdateLoop());
    }
    static IEnumerator QuestUpdateLoop()
    {
        while (true)
        {
            if (ConfigService.EliteShardBearers && !ShardBearersReset) // makes sure server doesn't un-elite shard bearers on restarts by forcing them to spawn again
            {
                IEnumerable<Entity> vBloods = GetEntitiesEnumerable(UnitQuery);
                foreach (Entity entity in vBloods)
                {
                    PrefabGUID vBloodPrefab = entity.Read<PrefabGUID>();
                    if (vBloodPrefab == manticore || vBloodPrefab == dracula || vBloodPrefab == monster || vBloodPrefab == solarus)
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                }
                ShardBearersReset = true;
            }

            if (PlayerCache.Keys.Count == 0)
            {
                yield return startDelay; // Wait 30 seconds if no players
                continue;
            }

            Dictionary<string, PlayerInfo> players = new(PlayerCache); // Copy the player cache to make sure updates to that don't interfere with loop
            foreach (PlayerInfo playerInfo in players.Values)
            {
                User user = playerInfo.User;
                ulong steamId = playerInfo.User.PlatformId;

                if (!ConfigService.LevelingSystem)
                {
                    RefreshQuests(user, steamId, (int)playerInfo.CharEntity.Read<Equipment>().GetFullLevel());
                }
                else if (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var xpData))
                {
                    RefreshQuests(user, steamId, xpData.Key);
                }
            }

            TargetCache = GetEntitiesEnumerable(UnitQuery, true)
                .GroupBy(entity => entity.Read<PrefabGUID>())
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        if (TargetCache.TryGetValue(group.Key, out var existingSet))
                        {
                            existingSet.Clear();
                            existingSet.UnionWith(group);
                            return existingSet;
                        }
                        else
                        {
                            return new HashSet<Entity>(group);
                        }
                    }
                );

            if (TargetCache.ContainsKey(enchantedCross)) TargetCache.Remove(enchantedCross);
            if (TargetCache.ContainsKey(divineAngel)) TargetCache.Remove(divineAngel);
            if (TargetCache.ContainsKey(fallenAngel)) TargetCache.Remove(fallenAngel);

            LastUpdate = DateTime.UtcNow;
            yield return updateDelay; // Wait 60 seconds before processing players/units again
        }
    }
}

