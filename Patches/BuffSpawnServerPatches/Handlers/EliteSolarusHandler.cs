using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class EliteSolarusHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID GateBossFeedCompleteBuff = Buffs.GateBossFeedCompleteBuff;
    static readonly PrefabGUID HolyBeamPowerBuff = Buffs.HolyBeamPowerBuff;
    static readonly PrefabGUID Solarus = PrefabGUIDs.CHAR_ChurchOfLight_Paladin_VBlood;

    public bool CanHandle(BuffSpawnContext ctx)
        => ConfigService.EliteShardBearers && ctx.PrefabGuid.Equals(GateBossFeedCompleteBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.Target.GetPrefabGuid().Equals(Solarus))
            return;

        if (ctx.Target.HasBuff(HolyBeamPowerBuff) || ctx.BlockFeedLookup.HasComponent(ctx.Target))
            return;

        if (ctx.Target.TryApplyAndGetBuff(HolyBeamPowerBuff, out Entity buffEntity))
        {
            if (buffEntity.Has<LifeTime>())
            {
                buffEntity.With((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 0f;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                });
            }
        }
    }
}
