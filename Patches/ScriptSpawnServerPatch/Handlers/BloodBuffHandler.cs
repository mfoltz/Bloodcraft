using Bloodcraft.Services;
using Bloodcraft.Utilities;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;

class BloodBuffHandler : IScriptSpawnHandler
{
    static readonly bool LegaciesEnabled = ConfigService.LegacySystem;

    public bool CanHandle(ScriptSpawnContext ctx) => LegaciesEnabled && ctx.IsBloodBuff;

    public void Handle(ScriptSpawnContext ctx)
    {
        Buffs.RefreshStats(ctx.Target);
    }
}
