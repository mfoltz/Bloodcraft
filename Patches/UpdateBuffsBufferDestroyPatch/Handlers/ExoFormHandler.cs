using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS.Handlers;

sealed class ExoFormHandler : IBuffDestroyHandler
{
    static readonly bool Enabled = ConfigService.ExoPrestiging;
    static readonly PrefabGUID EvolvedVampireFormBuff = Buffs.EvolvedVampireBuff;
    static readonly PrefabGUID PhasingBuff = Buffs.PhasingBuff;
    static readonly PrefabGUID GateBossFeedCompleteBuff = Buffs.GateBossFeedCompleteBuff;

    public bool CanHandle(UpdateBuffDestroyContext ctx)
    {
        return Enabled && ctx.IsPlayerTarget &&
               (ctx.PrefabGuid.Equals(EvolvedVampireFormBuff) || ctx.PrefabGuid.Equals(PhasingBuff));
    }

    public bool Handle(UpdateBuffDestroyContext ctx)
    {
        ulong steamId = ctx.SteamId;
        ctx.Target.TryApplyBuff(GateBossFeedCompleteBuff);
        Shapeshifts.UpdatePartialExoFormChargeUsed(ctx.BuffEntity, steamId);
        return false;
    }
}

