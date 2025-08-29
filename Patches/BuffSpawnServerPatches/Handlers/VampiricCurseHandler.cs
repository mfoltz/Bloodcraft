using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class VampiricCurseHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID VampiricCurseBuff = Buffs.BloodCurseBuff;
    static readonly PrefabGUID TargetSwallowedBuff = Buffs.TargetSwallowedBuff;
    const float TravelDuration = 7.5f;

    public bool CanHandle(BuffSpawnContext ctx)
        => ConfigService.FamiliarSystem && ctx.IsPlayer && ctx.PrefabGuid.Equals(VampiricCurseBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (ctx.BuffEntity.Has<GameplayEventListeners>())
            return;

        Entity familiar = Familiars.GetActiveFamiliar(ctx.Target);
        if (!familiar.Exists())
            return;

        if (familiar.TryApplyAndGetBuffWithOwner(ctx.Target, TargetSwallowedBuff, out Entity buff))
        {
            if (buff.Has<LifeTime>())
            {
                buff.With((ref LifeTime lifeTime) => lifeTime.Duration = TravelDuration);
            }
        }
    }
}
