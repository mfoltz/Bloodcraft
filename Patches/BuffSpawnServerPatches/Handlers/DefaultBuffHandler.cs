using Bloodcraft;
using Bloodcraft.Resources;
using Bloodcraft.Services;
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

        GameModeType gameMode = Core.SystemService.ServerGameSettingsSystem._Settings.GameModeType;

        if (gameMode == GameModeType.PvE)
        {
            prevent = ownerPlayer && !owner.Equals(ctx.Target);
        }
        else if (gameMode == GameModeType.PvP && ctx.Target.HasBuff(PvPProtectedBuff))
        {
            prevent = ownerPlayer && !owner.Equals(ctx.Target);
        }

        if (!prevent && ConfigService.FamiliarSystem)
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
