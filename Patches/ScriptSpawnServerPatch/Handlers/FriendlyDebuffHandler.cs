using Bloodcraft.Services;
using Bloodcraft;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;

class FriendlyDebuffHandler : IScriptSpawnHandler
{
    static readonly bool FamiliarsEnabled = ConfigService.FamiliarSystem;

    public bool CanHandle(ScriptSpawnContext ctx) => FamiliarsEnabled && ctx.IsDebuff && ctx.OwnerIsFamiliar && ctx.Owner.IsAllied(ctx.Target);

    public void Handle(ScriptSpawnContext ctx)
    {
        ctx.BuffEntity.Destroy();
    }
}
