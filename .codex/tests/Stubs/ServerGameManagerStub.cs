using Stunlock.Core;
using Unity.Entities;

namespace ProjectM;

/// <summary>
/// Minimal stub of the <c>ServerGameManager</c> type to allow tests to patch and intercept
/// <see cref="TryAddInventoryItem(Unity.Entities.Entity, Stunlock.Core.PrefabGUID, int)"/>
/// without relying on the game assemblies at build time.
/// </summary>
public class ServerGameManager
{
    public double ServerTime { get; set; }

    public double DeltaTime { get; set; }

    public Entity InstantiateEntityImmediate(Entity character, PrefabGUID prefabGuid)
    {
        return new Entity { Index = 0, Version = 1 };
    }

    public Entity InstantiateBuffEntityImmediate(
        Entity instigator,
        Entity target,
        PrefabGUID buffPrefabGuid,
        object? unused,
        int stacks)
    {
        return new Entity { Index = 0, Version = 1 };
    }

    public static bool TryAddInventoryItem(Entity character, PrefabGUID itemPrefabGuid, int amount) => false;

    public bool TryGetBuff(Entity entity, PrefabGUID prefabGuid, out Entity buffEntity)
    {
        buffEntity = Entity.Null;
        return false;
    }
}
