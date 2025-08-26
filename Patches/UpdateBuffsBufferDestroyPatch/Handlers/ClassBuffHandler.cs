using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBools;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS.Handlers;

sealed class ClassBuffHandler : IBuffDestroyHandler
{
    static readonly bool Enabled = ConfigService.ClassSystem;

    public bool CanHandle(UpdateBuffDestroyContext ctx)
    {
        if (!Enabled || !ctx.IsPlayerTarget)
            return false;

        ulong steamId = ctx.SteamId;
        if (!GetPlayerBool(steamId, CLASS_BUFFS_KEY) || !Classes.HasClass(steamId))
            return false;

        ClassManager.PlayerClass playerClass = Classes.GetPlayerClass(steamId);
        return UpdateBuffsBufferDestroyPatch.ClassBuffsSet.TryGetValue(playerClass, out var buffs) && buffs.Contains(ctx.PrefabGuid);
    }

    public bool Handle(UpdateBuffDestroyContext ctx)
    {
        Buffs.TryApplyPermanentBuff(ctx.Target, ctx.PrefabGuid);
        return false;
    }
}

