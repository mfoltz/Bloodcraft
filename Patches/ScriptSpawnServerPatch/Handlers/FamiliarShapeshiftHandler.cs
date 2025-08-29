using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Unity.Entities;
using Bloodcraft;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;

class FamiliarShapeshiftHandler : IScriptSpawnHandler
{
    static readonly bool FamiliarsEnabled = ConfigService.FamiliarSystem;

    public bool CanHandle(ScriptSpawnContext ctx) => FamiliarsEnabled && ctx.TargetIsFamiliar;

    public void Handle(ScriptSpawnContext ctx)
    {
        if (ctx.Target.TryGetFollowedPlayer(out Entity playerCharacter))
        {
            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
            if (familiar.Exists())
            {
                Familiars.HandleFamiliarShapeshiftRoutine(playerCharacter.GetUser(), playerCharacter, familiar).Start();
            }
        }
    }
}
