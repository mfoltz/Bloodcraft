using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches;

readonly struct BuffSpawnContext
{
    public Entity BuffEntity { get; init; }
    public Entity Target { get; init; }
    public PrefabGUID PrefabGuid { get; init; }
    public string PrefabName { get; init; }
    public bool IsPlayer { get; init; }
    public ulong SteamId { get; init; }
    public ComponentLookup<BlockFeedBuff> BlockFeedLookup { get; init; }
}
