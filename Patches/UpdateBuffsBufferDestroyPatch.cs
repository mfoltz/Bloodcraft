using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UpdateBuffsBufferDestroyPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID CombatBuff = new(581443919);
    static readonly PrefabGUID ExternalInventory = new(1183666186);

    public static readonly List<PrefabGUID> PrestigeBuffs = [];
    public static readonly Dictionary<LevelingSystem.PlayerClasses, List<PrefabGUID>> ClassBuffs = [];

    static readonly bool Classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;

    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(UpdateBuffsBuffer_Destroy __instance)
    {
        if (!Core.hasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_401358720_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;           

                if (ConfigService.FamiliarSystem && prefabGUID.Equals(CombatBuff))
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
                        }
                    }
                }
                
                if (ConfigService.PrestigeSystem && entity.GetBuffTarget().TryGetPlayer(out Entity player)) // check if need to reapply prestige buff
                {
                    if (PrestigeBuffs.Contains(prefabGUID)) // check if the buff is for prestige and reapply if so
                    {
                        ulong steamId = player.GetSteamId();

                        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {
                            //Core.Log.LogInfo($"UpdateBuffsBuffer_Destroy | {steamId} | {prestigeLevel} | {PrestigeBuffs.IndexOf(prefabGUID)} | {prefabGUID.LookupName()}");
                            if (prestigeLevel > PrestigeBuffs.IndexOf(prefabGUID)) BuffUtilities.HandlePermaBuff(player, prefabGUID); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
                        }
                    }
                }
                
                if (Classes && entity.GetBuffTarget().TryGetPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (ClassUtilities.HasClass(steamId))
                    {
                        LevelingSystem.PlayerClasses playerClass = ClassUtilities.GetPlayerClass(steamId);
                        List<PrefabGUID> classBuffs = ClassBuffs.ContainsKey(playerClass) ? ClassBuffs[playerClass] : [];

                        if (classBuffs.Contains(prefabGUID)) BuffUtilities.HandlePermaBuff(player, prefabGUID);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
