using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class DraculaReturnHideHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID DraculaReturnHideBuff = Buffs.DraculaReturnHideBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.Familiars && ctx.PrefabGuid.Equals(DraculaReturnHideBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (ctx.Target.IsFollowingPlayer())
        {
            ctx.BuffEntity.Destroy();
        }
    }
}
