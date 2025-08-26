using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class DefaultBuffHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID PvPProtectedBuff = Buffs.PvPProtectedBuff;

    public bool CanHandle(BuffSpawnContext ctx) => true;

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.IsPlayer || ctx.Target.IsDueling())
            return;

        Entity owner = ctx.BuffEntity.GetOwner();
        bool ownerPlayer = owner.IsPlayer();

        bool prevent = false;

        if (ctx.GameMode == GameModeType.PvE)
        {
            prevent = ownerPlayer && !owner.Equals(ctx.Target);
        }
        else if (ctx.GameMode == GameModeType.PvP && ctx.Target.HasBuff(PvPProtectedBuff))
        {
            prevent = ownerPlayer && !owner.Equals(ctx.Target);
        }

        if (!prevent && ctx.Familiars)
        {
            if (owner.IsFollowingPlayer() || owner.GetOwner().IsFollowingPlayer())
            {
                prevent = true;
            }
        }

        if (prevent && ctx.BuffEntity.TryGetComponent(out Buff buff) && buff.BuffEffectType.Equals(BuffEffectType.Debuff))
        {
            ctx.BuffEntity.Destroy();
        }
    }
}
