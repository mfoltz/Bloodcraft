using Bloodcraft.Utilities;
using ProjectM;
using System;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class UseRelicHandler : IBuffSpawnHandler
{
    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.PrefabName.Contains("userelic", StringComparison.OrdinalIgnoreCase);

    public void Handle(BuffSpawnContext ctx)
    {
        if (!ctx.Familiars || !ctx.IsPlayer)
            return;

        Entity familiar = Familiars.GetActiveFamiliar(ctx.Target);
        if (familiar.Exists())
        {
            familiar.TryApplyBuff(ctx.BuffEntity.GetPrefabGuid());
        }
    }
}
