using Bloodcraft.SystemUtilities.Legacy;
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
    static readonly bool BloodSystem = Plugin.BloodSystem.Value;
    static readonly bool LevelingSystem = Plugin.LevelingSystem.Value;

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

                //Core.Log.LogInfo($"ScriptSpawnServer: {entity.Read<PrefabGUID>().LookupName()}");

                if (entity.Has<BloodBuff>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    if (LevelingSystem && entity.Has<BloodBuff_Brute_ArmorLevelBonus_DataShared>())
                    {
                        BloodBuff_Brute_ArmorLevelBonus_DataShared bloodBuff_Brute_ArmorLevelBonus_DataShared = entity.Read<BloodBuff_Brute_ArmorLevelBonus_DataShared>();
                        bloodBuff_Brute_ArmorLevelBonus_DataShared.GearLevel = 0;
                        entity.Write(bloodBuff_Brute_ArmorLevelBonus_DataShared);
                    }
                    if (BloodSystem && LegacyUtilities.BuffToBloodTypeMap.TryGetValue(entity.Read<PrefabGUID>(), out LegacyUtilities.BloodType bloodType)) // applies stat choices to blood types when changed
                    {
                        Entity character = entity.Read<EntityOwner>().Owner;
                        ModifyUnitStatBuffUtils.ApplyBloodBonuses(character, bloodType, entity);
                        Core.ModifyUnitStatBuffSystem_Spawn.OnUpdate();
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogInfo($"Exited ScriptSpawnServer hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
}