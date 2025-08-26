using System.Collections.Generic;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using Bloodcraft.Patches.UpdateBuffsBufferDestroyPatchNS;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UpdateBuffsBufferDestroyPatch
{
    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    static readonly PrefabGUID _phasingBuff = Buffs.PhasingBuff;
    static readonly PrefabGUID _gateBossFeedCompleteBuff = Buffs.GateBossFeedCompleteBuff;
    static readonly PrefabGUID _gateBossFeedCompleteGroup = PrefabGUIDs.AB_FeedGateBoss_03_Complete_AbilityGroup;

    internal static readonly List<PrefabGUID> PrestigeBuffs = [];
    internal static readonly Dictionary<ClassManager.PlayerClass, HashSet<PrefabGUID>> ClassBuffsSet = [];
    internal static readonly Dictionary<ClassManager.PlayerClass, List<PrefabGUID>> ClassBuffsOrdered = [];

    static readonly EntityQuery _query = QueryService.UpdateBuffsBufferDestroyQuery;

    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(UpdateBuffsBuffer_Destroy __instance)
    {
        if (!Core._initialized) return;
        else if (!(_familiars || _prestige || _classes)) return;

        NativeArray<Entity> entities = _query.ToEntityArray(Allocator.Temp);
        NativeArray<PrefabGUID> prefabGuids = _query.ToComponentDataArray<PrefabGUID>(Allocator.Temp);
        NativeArray<Buff> buffs = _query.ToComponentDataArray<Buff>(Allocator.Temp);

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);
        ComponentLookup<WeaponLevel> weaponLevelLookup = __instance.GetComponentLookup<WeaponLevel>(true);
        ComponentLookup<BloodBuff> bloodBuffLookup = __instance.GetComponentLookup<BloodBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                Entity buffTarget = buffs[i].Target;

                bool isPlayerTarget = playerCharacterLookup.HasComponent(buffTarget);
                bool isFamiliarTarget = blockFeedBuffLookup.HasComponent(buffTarget);
                bool isWeaponEquipBuff = weaponLevelLookup.HasComponent(entity);
                bool isBloodBuff = bloodBuffLookup.HasComponent(entity);

                PrefabGUID buffPrefabGuid = prefabGuids[i];
                ulong steamId = isPlayerTarget ? buffTarget.GetSteamId() : 0;

                var ctx = new UpdateBuffDestroyContext
                {
                    BuffEntity = entity,
                    Target = buffTarget,
                    PrefabGuid = buffPrefabGuid,
                    IsPlayerTarget = isPlayerTarget,
                    IsFamiliarTarget = isFamiliarTarget,
                    IsWeaponEquipBuff = isWeaponEquipBuff,
                    IsBloodBuff = isBloodBuff,
                    SteamId = steamId
                };

                foreach (IBuffDestroyHandler handler in UpdateBuffDestroyHandlerRegistry.Handlers)
                {
                    if (!handler.CanHandle(ctx))
                        continue;

                    bool shouldContinue = handler.Handle(ctx);
                    if (!shouldContinue)
                        break;
                }
            }
        }
        finally
        {
            entities.Dispose();
            prefabGuids.Dispose();
            buffs.Dispose();
        }
    }

    internal static void ApplyShapeshiftBuff(ulong steamId, Entity playerCharacter)
    {
        if (!Shapeshifts.ShapeshiftCache.TryGetShapeshiftBuff(steamId, out PrefabGUID shapeshiftBuff))
        {
            Core.Log.LogWarning($"Shapeshift buff not found for {steamId}");
            return;
        }

        playerCharacter.TryApplyBuff(shapeshiftBuff);
        playerCharacter.TryApplyBuff(_phasingBuff);
        playerCharacter.CastAbility(_gateBossFeedCompleteGroup);
        playerCharacter.TryApplyBuff(_gateBossFeedCompleteBuff);
    }
}

