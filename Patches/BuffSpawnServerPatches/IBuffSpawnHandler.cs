namespace Bloodcraft.Patches.BuffSpawnServerPatches;

interface IBuffSpawnHandler
{
    bool CanHandle(BuffSpawnContext ctx);
    void Handle(BuffSpawnContext ctx);
}
