using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BuffSpawnSystemPatches
{
    static readonly PrefabGUID draculaReturnHide = new(404387047);
    static readonly PrefabGUID draculaFinal = new(1269681960); // final stage buff Dracula, force him to evolve -2005193286 (ability group)
    static readonly PrefabGUID solarusFinal = new(2144624015); // final stage buff Solarus, remove 358972271 (holy bubble)
    static readonly PrefabGUID monsterFinal = new(-2079981449); // this one looks like it might just work
    static readonly PrefabGUID phasing = new(-79611032); // lol switch bodies with familiar? hmmm
    static readonly PrefabGUID draculaEvolve = new(-2005193286);
    static readonly PrefabGUID swordBuff = new(-6635580);
    static readonly PrefabGUID highlordSwordBuff = new(-916946628);
    static readonly PrefabGUID dominateAbility = new(-1908054166);

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (!entity.Has<PrefabGUID>() || !entity.Has<Buff>()) continue;

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                //Core.Log.LogInfo(prefabGUID.LookupName());

                if (entity.Read<Buff>().Target.Has<Follower>() && entity.Read<Buff>().Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>()) // 914043867 golem
                {
                    Entity player = entity.Read<Buff>().Target.Read<Follower>().Followed._Value;
                    //entity.LogComponentTypes();
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (Core.EntityManager.Exists(familiar))
                    {
                        //Core.Log.LogInfo(PrefabGUID.LookupName());
                        if (prefabGUID.Equals(draculaReturnHide))
                        {
                            DestroyUtility.CreateDestroyEvent(Core.EntityManager, entity, DestroyReason.Default, DestroyDebugReason.None);
                        }
                        if (prefabGUID.Equals(draculaFinal))
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
                        if (prefabGUID.Equals(swordBuff))
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

                if (Plugin.FamiliarSystem.Value && prefabGUID.LookupName().ToLower().Contains("combat"))
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

                if (Plugin.ProfessionSystem.Value && prefabGUID.LookupName().ToLower().Contains("consumable") && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID, "alchemy");
                    ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    int level = handler.GetExperienceData(steamId).Key;
                    if (entity.Has<LifeTime>())
                    {
                        LifeTime lifeTime = entity.Read<LifeTime>();
                        if (lifeTime.Duration != -1) lifeTime.Duration *= (1 + level / Plugin.MaxProfessionLevel.Value);
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

                if (Plugin.FamiliarSystem.Value && prefabGUID.LookupName().ToLower().Contains("consumable") && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    Entity player = entity.Read<Buff>().Target;
                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (familiar != Entity.Null)
                    {
                        DebugEventsSystem debugEventsSystem = Core.DebugEventsSystem;
                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = prefabGUID,
                        };
                        FromCharacter fromCharacter = new()
                        {
                            Character = familiar,
                            User = player.Read<PlayerCharacter>().UserEntity,
                        };
                        debugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    }
                }

                if (Plugin.FamiliarSystem.Value && prefabGUID.Equals(phasing) && entity.Read<Buff>().Target.Has<PlayerCharacter>())
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

                if (prefabGUID.GuidHash.Equals(366323518) && entity.Read<Buff>().Target.Has<PlayerCharacter>()) // feed execute kills
                {
                    ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    Entity killer = entity.Read<EntityOwner>().Owner;
                    if (Plugin.BloodSystem.Value) BloodSystem.UpdateLegacy(killer, died);
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
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(ShapeshiftSystem), nameof(ShapeshiftSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(ShapeshiftSystem __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                EnterShapeshiftEvent enterShapeshiftEvent = entity.Read<EnterShapeshiftEvent>();
                FromCharacter fromCharacter = entity.Read<FromCharacter>();
                //Core.Log.LogInfo(enterShapeshiftEvent.Shapeshift.LookupName());
                if (enterShapeshiftEvent.Shapeshift.Equals(dominateAbility))
                {
                    Entity character = fromCharacter.Character;
                    Entity userEntity = fromCharacter.User;
                    ulong steamId = userEntity.Read<User>().PlatformId;

                    //Core.Log.LogInfo("Dominate buff found, dismissing familiar if present and not disabled...");

                    Entity familiar = FamiliarSummonSystem.FamiliarUtilities.FindPlayerFamiliar(character);
                    if (familiar != Entity.Null && !familiar.Has<Disabled>())
                    {
                        //Core.Log.LogInfo("Familiar found, dismissing...");
                        EmoteSystemPatch.CallDismiss(userEntity, character, steamId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogInfo(ex);
        }
        finally
        {
            entities.Dispose();
        }
    }
}