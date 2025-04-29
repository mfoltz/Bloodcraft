using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
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
    public static EntityQuery ModifyEntityQuery(EntityQuery originalQuery, ComponentType[] includeComponents = null, ComponentType[] excludeComponents = null)
    {
        /*
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

        Il2CppReferenceArray<EntityQueryDesc> queryDescArray = new(1);
        queryDescArray[0] = modifiedQueryDesc;

        return EntityManager.CreateEntityQuery(queryDescArray);
        */

        EntityQueryDesc queryDesc = originalQuery.GetEntityQueryDesc();
        EntityQueryBuilder entityQueryBuilder = new(Allocator.Temp);

        if (queryDesc.All.Any())
        {
            foreach (var componentType in queryDesc.All)
            {
                entityQueryBuilder.AddAll(componentType);
            }

            if (includeComponents != null && includeComponents.Any())
            {
                foreach (var componentType in includeComponents)
                {
                    entityQueryBuilder.AddAll(componentType);
                }
            }
        }

        if (queryDesc.Any.Any())
        {
            foreach (var componentType in queryDesc.Any)
            {
                entityQueryBuilder.AddAny(componentType);
            }
        }

        if (queryDesc.Absent.Any())
        {
            foreach (var componentType in queryDesc.Absent)
            {
                entityQueryBuilder.AddAny(componentType);
            }
        }

        if (queryDesc.None.Any())
        {
            foreach (var componentType in queryDesc.None)
            {
                entityQueryBuilder.AddNone(componentType);
            }

            if (excludeComponents != null && excludeComponents.Any())
            {
                foreach (var componentType in excludeComponents)
                {
                    entityQueryBuilder.AddNone(componentType);
                }
            }
        }

        entityQueryBuilder.WithOptions(queryDesc.Options);
        return EntityManager.CreateEntityQuery(ref entityQueryBuilder);
    }
}

