using Bloodcraft.Resources;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class TrueImmortalHandler : IBuffSpawnHandler
{
    static readonly PrefabGUID BloodCurseBuff = Buffs.DraculaBloodCurseBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.TrueImmortal && ctx.IsPlayer && ctx.PrefabGuid.Equals(BloodCurseBuff);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.BuffEntity.Has<SpellTarget>())
            return;

        Entity spellTarget = ctx.BuffEntity.GetSpellTarget();
        if (!spellTarget.IsVBloodOrGateBoss())
        {
            Shapeshifts.TrueImmortal(ctx.BuffEntity, ctx.Target);
        }
    }
}
