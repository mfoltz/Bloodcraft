using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBools;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS.Handlers;

sealed class TauntEmoteHandler : IBuffDestroyHandler
{
    static readonly PrefabGUID TauntEmoteBuff = Buffs.TauntEmoteBuff;

    public bool CanHandle(UpdateBuffDestroyContext ctx)
    {
        return ctx.IsPlayerTarget && ctx.PrefabGuid.Equals(TauntEmoteBuff);
    }

    public bool Handle(UpdateBuffDestroyContext ctx)
    {
        User user = ctx.Target.GetUser();
        ulong steamId = user.PlatformId;
        if (GetPlayerBool(steamId, SHAPESHIFT_KEY))
        {
            if (EmoteSystemPatch.BlockShapeshift.Contains(steamId))
            {
                EmoteSystemPatch.BlockShapeshift.Remove(steamId);
            }
            else if (Shapeshifts.CheckExoFormCharge(user, steamId))
            {
                UpdateBuffsBufferDestroyPatch.ApplyShapeshiftBuff(steamId, ctx.Target);
            }
        }
        return false;
    }
}

