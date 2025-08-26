using Bloodcraft.Resources;
using Bloodcraft.Services;
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

        if (ConfigService.FamiliarSystem && ctx.SteamId.HasDismissedFamiliar() && Familiars.AutoCallMap.TryRemove(ctx.Target, out Entity familiar))
        {
            Familiars.CallFamiliar(ctx.Target, familiar, ctx.Target.GetUser(), ctx.SteamId);
        }

        if (ConfigService.LegacySystem || ConfigService.ExpertiseSystem)
        {
            Buffs.RefreshStats(ctx.Target);
        }
    }
}
