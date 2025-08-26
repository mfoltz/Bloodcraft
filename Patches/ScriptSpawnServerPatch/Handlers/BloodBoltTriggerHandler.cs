using ProjectM;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;

class BloodBoltTriggerHandler : IScriptSpawnHandler
{
    public bool CanHandle(ScriptSpawnContext ctx) => ctx.TargetIsPlayer;

    public void Handle(ScriptSpawnContext ctx)
    {
        ctx.BuffEntity.Remove<ScriptSpawn>();
        ctx.BuffEntity.Remove<Script_ApplyBuffOnAggroListTarget_DataServer>();
    }
}
