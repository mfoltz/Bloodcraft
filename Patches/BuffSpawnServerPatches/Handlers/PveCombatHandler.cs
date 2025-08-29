using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class PveCombatHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID PveCombatBuff = Buffs.PvECombatBuff;
    static readonly PrefabGUID EvolvedVampireBuff = Buffs.EvolvedVampireBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.PrefabGuid.Equals(PveCombatBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.IsPlayer)
            return;

        if (ctx.Target.HasBuff(EvolvedVampireBuff))
        {
            ctx.BuffEntity.Remove<SetOwnerRotateTowardsMouse>();
        }

        if (ConfigService.FamiliarSystem && ctx.SteamId.HasActiveFamiliar())
        {
            Entity familiar = Familiars.GetActiveFamiliar(ctx.Target);
            Familiars.HandleFamiliarEnteringCombat(ctx.Target, familiar);
            Familiars.SyncAggro(ctx.Target, familiar);
        }
    }
}
