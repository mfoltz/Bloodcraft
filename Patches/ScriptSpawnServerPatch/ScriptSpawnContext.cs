using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch;

readonly struct ScriptSpawnContext
{
    public Entity BuffEntity { get; init; }
    public Entity Target { get; init; }
    public Entity Owner { get; init; }
    public PrefabGUID PrefabGuid { get; init; }
    public bool TargetIsPlayer { get; init; }
    public bool TargetIsFamiliar { get; init; }
    public bool OwnerIsFamiliar { get; init; }
    public bool IsBloodBuff { get; init; }
    public bool IsDebuff { get; init; }
}
