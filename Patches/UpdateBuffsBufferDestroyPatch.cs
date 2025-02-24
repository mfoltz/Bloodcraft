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
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    static readonly PrefabGUID _combatBuff = new(581443919);
    static readonly PrefabGUID _tauntEmoteBuff = new(-508293388);
    static readonly PrefabGUID _phasingBuff = new(-79611032);
    static readonly PrefabGUID _exoFormBuff = new(-31099041);
    static readonly PrefabGUID _gateBossFeedCompleteBuff = new(-354622715);
    static readonly PrefabGUID _vBloodBloodBuff = new(20081801);
    static readonly PrefabGUID _werewolfStandardBuff = new(-1598161201);
    static readonly PrefabGUID _werewolfVBloodBuff = new(-622259665);
    static readonly PrefabGUID _bossFeedCompleteBuff = new(1233405326);

    static readonly PrefabGUID _gateBossFeedCompleteGroup = new(-1446310610);
    static readonly PrefabGUID _bloodBoltSwarmGroup = new(797450963);

    static readonly PrefabGUID _shroudBuff = new(1504279833);
    static readonly PrefabGUID _shroudCloak = new(1063517722);

    static readonly PrefabGUID _travelStoneBuff = new(-342726392);
    static readonly PrefabGUID _travelWoodenBuff = new(-1194613929);
    static readonly PrefabGUID _insideWoodenCoffin = new(381160212);
    static readonly PrefabGUID _insideStoneCoffin = new(569692162);

    public static readonly List<PrefabGUID> PrestigeBuffs = [];
    public static readonly Dictionary<Classes.PlayerClass, HashSet<PrefabGUID>> ClassBuffsSet = [];
    public static readonly Dictionary<Classes.PlayerClass, List<PrefabGUID>> ClassBuffsOrdered = [];

    static readonly EntityQuery _query = QueryService.UpdateBuffsBufferDestroyQuery;

    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(UpdateBuffsBuffer_Destroy __instance)
    {
        if (!Core._initialized) return;
        else if (!(_familiars || _prestige || _classes)) return;

        NativeArray<Entity> entities = _query.ToEntityArray(Allocator.Temp);
        NativeArray<PrefabGUID> prefabGuids = _query.ToComponentDataArray<PrefabGUID>(Allocator.Temp);
        NativeArray<Buff> buffs = _query.ToComponentDataArray<Buff>(Allocator.Temp);

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                Entity buffTarget = buffs[i].Target;

                // if (!playerCharacterLookup.HasComponent(buffTarget)) continue;
                bool isPlayerTarget = playerCharacterLookup.HasComponent(buffTarget);

                PrefabGUID buffPrefabGuid = prefabGuids[i];
                int buffType = GetBuffType(buffPrefabGuid._Value);

                switch (buffType)
                {
                    case 1 when _exoForm && isPlayerTarget: // ExoForm Buff
                        ulong steamId = buffTarget.GetSteamId();
                        buffTarget.TryApplyBuff(_gateBossFeedCompleteBuff);
                        ExoForm.UpdatePartialExoFormChargeUsed(entity, steamId);
                        break;
                    case 2 when (_legacies || _expertise) && isPlayerTarget: // VBlood Buff
                        buffTarget.TryApplyBuff(_vBloodBloodBuff);
                        break;
                    case 3 when _familiars && isPlayerTarget: // Prevent unending combat music; might have been handled elsewhere by now, noting to check later
                        Entity familiar = Familiars.GetActiveFamiliar(buffTarget);

                        if (familiar.Exists())
                        {
                            buffTarget.With((ref CombatMusicListener_Shared shared) =>
                            {
                                shared.UnitPrefabGuid = PrefabGUID.Empty;
                            });

                            Familiars.TryReturnFamiliar(buffTarget, familiar);
                        }

                        if (_legacies || _expertise)
                        {
                            // Core.Log.LogInfo($"Refreshing stats for {buffTarget.GetSteamId()} after combat...");
                            // Buffs.RefreshStats(buffTarget); // doubling not happening with that patch commented out, interesting...
                        }

                        break;
                    case 4 when _exoForm && isPlayerTarget: // Taunt Emote Buff
                        User user = buffTarget.GetUser();
                        steamId = user.PlatformId;

                        if (GetPlayerBool(steamId, EXO_FORM_KEY))
                        {
                            if (EmoteSystemPatch.ExitingForm.Contains(steamId))
                            {
                                EmoteSystemPatch.ExitingForm.Remove(steamId);
                            }
                            else if (ExoForm.CheckExoFormCharge(user, steamId)) ApplyExoFormBuff(buffTarget);
                        }

                        break;
                    case 5 when isPlayerTarget: // Entering Coffin, cutting for now to see if causing destroy burst error via enter coffin->log out and routine interferes with destruction?
                        user = buffTarget.GetUser();
                        steamId = user.PlatformId;

                        if (_prestige)
                        {
                            SetPlayerBool(steamId, SHROUD_KEY, false);

                            if (buffTarget.HasBuff(_shroudBuff) && buffTarget.TryGetComponent(out Equipment equipment))
                            {
                                if (!equipment.IsEquipped(_shroudCloak, out var _)) buffTarget.TryRemoveBuff(_shroudBuff);
                            }
                        }

                        if (_familiars)
                        {
                            // ServerBootstrapSystemPatches.UnbindFamiliarDelayRoutine(user, buffTarget).Start();
                        }

                        break;
                    case 6 when _prestige && isPlayerTarget: // Leaving Coffin
                        steamId = buffTarget.GetSteamId();

                        SetPlayerBool(steamId, SHROUD_KEY, true);

                        if (PrestigeBuffs.Contains(_shroudBuff) && !buffTarget.HasBuff(_shroudBuff)
                            && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > PrestigeBuffs.IndexOf(_shroudBuff))
                        {
                            Buffs.TryApplyPermanentBuff(buffTarget, _shroudBuff);
                        }

                        break;
                    case 7 when _prestige && isPlayerTarget: // Prestige Buffs
                        steamId = buffTarget.GetSteamId();
                        int index = PrestigeBuffs.IndexOf(buffPrefabGuid);

                        if (buffPrefabGuid.Equals(_shroudBuff) && !GetPlayerBool(steamId, SHROUD_KEY)) 
                        {
                            continue; // allow shroud buff destruction
                        }
                        else if (steamId.TryGetPlayerPrestiges(out prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
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
                            steamId = buffTarget.GetSteamId();

                            if (GetPlayerBool(steamId, CLASS_BUFFS_KEY) && Classes.HasClass(steamId))
                            {
                                Classes.PlayerClass playerClass = Classes.GetPlayerClass(steamId);

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
    static int GetBuffType(int prefabGuid)
    {
        if (PrestigeBuffs.Any(buff => buff.GuidHash.Equals(prefabGuid)))
        {
            return 7;
        }
        else return prefabGuid switch
        {
            -31099041 => 1,                 // ExoForm Buff
            20081801 => 2,                  // VBlood Buff
            581443919 => 3,                 // Combat Buff
            -508293388 => 4,                // Taunt Emote Buff
            // -342726392 or -1194613929 => 5, // Travel Coffin Buffs
            569692162 or 381160212 => 6,    // Inside Coffin Buffs
            -1598161201 or -622259665 => 8, // Werewolf Buffs
            _ => 0,
        };
    }
    static void ApplyExoFormBuff(Entity playerCharacter)
    {
        playerCharacter.TryApplyBuff(_exoFormBuff);
        playerCharacter.TryApplyBuff(_phasingBuff);
        playerCharacter.CastAbility(playerCharacter, _gateBossFeedCompleteGroup);
        playerCharacter.TryApplyBuff(_gateBossFeedCompleteBuff);
    }
}
