using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Unity.Collections;
using Unity.Entities;
using Unity.Services.Authentication.Internal;
using Unity.Transforms;
using UnityEngine.Experimental.GlobalIllumination;
using VampireCommandFramework;
using VCreate.Core;
using VCreate.Core.Services;
using VCreate.Core.Toolbox;
using VCreate.Systems;
using static VCreate.Core.Commands.PetCommands;
using static VCreate.Core.Services.PlayerService;
using User = ProjectM.Network.User;

namespace VCreate.Hooks;

[HarmonyPatch]
internal class EmoteSystemPatch
{
    private static readonly string enabledColor = VCreate.Core.Toolbox.FontColors.Green("enabled");
    private static readonly string disabledColor = VCreate.Core.Toolbox.FontColors.Red("disabled");
    private static int index = 0; // 0 familiar commands by default, if buildemotes active then 1

    public static readonly Dictionary<int, Action<Player, ulong>>[] emoteActionsArray = new Dictionary<int, Action<Player, ulong>>[]
    {
        new Dictionary<int, Action<Player, ulong>>() // Dictionary 1
        {
            { -658066984, ToggleTileMode }, // Beckon
            { -1462274656, ToggleTileRotation }, // Bow
            { -26826346, ToggleImmortalTiles }, // Clap
            { -452406649, ToggleInspectMode }, // Point
            { -53273186, ToggleDestroyMode }, // No
            { -370061286, CycleGridSize }, // Salute
            { -578764388, UndoLastTilePlacement }, // Shrug
            { 808904257, ToggleBuffMode }, // Sit
            { -1064533554, ToggleMapIconPlacement}, // Surrender
            { -158502505, ToggleDebuffMode }, // Taunt
            { 1177797340,ToggleCopyMode }, // Wave
            { -1525577000, ToggleSnapping } // Yes
        },
        new Dictionary<int, Action<Player, ulong>>() // Dictionary 2
        {
            //{ -658066984, ToggleTileMode }, // Beckon
            //{ -1462274656, ToggleTileRotation }, // Bow
            //{ -26826346, CallDismiss }, // Clap to call/dismiss
            { -452406649, ToggleTrainer }, // Point
            //{ -53273186, ToggleDestroyMode }, // No
            { -370061286, ToggleCombat }, // Salute to toggle combat mode
            //{ -578764388, ToggleImmortalTiles }, // Shrug
            //{ 808904257, ToggleBuffMode }, // Sit
            //{ -1064533554, ToggleMapIconPlacement}, // Surrender
            //{ -158502505, ToggleDebuffMode }, // Taunt
            { 1177797340, CallDismiss }, // Wave
            //{ -1525577000, ToggleSnapping } // Yes
        },
        // Add more dictionaries as needed
    };

    [HarmonyPatch(typeof(EmoteSystem), nameof(EmoteSystem.OnUpdate))]
    [HarmonyPrefix]
    public static void OnUpdate_Emote(ProjectM.EmoteSystem __instance)
    {
        var _entities = __instance.__UseEmoteJob_entityQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (var _entity in _entities)
            {
                var _event = _entity.Read<UseEmoteEvent>();
                var _from = _entity.Read<FromCharacter>();

                Player _player = new(_from.User);
                ulong _playerId = _player.SteamID;
                if (DataStructures.PlayerSettings.TryGetValue(_playerId, out Omnitool data) && data.Emotes)
                {
                    index = data.Build ? 0 : 1; // if active index is 1 for building emotes, if inactive index is 0 for familiar emotes
                    if (emoteActionsArray[index].TryGetValue(_event.Action.GuidHash, out var action))
                    {
                        // Execute the associated action
                        action.Invoke(_player, _playerId);
                    }
                }
                else
                {
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            Plugin.Log.LogInfo(ex.Message);
        }
        finally
        {
            _entities.Dispose();
        }
    }

    public static void CallDismiss(Player player, ulong playerId)
    {
        EntityManager entityManager = VWorld.Server.EntityManager;
        ulong platformId = playerId;
        EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        Entity character = player.Character;

        if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
        {
            if (PlayerFamiliarStasisMap.TryGetValue(platformId, out FamiliarStasisState familiarStasisState) && familiarStasisState.IsInStasis)
            {
               
                if (!FollowerSystemPatchV2.StateCheckUtility.ValidatePlayerState(character, platformId, entityManager))
                {
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "You can't call your familiar while shapeshifted (wolf and bear allowed) or dominating presence is active.");
                    return;
                }
                var followers = character.ReadBuffer<FollowerBuffer>();
                foreach (var follower in followers)
                {
                    var buffs = follower.Entity._Entity.ReadBuffer<BuffBuffer>();
                    foreach (var buff in buffs)
                    {
                        if (buff.PrefabGuid.GuidHash == VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff.GuidHash)
                        {
                            ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Looks like you have a charmed human. Take care of that before calling your familiar.");
                            return;
                        }
                    }
                }

                if (entityManager.Exists(familiarStasisState.FamiliarEntity))
                {
                    SystemPatchUtil.Enable(familiarStasisState.FamiliarEntity);
                    Follower follower = familiarStasisState.FamiliarEntity.Read<Follower>();
                    follower.Followed._Value = player.Character;
                    follower.ModeModifiable = ModifiableInt.CreateFixed(1);
                    familiarStasisState.FamiliarEntity.Write(follower);
                    familiarStasisState.FamiliarEntity.Write(new Translation { Value = player.Character.Read<Translation>().Value });
                    familiarStasisState.FamiliarEntity.Write(new LastTranslation { Value = player.Character.Read<Translation>().Value });
                    familiarStasisState.IsInStasis = false;
                    familiarStasisState.FamiliarEntity = Entity.Null;
                    PlayerFamiliarStasisMap[platformId] = familiarStasisState;
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Your familiar has been summoned.");
                }
                else
                {
                    familiarStasisState.IsInStasis = false;
                    familiarStasisState.FamiliarEntity = Entity.Null;
                    PlayerFamiliarStasisMap[platformId] = familiarStasisState;
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Familiar in stasis couldn't be found to enable, assuming dead. You may now unbind.");
                }

                return;
            }
            else if (!familiarStasisState.IsInStasis)
            {
                Entity familiar = FindPlayerFamiliar(player.Character);
                if (familiar.Equals(Entity.Null))
                {
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "You don't have an active familiar following you.");
                }
                else if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                {
                    Follower follower = familiar.Read<Follower>();
                    follower.Followed._Value = Entity.Null;
                    familiar.Write(follower);
                    SystemPatchUtil.Disable(familiar);
                    PlayerFamiliarStasisMap[platformId] = new FamiliarStasisState(familiar, true);
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Your familar has been placed in stasis.");
                }
                else
                {
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Couldn't verify familiar to dismiss.");
                }
            }
        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "No bound familiar to summon.");
        }
    }

    public static void ToggleFamiliarAtMouse(Player player, ulong playerId)
    {
        
        var aimPos = player.User.Read<EntityInput>().AimPosition;
        EntityManager entityManager = VWorld.Server.EntityManager;
        ulong platformId = playerId;
        EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        Entity character = player.Character;

        if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
        {
            if (PlayerFamiliarStasisMap.TryGetValue(platformId, out FamiliarStasisState familiarStasisState) && familiarStasisState.IsInStasis)
            {

                if (!FollowerSystemPatchV2.StateCheckUtility.ValidatePlayerState(character, platformId, entityManager))
                {
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "You can't call your familiar while shapeshifted (wolf and bear allowed) or dominating presence is active.");
                    return;
                }
                var followers = character.ReadBuffer<FollowerBuffer>();
                foreach (var follower in followers)
                {
                    var buffs = follower.Entity._Entity.ReadBuffer<BuffBuffer>();
                    foreach (var buff in buffs)
                    {
                        if (buff.PrefabGuid.GuidHash == VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff.GuidHash)
                        {
                            ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Looks like you have a charmed human. Take care of that before calling your familiar.");
                            return;
                        }
                    }
                }

                if (entityManager.Exists(familiarStasisState.FamiliarEntity))
                {
                    SystemPatchUtil.Enable(familiarStasisState.FamiliarEntity);
                    Follower follower = familiarStasisState.FamiliarEntity.Read<Follower>();
                    follower.Followed._Value = player.Character;
                    follower.ModeModifiable = ModifiableInt.CreateFixed(1);
                    familiarStasisState.FamiliarEntity.Write(follower);
                    familiarStasisState.FamiliarEntity.Write(new Translation { Value = aimPos });
                    familiarStasisState.FamiliarEntity.Write(new LastTranslation { Value = aimPos });
                    familiarStasisState.IsInStasis = false;
                    familiarStasisState.FamiliarEntity = Entity.Null;
                    PlayerFamiliarStasisMap[platformId] = familiarStasisState;
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Your familiar has been summoned.");
                }
                else
                {
                    familiarStasisState.IsInStasis = false;
                    familiarStasisState.FamiliarEntity = Entity.Null;
                    PlayerFamiliarStasisMap[platformId] = familiarStasisState;
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Familiar in stasis couldn't be found to enable, assuming dead. You may now unbind.");
                }

                return;
            }
            else if (!familiarStasisState.IsInStasis)
            {
                Entity familiar = FindPlayerFamiliar(player.Character);
                if (familiar.Equals(Entity.Null))
                {
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "You don't have an active familiar following you.");
                }
                else if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
                {
                    Follower follower = familiar.Read<Follower>();
                    follower.Followed._Value = Entity.Null;
                    familiar.Write(follower);
                    SystemPatchUtil.Disable(familiar);
                    PlayerFamiliarStasisMap[platformId] = new FamiliarStasisState(familiar, true);
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Your familar has been placed in stasis.");
                }
                else
                {
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Couldn't verify familiar to dismiss.");
                }
            }
        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "No bound familiar to summon.");
        }
    }

    public static void ToggleCombat(Player player, ulong playerId)
    {
        Entity Character = Entity.Null;
        // du1

        EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();
        ulong platformId = player.SteamID;
        var buffs = player.Character.ReadBuffer<BuffBuffer>();

        if (DataStructures.PlayerPetsMap.TryGetValue(platformId, out Dictionary<string, PetExperienceProfile> data))
        {
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
            BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(serverGameManager);

            Entity familiar = FindPlayerFamiliar(player.Character);
            if (familiar.Equals(Entity.Null))
            {
                ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Call your familiar before toggling this.");
                return;
            }
            else if (data.TryGetValue(familiar.Read<PrefabGUID>().LookupName().ToString(), out PetExperienceProfile profile) && profile.Active)
            {
                profile.Combat = !profile.Combat; // this will be false when first triggered
                FactionReference factionReference = familiar.Read<FactionReference>();
                PrefabGUID ignored = new(-1430861195);
                PrefabGUID playerfaction = new(1106458752);
                if (!profile.Combat)
                {
                    factionReference.FactionGuid._Value = ignored;
                }
                else
                {
                    factionReference.FactionGuid._Value = playerfaction;
                }

                familiar.Write(factionReference);
                BufferFromEntity<BuffBuffer> bufferFromEntity = VWorld.Server.EntityManager.GetBufferFromEntity<BuffBuffer>();
                if (profile.Combat)
                {
                    //BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff, familiar);
                    AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                    aggroConsumer.Active = ModifiableBool.CreateFixed(true);
                    familiar.Write(aggroConsumer);
                    
                    Aggroable aggroable = familiar.Read<Aggroable>();
                    aggroable.Value = ModifiableBool.CreateFixed(true);
                    aggroable.AggroFactor._Value = 1f;
                    aggroable.DistanceFactor._Value = 1f;
                    familiar.Write(aggroable);
                    BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.Admin_Invulnerable_Buff, familiar);
                    BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, VCreate.Data.Prefabs.AB_Militia_HoundMaster_QuickShot_Buff, familiar);
                }
                else
                {
                    AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
                    aggroConsumer.Active = ModifiableBool.CreateFixed(false);
                    familiar.Write(aggroConsumer);

                    Aggroable aggroable = familiar.Read<Aggroable>();
                    aggroable.Value = ModifiableBool.CreateFixed(false);
                    aggroable.AggroFactor._Value = 0f;
                    aggroable.DistanceFactor._Value = 0f;
                    familiar.Write(aggroable);
                    OnHover.BuffNonPlayer(familiar, VCreate.Data.Prefabs.Admin_Invulnerable_Buff);
                    OnHover.BuffNonPlayer(familiar, VCreate.Data.Prefabs.AB_Militia_HoundMaster_QuickShot_Buff);
                }

                data[familiar.Read<PrefabGUID>().LookupName().ToString()] = profile;
                DataStructures.PlayerPetsMap[platformId] = data;
                DataStructures.SavePetExperience();
                if (!profile.Combat)
                {
                    string disabledColor = VCreate.Core.Toolbox.FontColors.Pink("disabled");
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), $"Combat for familiar is {disabledColor}. It cannot die and won't participate, however, no experience will be gained.");
                }
                else
                {
                    string enabledColor = VCreate.Core.Toolbox.FontColors.Green("enabled");
                    ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), $"Combat for familiar is {enabledColor}. It will fight till glory or death and gain experience.");
                }
            }
            else
            {
                ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "Couldn't find active familiar in followers.");
            }
        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityCommandBuffer, player.User.Read<User>(), "You don't have any familiars.");
            return;
        }
    }

    private static void ToggleCopyMode(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "CopyToggle");
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            string stateMessage = settings.GetMode("CopyToggle") ? enabledColor : disabledColor;
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"CopyMode: |{stateMessage}|");
        }
    }

    private static void ToggleBuffMode(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "BuffToggle");
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            string stateMessage = settings.GetMode("BuffToggle") ? enabledColor : disabledColor;
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"BuffMode: |{stateMessage}|");
        }
    }

    private static void ToggleTileMode(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "TileToggle");
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            string stateMessage = settings.GetMode("TileToggle") ? enabledColor : disabledColor;
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"TileMode: |{stateMessage}|");
        }
    }

    private static void ToggleEquipMode(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "EquipToggle");
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            string stateMessage = settings.GetMode("EquipToggle") ? enabledColor : disabledColor;
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"EquipMode: |{stateMessage}|");
        }
    }

    private static void ToggleDestroyMode(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "DestroyToggle");
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            // The actual value is set in ResetAllToggles; here, we just trigger UI update and messaging
            bool currentValue = settings.GetMode("DestroyToggle");
            string stateMessage = currentValue ? enabledColor : disabledColor; // Notice the change due to toggle reset behavior
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"DestroyMode: |{stateMessage}|");
        }
    }

    private static void ToggleInspectMode(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "InspectToggle");

        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            // The actual value is set in ResetAllToggles; here, we just trigger UI update and messaging
            bool currentValue = settings.GetMode("InspectToggle");
            string stateMessage = currentValue ? enabledColor : disabledColor; // Notice the change due to toggle reset behavior
            DataStructures.SavePlayerSettings();
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"InspectMode: |{stateMessage}|");
        }
    }

    private static void ToggleDebuffMode(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "DebuffToggle");

        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            // The actual value is set in ResetAllToggles; here, we just trigger UI update and messaging
            bool currentValue = settings.GetMode("DebuffToggle");
            string stateMessage = currentValue ? enabledColor : disabledColor; // Notice the change due to toggle reset behavior
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"DebuffMode: |{stateMessage}|");
        }
    }

    private static void ToggleConvert(Player player, ulong playerId)
    {
        ResetAllToggles(playerId, "ConvertToggle");

        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            bool currentValue = settings.GetMode("ConvertToggle");
            string stateMessage = currentValue ? enabledColor : disabledColor; // Notice the change due to toggle reset behavior
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"ConvertMode: |{stateMessage}|");
        }
    }

    private static void ToggleImmortalTiles(Player player, ulong playerId)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            bool currentValue = settings.GetMode("ImmortalToggle");
            settings.SetMode("ImmortalToggle", !currentValue);
            string stateMessage = !currentValue ? enabledColor : disabledColor;
            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"ImmortalTiles: |{stateMessage}|");
        }
    }

    private static void CycleGridSize(Player player, ulong playerId)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out Omnitool settings))
        {
            settings.SetData("GridSize", (settings.GetData("GridSize") + 1) % OnHover.gridSizes.Length);
            //settings.TileSnap = (settings.TileSnap + 1) % OnHover.gridSizes.Length;
            DataStructures.PlayerSettings[playerId] = settings;
            float currentGridSize = OnHover.gridSizes[settings.GetData("GridSize")];
            string colorFloat = VCreate.Core.Toolbox.FontColors.Cyan(currentGridSize.ToString());
            DataStructures.SavePlayerSettings();
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"GridSize: {colorFloat}u");
        }
    }

    private static void ToggleMapIconPlacement(Player player, ulong playerId)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out var settings))
        {
            bool currentValue = settings.GetMode("MapIconToggle");
            settings.SetMode("MapIconToggle", !currentValue);
            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
            string stateMessage = !currentValue ? enabledColor : disabledColor;
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"MapIcons: |{stateMessage}|");
        }
    }
    private static void ToggleTrainer(Player player, ulong playerId)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out var settings))
        {
            bool currentValue = settings.GetMode("Trainer");
            settings.SetMode("Trainer", !currentValue);
            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
            string stateMessage = !currentValue ? enabledColor : disabledColor;
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"TrainerMode: |{stateMessage}|");
        }
    }

    private static void ToggleSnapping(Player player, ulong playerId)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out var settings))
        {
            bool currentValue = settings.GetMode("SnappingToggle");
            settings.SetMode("SnappingToggle", !currentValue);
            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
            string stateMessage = !currentValue ? enabledColor : disabledColor;
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"GridSnapping: |{stateMessage}|");
        }
    }

    private static void ToggleTileRotation(Player player, ulong playerId)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out var settings))
        {
            switch (settings.GetData("Rotation"))
            {
                case 0:
                    settings.SetData("Rotation", 90);
                    break;

                case 90:
                    settings.SetData("Rotation", 180);
                    break;

                case 180:
                    settings.SetData("Rotation", 270);
                    break;

                case 270:
                    settings.SetData("Rotation", 0);
                    break;

                default:
                    settings.SetData("Rotation", 0); // Reset to 0 if somehow an invalid value is set
                    break;
            }

            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
            string colorString = VCreate.Core.Toolbox.FontColors.Cyan(settings.GetData("Rotation").ToString());
            // Assuming you have a similar utility method for sending messages as in your base example
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"TileRotatiom: {colorString}°");
        }
    }

    private static void SetTileRotationTo0(Player player, ulong playerId)
    {
        SetTileRotation(player, playerId, 0);
    }

    private static void SetTileRotationTo90(Player player, ulong playerId)
    {
        SetTileRotation(player, playerId, 90);
    }

    private static void SetTileRotationTo180(Player player, ulong playerId)
    {
        SetTileRotation(player, playerId, 180);
    }

    private static void SetTileRotationTo270(Player player, ulong playerId)
    {
        SetTileRotation(player, playerId, 270);
    }

    // General method to set tile rotation
    private static void SetTileRotation(Player player, ulong playerId, int rotation)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out var settings))
        {
            settings.SetData("Rotation", rotation);
            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), $"TileRotation: {rotation}°");
        }
    }

    //[Command(name: "returnToBody", shortHand: "return", adminOnly: true, usage: ".return", description: "Backup method to return to body on hover.")]

    private static void ResetToggles(Player player, ulong playerId)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out var settings))
        {
            // Default all toggles to false
            settings.SetMode("DestroyToggle", false);
            settings.SetMode("TileToggle", false);
            settings.SetMode("InspectToggle", false);
            settings.SetMode("SnappingToggle", false);
            settings.SetMode("ImmortalToggle", false);
            settings.SetMode("MapIconToggle", false);
            settings.SetMode("CopyToggle", false);
            settings.SetMode("DebuffToggle", false);
            settings.SetMode("ConvertToggle", false);
            settings.SetMode("BuffToggle", false);

            // Enable the exceptToggle, if specified

            // Update the player's build settings in the database
            ServerChatUtils.SendSystemMessageToClient(VWorld.Server.EntityManager, player.User.Read<User>(), "All toggles reset.");
            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
        }
    }

    private static void ResetAllToggles(ulong playerId, string exceptToggle)
    {
        if (DataStructures.PlayerSettings.TryGetValue(playerId, out var settings))
        {
            // Default all toggles to false
            settings.SetMode("DestroyToggle", false);
            settings.SetMode("TileToggle", false);
            settings.SetMode("InspectToggle", false);
            //settings.SetMode("SnappingToggle", false);
            //settings.SetMode("ImmortalToggle", false);
            //settings.SetMode("MapIconToggle", false);
            settings.SetMode("CopyToggle", false);
            settings.SetMode("DebuffToggle", false);
            settings.SetMode("ConvertToggle", false);
            settings.SetMode("BuffToggle", false);

            // Enable the exceptToggle, if specified
            if (!string.IsNullOrEmpty(exceptToggle))
            {
                settings.SetMode(exceptToggle, true);
            }

            // Update the player's build settings in the database
            DataStructures.PlayerSettings[playerId] = settings;
            DataStructures.SavePlayerSettings();
        }
    }

    private static void UndoLastTilePlacement(Player player, ulong playerId)
    {
        EntityManager entityManager = VWorld.Server.EntityManager;
        ulong platformId = playerId; // Assuming playerId maps directly to platformId in your context

        if (DataStructures.PlayerSettings.TryGetValue(platformId, out var settings))
        {
            string lastTileRef = settings.PopEntity();
            if (!string.IsNullOrEmpty(lastTileRef))
            {
                string[] parts = lastTileRef.Split(", ");
                if (parts.Length == 2 && int.TryParse(parts[0], out int index) && int.TryParse(parts[1], out int version))
                {
                    Entity tileEntity = new Entity { Index = index, Version = version };
                    if (entityManager.Exists(tileEntity))
                    {
                        SystemPatchUtil.Destroy(tileEntity);
                        ServerChatUtils.SendSystemMessageToClient(entityManager, player.User.Read<User>(), "Successfully destroyed last tile placed.");
                        DataStructures.SavePlayerSettings();
                    }
                    else
                    {
                        ServerChatUtils.SendSystemMessageToClient(entityManager, player.User.Read<User>(), "Failed to find the last tile placed.");
                    }
                }
                else
                {
                    ServerChatUtils.SendSystemMessageToClient(entityManager, player.User.Read<User>(), "Failed to parse the reference to the last tile placed.");
                }
            }
            else
            {
                ServerChatUtils.SendSystemMessageToClient(entityManager, player.User.Read<User>(), "You have not placed any tiles yet or all undos have been used.");
            }
        }
        else
        {
            ServerChatUtils.SendSystemMessageToClient(entityManager, player.User.Read<User>(), "You have not placed any tiles yet.");
        }
    }
}