using System.Collections.Generic;
using Bloodcraft.Systems.Leveling;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches;

/// <summary>
/// Minimal stub to satisfy build when server-specific patch sources are excluded.
/// </summary>
internal static class UpdateBuffsBufferDestroyPatch
{
    internal static List<PrefabGUID> PrestigeBuffs { get; } = new();
    internal static Dictionary<ClassManager.PlayerClass, List<PrefabGUID>> ClassBuffsOrdered { get; } = new();
    internal static Dictionary<ClassManager.PlayerClass, HashSet<PrefabGUID>> ClassBuffsSet { get; } = new();

    internal static void ApplyShapeshiftBuff(ulong steamId, Entity target)
    {
        // no-op stub for build-only environment
    }
}
