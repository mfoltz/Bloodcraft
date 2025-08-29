using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class CombatStanceHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID CombatStanceBuff = Buffs.CombatStanceBuff;
    static readonly PrefabGUID EvolvedVampireBuff = Buffs.EvolvedVampireBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.PrefabGuid.Equals(CombatStanceBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.IsPlayer)
            return;

        if (ctx.Target.HasBuff(EvolvedVampireBuff))
        {
            ctx.BuffEntity.Remove<SetOwnerRotateTowardsMouse>();
        }
        else if (ConfigService.FamiliarSystem && ctx.SteamId.HasActiveFamiliar())
        {
            Entity familiar = Familiars.GetActiveFamiliar(ctx.Target);
            Familiars.SyncAggro(ctx.Target, familiar);
        }
    }
}
