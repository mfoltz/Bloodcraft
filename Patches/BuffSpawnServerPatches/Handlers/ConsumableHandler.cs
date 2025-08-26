using System;
using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Patches.BuffSpawnServerPatches.Handlers;

sealed class ConsumableHandler : IBuffSpawnHandler
{
    const int MaxProfessionLevel = ProfessionSystem.MAX_PROFESSION_LEVEL;
    static readonly PrefabGUID WranglerPotionBuff = Buffs.WranglerPotionBuff;

    public bool CanHandle(BuffSpawnContext ctx)
        => ctx.PrefabName.Contains("consumable", StringComparison.OrdinalIgnoreCase) ||
           ctx.PrefabName.Contains("elixir", StringComparison.OrdinalIgnoreCase);

    public void Handle(BuffSpawnContext ctx)
    {
        if (ctx.IsPlayer)
        {
            ApplyConsumableToPlayer(ctx);
        }
        else if (ctx.Target.TryGetFollowedPlayer(out Entity playerChar))
        {
            ApplyConsumableToFollower(ctx, playerChar);
        }
    }

    void ApplyConsumableToPlayer(BuffSpawnContext ctx)
    {
        if (ConfigService.PotionStacking && !ctx.PrefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
        {
            ctx.BuffEntity.Remove<RemoveBuffOnGameplayEvent>();
            ctx.BuffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
        }

        if (ConfigService.ProfessionSystem)
        {
            IProfession handler = ProfessionFactory.GetProfession(ctx.PrefabGuid);
            int level = handler.GetProfessionLevel(ctx.SteamId);
            float bonus = 1 + level / (float)MaxProfessionLevel;

            ctx.BuffEntity.With((ref LifeTime lifeTime) =>
            {
                if (!lifeTime.EndAction.Equals(LifeTimeEndAction.None))
                {
                    lifeTime.Duration *= bonus;
                }
            });

            if (!ctx.PrefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase) &&
                ctx.BuffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var statBuffer) && !statBuffer.IsEmpty)
            {
                for (int j = 0; j < statBuffer.Length; ++j)
                {
                    var statBuff = statBuffer[j];
                    statBuff.Value *= bonus;
                    statBuffer[j] = statBuff;
                }
            }

            if (ctx.BuffEntity.Has<HealOnGameplayEvent>() && ctx.BuffEntity.TryGetBuffer<CreateGameplayEventsOnTick>(out var tickBuffer) && !tickBuffer.IsEmpty)
            {
                var eventsOnTick = tickBuffer[0];
                eventsOnTick.MaxTicks = (int)(eventsOnTick.MaxTicks * bonus);
                tickBuffer[0] = eventsOnTick;
            }
        }

        if (ConfigService.FamiliarSystem && !ctx.PrefabGuid.Equals(WranglerPotionBuff))
        {
            Entity familiar = Familiars.GetActiveFamiliar(ctx.Target);
            if (familiar.Exists())
            {
                if (ctx.BuffEntity.Has<HealOnGameplayEvent>() && familiar.IsDisabled())
                    return;
                familiar.TryApplyBuff(ctx.PrefabGuid);
            }
        }
    }

    void ApplyConsumableToFollower(BuffSpawnContext ctx, Entity playerChar)
    {
        if (ConfigService.PotionStacking && !ctx.PrefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
        {
            ctx.BuffEntity.Remove<RemoveBuffOnGameplayEvent>();
            ctx.BuffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
        }

        if (ConfigService.FamiliarSystem && !ctx.PrefabGuid.Equals(WranglerPotionBuff))
        {
            Entity familiar = Familiars.GetActiveFamiliar(playerChar);
            if (familiar.Exists())
            {
                if (ctx.BuffEntity.Has<HealOnGameplayEvent>() && familiar.IsDisabled())
                    return;
                familiar.TryApplyBuff(ctx.PrefabGuid);
            }
        }
    }
}
