using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class InkCrawlerDeathHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID InkCrawlerDeathBuff = Buffs.InkCrawlerDeathBuff;
    const float MinionLifetime = 30f;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.Familiars && ctx.PrefabGuid.Equals(InkCrawlerDeathBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.Target.TryGetFollowedPlayer(out _))
            return;

        ctx.BuffEntity.With((ref LifeTime lifeTime) =>
        {
            lifeTime.Duration = MinionLifetime;
            lifeTime.EndAction = LifeTimeEndAction.Destroy;
        });
    }
}
