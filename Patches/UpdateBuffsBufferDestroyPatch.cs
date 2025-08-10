using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UpdateBuffsBufferDestroyPatch
{
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _shouldHandleStats = _legacies || _expertise || _classes || _leveling || _familiars;

    static readonly PrefabGUID _combatBuff = Buffs.PvECombatBuff;
    static readonly PrefabGUID _tauntEmoteBuff = Buffs.TauntEmoteBuff;
    static readonly PrefabGUID _phasingBuff = Buffs.PhasingBuff;
    static readonly PrefabGUID _evolvedVampireFormBuff = Buffs.EvolvedVampireBuff;
    static readonly PrefabGUID _gateBossFeedCompleteBuff = Buffs.GateBossFeedCompleteBuff;
    static readonly PrefabGUID _standardWerewolfBuff = Buffs.StandardWerewolfBuff;
    static readonly PrefabGUID _vBloodWerewolfBuff = Buffs.VBloodWerewolfBuff;
    static readonly PrefabGUID _vanishBuff = Buffs.VanishBuff;

    static readonly PrefabGUID _gateBossFeedCompleteGroup = PrefabGUIDs.AB_FeedGateBoss_03_Complete_AbilityGroup;

    static readonly PrefabGUID _shroudBuff = Buffs.ShroudBuff;
    static readonly PrefabGUID _shroudCloak = PrefabGUIDs.Item_Cloak_Main_ShroudOfTheForest;

    static readonly PrefabGUID _travelStoneBuff = new(-342726392);
    static readonly PrefabGUID _travelWoodenBuff = new(-1194613929);
    static readonly PrefabGUID _insideWoodenCoffin = new(381160212);
    static readonly PrefabGUID _insideStoneCoffin = new(569692162);

    static readonly PrefabGUID _bonusStatsBuff = Buffs.BonusStatsBuff;

    public static readonly List<PrefabGUID> PrestigeBuffs = [];
    public static readonly Dictionary<ClassManager.PlayerClass, HashSet<PrefabGUID>> ClassBuffsSet = [];
    public static readonly Dictionary<ClassManager.PlayerClass, List<PrefabGUID>> ClassBuffsOrdered = [];

    static readonly EntityQuery _query = QueryService.UpdateBuffsBufferDestroyQuery;

    /*
    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(UpdateBuffsBuffer_Destroy __instance)
    {
        if (!Core._initialized) return;
        else if (!(_familiars || _prestige || _classes)) return;

        NativeArray<Entity> entities = _query.ToEntityArray(Allocator.Temp);

        NativeArray<PrefabGUID> prefabGuids = _query.ToComponentDataArray<PrefabGUID>(Allocator.Temp);
        NativeArray<Buff> buffs = _query.ToComponentDataArray<Buff>(Allocator.Temp);

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities.get_Item(i);
                Entity buffTarget = buffs.get_Item(i).Target;

                bool isPlayerTarget = playerCharacterLookup.HasComponent(buffTarget);

                PrefabGUID buffPrefabGuid = prefabGuids.get_Item(i);
                int buffType = GetBuffType(buffPrefabGuid);

                // Core.Log.LogWarning($"[UpdateBuffsBufferDestroyPatch] - {buffPrefabGuid.GetPrefabName()}");

                }
            }
        }
    }
    */

    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostfix(UpdateBuffsBuffer_Destroy __instance)
    {
        if (!Core.IsReady) return;
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
                string prefabName = buffPrefabGuid.GetPrefabName();
                int buffType = GetBuffType(buffPrefabGuid, isWeaponEquipBuff, isPlayerTarget, isFamiliarTarget, isBloodBuff);

                // if (!prefabName.Contains("AntennaBuff")) Core.Log.LogWarning($"[UpdateBuffsBuffer_Destroy] - {buffTarget.GetPrefabGuid().GetPrefabName()} | {prefabName}");

                switch (buffType)
                {
                    case 1 when _exoForm && isPlayerTarget: // ExoForm Buff
                        ulong steamId = buffTarget.GetSteamId();
                        buffTarget.TryApplyBuff(_gateBossFeedCompleteBuff);
                        Shapeshifts.UpdatePartialExoFormChargeUsed(entity, steamId);
                        break;
                    case 2:
                        // Core.Log.LogWarning($"[UpdateBuffsBufferDestroyPatch] Triggering stat refresh - {buffTarget.GetPrefabGuid().GetPrefabName()}");
                        // if (playerCharacterLookup.HasComponent(buffTarget)) buffTarget.TryApplyBuff(_bonusStatsBuff);
                        // if (blockFeedBuffLookup.HasComponent(buffTarget) && !buffTarget.HasBuff(_vanishBuff)) buffTarget.TryApplyBuff(_bonusStatsBuff);
                        break;
                    case 3 when _familiars && isPlayerTarget: // Prevent unending combat music; might have been handled elsewhere by now, noting to check later
                        Entity familiar = Familiars.GetActiveFamiliar(buffTarget);
                        if (familiar.Exists())
                        {
                            buffTarget.With((ref CombatMusicListener_Shared shared) => shared.UnitPrefabGuid = PrefabGUID.Empty);
                            Familiars.TryReturnFamiliar(buffTarget, familiar);
                        }
                        break;
                    case 4 when isPlayerTarget: // Taunt Emote Buff
                        User user = buffTarget.GetUser();
                        steamId = user.PlatformId;
                        if (GetPlayerBool(steamId, SHAPESHIFT_KEY))
                        {
                            if (EmoteSystemPatch.BlockShapeshift.Contains(steamId))
                            {
                                EmoteSystemPatch.BlockShapeshift.Remove(steamId);
                            }
                            else if (Shapeshifts.CheckExoFormCharge(user, steamId)) ApplyShapeshiftBuff(steamId, buffTarget);
                            // else ApplyShapeshiftBuff(steamId, buffTarget);
                        }
                        break;
                    case 7 when _prestige && isPlayerTarget: // Prestige Buffs
                        steamId = buffTarget.GetSteamId();
                        int index = PrestigeBuffs.IndexOf(buffPrefabGuid);

                        if (buffPrefabGuid.Equals(_shroudBuff) && !GetPlayerBool(steamId, SHROUD_KEY))
                        {
                            continue; // allow shroud buff destruction
                        }
                        else if (!GetPlayerBool(steamId, PRESTIGE_BUFFS_KEY))
                        {
                            continue;
                        }
                        else if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {
                            if (prestigeLevel > index) Buffs.TryApplyPermanentBuff(buffTarget, buffPrefabGuid); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
                        }
                        break;
                    case 8 when _familiars:
                        if (buffTarget.TryGetFollowedPlayer(out Entity playerCharacter))
                        {
                            familiar = Familiars.GetActiveFamiliar(playerCharacter);
                            if (familiar.Exists()) Familiars.HandleFamiliarShapeshiftRoutine(playerCharacter.GetUser(), playerCharacter, familiar).Start();
                        }
                        break;
                    default: // class buffs otherwise, probably merits a case for switch but later
                        if (_classes && isPlayerTarget)
                        {
                            /*
                            steamId = buffTarget.GetSteamId();
                            if (GetPlayerBool(steamId, CLASS_BUFFS_KEY) && Classes.HasClass(steamId))
                            {
                                ClassManager.PlayerClass playerClass = Classes.GetPlayerClass(steamId);

                                if (ClassBuffsSet.TryGetValue(playerClass, out HashSet<PrefabGUID> classBuffs) && classBuffs.Contains(buffPrefabGuid))
                                {
                                    // Core.Log.LogInfo($"Preventing destruction for {playerClass} - {buffPrefabGuid.GetPrefabName()}");
                                    Buffs.TryApplyPermanentBuff(buffTarget, buffPrefabGuid);
                                }
                            }
                            else
                            {
                                continue; // allow class buff destruction
                            }
                            */
                        }
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
    static int GetBuffType(PrefabGUID prefabGuid, bool isWeaponEquipBuff, bool isPlayerTarget, bool isFamiliarTarget, bool isBloodBuff)
    {
        int guidHash = prefabGuid.GuidHash;

        if ((isWeaponEquipBuff || isBloodBuff) && (isPlayerTarget || isFamiliarTarget))
        {
            return 5; // Handle Stats
        }
        /*
        else if (prefabGuid.Equals(_bonusStatsBuff))
        {
            // return 2;
        }
        */
        else if (PrestigeBuffs.Contains(prefabGuid))
        {
            return 7;
        }
        else return guidHash switch
        {
            -31099041 or -1859425781 => 1,     // ExoForm Buffs
            // 20081801 => 2,                  // bonus stats testing
            // 737485591 => 2,                 // Set bonus buff trying for bonus stats
            581443919 => 3,                    // Combat Buff
            -508293388 => 4,                   // Taunt Emote Buff
            -1598161201 or -622259665 => 8,    // Werewolf Buffs
            _ => 0,
        };
    }
    static void ApplyShapeshiftBuff(ulong steamId, Entity playerCharacter)
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
