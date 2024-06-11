using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bloodcraft.Patches;

[HarmonyPatch]
public class BuffPatch
{
    static readonly PrefabGUID draculaReturnHide = new(404387047);
    static readonly PrefabGUID draculaFinal = new(1269681960); // final stage buff Dracula, force him to evolve -2005193286 (ability group)
    static readonly PrefabGUID solarusFinal = new(2144624015); // final stage buff Solarus, remove 358972271 (holy bubble)
    static readonly PrefabGUID monsterFinal = new(-2079981449); // this one looks like it might just work
    static readonly PrefabGUID phasing = new(-79611032); // lol switch bodies with familiar? hmmm
    static readonly PrefabGUID draculaEvolve = new(-2005193286);
    static readonly PrefabGUID swordBuff = new(-6635580);
    static readonly PrefabGUID highlordSwordBuff = new(-916946628);


    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<PrefabGUID>() || !entity.Has<Buff>()) continue;

                PrefabGUID PrefabGUID = entity.Read<PrefabGUID>();    

                if (entity.Read<Buff>().Target.Has<Follower>() && entity.Read<Buff>().Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    Entity player = entity.Read<Buff>().Target.Read<Follower>().Followed._Value;
                    //entity.LogComponentTypes();
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (Core.EntityManager.Exists(familiar))
                    {
                        //Core.Log.LogInfo(PrefabGUID.LookupName());
                        if (PrefabGUID.Equals(draculaReturnHide))
                        {
                            DestroyUtility.CreateDestroyEvent(Core.EntityManager, entity, DestroyReason.Default, DestroyDebugReason.None);
                        }
                        if (PrefabGUID.Equals(draculaFinal))
                        {
                            //Core.ServerGameManager.ForceCastAbilityGroup(familiar, 15);
                            //Core.Log.LogInfo("Forcing evolution...");
                            
                            DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
                            ApplyBuffDebugEvent applyBuffDebugEvent = new()
                            {
                                BuffPrefabGUID = new(-31099041), // Buff_Vampire_Dracula_SpellPhase
                            };
                            FromCharacter fromCharacter = new()
                            {
                                Character = familiar,
                                User = player.Read<PlayerCharacter>().UserEntity,
                            };
                            // apply level up buff here
                            debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);   
                        }
                        if (PrefabGUID.Equals(swordBuff))
                        {
                            //Core.Log.LogInfo("Sword buff found.");
                            if (Core.ServerGameManager.TryGetBuff(familiar, highlordSwordBuff.ToIdentifier(), out Entity swordPermabuff))
                            {
                                //Core.Log.LogInfo("Highlord sword buff found.");
                                if (swordPermabuff.Has<AmplifyBuff>()) swordPermabuff.Remove<AmplifyBuff>(); 
                            }
                        }
                    }
                }

                if (Plugin.FamiliarSystem.Value && PrefabGUID.LookupName().ToLower().Contains("combat"))
                {
                    if (entity.Read<Buff>().Target.Has<PlayerCharacter>())
                    {
                        Entity player = entity.Read<Buff>().Target;
                        Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(player);
                        if (familiar != Entity.Null && Core.EntityManager.Exists(familiar))
                        {
                            Follower follower = familiar.Read<Follower>();
                            follower.ModeModifiable._Value = 1;
                            familiar.Write(follower);
                            float3 playerPos = player.Read<LocalToWorld>().Position;
                            float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, playerPos);
                            if (distance > 25f)
                            {
                                familiar.Write(new LastTranslation { Value = player.Read<Translation>().Value });
                                familiar.Write(new Translation { Value = player.Read<Translation>().Value });
                                //Core.Log.LogInfo($"Familiar returned to owner.");
                            }
                        }
                        else if (Core.DataStructures.FamiliarActives.TryGetValue(player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId, out var data) && !data.Item1.Equals(Entity.Null))
                        {
                            if (Core.EntityManager.Exists(data.Item1))
                            {
                                float3 playerPos = player.Read<LocalToWorld>().Position;
                                float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, playerPos);
                                if (distance > 25f)
                                {
                                    familiar.Write(new LastTranslation { Value = player.Read<Translation>().Value });
                                    familiar.Write(new Translation { Value = player.Read<Translation>().Value });
                                    //Core.Log.LogInfo($"Familiar returned to owner.");
                                }
                            }
                        }
                    }
                }
                if (Plugin.ProfessionSystem.Value && PrefabGUID.LookupName().ToLower().Contains("consumable") && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(PrefabGUID, "alchemy");
                    ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    int level = handler.GetExperienceData(steamId).Key;
                    if (entity.Has<LifeTime>())
                    {
                        LifeTime lifeTime = entity.Read<LifeTime>();
                        lifeTime.Duration *= (1 + level / Plugin.MaxProfessionLevel.Value);
                        entity.Write(lifeTime);
                    }
                    if (entity.Has<ModifyUnitStatBuff_DOTS>())
                    {
                        var buffer = entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                            statBuff.Value *= (1 + level / Plugin.MaxProfessionLevel.Value);
                            buffer[i] = statBuff;
                        }
                    }
                }
                if (Plugin.FamiliarSystem.Value && PrefabGUID.LookupName().ToLower().Contains("consumable") && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    Entity player = entity.Read<Buff>().Target;
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (familiar != Entity.Null)
                    {
                        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = PrefabGUID,
                        };
                        FromCharacter fromCharacter = new()
                        {
                            Character = familiar,
                            User = player.Read<PlayerCharacter>().UserEntity,
                        };
                        debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    }
                }
                if (Plugin.FamiliarSystem.Value && PrefabGUID.Equals(phasing) && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    Entity player = entity.Read<Buff>().Target;
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (Core.EntityManager.Exists(familiar))
                    {
                        float3 playerPos = player.Read<LocalToWorld>().Position;
                        float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, playerPos);
                        if (distance > 25f)
                        {
                            familiar.Write(new LastTranslation { Value = player.Read<Translation>().Value });
                            familiar.Write(new Translation { Value = player.Read<Translation>().Value });
                            //Core.Log.LogInfo($"Familiar returned to owner.");
                        }
                    }
                    else if (Core.DataStructures.FamiliarActives.TryGetValue(player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId, out var data) && !data.Item1.Equals(Entity.Null))
                    {
                        if (Core.EntityManager.Exists(data.Item1))
                        {
                            float3 playerPos = player.Read<LocalToWorld>().Position;
                            float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, playerPos);
                            if (distance > 25f)
                            {
                                familiar.Write(new LastTranslation { Value = player.Read<Translation>().Value });
                                familiar.Write(new Translation { Value = player.Read<Translation>().Value });
                                //Core.Log.LogInfo($"Familiar returned to owner.");
                            }
                        }
                    }
                }

                if (Plugin.BloodSystem.Value && PrefabGUID.GuidHash.Equals(366323518) && entity.Read<Buff>().Target.Has<PlayerCharacter>()) // feed execute kills
                {
                    ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    Entity killer = entity.Read<EntityOwner>().Owner;
                    BloodSystem.UpdateLegacy(killer, died);
                    if (Plugin.ExpertiseSystem.Value) ExpertiseSystem.UpdateExpertise(killer, died);
                    if (Plugin.LevelingSystem.Value) LevelingSystem.UpdateLeveling(killer, died);
                    if (Plugin.FamiliarSystem.Value)
                    {
                        FamiliarLevelingSystem.UpdateFamiliar(killer, died);
                        FamiliarUnlockSystem.HandleUnitUnlock(killer, died);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(BuffSystem_Spawn_Server __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                PrefabGUID buffPrefab = entity.Read<PrefabGUID>();
                PrefabGUID PrefabGUID = entity.Read<Buff>().Target.Read<PrefabGUID>();

                string buffCheck = buffPrefab.LookupName().ToLower();
                string targetCheck = PrefabGUID.LookupName().ToLower();

                if (!targetCheck.Contains("werewolf") || !targetCheck.Contains("geomancer")) continue;
                if (Plugin.FamiliarSystem.Value && buffCheck.Contains("shapeshift") || buffCheck.Contains("transform"))
                {
                    if (entity.Read<Buff>().Target.Has<Follower>() && entity.Read<Buff>().Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                    {
                        Entity player = entity.Read<Buff>().Target.Read<Follower>().Followed._Value;
                        ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                        Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(player);
                        if (familiar != Entity.Null)
                        {
                            if (Core.FamiliarExperienceManager.LoadFamiliarExperience(steamId).FamiliarExperience.TryGetValue(PrefabGUID.GuidHash, out var xpData))
                            {
                                Core.Log.LogInfo("Handling werewolf familiar or geomancer...");
                                FamiliarSummonSystem.HandleFamiliarModifications(player, familiar, xpData.Key);
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Core.Log.LogError(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(StatChangeMutationSystem), nameof(StatChangeMutationSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StatChangeMutationSystem __instance)
    {
        NativeArray<Entity> entities = __instance._StatChangeEventQuery.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (Plugin.BloodSystem.Value && Plugin.BloodQualityBonus.Value && entity.Has<StatChangeEvent>() && entity.Has<BloodQualityChange>())
                {
                    StatChangeEvent statChangeEvent = entity.Read<StatChangeEvent>();
                    //statChangeEvent.StatChangeEntity.LogComponentTypes();
                    BloodQualityChange bloodQualityChange = entity.Read<BloodQualityChange>();
                    Blood blood = statChangeEvent.Entity.Read<Blood>();
                    BloodSystem.BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(bloodQualityChange.BloodType);
                    ulong steamID = statChangeEvent.Entity.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
                    var bloodQualityBuff = statChangeEvent.Entity.ReadBuffer<BloodQualityBuff>();
                    if (bloodHandler == null)
                    {
                        continue;
                    }

                    float legacyKey = bloodHandler.GetLegacyData(steamID).Value;

                    if (Plugin.PrestigeSystem.Value && Core.DataStructures.PlayerPrestiges.TryGetValue(steamID, out var prestiges) && prestiges.TryGetValue(BloodSystem.BloodPrestigeMap[bloodType], out var bloodPrestige) && bloodPrestige > 0)
                    {
                        legacyKey = (float)bloodPrestige * Plugin.PrestigeBloodQuality.Value;
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
                    }
                    else if (!Plugin.PrestigeSystem.Value)
                    {
                        if (legacyKey > 0)
                        {
                            bloodQualityChange.Quality += legacyKey;
                            bloodQualityChange.ForceReapplyBuff = true;
                            entity.Write(bloodQualityChange);
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Core.Log.LogError(ex);
        }
    }
}