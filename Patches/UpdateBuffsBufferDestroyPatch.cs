using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class UpdateBuffsBufferDestroyPatch
{
    static readonly PrefabGUID combatBuff = new(581443919);
    static readonly PrefabGUID captureBuff = new(548966542);

    public static readonly List<PrefabGUID> PrestigeBuffPrefabs = [];

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
                else if (ConfigService.FamiliarSystem && prefabGUID.Equals(combatBuff))
                {
                    if (entity.GetBuffTarget().TryGetPlayer(out Entity player))
                    {
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
                        if (familiar.Exists())
                        {
                            player.With((ref CombatMusicListener_Shared shared) =>
                            {
                                shared.UnitPrefabGuid = PrefabGUID.Empty;
                            });
                        }
                    }
                }
                else if (ConfigService.PrestigeSystem && entity.GetBuffTarget().TryGetPlayer(out Entity player) && entity.TryGetComponent(out LifeTime lifeTime)) // check if need to reapply prestige buff
                {
                    if (lifeTime.Duration != -1f) continue; // filter for not having infinite duration
                    else if (PrestigeBuffPrefabs.Contains(prefabGUID)) // check if the buff is for prestige and reapply if so
                    {
                        ulong steamId = player.GetSteamId();

                        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {
                            if (prestigeLevel > PrestigeBuffPrefabs.IndexOf(prefabGUID)) PrestigeSystem.HandlePrestigeBuff(player, prefabGUID); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
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
}
