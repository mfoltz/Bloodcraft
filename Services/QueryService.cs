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
        _updateBuffsBufferDestroyQuery = AddComponentsToQuery(
            SystemService.UpdateBuffsBuffer_Destroy.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>())]
        );

        _buffSpawnServerQuery = AddComponentsToQuery(
            SystemService.BuffSystem_Spawn_Server.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>())]
        );

        _scriptSpawnServerQuery = AddComponentsToQuery(
            SystemService.ScriptSpawnServer.EntityQueries[0],
            [ComponentType.ReadOnly(Il2CppType.Of<Buff>()), ComponentType.ReadOnly(Il2CppType.Of<EntityOwner>())]
        );
    }

    public static EntityQuery UpdateBuffsBufferDestroyQuery => _updateBuffsBufferDestroyQuery;
    public static EntityQuery BuffSpawnServerQuery => _buffSpawnServerQuery;
    public static EntityQuery ScriptSpawnServerQuery => _scriptSpawnServerQuery;

    static EntityQuery AddComponentsToQuery(EntityQuery originalQuery, ComponentType[] additionalAllComponents = null, ComponentType[] additionalNoneComponents = null)
    {
        EntityQueryDesc queryDesc = originalQuery.GetEntityQueryDesc();

        EntityQueryDesc modifiedQueryDesc = new()
        {
            All = (additionalAllComponents != null && additionalAllComponents.Any())
                ? queryDesc.All.Concat(additionalAllComponents).ToArray()
                : queryDesc.All,

            Any = queryDesc.Any,

            None = (additionalNoneComponents != null && additionalNoneComponents.Any())
                ? queryDesc.None.Concat(additionalNoneComponents).ToArray()
                : queryDesc.None,

            Options = queryDesc.Options
        };

        return EntityManager.CreateEntityQuery(modifiedQueryDesc);
    }
}

