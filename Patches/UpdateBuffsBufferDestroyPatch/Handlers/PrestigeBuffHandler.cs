using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBools;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS.Handlers;

sealed class PrestigeBuffHandler : IBuffDestroyHandler
{
    static readonly bool Enabled = ConfigService.PrestigeSystem;
    static readonly PrefabGUID ShroudBuff = Buffs.ShroudBuff;

    public bool CanHandle(UpdateBuffDestroyContext ctx)
    {
        return Enabled && ctx.IsPlayerTarget && UpdateBuffsBufferDestroyPatch.PrestigeBuffs.Contains(ctx.PrefabGuid);
    }

    public bool Handle(UpdateBuffDestroyContext ctx)
    {
        ulong steamId = ctx.SteamId;
        PrefabGUID buffGuid = ctx.PrefabGuid;
        if (buffGuid.Equals(ShroudBuff) && !GetPlayerBool(steamId, SHROUD_KEY))
        {
            return false;
        }
        if (!GetPlayerBool(steamId, PRESTIGE_BUFFS_KEY))
        {
            return false;
        }
        if (steamId.TryGetPlayerPrestiges(out var prestigeData) &&
            prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
        {
            int index = UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(buffGuid);
            if (prestigeLevel > index)
            {
                Buffs.TryApplyPermanentBuff(ctx.Target, buffGuid);
            }
        }
        return false;
    }
}

