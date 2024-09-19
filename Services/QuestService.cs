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

    static readonly ComponentType[] UnitComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<Movement>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
        ComponentType.ReadOnly(Il2CppType.Of<Team>()),
        ComponentType.ReadOnly(Il2CppType.Of<Translation>())
    ];

    static readonly ComponentType[] CraftingComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<InventoryItem>()),
        ComponentType.ReadOnly(Il2CppType.Of<ItemData>())
    ];

    static readonly ComponentType[] ResourceComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<YieldResourcesOnDamageTaken>()),
        ComponentType.ReadOnly(Il2CppType.Of<DropTableBuffer>())
    ];

    static readonly ComponentType[] NoneComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<SpawnTag>()),
        ComponentType.ReadOnly(Il2CppType.Of<Minion>())
    ];

    static EntityQuery UnitQuery;
    static EntityQuery ItemQuery;
    static EntityQuery ResourceQuery;

    public static Dictionary<PrefabGUID, HashSet<Entity>> TargetCache = [];
    public static DateTime LastUpdate;

    static readonly PrefabGUID manticore = new(-393555055);
    static readonly PrefabGUID dracula = new(-327335305);
    static readonly PrefabGUID monster = new(1233988687);
    static readonly PrefabGUID solarus = new(-740796338);

    static bool shardBearersReset = false;
    static bool craftAndGather = false;
    static bool targetsLogged = false;
    public QuestService()
    {
        UnitQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = UnitComponents,
            None = NoneComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });

        ItemQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = CraftingComponents,
            Options = EntityQueryOptions.IncludeAll
        });

        ResourceQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = ResourceComponents,
            Options = EntityQueryOptions.IncludeAll
        });

        ConfigUtilities.QuestRewards();
        Core.StartCoroutine(QuestUpdateLoop());
    }
    static IEnumerator QuestUpdateLoop()
    {
        while (true)
        {
            if (ConfigService.EliteShardBearers && !shardBearersReset) // makes sure server doesn't un-elite shard bearers on restarts by forcing them to spawn again
            {
                //Core.Log.LogInfo("Resetting shard bearers...");

                IEnumerable<Entity> vBloods = EntityUtilities.GetEntitiesEnumerable(UnitQuery);
                foreach (Entity entity in vBloods)
                {
                    PrefabGUID vBloodPrefab = entity.Read<PrefabGUID>();
                    if (vBloodPrefab == manticore || vBloodPrefab == dracula || vBloodPrefab == monster || vBloodPrefab == solarus)
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                }

                shardBearersReset = true;
            }

            if (!craftAndGather)
            {
                //Core.Log.LogInfo("Retrieving crafting and gathering prefabs...");

                IEnumerable<Entity> entities = EntityUtilities.GetEntitiesEnumerable(ItemQuery, (int)TargetType.Craft);
                foreach (Entity entity in entities)
                {
                    if (entity.TryGetComponent(out PrefabGUID prefab))
                    {
                        if (entity.Has<Equippable>() && entity.TryGetComponent(out Salvageable salveageable))
                        {
                            if (salveageable.RecipeGUID.HasValue()) CraftPrefabs.Add(prefab);
                        }
                        else if (entity.Has<ConsumableCondition>()) CraftPrefabs.Add(prefab);
                    }
                }

                entities = EntityUtilities.GetEntitiesEnumerable(ResourceQuery);
                foreach (Entity entity in entities)
                {
                    if (entity.TryGetComponent(out PrefabGUID prefab))
                    {
                        ResourcePrefabs.Add(prefab);
                    }
                }

                craftAndGather = true;
                foreach(PrefabGUID prefabGUID in CraftPrefabs)
                {
                    //Core.Log.LogInfo(prefabGUID.LookupName());
                }

                ItemQuery.Dispose();
                ResourceQuery.Dispose();
            }

            //Core.Log.LogInfo("Retrieving targets...");
            TargetCache = EntityUtilities.GetEntitiesEnumerable(UnitQuery, (int)TargetType.Kill)
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

            //Core.Log.LogInfo("Removing targets...");
            if (TargetCache.ContainsKey(manticore)) TargetCache.Remove(manticore);
            if (TargetCache.ContainsKey(dracula)) TargetCache.Remove(dracula);
            if (TargetCache.ContainsKey(monster)) TargetCache.Remove(monster);
            if (TargetCache.ContainsKey(solarus)) TargetCache.Remove(solarus);

            //Core.Log.LogInfo("Refreshing quests...");
            Dictionary<string, PlayerInfo> players = new(PlayerCache); // Copy the player cache to make sure updates to that don't interfere with loop
            foreach (PlayerInfo playerInfo in players.Values)
            {
                User user = playerInfo.User;
                ulong steamId = playerInfo.User.PlatformId;

                if (!ConfigService.LevelingSystem)
                {
                    Entity character = playerInfo.CharEntity.Has<Equipment>() ? playerInfo.CharEntity : Entity.Null;
                    if (character == Entity.Null) continue; // just incase players dont have equipment or something until getting out of the first coffin, best guess at a bug without leveling atm
                    // should just have to check when first giving a player quests since if this is called otherwise player should definitely have this component at that point

                    RefreshQuests(user, steamId, (int)playerInfo.CharEntity.Read<Equipment>().GetFullLevel());
                }
                else if (ConfigService.LevelingSystem && steamId.TryGetPlayerExperience(out var xpData))
                {
                    RefreshQuests(user, steamId, xpData.Key);
                }
            }

            if (!targetsLogged)
            {
                //Core.Log.LogInfo("Logging targets...");
                //Core.Log.LogInfo(TargetCache.Count);
                foreach (var kvp in TargetCache)
                {
                    //Core.Log.LogInfo(kvp.Key.LookupName());
                }
                targetsLogged = true;
            }   

            LastUpdate = DateTime.UtcNow;
            yield return updateDelay; // Wait 60 seconds before processing players/units again
        }
    }
}

