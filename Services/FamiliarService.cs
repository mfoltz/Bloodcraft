using Bloodcraft.Patches;
using Bloodcraft.Systems.Familiars;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Services;
internal class FamiliarService 
{
    static readonly ComponentType[] MinionComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Minion>())
        ];

    static readonly ComponentType[] FamiliarComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
        ];

    //readonly IgnorePhysicsDebugSystem familiarMonoBehaviour;

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
        if (Plugin.FamiliarSystem.Value)
        {
            List<int> unitBans = Core.ParseConfigString(Plugin.BannedUnits.Value);
            List<string> typeBans = Plugin.BannedTypes.Value.Split(',').Select(s => s.Trim()).ToList();
            if (unitBans.Count > 0) FamiliarUnlockSystem.ExemptPrefabs = unitBans;
            if (typeBans.Count > 0) FamiliarUnlockSystem.ExemptTypes = typeBans;
            //familiarMonoBehaviour = (new GameObject("FamiliarService")).AddComponent<IgnorePhysicsDebugSystem>();
            //familiarMonoBehaviour.StartCoroutine(UpdateLoop().WrapToIl2Cpp());

        }
    }
    /*
    IEnumerator UpdateLoop()
    {
        WaitForSeconds waitForSeconds = new(30); // Convert minutes to seconds for update loop

        while (true)
        {
            yield return waitForSeconds;

            var actives = Core.DataStructures.FamiliarActives.Keys;
            foreach (ulong steamId in actives)
            {
                
            }
        
            Entity player = familiar.Read<Follower>().Followed._Value;
            float3 playerPos = player.Read<LocalToWorld>().Position;
            float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, playerPos);
            if (distance > 25f)
            {
                familiar.Write(new Translation { Value = player.Read<LocalToWorld>().Position });
                //Core.Log.LogInfo($"Familiar returned to owner.");
            }
            
            
        }
    }
    */
    public void HandleFamiliarsOnSpawn()
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
    
    public void CleanUpMinions()
    {
        NativeArray<Entity> minions = minionQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity minion in minions)
            {
                //if (minion.Has<PrefabGUID>()) Core.Log.LogInfo($"Cleaning up minion: {minion.Read<PrefabGUID>().LookupName()}");
                DestroyUtility.CreateDestroyEvent(Core.EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
            }
        }
        finally
        {
            minions.Dispose();
        }
    }
    
}
