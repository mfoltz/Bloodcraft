using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS.Handlers;

sealed class CombatMusicCleanupHandler : IBuffDestroyHandler
{
    static readonly bool Enabled = ConfigService.FamiliarSystem;
    static readonly PrefabGUID CombatBuff = Buffs.PvECombatBuff;

    public bool CanHandle(UpdateBuffDestroyContext ctx)
    {
        return Enabled && ctx.IsPlayerTarget && ctx.PrefabGuid.Equals(CombatBuff);
    }

    public bool Handle(UpdateBuffDestroyContext ctx)
    {
        Entity familiar = Familiars.GetActiveFamiliar(ctx.Target);
        if (familiar.Exists())
        {
            ctx.Target.With((ref CombatMusicListener_Shared shared) =>
            {
                shared.UnitPrefabGuid = PrefabGUID.Empty;
            });
            Familiars.TryReturnFamiliar(ctx.Target, familiar);
        }
        return false;
    }
}

