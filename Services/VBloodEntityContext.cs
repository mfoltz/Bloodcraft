using System.Collections.Generic;
using Bloodcraft;
using Bloodcraft.Interfaces;
using Bloodcraft.Utilities;
using Stunlock.Core;
using Il2CppInterop.Runtime;
using ProjectM;
using Unity.Entities;

namespace Bloodcraft.Services;

/// <summary>
/// Provides access to VBlood entities using the real DOTS <see cref="EntityManager"/> APIs.
/// </summary>
internal sealed class VBloodEntityContext : IVBloodEntityContext
{
    static readonly ComponentType[] VBloodAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
        ComponentType.ReadOnly(Il2CppType.Of<VBloodConsumeSource>()),
        ComponentType.ReadOnly(Il2CppType.Of<VBloodUnit>())
    ];

    readonly EntityManager entityManager;

    public VBloodEntityContext(EntityManager entityManager)
    {
        this.entityManager = entityManager;
    }

    /// <inheritdoc />
    public IEnumerable<Entity> EnumerateVBloodEntities()
    {
        EntityQuery query = entityManager.BuildEntityQuery(VBloodAllComponents, EntityQueryOptions.IncludeDisabled);
        using NativeAccessor<Entity> entities = query.ToEntityArrayAccessor();

        foreach (Entity entity in entities)
        {
            yield return entity;
        }
    }

    /// <inheritdoc />
    public bool TryGetPrefabGuid(Entity entity, out PrefabGUID prefabGuid)
    {
        return entity.TryGetComponent(out prefabGuid);
    }

    /// <inheritdoc />
    public void Destroy(Entity entity)
    {
        entity.Destroy();
    }
}
