using System.Collections.Generic;
using Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

namespace Bloodcraft.Patches.BuffSpawnServerPatches;

static class BuffSpawnHandlerRegistry
{
    static readonly List<IBuffSpawnHandler> _handlers = new()
    {
        new EliteSolarusHandler(),
        new TrueImmortalHandler(),
        new PveCombatHandler(),
        new PvpCombatHandler(),
        new VampiricCurseHandler(),
        new WitchPigTransformationHandler(),
        new PhasingHandler(),
        new HighlordSwordHandler(),
        new InkCrawlerDeathHandler(),
        new ConsumableHandler(),
        new AggroEmoteHandler(),
        new UseRelicHandler(),
        new CombatStanceHandler(),
        new DraculaReturnHideHandler(),
        new BatLandingHandler(),
        new DefaultBuffHandler()
    };

    public static IEnumerable<IBuffSpawnHandler> Handlers => _handlers;
}
