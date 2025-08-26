using ProjectM;
using ProjectM.Network;
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
    public GameModeType GameMode { get; init; }
    public bool EliteShardBearers { get; init; }
    public bool Legacies { get; init; }
    public bool Expertise { get; init; }
    public bool TrueImmortal { get; init; }
    public bool Familiars { get; init; }
    public bool FamiliarPvP { get; init; }
    public bool PotionStacking { get; init; }
    public bool Professions { get; init; }
    public ComponentLookup<BlockFeedBuff> BlockFeedLookup { get; init; }
}
