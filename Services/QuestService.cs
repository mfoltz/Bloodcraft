using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.SystemUtilities.Quests.QuestSystem;
using static Bloodcraft.Utilities;
using static Bloodcraft.Core.DataStructures;

namespace Bloodcraft.Services;
internal class QuestService
{
    static EntityManager EntityManager => Core.EntityManager;
    static ConfigService ConfigService => Core.ConfigService;
    static PlayerService PlayerService => Core.PlayerService;

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

    static readonly ComponentType[] VBloodComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<VBloodUnit>()),
        ComponentType.ReadOnly(Il2CppType.Of<VBloodConsumeSource>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitRespawnTime>())
    ];

    static EntityQuery UnitQuery;
    static EntityQuery VBloodQuery; // using other one worked better, need to check this at some point but just using other query for now

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
        Core.StartCoroutine(QuestUpdateLoop());
    }
    static IEnumerator QuestUpdateLoop()
    {
        while (true)
        {
            if (ConfigService.EliteShardBearers && !ShardBearersReset) // would fit better elsewhere probably but can reuse the query so here it stays for now
            {
                IEnumerable<Entity> vBloods = GetEntitiesEnumerable(UnitQuery);
                foreach (Entity entity in vBloods)
                {
                    PrefabGUID vBloodPrefab = entity.Read<PrefabGUID>();
                    if (vBloodPrefab == manticore || vBloodPrefab == dracula || vBloodPrefab == monster || vBloodPrefab == solarus)
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                        Core.Log.LogInfo($"Destroyed {vBloodPrefab.LookupName()}");
                    }
                }
                ShardBearersReset = true;
            }

            if (PlayerService.UserCache.Keys.Count == 0)
            {
                yield return startDelay; // Wait 30 seconds if no players
                continue;
            }

            Dictionary<string, Entity> players = new(PlayerService.UserCache); // Copy the player cache to make sure updates to that don't interfere with loop
            foreach (string player in players.Keys)
            {
                User user = players[player].Read<User>();
                ulong steamId = user.PlatformId;
                if (!ConfigService.LevelingSystem)
                {
                    Entity character = user.LocalCharacter._Entity;
                    if (PlayerQuests.ContainsKey(steamId))
                    {
                        RefreshQuests(user, steamId, (int)character.Read<Equipment>().GetFullLevel());
                    }
                    else
                    {
                        InitializePlayerQuests(steamId, (int)character.Read<Equipment>().GetFullLevel());
                    }
                }
                else if (ConfigService.LevelingSystem && PlayerExperience.TryGetValue(steamId, out var xpData))
                {
                    if (PlayerQuests.ContainsKey(steamId))
                    {
                        RefreshQuests(user, steamId, xpData.Key);
                    }
                    else
                    {
                        InitializePlayerQuests(steamId, xpData.Key);
                    }
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

