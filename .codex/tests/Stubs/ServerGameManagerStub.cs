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
    public static bool TryAddInventoryItem(Entity character, PrefabGUID itemPrefabGuid, int amount) => false;
}
