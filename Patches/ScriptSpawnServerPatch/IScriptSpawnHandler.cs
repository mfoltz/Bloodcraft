namespace Bloodcraft.Patches.ScriptSpawnServerPatch;

interface IScriptSpawnHandler
{
    bool CanHandle(ScriptSpawnContext ctx);
    void Handle(ScriptSpawnContext ctx);
}
