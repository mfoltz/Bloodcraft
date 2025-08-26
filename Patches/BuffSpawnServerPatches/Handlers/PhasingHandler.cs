using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class PhasingHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID PhasingBuff = Buffs.PhasingBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.PrefabGuid.Equals(PhasingBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.IsPlayer)
            return;

        if (ctx.Familiars && ctx.SteamId.HasDismissedFamiliar() && Familiars.AutoCallMap.TryRemove(ctx.Target, out Entity familiar))
        {
            Familiars.CallFamiliar(ctx.Target, familiar, ctx.Target.GetUser(), ctx.SteamId);
        }

        if (ctx.Legacies || ctx.Expertise)
        {
            Buffs.RefreshStats(ctx.Target);
        }
    }
}
