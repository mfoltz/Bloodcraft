using System;
using Bloodcraft.Patches.ScriptSpawnServerPatch;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using Bloodcraft.Systems.Leveling;
using Bloodcraft;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly EntityQuery _query = QueryService.ScriptSpawnServerQuery;

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        if (!Core._initialized) return;

        using NativeAccessor<Entity> entities = _query.ToEntityArrayAccessor();
        using NativeAccessor<PrefabGUID> prefabGuids = _query.ToComponentDataArrayAccessor<PrefabGUID>();
        using NativeAccessor<Buff> buffs = _query.ToComponentDataArrayAccessor<Buff>();
        using NativeAccessor<EntityOwner> entityOwners = _query.ToComponentDataArrayAccessor<EntityOwner>();

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);
        ComponentLookup<BloodBuff> bloodBuffLookup = __instance.GetComponentLookup<BloodBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;
                Entity owner = entityOwners[i].Owner;
                PrefabGUID prefabGuid = prefabGuids[i];

                if (!buffTarget.Exists()) continue;

                bool targetIsPlayer = playerCharacterLookup.HasComponent(buffTarget);
                bool targetIsFamiliar = blockFeedBuffLookup.HasComponent(buffTarget);
                bool ownerIsFamiliar = blockFeedBuffLookup.HasComponent(owner);
                bool isBloodBuff = bloodBuffLookup.HasComponent(buffEntity);
                bool isDebuff = buffs[i].BuffEffectType.Equals(BuffEffectType.Debuff);

                var ctx = new ScriptSpawnContext
                {
                    BuffEntity = buffEntity,
                    Target = buffTarget,
                    Owner = owner,
                    PrefabGuid = prefabGuid,
                    TargetIsPlayer = targetIsPlayer,
                    TargetIsFamiliar = targetIsFamiliar,
                    OwnerIsFamiliar = ownerIsFamiliar,
                    IsBloodBuff = isBloodBuff,
                    IsDebuff = isDebuff
                };

                foreach (var handler in ScriptSpawnHandlerRegistry.Resolve(prefabGuid.GuidHash))
                {
                    if (handler.CanHandle(ctx))
                    {
                        handler.Handle(ctx);
                    }
                }

                foreach (var handler in ScriptSpawnHandlerRegistry.GlobalHandlers)
                {
                    if (handler.CanHandle(ctx))
                    {
                        handler.Handle(ctx);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[ScriptSpawnServer.OnUpdatePrefix] - {e}");
        }
    }

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(ScriptSpawnServer __instance)
    {
        if (!Core._initialized) return;
        else if (!_leveling) return;

        using NativeAccessor<Entity> entities = _query.ToEntityArrayAccessor();
        using NativeAccessor<Buff> buffs = _query.ToComponentDataArrayAccessor<Buff>();

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;

                if (buffEntity.HasSpellLevel() && buffTarget.IsPlayer())
                {
                    LevelingSystem.SetLevel(buffTarget);
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[ScriptSpawnServer.OnUpdatePostfix] - {e}");
        }
    }
}
