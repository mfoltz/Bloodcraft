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
    /// <summary>
    /// Gets or sets the simulated server time surfaced to consumers.
    /// </summary>
    public double ServerTime { get; set; }

    /// <summary>
    /// Gets or sets the simulated frame delta time returned to callers.
    /// </summary>
    public double DeltaTime { get; set; }

    public static bool TryAddInventoryItem(Entity character, PrefabGUID itemPrefabGuid, int amount) => false;
}
