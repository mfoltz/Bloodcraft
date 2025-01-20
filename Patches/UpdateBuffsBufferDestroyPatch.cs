using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UpdateBuffsBufferDestroyPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _legacies = ConfigService.BloodSystem;
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

    static readonly PrefabGUID _gateBossFeedCompleteGroup = new(-1446310610);

    static readonly PrefabGUID _shroudBuff = new(1504279833);
    static readonly PrefabGUID _shroudCloak = new(1063517722);

    static readonly PrefabGUID _travelStoneBuff = new(-342726392);
    static readonly PrefabGUID _travelWoodenBuff = new(-1194613929);
    static readonly PrefabGUID _insideWoodenCoffin = new(381160212);
    static readonly PrefabGUID _insideStoneCoffin = new(569692162);

    public static readonly List<PrefabGUID> PrestigeBuffs = [];
    public static readonly Dictionary<Classes.PlayerClass, List<PrefabGUID>> ClassBuffs = [];

    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(UpdateBuffsBuffer_Destroy __instance)
    {
        if (!Core._initialized) return;
        else if (!(_familiars || _prestige || _classes)) return;

        NativeArray<Entity> entities = __instance.__query_401358720_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGuid)) continue;

                if (_exoForm && prefabGuid.Equals(_exoFormBuff))
                {
                    Entity buffTarget = entity.GetBuffTarget();

                    if (buffTarget.IsPlayer())
                    {
                        ulong steamId = buffTarget.GetSteamId();

                        buffTarget.TryApplyBuff(_gateBossFeedCompleteBuff);
                        DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.TryRemoveBuff);

                        ExoForm.UpdatePartialExoFormChargeUsed(entity, steamId);
                        continue;
                    }
                }

                if (_legacies && prefabGuid.Equals(_vBloodBloodBuff) && entity.GetBuffTarget().TryGetPlayer(out Entity playerCharacter))
                {

                }

                if (_familiars && prefabGuid.Equals(_combatBuff) && entity.GetBuffTarget().TryGetPlayer(out playerCharacter))
                {
                    Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                    if (familiar.Exists())
                    {
                        playerCharacter.With((ref CombatMusicListener_Shared shared) =>
                        {
                            shared.UnitPrefabGuid = PrefabGUID.Empty;
                        });

                        Familiars.TryReturnFamiliar(playerCharacter, familiar);
                    }
                }

                if (_prestige && entity.GetBuffTarget().TryGetPlayer(out playerCharacter)) // check if need to reapply prestige buff
                {
                    User user = playerCharacter.GetUser();
                    ulong steamId = user.PlatformId;

                    if (PrestigeBuffs.Contains(prefabGuid)) // check if the buff is for prestige and reapply if so
                    {
                        int index = PrestigeBuffs.IndexOf(prefabGuid);

                        if (prefabGuid.Equals(_shroudBuff) && !GetPlayerBool(steamId, SHROUD_KEY)) // allow shroud buff destruction
                        {
                            continue;
                        }
                        else if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {
                            if (prestigeLevel > index) Buffs.ApplyPermanentBuff(playerCharacter, prefabGuid); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
                        }
                    }
                    else if (_exoForm && prefabGuid.Equals(_tauntEmoteBuff) && GetPlayerBool(steamId, EXO_FORM_KEY))
                    {
                        if (EmoteSystemPatch.ExitingForm.Contains(steamId))
                        {
                            EmoteSystemPatch.ExitingForm.Remove(steamId);

                            continue;
                        }
                        else if (ExoForm.CheckExoFormCharge(user, steamId)) ApplyExoFormBuff(playerCharacter); // could maybe try SpawnPrefabOnGameplayEvent or something like that instead of slingshotting this around, will ponder
                    }
                }

                if (_classes && entity.GetBuffTarget().TryGetPlayer(out playerCharacter))
                {
                    ulong steamId = playerCharacter.GetSteamId();

                    if (Classes.HasClass(steamId))
                    {
                        Classes.PlayerClass playerClass = Classes.GetPlayerClass(steamId);

                        if (ClassBuffs.TryGetValue(playerClass, out List<PrefabGUID> classBuffs) && classBuffs.Contains(prefabGuid)) Buffs.ApplyPermanentBuff(playerCharacter, prefabGuid);
                    }
                }

                if ((prefabGuid.Equals(_travelStoneBuff) || prefabGuid.Equals(_travelWoodenBuff)) && entity.GetBuffTarget().TryGetPlayer(out playerCharacter))
                {
                    User user = playerCharacter.GetUser();
                    ulong steamId = user.PlatformId;

                    if (_prestige)
                    {
                        SetPlayerBool(steamId, SHROUD_KEY, false);

                        if (playerCharacter.HasBuff(_shroudBuff) && playerCharacter.TryGetComponent(out Equipment equipment))
                        {
                            if (!equipment.IsEquipped(_shroudCloak, out var _)) playerCharacter.TryRemoveBuff(_shroudBuff);
                        }
                    }

                    if (_familiars)
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            Familiars.UnbindFamiliar(user, playerCharacter);
                        }
                    }
                }
                else if ((prefabGuid.Equals(_insideStoneCoffin) || prefabGuid.Equals(_insideWoodenCoffin)) && entity.GetBuffTarget().TryGetPlayer(out playerCharacter)) // do log in stuff when inside coffin buff is destroyed
                {
                    ulong steamId = playerCharacter.GetSteamId();

                    if (_prestige)
                    {
                        SetPlayerBool(steamId, SHROUD_KEY, true);

                        if (PrestigeBuffs.Contains(_shroudBuff) && !playerCharacter.HasBuff(_shroudBuff)
                            && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(_shroudBuff))
                        {
                            Buffs.ApplyPermanentBuff(playerCharacter, _shroudBuff);
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
    static void ApplyExoFormBuff(Entity playerCharacter)
    {
        playerCharacter.TryApplyBuff(_exoFormBuff);
        playerCharacter.TryApplyBuff(_phasingBuff); // check if need this here for the entire visual effect after transforming
        playerCharacter.CastAbility(playerCharacter, _gateBossFeedCompleteGroup);
        playerCharacter.TryApplyBuff(_gateBossFeedCompleteBuff);
    }
}
