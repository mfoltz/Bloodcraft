using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.SystemUtilities.Quests.QuestUtilities;

namespace Bloodcraft.Services;
internal class QuestService
{
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    public QuestService() // add coroutine to handle updating quests at midnight and distributing them as needed? make random generator or something in QuestUtilities
    {
        List<PrefabGUID> questRewards = Core.ParseConfigString(Plugin.QuestRewards.Value).Select(x => new PrefabGUID(x)).ToList();
        List<int> rewardAmounts = [.. Core.ParseConfigString(Plugin.QuestRewardAmounts.Value)];
        QuestRewards = questRewards.Zip(rewardAmounts, (reward, amount) => new { reward, amount }).ToDictionary(x => x.reward, x => x.amount);
        PopulatePrefabs();
        //Core.Log.LogInfo("Quest service initialized, starting coroutine...");
        Core.StartCoroutine(QuestHandler());
    }
    static void PopulatePrefabs()
    {
        //NativeArray<Entity> prefabs = PrefabQuery.ToEntityArray(Allocator.Temp);
        var prefabs = Core.PrefabCollectionSystem.NameToPrefabGuidDictionary;
        foreach(string name in prefabs.Keys)
        {
            if (name.Contains("CHAR"))
            {
                UnitPrefabs.Add(prefabs[name]);
            }
            /*
            else if (Core.PrefabCollectionSystem._PrefabGuidToEntityMap[prefabs[name]].Has<ItemData>())
            {
                CraftPrefabs.Add(prefabs[name]);
            }
            
            else if (entity.Has<YieldResourcesOnDamageTaken>())
            {
                GatherEntities.Add(entity);
                GatherPrefabs.Add(entity.Read<PrefabGUID>());
            }
            */
        }

    }
    static IEnumerator QuestHandler()
    {
        WaitForSeconds updateDelay = new(300);
        WaitForSeconds startDelay = new(60);
        WaitForSeconds playerDelay = new(1);

        while (true)
        {
            yield return startDelay; // Wait 60 seconds before processing players

            if (PlayerService.playerIdCache.Keys.Count == 0)
            {
                yield return updateDelay; // Wait 300 seconds if there are no players
                continue;
            }

            Dictionary<ulong, Entity> players = new(PlayerService.playerIdCache); // Copy the player cache to make sure updates to that don't interfere with this loop

            foreach (ulong steamId in players.Keys)
            {
                User user = PlayerService.playerIdCache[steamId].Read<PlayerCharacter>().UserEntity.Read<User>();

                if (!Leveling)
                {
                    if (Core.DataStructures.PlayerQuests.ContainsKey(steamId))
                    {
                        RefreshQuests(user, steamId, (int)PlayerService.playerIdCache[steamId].Read<Equipment>().GetFullLevel());
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
                        RefreshQuests(user, steamId, xpData.Key);
                    }
                    else
                    {
                        InitializePlayerQuests(steamId, xpData.Key);
                    }
                }

                yield return playerDelay; // Wait 1 second between processing each player
            }

            yield return updateDelay; // Wait 300 seconds before processing players again
        }
    }
}
