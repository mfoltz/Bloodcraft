using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class BatLandingHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID BatLandingTravel = new(-371745443);

    public bool CanHandle(BuffSpawnContext ctx)
        => ConfigService.FamiliarSystem && ctx.IsPlayer && ctx.PrefabGuid.Equals(BatLandingTravel);

    public void Handle(BuffSpawnContext ctx)
    {
        if (Familiars.AutoCallMap.TryRemove(ctx.Target, out Entity familiar) && familiar.Exists())
        {
            Familiars.CallFamiliar(ctx.Target, familiar, ctx.Target.GetUser(), ctx.SteamId);
        }
    }
}
