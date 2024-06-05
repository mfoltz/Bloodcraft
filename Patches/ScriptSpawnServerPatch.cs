using Bloodcraft.Systems.Legacy;
using HarmonyLib;
using ProjectM;
using ProjectM.Shared.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class ScriptSpawnServerPatch
{
    [HarmonyPatch(typeof(ScriptSpawnServer), nameof(ScriptSpawnServer.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ScriptSpawnServer __instance)
    {
        NativeArray<Entity> entities = __instance.__query_1231292176_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Has<BloodBuff>() && entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                {
                    
                    if (BloodSystem.BuffToBloodTypeMap.TryGetValue(entity.Read<PrefabGUID>(), out BloodSystem.BloodType bloodType))
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
            Core.Log.LogError($"Exited ScriptSpawnServer hook early: {e}");
        }
        finally
        {
            entities.Dispose();
        }
    }
}