using Il2CppInterop.Runtime;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Services;
internal static class QueryService
{
    static EntityManager EntityManager => Core.EntityManager;
    static SystemService SystemService => Core.SystemService;

    static readonly EntityQuery _updateBuffsBufferDestroyQuery;
    static readonly EntityQuery _buffSpawnServerQuery;
    static readonly EntityQuery _scriptSpawnServerQuery;
    static QueryService()
    {
        _updateBuffsBufferDestroyQuery = ModifyEntityQuery(
            SystemService.UpdateBuffsBuffer_Destroy.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>())]
        );

        _buffSpawnServerQuery = ModifyEntityQuery(
            SystemService.BuffSystem_Spawn_Server.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>())]
        );

        _scriptSpawnServerQuery = ModifyEntityQuery(
            SystemService.ScriptSpawnServer.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<Buff>()), ComponentType.ReadOnly(Il2CppType.Of<EntityOwner>())]
        );
    }
    public static EntityQuery UpdateBuffsBufferDestroyQuery => _updateBuffsBufferDestroyQuery;
    public static EntityQuery BuffSpawnServerQuery => _buffSpawnServerQuery;
    public static EntityQuery ScriptSpawnServerQuery => _scriptSpawnServerQuery;
    static EntityQuery ModifyEntityQuery(EntityQuery originalQuery, ComponentType[] includeComponents = null, ComponentType[] excludeComponents = null)
    {
        EntityQueryDesc queryDesc = originalQuery.GetEntityQueryDesc();

        EntityQueryDesc modifiedQueryDesc = new()
        {
            All = (includeComponents != null && includeComponents.Any())
                ? queryDesc.All.Concat(includeComponents).ToArray()
                : queryDesc.All,

            Any = queryDesc.Any,

            None = (excludeComponents != null && excludeComponents.Any())
                ? queryDesc.None.Concat(excludeComponents).ToArray()
                : queryDesc.None,

            Options = queryDesc.Options
        };

        return EntityManager.CreateEntityQuery(modifiedQueryDesc);
    }

    /*
    static void ModifyEntityQueryInPlace(SystemBase system, EntityQuery query, ComponentType[] includeComponents, ComponentType[] excludeComponents, int queryIndex)
    {
        Core.Log.LogWarning($"Modifying EntityQuery - [{system.GetIl2CppType().Name}][Query {queryIndex}]");

        if (includeComponents != null && includeComponents.Length > 0)
        {
            query.AddSharedComponentFilter(includeComponents);
            Core.Log.LogInfo($"Added include components to [{system.GetIl2CppType().Name}][Query({queryIndex})]: {string.Join(", ", includeComponents.Select(c => c.ToString()))}");
        }

        if (excludeComponents != null && excludeComponents.Length > 0)
        {
            query.AddSharedComponentFilter(excludeComponents);
            Core.Log.LogInfo($"Added exclude components to [{system.GetIl2CppType().Name}][Query {queryIndex}]: {string.Join(", ", excludeComponents.Select(c => c.ToString()))}");
        }
    }
    public static void ModifySystemQueries(SystemBase system, ComponentType[] includeComponents = null, ComponentType[] excludeComponents = null, bool modifyAll = false, int queryIndex = 0)
    {
        if (system == null || system.EntityQueries == null || system.EntityQueries.Length == 0)
        {
            Core.Log.LogWarning($"ModifySystemQueries: System {system?.GetIl2CppType().Name} has no queries.");
            return;
        }

        if (modifyAll)
        {
            for (int i = 0; i < system.EntityQueries.Length; i++)
            {
                ModifyEntityQueryInPlace(system, system.EntityQueries[i], includeComponents, excludeComponents, i);
            }
        }
        else if (queryIndex >= 0 && queryIndex < system.EntityQueries.Length)
        {
            ModifyEntityQueryInPlace(system, system.EntityQueries[queryIndex], includeComponents, excludeComponents, queryIndex);
        }
        else
        {
            Core.Log.LogWarning($"ModifySystemQueries: Invalid query index {queryIndex} for system {system.GetIl2CppType().Name}");
        }
    }

    public static void ModifySystemQueries(SystemBase system, ComponentType[] includeComponents = null, ComponentType[] excludeComponents = null, bool modifyAll = false, int queryIndex = 0)
    {
        if (system == null || system.EntityQueries == null || system.EntityQueries.Length == 0)
            return;

        if (modifyAll)
        {
            for (int i = 0; i < system.EntityQueries.Length; i++)
            {
                EntityQuery originalQuery = system.EntityQueries[i];
                Core.Log.LogWarning($"Modifying EntityQuery - [{system.GetIl2CppType().Name}][{originalQuery.}]");
                system.EntityQueries[i] = ModifyEntityQuery(system.EntityQueries[i], includeComponents, excludeComponents);
            }
        }
        else if (queryIndex >= 0 && queryIndex < system.EntityQueries.Length)
        {
            system.EntityQueries[queryIndex] = ModifyEntityQuery(system.EntityQueries[queryIndex], includeComponents, excludeComponents);
        }
    }
    */
}

