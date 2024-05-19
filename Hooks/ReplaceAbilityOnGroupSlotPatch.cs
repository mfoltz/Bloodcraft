using Cobalt.Systems.Expertise;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.TextCore.Text;
/*
namespace Cobalt.Hooks
{
    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), nameof(ReplaceAbilityOnSlotSystem.OnUpdate))]
    public static class ReplaceAbilityOnGroupSlotPatch
    {
        [HarmonyPrefix]
        public static void OnUpdatePrefix(ReplaceAbilityOnSlotSystem __instance)
        {
            Core.Log.LogInfo("ReplaceAbilityOnGroupSlotPatch");
            NativeArray<Entity> entities = __instance.__query_1482480545_0.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    if (entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>())
                    {
                        Entity character = entity.Read<EntityOwner>().Owner;
                        ulong steamId = character.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        if (entity.Read<PrefabGUID>().LookupName().ToLower().Contains("unarmed"))
                        {
                            ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, ExpertiseSystem.WeaponType.Unarmed, entity);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Core.Log.LogError($"Error in ReplaceAbilityOnGroupSlotPatch: {e}");
            }
            finally
            {
                entities.Dispose();
            }
        }
    }
}
*/