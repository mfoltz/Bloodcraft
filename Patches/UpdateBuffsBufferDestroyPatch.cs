using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TextCore.Text;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UpdateBuffsBufferDestroyPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    static readonly PrefabGUID CombatBuff = new(581443919);
    static readonly PrefabGUID TauntEmoteBuff = new(-508293388);
    static readonly PrefabGUID PhasingBuff = new(-79611032);
    static readonly PrefabGUID ExoFormBuff = new(-31099041);

    static readonly PrefabGUID ShroudBuff = new(1504279833);
    static readonly PrefabGUID ShroudCloak = new(1063517722);

    static readonly PrefabGUID TravelStoneBuff = new(-342726392);
    static readonly PrefabGUID TravelWoodenBuff = new(-1194613929);

    static readonly PrefabGUID InsideWoodenCoffin = new(381160212);
    static readonly PrefabGUID InsideStoneCoffin = new(569692162);

    static readonly bool Prestige = ConfigService.PrestigeSystem;
    static readonly bool ExoPrestige = ConfigService.ExoPrestiging;
    static readonly bool Familiars = ConfigService.FamiliarSystem;

    public static readonly List<PrefabGUID> PrestigeBuffs = [];
    public static readonly Dictionary<LevelingSystem.PlayerClass, List<PrefabGUID>> ClassBuffs = [];

    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(UpdateBuffsBuffer_Destroy __instance)
    {
        if (!Core.hasInitialized) return;
        else if (!(Familiars || Prestige || Classes)) return;

        NativeArray<Entity> entities = __instance.__query_401358720_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;           

                if (Familiars && prefabGUID.Equals(CombatBuff))
                {
                    if (entity.GetBuffTarget().TryGetPlayer(out Entity character))
                    {
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(character);

                        if (familiar.Exists())
                        {
                            character.With((ref CombatMusicListener_Shared shared) =>
                            {
                                shared.UnitPrefabGuid = PrefabGUID.Empty;
                            });

                            FamiliarUtilities.TryReturnFamiliar(character, familiar);
                        }
                    }
                }
                
                if (Prestige && entity.GetBuffTarget().TryGetPlayer(out Entity player)) // check if need to reapply prestige buff
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    if (PrestigeBuffs.Contains(prefabGUID)) // check if the buff is for prestige and reapply if so
                    {
                        int index = PrestigeBuffs.IndexOf(prefabGUID);

                        if (prefabGUID.Equals(ShroudBuff) && !PlayerUtilities.GetPlayerBool(steamId, "Shroud")) // allow shroud buff destruction
                        {
                            continue;
                        }
                        else if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {                            
                            if (prestigeLevel > index) BuffUtilities.ApplyPermanentBuff(player, prefabGUID); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
                        }
                    }
                    else if (ExoPrestige && prefabGUID.Equals(TauntEmoteBuff) && PlayerUtilities.GetPlayerBool(steamId, "ExoForm"))
                    {
                        if (EmoteSystemPatch.ExitingForm.Contains(steamId))
                        {
                            EmoteSystemPatch.ExitingForm.Remove(steamId);

                            continue;
                        }
                        else if (ExoFormUtilities.CheckExoFormCharge(user, steamId)) ApplyExoFormBuff(player); // could maybe try SpawnPrefabOnGameplayEvent or something like that instead of slingshotting this around, will ponder
                    }
                }
                
                if (Classes && entity.GetBuffTarget().TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (ClassUtilities.HasClass(steamId))
                    {
                        LevelingSystem.PlayerClass playerClass = ClassUtilities.GetPlayerClass(steamId);

                        if (ClassBuffs.TryGetValue(playerClass, out List<PrefabGUID> classBuffs) && classBuffs.Contains(prefabGUID)) BuffUtilities.ApplyPermanentBuff(player, prefabGUID);
                    }
                }

                // do log out stuff when travel into coffin buff is destroyed
                if ((prefabGUID.Equals(TravelStoneBuff) || prefabGUID.Equals(TravelWoodenBuff)) && entity.GetBuffTarget().TryGetPlayer(out player))
                {
                    Core.Log.LogInfo("Entering coffin...");
                    ulong steamId = player.GetSteamId();

                    if (Prestige)
                    {
                        PlayerUtilities.SetPlayerBool(steamId, "Shroud", false);

                        if (player.TryGetBuff(ShroudBuff, out Entity shroudBuff))
                        {
                            Equipment equipment = player.Read<Equipment>();

                            if (!equipment.IsEquipped(ShroudCloak, out var _)) DestroyUtility.Destroy(EntityManager, shroudBuff, DestroyDebugReason.TryRemoveBuff);
                        }
                    }

                    if (Familiars)
                    {
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
                        if (familiar.Exists())
                        {
                            Entity userEntity = player.GetUserEntity();

                            FamiliarUtilities.UnbindFamiliar(player, userEntity, steamId);
                        }
                    }
                }
                else if ((prefabGUID.Equals(InsideStoneCoffin) || prefabGUID.Equals(InsideWoodenCoffin)) && entity.GetBuffTarget().TryGetPlayer(out player)) // do log in stuff when inside coffin buff is destroyed
                {
                    Core.Log.LogInfo("Leaving coffin...");
                    ulong steamId = player.GetSteamId();

                    if (Prestige)
                    {
                        PlayerUtilities.SetPlayerBool(steamId, "Shroud", true);

                        if (PrestigeBuffs.Contains(ShroudBuff) && !player.HasBuff(ShroudBuff)
                            && steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var experiencePrestiges) && experiencePrestiges > UpdateBuffsBufferDestroyPatch.PrestigeBuffs.IndexOf(ShroudBuff))
                        {
                            BuffUtilities.ApplyPermanentBuff(player, ShroudBuff);
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
        // check for cooldown here and other such qualifiers before proceeding, also charge at 15 seconds of form time a day for level 1 up to maxDuration seconds of form time at max exo
        BuffUtilities.TryApplyBuff(character, ExoFormBuff);
        BuffUtilities.TryApplyBuff(character, PhasingBuff);
    }
}
