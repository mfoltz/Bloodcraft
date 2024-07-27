using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
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

    static readonly ComponentType[] UnitComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<AiMoveSpeeds>()),
        ComponentType.ReadOnly(Il2CppType.Of<UnitLevel>()),
        ComponentType.ReadOnly(Il2CppType.Of<TilePosition>()),
        ComponentType.ReadOnly(Il2CppType.Of<Team>()),
        ComponentType.ReadOnly(Il2CppType.Of<BuffBuffer>()),
        ComponentType.ReadOnly(Il2CppType.Of<Translation>()),
    ];

    static EntityQuery UnitQuery;

    public static Dictionary<PrefabGUID, HashSet<Entity>> TargetCache = [];
    public static DateTime LastUpdate;

    static readonly PrefabGUID enchantedCross = new(-1449314709);
    public QuestService()
    {
        UnitQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = UnitComponents,
            Options = EntityQueryOptions.IncludeDisabled
        });
        List<PrefabGUID> questRewards = Core.ParseConfigString(Plugin.QuestRewards.Value).Select(x => new PrefabGUID(x)).ToList();
        List<int> rewardAmounts = [.. Core.ParseConfigString(Plugin.QuestRewardAmounts.Value)];
        QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToDictionary(x => x.reward, x => x.amount);
        Core.StartCoroutine(QuestUpdateLoop());
    }
    /*
    static void PopulatePrefabs()
    {
        var prefabs = PrefabCollectionSystem.NameToPrefabGuidDictionary;
        foreach(string name in prefabs.Keys)
        {
            if (name.Contains("CHAR"))
            {
                //UnitPrefabs.Add(prefabs[name]);
            }
            
            else if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefabs[name]].Has<ItemData>())
            {
                CraftPrefabs.Add(prefabs[name]);
            }
            
            else if (entity.Has<YieldResourcesOnDamageTaken>())
            {
                GatherEntities.Add(entity);
                GatherPrefabs.Add(entity.Read<PrefabGUID>());
            }
            
        }
    }
    */
    static IEnumerator QuestUpdateLoop()
    {
        WaitForSeconds updateDelay = new(60);
        WaitForSeconds startDelay = new(30);
        WaitForSeconds playerDelay = new(0.5f);

        while (true)
        {
            if (PlayerService.PlayerCache.Keys.Count == 0)
            {
                yield return startDelay; // Wait 30 seconds if no players
                continue;
            }

            Dictionary<string, Entity> players = new(PlayerService.PlayerCache); // Copy the player cache to make sure updates to that don't interfere with this loop

            foreach (string player in players.Keys)
            {
                User user = PlayerService.PlayerCache[player].Read<User>();
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
                yield return playerDelay; // Wait 1 second between processing each player
            }

            TargetCache = GetTargetsEnumerable()
                .GroupBy(entity => entity.Read<PrefabGUID>())
                .ToDictionary(group => group.Key, group => new HashSet<Entity>(group));
            if (TargetCache.ContainsKey(enchantedCross)) TargetCache.Remove(enchantedCross);
            //Core.Log.LogInfo($"QuestService: Updated TargetCache with {TargetCache.Count} entries");
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
    /*
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

