using Bloodcraft.Systems.Familiars;
using HarmonyLib;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class InitializationPatch
{
    static readonly ComponentType[] FamiliarComponents =
            [
                ComponentType.ReadOnly(Il2CppType.Of<Follower>()),
                ComponentType.ReadOnly(Il2CppType.Of<UnitStats>())
            ];

    [HarmonyPatch(typeof(SpawnTeamSystem_OnPersistenceLoad), nameof(SpawnTeamSystem_OnPersistenceLoad.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix()
    {
        Core.Initialize();
        SetFamiliarsOnSpawn();
    }
    static void SetFamiliarsOnSpawn()
    {
        EntityQuery familiarQuery = Core.EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = FamiliarComponents,
            Options = EntityQueryOptions.IncludeAll
        });

        NativeArray<Entity> familiars = familiarQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity familiar in familiars)
            {
                Follower follower = familiar.Read<Follower>();
                PrefabGUID prefabGUID = familiar.Read<PrefabGUID>();
                
                if (follower.Followed._Value.Has<PlayerCharacter>())
                {
                    //Core.Log.LogInfo($"{prefabGUID.LookupName()}");
                    //Core.Log.LogInfo($"Following player character...");
                    ulong steamId = follower.Followed._Value.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.FamiliarActives.TryGetValue(steamId, out var actives) && actives.Item2.Equals(prefabGUID.GuidHash) && Core.FamiliarExperienceManager.LoadFamiliarExperience(steamId).FamiliarExperience.TryGetValue(prefabGUID.GuidHash, out var xpData))
                    {
                        FamiliarSummonSystem.HandleFamiliarModifications(follower.Followed._Value, familiar, xpData.Key);
                    }
                }
               
            }
        }
        finally
        {
            familiars.Dispose();
            familiarQuery.Dispose();
        }
    }

}
