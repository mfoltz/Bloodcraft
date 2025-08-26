using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class WitchPigTransformationHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID WitchPigTransformationBuff = Buffs.WitchPigTransformationBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ConfigService.FamiliarSystem && ctx.PrefabGuid.Equals(WitchPigTransformationBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (ctx.Target.IsVBloodOrGateBoss())
        {
            ctx.BuffEntity.Destroy();
        }
    }
}
