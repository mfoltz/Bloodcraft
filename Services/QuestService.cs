using Bloodcraft.Utilities;
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

namespace Bloodcraft.Services;
internal class QuestService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly WaitForSeconds updateDelay = new(60);
    static readonly WaitForSeconds startDelay = new(30);

    static readonly ComponentType[] UnitComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Movement>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
        //ComponentType.ReadOnly(Il2CppType.Of<TilePosition>()),
        ComponentType.ReadOnly(Il2CppType.Of<Team>()),
        //ComponentType.ReadOnly(Il2CppType.Of<BuffBuffer>()),
        ComponentType.ReadOnly(Il2CppType.Of<Translation>())
    ];

    static readonly ComponentType[] SpawnTag =
    [
        ComponentType.ReadOnly(Il2CppType.Of<SpawnTag>())
    ];

    static EntityQuery UnitQuery;

    public static Dictionary<PrefabGUID, HashSet<Entity>> TargetCache = [];
    public static DateTime LastUpdate;

    static readonly PrefabGUID manticore = new(-393555055);
    static readonly PrefabGUID dracula = new(-327335305);
    static readonly PrefabGUID monster = new(1233988687);
    static readonly PrefabGUID solarus = new(-740796338);

    static bool ShardBearersReset = false;
    //static bool targetsLogged = false;
    public QuestService()
    {
        UnitQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = UnitComponents,
            None = SpawnTag,
            Options = EntityQueryOptions.IncludeDisabled
        });

        ConfigUtilities.QuestRewards();
        Core.StartCoroutine(QuestUpdateLoop());
    }
    static IEnumerator QuestUpdateLoop()
    {
        while (true)
        {
            if (ConfigService.EliteShardBearers && !ShardBearersReset) // makes sure server doesn't un-elite shard bearers on restarts by forcing them to spawn again
            {
                IEnumerable<Entity> vBloods = EntityUtilities.GetEntitiesEnumerable(UnitQuery);
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

            TargetCache = EntityUtilities.GetEntitiesEnumerable(UnitQuery, true)
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

            if (TargetCache.ContainsKey(manticore)) TargetCache.Remove(manticore);
            if (TargetCache.ContainsKey(dracula)) TargetCache.Remove(dracula);
            if (TargetCache.ContainsKey(monster)) TargetCache.Remove(monster);
            if (TargetCache.ContainsKey(solarus)) TargetCache.Remove(solarus);

            /*
            if (!targetsLogged)
            {
                Core.Log.LogInfo(TargetCache.Count);
                foreach (var kvp in TargetCache)
                {
                    //Core.Log.LogInfo(kvp.Key.LookupName());
                }
                targetsLogged = true;
            }
            */

            LastUpdate = DateTime.UtcNow;
            yield return updateDelay; // Wait 60 seconds before processing players/units again
        }
    }
}

