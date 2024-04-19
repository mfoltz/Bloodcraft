using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using VCreate.Core;
using VCreate.Systems;

namespace WorldBuild.Hooks
{
    [HarmonyPatch(typeof(ReplaceAbilityOnSlotSystem), "OnUpdate")]
    public class ReplaceAbilityOnSlotSystem_Patch
    {
        private static void Prefix(ReplaceAbilityOnSlotSystem __instance)
        {
            try
            {
                EntityManager entityManager = VWorld.Server.EntityManager;
                NativeArray<Entity> entityArray = __instance.__Spawn_entityQuery.ToEntityArray(Allocator.Temp);
                //Plugin.Log.LogInfo("ReplaceAbilityOnSlotSystem Prefix called...");

                foreach (Entity entity in entityArray)
                {
                    Entity owner = entityManager.GetComponentData<EntityOwner>(entity).Owner;

                    if (!entityManager.HasComponent<PlayerCharacter>(owner)) continue;

                    Entity userEntity = entityManager.GetComponentData<PlayerCharacter>(owner).UserEntity;
                    User user = entityManager.GetComponentData<User>(userEntity);

                    if (!DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool data) || data.EquipSkills.Equals(false)) continue;
                    if (!user.IsAdmin)
                    {
                        DynamicBuffer<ReplaceAbilityOnSlotBuff> buffer = entityManager.GetBuffer<ReplaceAbilityOnSlotBuff>(entity);
                        if (buffer[0].NewGroupId == VCreate.Data.Prefabs.AB_Vampire_Unarmed_Primary_MeleeAttack_AbilityGroup)
                        {
                            PrefabGUID spell1 = VCreate.Data.Prefabs.AB_Interact_Siege_Structure_T02_AbilityGroup; // Assigning pet ability
                            //PrefabGUID spell2 = VCreate.Data.Prefabs.AB_Debug_NukeAll_Group; // Assigning nuke ability

                            ReplaceAbilityOnSlotBuff buildAbility = new ReplaceAbilityOnSlotBuff { Slot = 1, NewGroupId = spell1 };
                            //ReplaceAbilityOnSlotBuff nukeAbility = new ReplaceAbilityOnSlotBuff { Slot = 4, NewGroupId = spell2 };

                            buffer.Add(buildAbility);
                            //buffer.Add(nukeAbility);
                            Plugin.Log.LogInfo("Modification complete.");
                        }
                    }
                    else
                    {
                        DynamicBuffer<ReplaceAbilityOnSlotBuff> buffer = entityManager.GetBuffer<ReplaceAbilityOnSlotBuff>(entity);
                        if (buffer[0].NewGroupId == VCreate.Data.Prefabs.AB_Vampire_Unarmed_Primary_MeleeAttack_AbilityGroup)
                        {
                            PrefabGUID spell1 = VCreate.Data.Prefabs.AB_Interact_Siege_Structure_T02_AbilityGroup; // Assigning build ability
                            PrefabGUID spell2 = VCreate.Data.Prefabs.AB_Debug_NukeAll_Group; // Assigning nuke ability

                            ReplaceAbilityOnSlotBuff buildAbility = new ReplaceAbilityOnSlotBuff { Slot = 1, NewGroupId = spell1 };
                            ReplaceAbilityOnSlotBuff nukeAbility = new ReplaceAbilityOnSlotBuff { Slot = 4, NewGroupId = spell2 };

                            buffer.Add(buildAbility);
                            buffer.Add(nukeAbility);
                            Plugin.Log.LogInfo("Modification complete.");
                        }
                    }
                    
                }
                entityArray.Dispose();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogInfo($"Error in ReplaceAbilityOnSlotSystem Prefix: {ex.Message}");
            }
        }
    }
}