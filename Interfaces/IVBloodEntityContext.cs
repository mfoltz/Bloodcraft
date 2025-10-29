using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Interfaces;

/// <summary>
/// Represents a context capable of enumerating VBlood entities and performing
/// the minimal operations required by <see cref="Services.ShardBearerResetService"/>.
/// </summary>
internal interface IVBloodEntityContext
{
    /// <summary>
    /// Enumerates the entities that qualify as VBlood units in the current context.
    /// </summary>
    /// <returns>An enumerable sequence of VBlood entities.</returns>
    IEnumerable<Entity> EnumerateVBloodEntities();

    /// <summary>
    /// Attempts to resolve the prefab GUID for the provided entity.
    /// </summary>
    /// <param name="entity">The entity whose prefab GUID should be resolved.</param>
    /// <param name="prefabGuid">When this method returns, contains the prefab GUID for <paramref name="entity"/> if available.</param>
    /// <returns><see langword="true"/> when the prefab GUID is available; otherwise <see langword="false"/>.</returns>
    bool TryGetPrefabGuid(Entity entity, out PrefabGUID prefabGuid);

    /// <summary>
    /// Destroys the provided entity.
    /// </summary>
    /// <param name="entity">The entity to remove from the world.</param>
    void Destroy(Entity entity);
}
