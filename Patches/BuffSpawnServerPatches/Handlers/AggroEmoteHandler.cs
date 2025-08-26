using System;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBools;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class AggroEmoteHandler : IBuffSpawnHandler
{
    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.PrefabName.Contains("emote_onaggro", StringComparison.OrdinalIgnoreCase);

    public void Handle(BuffSpawnContext ctx)
    {
        if (ConfigService.FamiliarSystem && ctx.Target.TryGetFollowedPlayer(out Entity player) && !GetPlayerBool(player.GetSteamId(), VBLOOD_EMOTES_KEY))
        {
            ctx.BuffEntity.Destroy();
        }
    }
}
