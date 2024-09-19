using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
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

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();

                //if (prefabGUID.Equals(new PrefabGUID(-182838302))) Core.Log.LogInfo("Destroyed Buff: " + prefabGUID.LookupName());

                //if (prefabGUID.Equals(captureBuff)) Core.Log.LogInfo($"UpdateBuffsBufferDestroy: {captureBuff.LookupName()}");

                if (ConfigService.FamiliarSystem && prefabGUID.Equals(combatBuff))
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
                else if (ConfigService.PrestigeSystem && entity.GetBuffTarget().TryGetPlayer(out Entity player) && entity.Has<LifeTime>()) // check if need to reapply prestige buff
                {
                    if (entity.Read<LifeTime>().Duration != -1f) continue; // filter for not having infinite duration

                    if (PrestigeBuffPrefabs.Contains(prefabGUID)) // check if the buff is a prestige buff, will filter for any empty prestige buffs prefabs here as well
                    {
                        ulong steamId = player.GetSteamId();

                        if (steamId.TryGetPlayerPrestiges(out var prestigeData) && prestigeData.TryGetValue(PrestigeType.Experience, out var prestigeLevel))
                        {
                            if (prestigeLevel > PrestigeBuffPrefabs.IndexOf(prefabGUID)) PrestigeSystem.HandlePrestigeBuff(player, prefabGUID); // at 0 will not be greater than index of 0 so won't apply buffs, if greater than 0 will apply if allowed based on order of prefabs
                        }
                    }
                }
                else if (ConfigService.FamiliarSystem && entity.GetOwner().TryGetPlayer(out player) && prefabGUID.Equals(captureBuff))
                {
                    Entity target = entity.GetBuffTarget();
                    FamiliarUnlockSystem.HandleRoll(1f, target, player);
                    target.Add<DestroyTag>();       
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}
