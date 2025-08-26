using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS;

readonly struct UpdateBuffDestroyContext
{
    public Entity BuffEntity { get; init; }
    public Entity Target { get; init; }
    public PrefabGUID PrefabGuid { get; init; }
    public bool IsPlayerTarget { get; init; }
    public bool IsFamiliarTarget { get; init; }
    public bool IsWeaponEquipBuff { get; init; }
    public bool IsBloodBuff { get; init; }
    public ulong SteamId { get; init; }
}

