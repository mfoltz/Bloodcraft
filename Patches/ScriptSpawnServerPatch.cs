using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Legacies;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    static SystemService SystemService => Core.SystemService;
    static ConfigService ConfigService => Core.ConfigService;
    static ModifyUnitStatBuffSystem_Spawn ModifyUnitStatBuffSystem_Spawn => SystemService.ModifyUnitStatBuffSystem_Spawn;

    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1231292176_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<BloodBuff>() || !entity.Has<EntityOwner>()) continue;

                if (entity.GetOwner().HasPlayer(out Entity player))
                {
                    if (ConfigService.LevelingSystem && entity.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>())
                    {
                        BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared = entity.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                        bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        entity.Write(bloodBuff_Brute_ArmorLevelBonus_DataShared);
                    }

                    if (ConfigService.BloodSystem && BloodSystem.BuffToBloodTypeMap.TryGetValue(entity.Read<PrefabGUID>(), out BloodSystem.BloodType bloodType)) // applies stat choices to blood types when changed
                    {
                        ulong steamId = player.GetSteamId();
                        BloodHandler.ApplyBloodBonuses(steamId, bloodType, entity);
                        ModifyUnitStatBuffSystem_Spawn.OnUpdate();
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