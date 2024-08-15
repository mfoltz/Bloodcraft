using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using static Bloodcraft.SystemUtilities.Quests.QuestUtilities;

namespace Bloodcraft.Services;
internal class QuestService
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly bool EliteShardBearers = Plugin.EliteShardBearers.Value;

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
        VBloodQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = VBloodComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });
        List<PrefabGUID> questRewards = Core.ParseConfigString(Plugin.QuestRewards.Value).Select(x => new PrefabGUID(x)).ToList();
        List<int> rewardAmounts = [.. Core.ParseConfigString(Plugin.QuestRewardAmounts.Value)];
        QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToDictionary(x => x.reward, x => x.amount);
        Core.StartCoroutine(QuestUpdateLoop());
    }
    static IEnumerator QuestUpdateLoop()
    {
        while (true)
        {
            if (EliteShardBearers && !ShardBearersReset)
            {
                IEnumerable<Entity> vBloods = GetVBloodsEnumerable();
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
                if (!Leveling)
                {
                    Entity character = user.LocalCharacter._Entity;
                    if (Core.DataStructures.PlayerQuests.ContainsKey(steamId))
                    {
                        RefreshQuests(user, steamId, (int)character.Read<Equipment>().GetFullLevel());
                    }
                    else
                    {
                        InitializePlayerQuests(steamId, (int)character.Read<Equipment>().GetFullLevel());
                    }
                }
                else if (Leveling && Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
                {
                    if (Core.DataStructures.PlayerQuests.ContainsKey(steamId))
                    {
                        RefreshQuests(user, steamId, xpData.Key);
                    }
                    else
                    {
                        InitializePlayerQuests(steamId, xpData.Key);
                    }
                }
            }

            TargetCache = GetTargetsEnumerable()
                .GroupBy(entity => entity.Read<PrefabGUID>())
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        if (TargetCache.TryGetValue(group.Key, out var existingSet))
                        {
                            // Update the existing set
                            existingSet.Clear();
                            existingSet.UnionWith(group);
                            return existingSet;
                        }
                        else
                        {
                            // Create a new set
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
    static IEnumerable<Entity> GetTargetsEnumerable()
    {
        JobHandle handle = GetTargets(out NativeArray<Entity> unitEntities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in unitEntities)
            {
                if (EntityManager.Exists(entity) && !entity.ReadBuffer<BuffBuffer>().IsEmpty)
                {
                    yield return entity;
                }
            }
        }
        finally
        {
            unitEntities.Dispose();
        }
    }
    static JobHandle GetTargets(out NativeArray<Entity> unitEntities, Allocator allocator = Allocator.TempJob)
    {
        unitEntities = UnitQuery.ToEntityArray(allocator);
        return default;
    }
    static IEnumerable<Entity> GetVBloodsEnumerable()
    {
        JobHandle handle = GetTargets(out NativeArray<Entity> unitEntities, Allocator.TempJob);
        handle.Complete();
        try
        {
            foreach (Entity entity in unitEntities)
            {
                if (EntityManager.Exists(entity))
                {
                    yield return entity;
                }
            }
        }
        finally
        {
            unitEntities.Dispose();
        }
    }
    /*
    static JobHandle GetVBloods(out NativeArray<Entity> unitEntities, Allocator allocator = Allocator.TempJob)
    {
        unitEntities = VBloodQuery.ToEntityArray(allocator);
        return default;
    }
    
    static void AddPrefabsToBloodHuntBuffer()
    {
        ProjectM.UI.GetVBloodsPositionResponseSystem getVBloodsPositionResponseSystem = Core.GetVBloodsPositionResponseSystem;
        var positions = getVBloodsPositionResponseSystem._Positions;
        Entity bloodHuntData = PrefabCollectionSystem._PrefabGuidToEntityMap[BloodHunts];
        DynamicBuffer<BloodHuntBuffer> bloodHuntBuffer = bloodHuntData.ReadBuffer<BloodHuntBuffer>();

        List<PrefabGUID> prefabGUIDs = [.. TargetCache.Keys];

        NativeArray<BloodHuntBuffer> bloodHunts = new(prefabGUIDs.Count, Allocator.Temp);

        for (int i = 0; i < prefabGUIDs.Count; i++)
        {
            
            bloodHunts[i] = new BloodHuntBuffer
            {
                BloodHuntTarget = prefabGUIDs[i],
                IsUnlockedByStation = true
            };
        }

        // Resize the buffer if necessary
        int newCount = bloodHuntBuffer.Length + bloodHunts.Length;
        if (bloodHuntBuffer.Capacity < newCount)
        {
            bloodHuntBuffer.Capacity = newCount;
        }

        // Add the new BloodHuntBuffer elements to the buffer
        foreach (BloodHuntBuffer bloodHunt in bloodHunts)
        {
            bloodHuntBuffer.Add(bloodHunt);
        }

        // Dispose of the temporary NativeArray
        bloodHunts.Dispose();
    }
    */
}

