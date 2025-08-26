using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class PvpCombatHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID PvpCombatBuff = Buffs.PvPCombatBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.Familiars && ctx.IsPlayer && ctx.PrefabGuid.Equals(PvpCombatBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.SteamId.HasActiveFamiliar())
            return;

        Entity familiar = Familiars.GetActiveFamiliar(ctx.Target);

        if (!ctx.FamiliarPvP)
        {
            User user = ctx.Target.GetUser();
            Familiars.DismissFamiliar(ctx.Target, familiar, user, ctx.SteamId);
        }
        else
        {
            Familiars.HandleFamiliarEnteringCombat(ctx.Target, familiar);
        }
    }
}
