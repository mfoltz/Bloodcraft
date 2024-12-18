﻿using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UpdateBuffsBufferDestroyPatch
{
    static EntityManager EntityManager => Core.EntityManager;

    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID _combatBuff = new(581443919);
    static readonly PrefabGUID _tauntEmoteBuff = new(-508293388);
    static readonly PrefabGUID _phasingBuff = new(-79611032);
    static readonly PrefabGUID _exoFormBuff = new(-31099041);

    static readonly PrefabGUID _shroudBuff = new(1504279833);
    static readonly PrefabGUID _shroudCloak = new(1063517722);

    static readonly PrefabGUID _travelStoneBuff = new(-342726392);
    static readonly PrefabGUID _travelWoodenBuff = new(-1194613929);
    static readonly PrefabGUID _insideWoodenCoffin = new(381160212);
    static readonly PrefabGUID _insideStoneCoffin = new(569692162);

    static readonly bool _prestige = ConfigService.PrestigeSystem;
    static readonly bool _exoPrestige = ConfigService.ExoPrestiging;
    static readonly bool _familiars = ConfigService.FamiliarSystem;

    public static readonly List<PrefabGUID> PrestigeBuffs = [];
    public static readonly Dictionary<LevelingSystem.PlayerClass, List<PrefabGUID>> ClassBuffs = [];

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
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;

                if (_familiars && prefabGUID.Equals(_combatBuff))
                {
                    if (entity.GetBuffTarget().TryGetPlayer(out Entity character))
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(character);

                        if (familiar.Exists())
                        {
                            character.With((ref CombatMusicListener_Shared shared) =>
                            {
                                shared.UnitPrefabGuid = PrefabGUID.Empty;
                            });

                            Familiars.TryReturnFamiliar(character, familiar);
                        }
                    }
                }

                if (_prestige && entity.GetBuffTarget().TryGetPlayer(out Entity player)) // check if need to reapply prestige buff
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    if (PrestigeBuffs.Contains(prefabGUID)) // check if the buff is for prestige and reapply if so
                    {
                        int index = PrestigeBuffs.IndexOf(prefabGUID);

                        if (prefabGUID.Equals(_shroudBuff) && !Misc.GetPlayerBool(steamId, "Shroud")) // allow shroud buff destruction
                        {
                            continue;
                        }
                        else if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {
                            if (prestigeLevel > index) Buffs.ApplyPermanentBuff(player, prefabGUID); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
                        }
                    }
                    else if (_exoPrestige && prefabGUID.Equals(_tauntEmoteBuff) && Misc.GetPlayerBool(steamId, "ExoForm"))
                    {
                        if (EmoteSystemPatch.ExitingForm.Contains(steamId))
                        {
                            EmoteSystemPatch.ExitingForm.Remove(steamId);

                            continue;
                        }
                        else if (ExoForm.CheckExoFormCharge(user, steamId)) ApplyExoFormBuff(player); // could maybe try SpawnPrefabOnGameplayEvent or something like that instead of slingshotting this around, will ponder
                    }
                }

                if (_classes && entity.GetBuffTarget().TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (Classes.HasClass(steamId))
                    {
                        LevelingSystem.PlayerClass playerClass = Classes.GetPlayerClass(steamId);

                        if (ClassBuffs.TryGetValue(playerClass, out List<PrefabGUID> classBuffs) && classBuffs.Contains(prefabGUID)) Buffs.ApplyPermanentBuff(player, prefabGUID);
                    }
                }

                // do log out stuff when travel into coffin buff is destroyed
                if ((prefabGUID.Equals(_travelStoneBuff) || prefabGUID.Equals(_travelWoodenBuff)) && entity.GetBuffTarget().TryGetPlayer(out player))
                {
                    //Core.Log.LogInfo("Entering coffin...");
                    ulong steamId = player.GetSteamId();

                    if (_prestige)
                    {
                        Misc.SetPlayerBool(steamId, "Shroud", false);

                        if (player.TryGetBuff(_shroudBuff, out Entity shroudBuff))
                        {
                            Equipment equipment = player.ReadRO<Equipment>();

                            if (!equipment.IsEquipped(_shroudCloak, out var _)) DestroyUtility.Destroy(EntityManager, shroudBuff, DestroyDebugReason.TryRemoveBuff);
                        }
                    }

                    if (_familiars)
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(player);
                        if (familiar.Exists())
                        {
                            Entity userEntity = player.GetUserEntity();

                            Familiars.UnbindFamiliar(player, userEntity, steamId);
                        }
                    }
                }
                else if ((prefabGUID.Equals(_insideStoneCoffin) || prefabGUID.Equals(_insideWoodenCoffin)) && entity.GetBuffTarget().TryGetPlayer(out player)) // do log in stuff when inside coffin buff is destroyed
                {
                    ulong steamId = player.GetSteamId();

                    if (_prestige)
                    {
                        Misc.SetPlayerBool(steamId, "Shroud", true);

                        if (PrestigeBuffs.Contains(_shroudBuff) && !player.HasBuff(_shroudBuff)
                            && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(_shroudBuff))
                        {
                            Buffs.ApplyPermanentBuff(player, _shroudBuff);
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
    static void ApplyExoFormBuff(Entity character)
    {
        Buffs.TryApplyBuff(character, _exoFormBuff);
        Buffs.TryApplyBuff(character, _phasingBuff);
    }
}
