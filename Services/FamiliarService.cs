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
            ComponentType.ReadOnly(Il2CppType.Of<EntityOwner>()),
            ComponentType.ReadOnly(Il2CppType.Of<Minion>())
        ];

    static readonly ComponentType[] FamiliarComponents =
        [
            ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
            ComponentType.ReadOnly(Il2CppType.Of<ModifyTeamBuff>())
        ];

    //readonly IgnorePhysicsDebugSystem familiarMonoBehaviour;

    EntityQuery minionQuery;

    EntityQuery familiarQuery;
    public FamiliarService() // coroutine to occasionally move familiars to owners if disabled? hmmm
    {
        minionQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = MinionComponents,
            Options = EntityQueryOptions.Default
        });

        familiarQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = FamiliarComponents,
            Options = EntityQueryOptions.IncludeDisabledEntities
        });
        if (Plugin.FamiliarSystem.Value)
        {
            List<int> unitBans = Core.ParseConfigString(Plugin.BannedUnits.Value);
            List<string> typeBans = [..(Plugin.BannedTypes.Value).Split(',')];
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
        NativeArray<Entity> familiars = familiarQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity familiar in familiars)
            {
                //Core.Log.LogInfo("Destroying player familiar...");
                if (familiar.Has<MinionMaster>()) HandleFamiliarMinions(familiar);
                DestroyUtility.Destroy(Core.EntityManager, familiar, DestroyDebugReason.None);
            }
            foreach (var actives in Core.DataStructures.FamiliarActives)
            {
                Core.DataStructures.FamiliarActives[actives.Key] = new(Entity.Null, 0);
            }
            Core.DataStructures.SavePlayerFamiliarActives();
        }
        finally
        {
            familiars.Dispose();
        }      
    }
    public void HandleFamiliarMinions(Entity familiar)
    {
        NativeArray<Entity> minions = minionQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity minion in minions)
            {
                if (minion.Read<EntityOwner>().Owner.Equals(familiar))
                {
                    DestroyUtility.CreateDestroyEvent(Core.EntityManager, minion, DestroyReason.Default, DestroyDebugReason.None);
                }
            }
        }
        finally
        {
            minions.Dispose();
        }
    }
}
