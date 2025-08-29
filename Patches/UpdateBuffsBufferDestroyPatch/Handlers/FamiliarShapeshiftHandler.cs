using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS.Handlers;

sealed class FamiliarShapeshiftHandler : IBuffDestroyHandler
{
    static readonly bool Enabled = ConfigService.FamiliarSystem;
    static readonly PrefabGUID StandardWerewolfBuff = Buffs.StandardWerewolfBuff;
    static readonly PrefabGUID VBloodWerewolfBuff = Buffs.VBloodWerewolfBuff;

    public bool CanHandle(UpdateBuffDestroyContext ctx)
    {
        return Enabled && (ctx.PrefabGuid.Equals(StandardWerewolfBuff) || ctx.PrefabGuid.Equals(VBloodWerewolfBuff));
    }

    public bool Handle(UpdateBuffDestroyContext ctx)
    {
        if (ctx.Target.TryGetFollowedPlayer(out Entity playerCharacter))
        {
            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
            if (familiar.Exists())
            {
                Familiars.HandleFamiliarShapeshiftRoutine(playerCharacter.GetUser(), playerCharacter, familiar).Start();
            }
        }
        return false;
    }
}

