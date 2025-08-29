using System.Collections.Generic;
using Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS.Handlers;

namespace Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS;

static class UpdateBuffDestroyHandlerRegistry
{
    static readonly List<IBuffDestroyHandler> _handlers = new()
    {
        new ExoFormHandler(),
        new CombatMusicCleanupHandler(),
        new TauntEmoteHandler(),
        new PrestigeBuffHandler(),
        new FamiliarShapeshiftHandler(),
        new ClassBuffHandler()
    };

    public static IEnumerable<IBuffDestroyHandler> Handlers => _handlers;
}

