using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.TextCore;
using VCreate.Core;
using VCreate.Core.Toolbox;
using VCreate.Systems;
using VRising.GameData;
using VRising.GameData.Methods;
using VRising.GameData.Models;
using VRising.GameData.Models.Internals;
using VRising.GameData.Utils;
using static Il2CppSystem.Data.Common.ObjectStorage;

namespace VCreate.Hooks
{
    [HarmonyPatch]
    public static class MountSystem_ServerPatch
    {
        public static Entity cursedTile = Entity.Null;
        public static Entity eventPlayer = Entity.Null;
        public static Entity eventHorse = Entity.Null;
        public static bool hadShroud = false;
        private static readonly PrefabGUID siege = VCreate.Data.Prefabs.MapIcon_Siege_Summon_T01;
        private static readonly PrefabGUID shroud = VCreate.Data.Prefabs.EquipBuff_ShroudOfTheForest;

        public static readonly Dictionary<PrefabGUID, int> rodeoRewards = new()
        {
            { VCreate.Data.Prefabs.Item_Ingredient_Crystal, 250 }
        };
        
        [HarmonyPatch(typeof(MountSystem_Server), nameof(MountSystem_Server.OnUpdate))]
        [HarmonyPrefix]
        private static void Prefix(MountSystem_Server __instance)
        {
            //Plugin.Log.LogInfo("MountSystem_Server Prefix...");
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
            EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(serverGameManager);
            EntityManager entityManager = VWorld.Server.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    //entity.LogComponentTypes();
                    // want to filter for mounted horses here and monitor for the event horse when it gets inside a castle
                    // this is the mount buff entity

                    //Plugin.Log.LogInfo($"Mounted event horse detected in MountSystem Prefix... {entities.Length}");
                    //if (!entity.Has<Buff>()) continue;
                    //entity.LogComponentTypes();
                    Entity target = entity.Read<Buff>().Target;

                    Entity mount = target.Read<Mounter>().MountEntity;



                    //Plugin.Log.LogInfo(target.Read<Mounter>().GallopMaxSpeed)
                    if (target.Read<Mounter>().GallopMaxSpeed > 7f) continue;

                    

                    eventPlayer = target;
                    /*
                    if (Utilities.HasComponent<charm>(mount))
                    {
                        Follower follower = mount.Read<Follower>();
                        follower.Followed = ModifiableEntity.CreateFixed(target);
                        mount.Write(follower);
                    }
                    */
                    bool combatCheck = BuffUtility.TryGetBuff(target, VCreate.Data.Prefabs.Buff_InCombat, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
                    bool shroudCheck = BuffUtility.TryGetBuff(target, shroud, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
                    Entity curse = cursedTile;

                    if (!curse.Equals(Entity.Null))
                    {
                        curse.Write<Translation>(new Translation { Value = entity.Read<Translation>().Value });
                    }
                    if (shroudCheck)
                    {
                        hadShroud = true;
                        BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, shroud, target);
                        //Plugin.Log.LogInfo("Shroud buff removed from player...");
                    }
                    if (!combatCheck)
                    {
                        Helper.BuffCharacter(target, VCreate.Data.Prefabs.Buff_InCombat, 0);
                        //Plugin.Log.LogInfo("Combat buff added while mounting event horse...");
                        OnHover.BuffNonPlayer(target, VCreate.Data.Prefabs.AB_Militia_HoundMaster_QuickShot_Buff);
                        OnHover.BuffNonPlayer(target, VCreate.Data.Prefabs.Buff_General_RelicCarryDebuff);
                    }
                    
                    //Plugin.Log.LogInfo("Charm buff added while mounting event horse...");
                    
                    


                    //mounter.LogComponentTypes();

                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
            finally
            {
                entities.Dispose();
            }
        }

        [HarmonyPatch(typeof(MountBuffDestroySystem_Shared), nameof(MountBuffDestroySystem_Shared.OnUpdate))]
        [HarmonyPrefix]
        private static void Prefix(MountBuffDestroySystem_Shared __instance)
        {
            //Plugin.Log.LogInfo("MountBuffDestroySystem_Shared Prefix...");
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
            EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
            EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
            BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(serverGameManager);
            EntityManager entityManager = VWorld.Server.EntityManager;
            NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (Entity entity in entities)
                {
                    // want to catch this and reactivate pvp prot
                    //entity.LogComponentTypes();
                    //entity.LogComponentTypes();
                    Entity target = entity.Read<Buff>().Target;
                    //Plugin.Log.LogInfo($"{target.Index} | {eventPlayer.Index}");
                    //Plugin.Log.LogInfo($"{target.Version} | {eventPlayer.Version}");
                    if (!target.Equals(eventPlayer)) continue;
                    Entity mount = target.Read<Mounter>().MountEntity;

                    bool shroudCheck = BuffUtility.TryGetBuff(target, shroud, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
                    bool combatCheck = BuffUtility.TryGetBuff(target, VCreate.Data.Prefabs.Buff_InCombat, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
                    bool charmCheck = BuffUtility.TryGetBuff(target, VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
                    //target.LogComponentTypes();
                    //ulong platformId = target.Read<User>().PlatformId;
                    if (hadShroud)
                    {
                        Helper.BuffCharacter(target, shroud, 0);
                        hadShroud = false;
                        //Plugin.Log.LogInfo("Shroud buff applied to player...");
                    }
                    if (combatCheck)
                    {
                        Helper.UnbuffCharacter(target, VCreate.Data.Prefabs.Buff_InCombat);
                        //Plugin.Log.LogInfo("Combat buff removed from player on dismounting...");
                    }
                    if (charmCheck)
                    {
                        Helper.UnbuffCharacter(target, VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff);
                        //Plugin.Log.LogInfo("Charm buff removed from player on dismounting...");
                    }
                    
                    BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.Buff_General_RelicCarryDebuff, target);
                    BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.AB_Militia_HoundMaster_QuickShot_Buff, target);
                    UserModel userModel = VRising.GameData.GameData.Users.GetUserByCharacterName(target.Read<PlayerCharacter>().Name.ToString());
                    if (!MountSystem_ServerPatch.IsInCastleV2(userModel)) continue;

                    Plugin.Log.LogInfo("Event horse entered player castle while mounted by castle owner, killing horse and rewarding player...");

                    DestroyUtility.CreateDestroyEvent(entityCommandBuffer, cursedTile, DestroyReason.Default, DestroyDebugReason.None);
                    DestroyUtility.CreateDestroyEvent(entityCommandBuffer, mount, DestroyReason.Default, DestroyDebugReason.None);
                    DestroyUtility.CreateDestroyEvent(entityCommandBuffer, eventHorse, DestroyReason.Default, DestroyDebugReason.None);
                    cursedTile = Entity.Null;
                    //KillUtility.Kill(entityCommandBuffer, mount);
                    foreach (KeyValuePair<PrefabGUID, int> reward in rodeoRewards)
                    {
                        bool itemCheck = userModel.TryGiveItem(reward.Key, reward.Value, out var _);
                        if (!itemCheck)
                        {
                            userModel.DropItemNearby(reward.Key, reward.Value);
                        }
                    }
                    Plugin.Log.LogInfo("Event horse killed and player rewarded.");
                    string colorHorse = VCreate.Core.Toolbox.FontColors.Cyan("Jingles");
                    string name = userModel.CharacterName;
                    if (name.Last().Equals('s'))
                    {
                        name += "'";
                    }
                    else
                    {
                        name += "'s";
                    }
                    ServerChatUtils.SendSystemMessageToAllClients(entityCommandBuffer, $"{colorHorse} mysteriously vanishes after entering <color=yellow>{name}</color> castle...");
                    FollowerSystemPatchV2.spawned = false;
                    eventPlayer = Entity.Null;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
            finally
            {
                entities.Dispose();
            }
        }

        public static bool IsInCastleV2(this UserModel userModel)
        {
            EntityQuery entityQuery = GameData.World.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>(), ComponentType.ReadOnly<LocalToWorld>(), ComponentType.ReadOnly<UserOwner>(), ComponentType.ReadOnly<CastleFloor>());
            try
            {
                foreach (BaseEntityModel item in entityQuery.ToEnumerable())
                {
                    if (item.LocalToWorld.HasValue)
                    {
                        float3 position = item.LocalToWorld.Value.Position;
                        float3 position2 = userModel.Position;
                        if (Math.Abs(position2.x - position.x) < 3f && Math.Abs(position2.z - position.z) < 3f)
                        {
                            if (item.UserOwner.Value.Owner._Entity.Read<User>().PlatformId.Equals(userModel.PlatformId))
                                return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
            finally
            {
                entityQuery.Dispose();
            }
            return false;
        }
    }
}