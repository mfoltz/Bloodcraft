using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.SystemUtilities.Quests.QuestUtilities;

namespace Bloodcraft.Services;
internal class QuestService
{
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly ComponentType[] prefabComponent =
        [
            ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ];
    static EntityQuery PrefabQuery;
    public QuestService() // add coroutine to handle updating quests at midnight and distributing them as needed? make random generator or something in QuestUtilities
    {
        PrefabQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = prefabComponent,
            Options = EntityQueryOptions.IncludeDisabledEntities
        });
        QuestResets[QuestType.Daily] = GetNextDailyReset(); // random seed based on the current daily time?
        QuestResets[QuestType.Weekly] = GetNextWeeklyReset(); // random seed based on the current weekly time?
        List<PrefabGUID> questRewards = Core.ParseConfigString(Plugin.QuestRewards.Value).Select(x => new PrefabGUID(x)).ToList();
        List<int> rewardAmounts = [.. Core.ParseConfigString(Plugin.QuestRewardAmounts.Value)];
        QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToDictionary(x => x.reward, x => x.amount);
        PopulatePrefabs();
        //Core.Log.LogInfo("Quest service initialized, starting coroutine...");
        Core.StartCoroutine(QuestHandler());
    }
    static void PopulatePrefabs()
    {
        NativeArray<Entity> prefabs = PrefabQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach(Entity entity in prefabs)
            {
                if (entity.Has<PrefabGUID>() && entity.Read<PrefabGUID>().LookupName().Contains("CHAR"))
                {
                    UnitEntities.Add(entity);
                    UnitPrefabs.Add(entity.Read<PrefabGUID>());
                }
                else if (entity.Has<ItemData>())
                {
                    CraftEntities.Add(entity);
                    CraftPrefabs.Add(entity.Read<PrefabGUID>());
                }
                else if (entity.Has<YieldResourcesOnDamageTaken>())
                {
                    GatherEntities.Add(entity);
                    GatherPrefabs.Add(entity.Read<PrefabGUID>());
                }
            }
        }
        finally
        {
            prefabs.Dispose();
        }
    }
    static IEnumerator QuestHandler()
    {
        while (true)
        {
            yield return new WaitForSeconds(300);
            DateTime now = DateTime.UtcNow;
            bool dailyRefresh = QuestResets[QuestType.Daily] <= now;
            bool weeklyRefresh = QuestResets[QuestType.Weekly] <= now;
            if (dailyRefresh || weeklyRefresh)
            {
                if (dailyRefresh) QuestResets[QuestType.Daily] = GetNextDailyReset();
                if (weeklyRefresh) QuestResets[QuestType.Weekly] = GetNextWeeklyReset();
            }
            //Core.Log.LogInfo("Refreshing quests...");
            if (PlayerService.playerIdCache.Keys.Count == 0) continue;
            foreach (ulong steamId in PlayerService.playerIdCache.Keys)
            {
                if (!Leveling)
                {
                    if (Core.DataStructures.PlayerQuests.ContainsKey(steamId))
                    {
                        RefreshQuests(steamId, (int)PlayerService.playerIdCache[steamId].Read<Equipment>().GetFullLevel());
                    }
                    else
                    {
                        InitializePlayerQuests(steamId, (int)PlayerService.playerIdCache[steamId].Read<Equipment>().GetFullLevel());
                    }
                }
                else if (Leveling && Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var xpData))
                {
                    if (Core.DataStructures.PlayerQuests.ContainsKey(steamId))
                    {
                        RefreshQuests(steamId, xpData.Key);
                    }
                    else
                    {
                        InitializePlayerQuests(steamId, xpData.Key);
                    }
                }
                yield return new WaitForSeconds(10);
            }
        }
    }
}
