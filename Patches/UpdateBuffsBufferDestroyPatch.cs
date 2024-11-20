using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

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
                        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {                            
                            if (prestigeLevel > PrestigeBuffs.IndexOf(prefabGUID)) BuffUtilities.ApplyPermanentBuff(player, prefabGUID); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
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
