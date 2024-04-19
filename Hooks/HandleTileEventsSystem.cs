using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Network;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using VCreate.Core.Toolbox;
using VCreate.Core;
using VCreate.Core.Commands;
using VCreate.Systems;
using Plugin = VCreate.Core.Plugin;
using User = ProjectM.Network.User;
using VRising.GameData.Models;
using static VCF.Core.Basics.RoleCommands;
using static VCreate.Core.Services.PlayerService;
using VCreate.Core.Services;
using VCreate.Hooks;

namespace WorldBuild.Hooks
{
    [HarmonyPatch(typeof(PlaceTileModelSystem), nameof(PlaceTileModelSystem.OnUpdate))]
    public static class CastleHeartPlacementPatch
    {
        private static readonly PrefabGUID CastleHeartPrefabGUID = new(-485210554); // castle heart prefab

        public static void Prefix(PlaceTileModelSystem __instance)
        {
            EntityManager entityManager = VWorld.Server.EntityManager;

            var jobs = __instance._BuildTileQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var job in jobs)
                {
                    if (IsCastleHeart(job))
                    {
                        if (!WorldBuildToggle.WbFlag) continue;
                        CancelCastleHeartPlacement(entityManager, job);
                    }
                }
            }
            finally
            {
                jobs.Dispose();
            }

            jobs = __instance._AbilityCastFinishedQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var job in jobs)
                {
                    if (Utilities.HasComponent<AbilityPreCastFinishedEvent>(job))
                    {
                        AbilityPreCastFinishedEvent abilityPreCastFinishedEvent = Utilities.GetComponentData<AbilityPreCastFinishedEvent>(job);
                        Entity abilityGroupData = abilityPreCastFinishedEvent.AbilityGroup;
                        PrefabGUID prefabGUID = Utilities.GetComponentData<PrefabGUID>(abilityGroupData);
                        Entity character = abilityPreCastFinishedEvent.Character;

                        if (!Utilities.HasComponent<PlayerCharacter>(character)) continue;

                        PlayerCharacter playerCharacter = Utilities.GetComponentData<PlayerCharacter>(character);
                        Entity userEntity = playerCharacter.UserEntity;
                        User user = Utilities.GetComponentData<User>(userEntity);
                        if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out _) && prefabGUID.Equals(VCreate.Data.Prefabs.AB_Interact_Siege_Structure_T02_AbilityGroup))
                        {
                            //Plugin.Log.LogInfo("SiegeT02 ability cast detected...");
                            HandleAbilityCast(userEntity);
                        }
                    }
                }
            }
            finally
            {
                jobs.Dispose();
            }
            jobs = __instance._MoveTileQuery.ToEntityArray(Allocator.Temp);
            try
            {

                foreach (var job in jobs)
                {
                    MoveTileModelEvent moveTileModelEvent = job.Read<MoveTileModelEvent>();
                    moveTileModelEvent.Target.GetNetworkedEntity(VWorld.Server.GetExistingSystem<NetworkIdSystem>()._NetworkIdToEntityMap).TryGetSyncedEntity(out Entity tileEntity);
                    if (!tileEntity.Equals(Entity.Null))
                    {
                        EditableTileModel editableTileModel = tileEntity.Read<EditableTileModel>();
                        editableTileModel.CurrentEditor.TryGetSyncedEntity(out Entity editorEntity);
                        User user = editorEntity.Read<PlayerCharacter>().UserEntity.Read<User>(); // error
                        if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out var data) && data.Permissions)
                        {
                            //Plugin.Log.LogInfo("Permissions >> ownership, allowed.");
                            continue;
                        }
                        else if (!TileOperationUtility.HasValidCastleHeartConnectionOrAllied(user, tileEntity))
                        {
                            //Plugin.Log.LogInfo("Disallowing move based on ownership.");
                            SystemPatchUtil.Destroy(job);
                        }
                        else
                        {
                            //Plugin.Log.LogInfo("Allowing normal game handling for movement.");
                            continue;
                        }

                    }
                }
            }
            finally
            {

                jobs.Dispose();
            }
        }

        public static void HandleAbilityCast(Entity userEntity)
        {
            var user = Utilities.GetComponentData<User>(userEntity);

            if (DataStructures.PlayerSettings.TryGetValue(user.PlatformId, out Omnitool settings))
            {
                if (!settings.Emotes) return;
                var action = DecideAction(settings);
                action?.Invoke(userEntity, settings);
            }
            else
            {
                return;
            }
            
            // Assuming a method that decides the action based on the ability and settings

            // Execute the decided action
            
        }

        private static System.Action<Entity, Omnitool> DecideAction(Omnitool settings)
        {
            // Example: Checking for a specific prefabGUID and toggle
            //Plugin.Log.LogInfo("Deciding action based on mode...");
            if (settings.GetMode("Trainer"))
            {
                return (userEntity, _) =>
                {
                    //Plugin.Logger.LogInfo("Trainer mode enabled, skipping tile spawn...");
                    PlayerService.TryGetPlayerFromString(userEntity.Read<User>().CharacterName.ToString(), out Player player);
                    EmoteSystemPatch.ToggleFamiliarAtMouse(player, player.SteamID);
                };
            }
            else if (settings.GetMode("InspectToggle"))
            {
                return (userEntity, _) =>
                {
                    //Plugin.Logger.LogInfo("Inspect mode enabled, skipping tile spawn...");
                    OnHover.InspectHoveredEntity(userEntity);
                };
            }
            else if (settings.GetMode("DestroyToggle"))
            {
                return (userEntity, _) =>
                {
                    OnHover.DestroyAtHover(userEntity);
                };
            }
            else if (settings.GetMode("CopyToggle"))
            {
                return (userEntity, _) =>
                {
                    // change this to add specified component to hovered entity
                    OnHover.SpawnCopy(userEntity);
                };
            }
            else if (settings.GetMode("DebuffToggle"))
            {
                return (userEntity, _) =>
                {
                    OnHover.DebuffAtHover(userEntity);
                };
            }
            else if (settings.GetMode("ConvertToggle"))
            {
                return (userEntity, _) =>
                {
                    // change this to remove specified component from hovered entity
                    ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, userEntity.Read<User>(), "Deprecated for now.");
                };
            }
            else if (settings.GetMode("BuffToggle"))
            {
                return (userEntity, _) =>
                {
                    OnHover.BuffAtHover(userEntity);
                };
            }
            else if (settings.GetMode("TileToggle"))
            {
                return (userEntity, _) =>
                {
                    OnHover.SpawnTileModel(userEntity);
                };
            }
            else
            {
                return null;
            }
        }

        private static bool IsCastleHeart(Entity job)
        {
            var entityManager = VWorld.Server.EntityManager;
            var buildTileModelData = entityManager.GetComponentData<BuildTileModelEvent>(job);
            return buildTileModelData.PrefabGuid.Equals(CastleHeartPrefabGUID);
        }

        private static void CancelCastleHeartPlacement(EntityManager entityManager, Entity job)
        {
            var userEntity = entityManager.GetComponentData<FromCharacter>(job).User;
            var user = entityManager.GetComponentData<User>(userEntity);

            StringBuilder message = new StringBuilder("Bad vampire, no merlot! (Castle Heart placement is disabled during worldbuild)");

            ServerChatUtils.SendSystemMessageToClient(entityManager, user, message.ToString());
            SystemPatchUtil.Destroy(job);
        }
    }

    [HarmonyPatch(typeof(PlaceTileModelSystem), nameof(PlaceTileModelSystem.VerifyCanDismantle))]
    public static class VerifyCanDismantlePatch
    {
        public static void Postfix(ref bool __result, EntityManager entityManager, Entity tileModelEntity)
        {
            //if (!__result) return;

            //Plugin.Log.LogInfo("Verifying dismantle event...");

            if (Utilities.HasComponent<EditableTileModel>(tileModelEntity))
            {
                EditableTileModel editableTileModel = tileModelEntity.Read<EditableTileModel>();
                NetworkedEntity interactor = editableTileModel.CurrentEditor;

                if (interactor.TryGetSyncedEntity(out Entity entity))
                {
                    if (!entity.Equals(Entity.Null))
                    {
                        //entity.LogComponentTypes();
                        ulong platformId = entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        if (DataStructures.PlayerSettings.TryGetValue(platformId, out var data) && data.Permissions)
                        {
                            //Plugin.Log.LogInfo("Permissions >> ownership, allowed.");
                            __result = true;
                        }
                        else if (!TileOperationUtility.HasValidCastleHeartConnectionOrAllied(entity.Read<PlayerCharacter>().UserEntity.Read<User>(), tileModelEntity)) // returns false if the interactor is not the owner of the castle heart
                        {
                            //Plugin.Log.LogInfo("Disallowing dismantle based on ownership.");
                            __result = false;
                        }
                        else
                        {
                            //Plugin.Log.LogInfo("Allowing normal game handling for dismantle event.");
                            return;
                        }
                    }
                }
            }
            else
            {
                Plugin.Log.LogInfo("No editableTileModel component, allowing normal game handling for dismantle event.");
                return;
            }
        }
    }

    public static class TileOperationUtility
    {
        public static bool HasValidCastleHeartConnectionOrAllied(User user, Entity tileModelEntity)
        {
            if (!tileModelEntity.Has<CastleHeartConnection>()) return false;
            else
            {
                //Plugin.Log.LogInfo("Checking for valid castle heart connection...");
                CastleHeartConnection castleHeartConnection = tileModelEntity.Read<CastleHeartConnection>();
                Entity castleHeart = castleHeartConnection.CastleHeartEntity._Entity;
                if (castleHeart == Entity.Null) return false;
                else
                {
                    //Plugin.Log.LogInfo("Castle heart entity not null. Checking owner...");
                    Entity userOwner = castleHeart.Read<UserOwner>().Owner._Entity;
                    ulong platformId = userOwner.Read<User>().PlatformId;
                    if (!user.PlatformId.Equals(platformId))
                    { 
                        //want to check for ally here
                        //Plugin.Log.LogInfo("Owner not the same as interactor. Checking for clanmate...");
                        try
                        {
                            if (user.ClanEntity.TryGetSyncedEntity(out Entity clan))
                            {
                                if (Utilities.HasComponent<ClanTeam>(clan))
                                {
                                    ClanTeam clanTeam = clan.Read<ClanTeam>();
                                    int team = clanTeam.TeamValue; // team value of the interactor
                                    User tileOwner = userOwner.Read<User>();
                                    if (tileOwner.ClanEntity.TryGetSyncedEntity(out Entity tileClan))
                                    {
                                        if (Utilities.HasComponent<ClanTeam>(tileClan))
                                        {
                                            ClanTeam tileClanTeam = tileClan.Read<ClanTeam>();
                                            int tileTeam = tileClanTeam.TeamValue;
                                            if (team.Equals(tileTeam))
                                            {
                                                //Plugin.Log.LogInfo("Clanmate found, allowing.");
                                                return true;
                                            }
                                            else
                                            {
                                                //Plugin.Log.LogInfo("No clanmate found, disallowing.");
                                                return false;
                                            }
                                        }
                                        else
                                        {
                                            //Plugin.Log.LogInfo("No clan found, disallowing.");
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        //Plugin.Log.LogInfo("No clan found for tileOwner, disallowing.");
                                        return false;
                                    }
                                }
                                else
                                {
                                    //Plugin.Log.LogInfo("No clan found for interactor, disallowing.");
                                    return false;
                                }
                            }
                            else
                            {
                                //Plugin.Log.LogInfo("No clan found, disallowing.");
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            //Plugin.Log.LogInfo("Error: " + e.Message);
                            //Plugin.Log.LogInfo("Couldn't verify clan status, disallowing.");
                            return false;
                        }
                    }
                    return true;
                }
            }
        }
    }
}