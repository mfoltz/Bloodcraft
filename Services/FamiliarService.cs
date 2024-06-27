using Bloodcraft.Patches;
using Bloodcraft.Systems.Familiars;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft.Services;
internal class FamiliarService 
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static readonly ComponentType[] MinionComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Minion>())
        ];

    static readonly ComponentType[] FamiliarComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
        ];

    EntityQuery minionQuery;

    EntityQuery familiarQuery;
    public FamiliarService() // coroutine to occasionally move familiars to owners if disabled? hmmm
    {
        minionQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = MinionComponents,
            Options = EntityQueryOptions.IncludeDisabledEntities
        });

        familiarQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = FamiliarComponents,
            Options = EntityQueryOptions.IncludeDisabledEntities
        });
        List<int> unitBans = Core.ParseConfigString(Plugin.BannedUnits.Value);
        List<string> typeBans = Plugin.BannedTypes.Value.Split(',').Select(s => s.Trim()).ToList();
        if (unitBans.Count > 0) FamiliarUnlockSystem.ExemptPrefabs = unitBans;
        if (typeBans.Count > 0) FamiliarUnlockSystem.ExemptTypes = typeBans;
        HandleFamiliarsOnSpawn();
        Core.StartCoroutine(CleanUpMinions());
    }
    
    IEnumerator CleanUpMinions()
    {
        while (true)
        {
            yield return new WaitForSeconds(120f); // want to get rid of allied player minions that don't have unholy in the name every so often although it is somewhat rare compared to minions that are caught and handled via the dictionary in FamiliarPatches
            FindAndHandleFamiliarMinions();
        }
    }
    void HandleFamiliarsOnSpawn()
    {
        NativeArray<Entity> followers = familiarQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity follower in followers)
            {
                if (follower.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    //if (follower.Has<MinionMaster>()) HandleFamiliarMinions(follower);
                    DestroyUtility.CreateDestroyEvent(Core.EntityManager, follower, DestroyReason.Default, DestroyDebugReason.None);
                    ulong steamId = follower.Read<Follower>().Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives))
                    {
                        Core.DataStructures.FamiliarActives[steamId] = new(Entity.Null, 0);
                        Core.DataStructures.SavePlayerFamiliarActives();
                    }
                }
            }
        }
        finally
        {
            followers.Dispose();
        }      
    }
    public void HandleFamiliarMinions(Entity familiar)
    {
        if (FamiliarPatches.familiarMinions.ContainsKey(familiar))
        {
            foreach (Entity minion in FamiliarPatches.familiarMinions[familiar])
            {
                //Core.Log.LogInfo($"Destroying minion...");
                DestroyUtility.CreateDestroyEvent(Core.EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
            }
            FamiliarPatches.familiarMinions.Remove(familiar);
        }
    }
    void FindAndHandleFamiliarMinions()
    {
        NativeArray<Entity> minions = minionQuery.ToEntityArray(Allocator.Temp);
        HashSet<Entity> players = [..PlayerService.playerCache.Values];
        var unholyPrefix = "char_unholy";
        try
        {
            foreach (Entity minion in minions)
            {
                string minionName = minion.Read<PrefabGUID>().LookupName().ToLower();
                if (minionName.Contains(unholyPrefix)) continue;
                foreach (Entity player in players)
                {
                    if (ServerGameManager.IsAllies(minion, player))
                    {
                        Core.Log.LogInfo($"Destroying minion {minionName}...");
                        DestroyUtility.CreateDestroyEvent(Core.EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
                        break;
                    }
                }
            }
        }
        finally
        {
            minions.Dispose();
        }
    }
}
