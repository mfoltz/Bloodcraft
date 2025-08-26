using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class HighlordSwordHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID HighlordGroundSwordBossBuff = Buffs.HighlordGroundSwordBossBuff;
    const float MinionLifetime = 30f;

    public bool CanHandle(BuffSpawnContext ctx)
        => ConfigService.FamiliarSystem && ctx.PrefabGuid.Equals(HighlordGroundSwordBossBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.Target.TryGetFollowedPlayer(out Entity playerChar))
            return;

        Entity familiar = Familiars.GetActiveFamiliar(playerChar);
        if (familiar.Exists() && familiar.TryGetBuff(HighlordGroundSwordBossBuff, out Entity buff))
        {
            buff.AddWith((ref AmplifyBuff amplify) => amplify.AmplifyModifier = -0.75f);
            buff.AddWith((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = MinionLifetime;
                lifeTime.EndAction = LifeTimeEndAction.Destroy;
            });
        }
    }
}
