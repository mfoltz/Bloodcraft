using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using ProjectM;
using Bloodcraft;

namespace Bloodcraft.Patches.ScriptSpawnServerPatch.Handlers;

class BloodBoltCooldownHandler : IScriptSpawnHandler
{
    static readonly float Cooldown = Shapeshifts.GetShapeshiftAbilityCooldown<EvolvedVampire>(PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_AbilityGroup);
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    public bool CanHandle(ScriptSpawnContext ctx) => ctx.TargetIsPlayer && Cooldown != 0f;

    public void Handle(ScriptSpawnContext ctx)
    {
        ServerGameManager.SetAbilityGroupCooldown(ctx.BuffEntity.GetOwner(), PrefabGUIDs.AB_Vampire_Dracula_BloodBoltSwarm_AbilityGroup, Cooldown);
    }
}
