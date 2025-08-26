using Bloodcraft.Utilities;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;

class ShapeshiftAdjustmentHandler : IScriptSpawnHandler
{
    public bool CanHandle(ScriptSpawnContext ctx) => ctx.TargetIsPlayer;

    public void Handle(ScriptSpawnContext ctx)
    {
        Shapeshifts.ModifyShapeshiftBuff(ctx.BuffEntity, ctx.Target, ctx.PrefabGuid);
    }
}
