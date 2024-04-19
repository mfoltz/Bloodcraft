using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Collections;
using Unity.Entities;
using Unity.Services.Core;
using Unity.Transforms;
using VCreate.Core;
using VCreate.Core.Commands;
using VCreate.Core.Toolbox;
using VCreate.Data;
using VCreate.Hooks;
using VCreate.Systems;
using VRising.GameData.Models;
using static ProjectM.BuffUtility;

[HarmonyPatch(typeof(FollowerSystem), nameof(FollowerSystem.OnUpdate))]
public static class FollowerSystemPatchV2
{
    private static readonly PrefabGUID charm = VCreate.Data.Prefabs.AB_Charm_Active_Human_Buff;
    public static bool spawned = false;

    public static void Prefix(FollowerSystem __instance)
    {
        EntityManager entityManager = VWorld.Server.EntityManager;
        ServerGameManager serverGameManager = VWorld.Server.GetExistingSystem<ServerScriptMapper>()._ServerGameManager;
        BuffUtility.BuffSpawner buffSpawner = BuffUtility.BuffSpawner.Create(serverGameManager);

        EntityCommandBufferSystem entityCommandBufferSystem = VWorld.Server.GetExistingSystem<EntityCommandBufferSystem>();
        EntityCommandBuffer entityCommandBuffer = entityCommandBufferSystem.CreateCommandBuffer();

        NativeArray<Entity> entities = __instance.__OnUpdate_LambdaJob0_entityQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                Follower follower = entity.Read<Follower>();
                Entity followed = follower.Followed._Value;

                if (entity.Read<PrefabGUID>().Equals(VCreate.Data.Prefabs.CHAR_Mount_Horse_Spectral))
                {
                    // horse stuff

                    //Plugin.Log.LogInfo("Checking for event horse in FollowerSystemV2...");
                    HorseUtility.HandleHorse(entity, entityManager, buffSpawner, entityCommandBuffer);
                    continue;
                }

                if (!followed.Has<PlayerCharacter>()) continue;

                bool charmCheck = BuffUtility.TryGetBuff(entity, charm, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);

                if (charmCheck && !entity.Read<PrefabGUID>().Equals(VCreate.Data.Prefabs.CHAR_Mount_Horse_Spectral))
                {
                    Entity userEntity = followed.Read<PlayerCharacter>().UserEntity;

                    int check = entity.Read<PrefabGUID>().GuidHash;
                    ulong steamId = userEntity.Read<User>().PlatformId;
                    if (DataStructures.PlayerSettings.TryGetValue(steamId, out var dataset))
                    {
                        if (!dataset.Familiar.Equals(check) || !dataset.Binding)
                        {
                            //Plugin.Log.LogInfo("Failed set familiar check or no binding flag, returning.");
                            continue;
                        }
                        else
                        {
                            Plugin.Log.LogInfo("Found unbound/inactive, set familiar, removing charm and binding...");

                            BuffUtility.TryRemoveBuff(ref buffSpawner, entityCommandBuffer, charm, entity);

                            OnHover.ConvertCharacter(userEntity, entity);

                            continue;
                        }
                    }
                } //handle binding familiars, ignore charmed humans

                ulong followedSteamId = followed.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                if (!StateCheckUtility.ValidatePlayerState(followed, followedSteamId, entityManager))
                {
                    // if invalid state then autodismiss
                    //Plugin.Log.LogInfo("AutoDismiss called");
                    StateCheckUtility.AutoDismiss(followed, followedSteamId, entityManager);
                }
                //Plugin.Log.LogInfo("Skipping AutoDismiss called");
                if (DataStructures.PlayerSettings.TryGetValue(followedSteamId, out var omnitool))
                {
                    PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                    if (!omnitool.Familiar.Equals(prefabGUID.GuidHash)) continue;
                }
            }
        }
        catch (Exception ex)
        {
            // Log or handle the error as needed
            Plugin.Log.LogError(ex);
        }
        finally
        {
            // Ensure entities are disposed of even if an exception occurs
            entities.Dispose();
        }
    }

    public static class HorseUtility
    {
        private static readonly PrefabGUID siege = VCreate.Data.Prefabs.MapIcon_Siege_Summon_T02_Complete;

        public static void HandleHorse(Entity entity, EntityManager entityManager, BuffSpawner buffSpawner, EntityCommandBuffer ecb)
        {
            bool isCharmed = BuffUtility.TryGetBuff(entity, charm, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
            if (isCharmed)
            {
                BuffUtility.TryRemoveBuff(ref buffSpawner, ecb, charm, entity);
            }
            else
            {
                //Plugin.Log.LogInfo("Not an event horse...");
                return;
            }
            if (spawned)
            {
                return;
            }
            //Plugin.Log.LogInfo("Charmed horse detected in FollowerSystemV2...");
            //DestroyUtility.CreateDestroyEvent(entityManager, buff.Entity, DestroyReason.Default, DestroyDebugReason.None); // destroy charm buff

            ModifiableFloat fixedFloat = ModifiableFloat.CreateFixed(1f);

            Mountable mountable = entity.Read<Mountable>(); // reduce max speed of horse
            mountable.MaxSpeed = 4.5f;
            entity.Write(mountable);

            UnitStats stats = entity.Read<UnitStats>(); // make immune to damage
            stats.PhysicalResistance = fixedFloat;
            stats.SpellResistance = fixedFloat;
            entity.Write(stats);
            // buff with third eye
            //Plugin.Log.LogInfo("Horse modifications complete.");
            // do announcement here as well
            string colorHorse = VCreate.Core.Toolbox.FontColors.Cyan("Jingles");
            string colorCrystal = "Crystals";
            ServerChatUtils.SendSystemMessageToAllClients(ecb, $"{colorHorse} has appeared! Be the first to take him back to your castle and dismount for rewards.");
            spawned = true;
            try
            {
                Plugin.Log.LogInfo("Attaching cursed area...");
                Entity curse = VWorld.Server.GetExistingSystem<PrefabCollectionSystem>()._PrefabGuidToEntityMap[VCreate.Data.Prefabs.TM_Cursed_Zone_Area01];

                Entity newCurse = entityManager.Instantiate(curse);

                if (!Utilities.HasComponent<AttachMapIconsToEntity>(newCurse))
                {
                    entityManager.AddBuffer<AttachMapIconsToEntity>(newCurse);
                }
                newCurse.Write<Translation>(new Translation { Value = entity.Read<Translation>().Value });
                var maps = newCurse.ReadBuffer<AttachMapIconsToEntity>();
                maps.Add(new AttachMapIconsToEntity { Prefab = siege });
                VCreate.Hooks.MountSystem_ServerPatch.cursedTile = newCurse;
                VCreate.Hooks.MountSystem_ServerPatch.eventHorse = entity;
                Plugin.Log.LogInfo("Cursed area spawned at horse location.");
                if (Utilities.HasComponent<NameableInteractable>(entity))
                {
                    NameableInteractable nameable= entity.Read<NameableInteractable>();
                    nameable.Name = "Jingles";
                    nameable.OnlyAllyRename = true;
                    nameable.OnlyAllySee = false;
                    entity.Write(nameable);
                }
                else
                {
                    NameableInteractable newNameable = new NameableInteractable { Name = "Jingles", OnlyAllyRename = true, OnlyAllySee = false};
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }

    public static class StateCheckUtility
    {
        public static bool ValidatePlayerState(Entity followed, ulong followedSteamId, EntityManager entityManager)
        {
            bool dominatingPresenceCheck = BuffUtility.TryGetBuff(followed, VCreate.Data.Prefabs.AB_Shapeshift_DominatingPresence_PsychicForm_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
            bool ratCheck = BuffUtility.TryGetBuff(followed, VCreate.Data.Prefabs.AB_Shapeshift_Rat_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
            bool batCheck = BuffUtility.TryGetBuff(followed, VCreate.Data.Prefabs.AB_Shapeshift_Bat_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);
            bool frogCheck = BuffUtility.TryGetBuff(followed, VCreate.Data.Prefabs.AB_Shapeshift_Toad_Buff, entityManager.GetBufferFromEntity<BuffBuffer>(true), out var _);

            if (dominatingPresenceCheck || ratCheck || batCheck || frogCheck)
            {
                return false;
            }

            return true;
        }

        public static void AutoDismiss(Entity followed, ulong followedSteamId, EntityManager entityManager)
        {
            Entity fam = PetCommands.FindPlayerFamiliar(followed);
            if (fam.Equals(Entity.Null)) return;
            if (DataStructures.PlayerSettings.TryGetValue(followedSteamId, out var pet) && pet.Familiar.Equals(fam.Read<PrefabGUID>().GuidHash))
            {
                // If shapeshifted dismiss familiar
                //Plugin.Log.LogInfo("AutoDismiss called, dismissing familiar...");
                string name = entityManager.GetComponentData<PlayerCharacter>(followed).Name.ToString();
                if (!VCreate.Core.Services.PlayerService.TryGetPlayerFromString(name, out var player)) return;
                VCreate.Hooks.EmoteSystemPatch.CallDismiss(player, followedSteamId);
                //Plugin.Log.LogInfo($"{name} {followedSteamId} familiar dismissed.");
            }
        }
    }
}