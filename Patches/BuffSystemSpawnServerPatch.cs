using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacy;
using Bloodcraft.Systems.Professions;
using Bloodcraft.SystemUtilities.Quests;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using EnterShapeshiftEvent = ProjectM.Network.EnterShapeshiftEvent;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BuffSpawnSystemPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static DebugEventsSystem DebugEventsSystem => Core.DebugEventsSystem;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly PrefabGUID draculaReturnHide = new(404387047);
    static readonly PrefabGUID draculaFinal = new(1269681960); // final stage buff Dracula, force him to evolve -2005193286 (ability group)
    static readonly PrefabGUID solarusFinal = new(2144624015); // final stage buff Solarus, remove 358972271 (holy bubble)
    static readonly PrefabGUID monsterFinal = new(-2079981449); // this one looks like it might just work
    static readonly PrefabGUID phasing = new(-79611032); // lol switch bodies with familiar? hmmm
    static readonly PrefabGUID draculaEvolve = new(-2005193286);
    static readonly PrefabGUID swordBuff = new(-6635580);
    static readonly PrefabGUID highlordSwordBuff = new(-916946628);
    static readonly PrefabGUID dominateAbility = new(-1908054166);
    static readonly PrefabGUID pvpProtBuff = new(1111481396);
    static readonly PrefabGUID pvpVampire = new(697095869);
    static readonly PrefabGUID combatBuff = new(581443919);
    static readonly PrefabGUID combatStance = new(-952067173);


    static readonly bool Parties = Plugin.Parties.Value;
    static readonly bool PreventFriendlyFire = Plugin.PreventFriendlyFire.Value;
    static readonly bool Familiars = Plugin.FamiliarSystem.Value;
    static GameModeType GameMode => Core.ServerGameSettings.GameModeType;
    /*
    [HarmonyPatch(typeof(DebugEventsSystem.Debug_SpawnBuffs), nameof(DebugEventsSystem.Debug_SpawnBuffs.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(DebugEventsSystem.Debug_SpawnBuffs __instance)
    {
        NativeArray<Entity> entities = __instance._DebugBuffsSpawned.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) continue;
                if (entity.Has<DebugEventsSystem.Debug_ApplyBuffDelayed>())
                {
                    entity.LogComponentTypes();
                    DebugEventsSystem.Debug_ApplyBuffDelayed applyBuffDelayed = entity.Read<DebugEventsSystem.Debug_ApplyBuffDelayed>();
                    if (applyBuffDelayed.Prefab.Has<PrefabGUID>() && applyBuffDelayed.Prefab.Read<PrefabGUID>().Equals(pvpProtBuff))
                    {
                        if (Familiars && applyBuffDelayed.Prefab.Has<Buff>() && applyBuffDelayed.Prefab.Read<Buff>().Target.Has<PlayerCharacter>()) // reapply pvpprot to familiars when applied to owner
                        {
                            Entity player = applyBuffDelayed.Prefab.Read<Buff>().Target;
                            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
                            if (familiar != Entity.Null && Core.EntityManager.Exists(familiar))
                            {
                                ApplyBuffDebugEvent applyBuffDebugEvent = new()
                                {
                                    BuffPrefabGUID = pvpProtBuff,
                                };
                                FromCharacter fromCharacter = new()
                                {
                                    Character = familiar,
                                    User = player.Read<PlayerCharacter>().UserEntity,
                                };
                                Core.DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                                if (Core.ServerGameManager.TryGetBuff(familiar, pvpProtBuff.ToIdentifier(), out Entity buff))
                                {
                                    if (buff.Has<LifeTime>())
                                    {
                                        var lifetime = buff.Read<LifeTime>();
                                        lifetime.Duration = -1;
                                        lifetime.EndAction = LifeTimeEndAction.None;
                                        buff.Write(lifetime);
                                    }
                                }
                            }
                        }
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
    */
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
                if (!EntityManager.Exists(entity.Read<Buff>().Target)) continue;

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();

                //Core.Log.LogInfo($"Buff: {prefabGUID.LookupName()}");
                
                /*
                if (prefabGUID.Equals(holySpinners))
                {
                    Entity character = entity.Read<Buff>().Target;
                    DestroyUtility.Destroy(EntityManager, entity);
                    ServerGameManager.ForceCastAbilityGroup(character, holyBeamHard);
                    Core.Log.LogInfo($"{character.Read<PrefabGUID>().LookupName()} force cast attempt...");
                    continue;
                }
                */

                if (prefabGUID.LookupName().ToLower().Contains("emote_onaggro") && entity.Read<Buff>().Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    Entity player = entity.Read<Buff>().Target.Read<Follower>().Followed._Value;
                    ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (Core.DataStructures.PlayerBools.TryGetValue(steamId, out var bools) && !bools["VBloodEmotes"])
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                        //Core.Log.LogInfo($"VBlood familiar emote destroyed~");
                        continue;
                    }
                }
                if (entity.Read<Buff>().Target.Has<Follower>() && entity.Read<Buff>().Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    Entity player = entity.Read<Buff>().Target.Read<Follower>().Followed._Value;
                    Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (EntityManager.Exists(familiar))
                    {
                        //Core.Log.LogInfo(PrefabGUID.LookupName());
                        if (prefabGUID.Equals(draculaReturnHide))
                        {
                            DestroyUtility.CreateDestroyEvent(EntityManager, entity, DestroyReason.Default, DestroyDebugReason.None);
                        }
                        if (prefabGUID.Equals(draculaFinal))
                        {
                            //Core.ServerGameManager.ForceCastAbilityGroup(familiar, 15);
                            //Core.Log.LogInfo("Forcing evolution...");
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
                            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        }
                        if (prefabGUID.Equals(swordBuff))
                        {
                            if (ServerGameManager.TryGetBuff(familiar, highlordSwordBuff.ToIdentifier(), out Entity swordPermabuff))
                            {
                                if (swordPermabuff.Has<AmplifyBuff>()) swordPermabuff.Remove<AmplifyBuff>();
                            }
                        }
                    }
                } // cassius, drac, other weird boss phase stuff

                if (Plugin.FamiliarSystem.Value && prefabGUID.Equals(pvpVampire))
                {
                    if (entity.Read<Buff>().Target.Has<PlayerCharacter>())
                    {
                        Entity player = entity.Read<Buff>().Target;
                        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);

                        if (EntityManager.Exists(familiar))
                        {
                            UnitStats unitStats = familiar.Read<UnitStats>();
                            unitStats.PvPProtected._Value = false;
                            familiar.Write(unitStats);
                        }
                    }
                } // remove pvp protection from familiar when owner in pvp

                if (Plugin.FamiliarSystem.Value && prefabGUID.Equals(pvpProtBuff))
                {
                    if (entity.Read<Buff>().Target.Has<PlayerCharacter>())
                    {
                        Entity player = entity.Read<Buff>().Target;
                        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);

                        if (EntityManager.Exists(familiar))
                        {
                            UnitStats unitStats = familiar.Read<UnitStats>();
                            unitStats.PvPProtected._Value = true;
                            familiar.Write(unitStats);

                            /*
                            ApplyBuffDebugEvent applyBuffDebugEvent = new()
                            {
                                BuffPrefabGUID = pvpProtBuff,
                            };
                            FromCharacter fromCharacter = new()
                            {
                                Character = familiar,
                                User = player.Read<PlayerCharacter>().UserEntity,
                            };
                            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                            if (ServerGameManager.TryGetBuff(familiar, applyBuffDebugEvent.BuffPrefabGUID.ToIdentifier(), out Entity buff))
                            {
                                if (buff.Has<LifeTime>())
                                {
                                    var lifetime = buff.Read<LifeTime>();
                                    lifetime.Duration = -1;
                                    lifetime.EndAction = LifeTimeEndAction.None;
                                    buff.Write(lifetime);
                                }
                            }
                            */
                        }
                    }
                } // add pvp protection to familiar when applied to owner

                if (Plugin.FamiliarSystem.Value && (prefabGUID.Equals(combatStance) || prefabGUID.Equals(combatBuff)))
                {
                    Buff buff = entity.Read<Buff>();
                    if (buff.Target.Has<PlayerCharacter>())
                    {
                        Entity player = buff.Target;
                        //Core.Log.LogInfo($"Combat buff applied to {player.Read<PlayerCharacter>().Name.Value}...");
                        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
                        if (EntityManager.Exists(familiar))
                        {
                            Follower following = familiar.Read<Follower>();
                            following.ModeModifiable._Value = 1;
                            familiar.Write(following);
                            float3 playerPos = player.Read<Translation>().Value;
                            float distance = UnityEngine.Vector3.Distance(familiar.Read<Translation>().Value, playerPos);
                            if (distance > 25f)
                            {
                                familiar.Write(new LastTranslation { Value = player.Read<Translation>().Value });
                                familiar.Write(new Translation { Value = player.Read<Translation>().Value });
                                //Core.Log.LogInfo($"Familiar returned to owner.");
                            }
                        }
                        /*
                        else if (Core.DataStructures.FamiliarActives.TryGetValue(player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId, out var data) && !data.Familiar.Equals(Entity.Null))
                        {
                            if (EntityManager.Exists(data.Familiar))
                            {
                                float3 playerPos = player.Read<LocalToWorld>().Position;
                                float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, playerPos);
                                if (distance > 25f)
                                {
                                    familiar.Write(new LastTranslation { Value = player.Read<Translation>().Value });
                                    familiar.Write(new Translation { Value = player.Read<Translation>().Value });
                                }
                            }
                        }
                        */
                    }
                    /*
                    else if (buff.Target.Has<Follower>() && buff.Target.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                    {
                        Entity familiar = buff.Target;
                        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();

                        if ((GameMode.Equals(GameModeType.PvE) || Core.ServerGameManager.HasBuff(aggroConsumer.AggroTarget._Entity, pvpProtBuff.ToIdentifier())))
                        {
                            Follower follower = familiar.Read<Follower>();
                            follower.ModeModifiable._Value = 0;
                            familiar.Write(follower);
                        }
                    }
                    */
                } // return familiar when entering combat if far away or prevent familiar targetting other familiar when pvpprotected

                if (Plugin.ProfessionSystem.Value && prefabGUID.LookupName().ToLower().Contains("consumable") && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    //Core.Log.LogInfo($"Consumable found: {prefabGUID.LookupName()} for alchemy boosting...");
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID, "alchemy");
                    ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    int level = handler.GetExperienceData(steamId).Key;

                    if (entity.Has<RemoveBuffOnGameplayEvent>()) entity.Remove<RemoveBuffOnGameplayEvent>();
                    if (entity.Has<RemoveBuffOnGameplayEventEntry>()) entity.Remove<RemoveBuffOnGameplayEventEntry>();
                    //if (entity.Has<GameplayEventListeners>()) entity.Remove<GameplayEventListeners>();

                    if (entity.Has<LifeTime>())
                    {
                        LifeTime lifeTime = entity.Read<LifeTime>();
                        if (lifeTime.Duration != -1) lifeTime.Duration *= (float)(1 + (float)level / (float)Plugin.MaxProfessionLevel.Value);
                        entity.Write(lifeTime);
                    }
                    if (entity.Has<ModifyUnitStatBuff_DOTS>())
                    {
                        var buffer = entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                            statBuff.Value *= (float)(1 + (float)level / (float)Plugin.MaxProfessionLevel.Value);
                            buffer[i] = statBuff;
                        }
                    }
                } // alchemy stuff

                if (Plugin.FamiliarSystem.Value && prefabGUID.LookupName().ToLower().Contains("consumable") && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    Entity player = entity.Read<Buff>().Target;
                    Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (familiar != Entity.Null)
                    {
                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = prefabGUID,
                        };
                        FromCharacter fromCharacter = new()
                        {
                            Character = familiar,
                            User = player.Read<PlayerCharacter>().UserEntity,
                        };
                        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    }
                } // familiar potion sharing

                if (Plugin.FamiliarSystem.Value && prefabGUID.Equals(phasing) && entity.Read<Buff>().Target.Has<PlayerCharacter>()) // teleport familiar to player after waygate
                {
                    Entity player = entity.Read<Buff>().Target;
                    Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(player);
                    if (EntityManager.Exists(familiar))
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
                    else if (Core.DataStructures.FamiliarActives.TryGetValue(player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId, out var data) && !data.Familiar.Equals(Entity.Null))
                    {
                        if (EntityManager.Exists(data.Familiar))
                        {
                            float3 playerPos = player.Read<LocalToWorld>().Position;
                            float distance = UnityEngine.Vector3.Distance(familiar.Read<LocalToWorld>().Position, playerPos);
                            if (distance > 25f)
                            {
                                familiar.Write(new LastTranslation { Value = player.Read<Translation>().Value });
                                familiar.Write(new Translation { Value = player.Read<Translation>().Value });
                            }
                        }
                    }
                }

                if (prefabGUID.GuidHash.Equals(366323518) && entity.Read<Buff>().Target.Has<PlayerCharacter>()) // feed execute kills
                {
                    //ulong steamId = entity.Read<Buff>().Target.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    //Entity killer = entity.Read<EntityOwner>().Owner;
                    Entity killer = entity.Read<Buff>().Target;
                    Entity userEntity = killer.Read<PlayerCharacter>().UserEntity;
                    if (Plugin.BloodSystem.Value) LegacyUtilities.UpdateLegacy(killer, died);
                    if (Plugin.ExpertiseSystem.Value) ExpertiseUtilities.UpdateExpertise(killer, died);
                    if (Plugin.LevelingSystem.Value) PlayerLevelingUtilities.UpdateLeveling(killer, died);
                    if (Plugin.FamiliarSystem.Value)
                    {
                        FamiliarLevelingUtilities.UpdateFamiliar(killer, died);
                        FamiliarUnlockUtilities.HandleUnitUnlock(killer, died);
                    }
                    if (Plugin.QuestSystem.Value)
                    {
                        QuestUtilities.UpdateQuests(killer, userEntity, died.Read<PrefabGUID>());
                    }
                }

                if (entity.Read<Buff>().Target.Has<PlayerCharacter>() && entity.Read<EntityOwner>().Owner.Has<Follower>() && entity.Read<EntityOwner>().Owner.Read<Follower>().Followed._Value.Has<PlayerCharacter>())
                {
                    if (Plugin.FamiliarSystem.Value)
                    {
                        Follower follower = entity.Read<EntityOwner>().Owner.Read<Follower>();
                        if (GameMode.Equals(GameModeType.PvE)) // always stop in PvE
                        {
                            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null)
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event PvE...");
                                //Core.EntityManager.DestroyEntity(entity);
                                DestroyUtility.CreateDestroyEvent(EntityManager, entity, DestroyReason.Default, DestroyDebugReason.TryRemoveBuff);
                                continue;
                            }
                        }
                        else if (ServerGameManager.TryGetBuff(entity.Read<Buff>().Target, pvpProtBuff.ToIdentifier(), out Entity _)) // account for KindredArenas <3
                        {
                            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null)
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event PvP protected...");
                                //Core.EntityManager.DestroyEntity(entity);
                                DestroyUtility.CreateDestroyEvent(EntityManager, entity, DestroyReason.Default, DestroyDebugReason.TryRemoveBuff);
                                continue;
                            }
                        }
                        else if (Parties && PreventFriendlyFire) // check for parties in PvP 
                        {
                            Dictionary<ulong, HashSet<string>> playerParties = Core.DataStructures.PlayerParties;
                            string targetName = entity.Read<Buff>().Target.Read<PlayerCharacter>().Name.Value;
                            string ownerName = follower.Followed._Value.Read<PlayerCharacter>().Name.Value;

                            Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(follower.Followed._Value);
                            if (familiar != Entity.Null && playerParties.Values.Any(set => set.Contains(targetName) && set.Contains(ownerName)))
                            {
                                //Core.Log.LogInfo($"Destroying familiar damage event Parties & PreventFriendlyFire...");
                                //Core.EntityManager.DestroyEntity(entity);
                                DestroyUtility.CreateDestroyEvent(EntityManager, entity, DestroyReason.Default, DestroyDebugReason.TryRemoveBuff);
                                continue;
                            }
                        }
                    }
                } // stop debuff and other negative effects for parties and such
                /*
                else if (Parties && PreventFriendlyFire && !GameMode.Equals(GameModeType.PvE) && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>() && entity.Read<Buff>().Target.Has<PlayerCharacter>())
                {
                    Dictionary<ulong, HashSet<string>> playerParties = Core.DataStructures.PlayerParties;
                    string targetName = entity.Read<Buff>().Target.Read<PlayerCharacter>().Name.Value;
                    string sourceName = entity.Read<EntityOwner>().Owner.Read<PlayerCharacter>().Name.Value;
                    if (playerParties.Values.Any(set => set.Contains(targetName) && set.Contains(sourceName)))
                    {
                        Core.EntityManager.DestroyEntity(entity);
                        continue;
                    }
                }
                */
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
                if (enterShapeshiftEvent.Shapeshift.Equals(dominateAbility))
                {
                    Entity character = fromCharacter.Character;
                    Entity userEntity = fromCharacter.User;
                    ulong steamId = userEntity.Read<User>().PlatformId;
                    Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);
                    if (familiar != Entity.Null && !familiar.Has<Disabled>())
                    {
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
    
    [HarmonyPatch(typeof(UpdateBuffsBuffer_Destroy), nameof(UpdateBuffsBuffer_Destroy.OnUpdate))]
    [HarmonyPostfix]
    static void OnUpdatePostix(UpdateBuffsBuffer_Destroy __instance)
    {
        NativeArray<Entity> entities = __instance.__query_401358720_0.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {                
                if (!Core.hasInitialized) continue;
                if (entity.Read<PrefabGUID>().Equals(combatBuff))
                {
                    Entity character = entity.Read<Buff>().Target;
                    if (character.Has<PlayerCharacter>())
                    {
                        Entity familiar = FamiliarSummonUtilities.FamiliarUtilities.FindPlayerFamiliar(character);
                        if (EntityManager.Exists(familiar))
                        {
                            CombatMusicListener_Shared combatMusicListener_Shared = character.Read<CombatMusicListener_Shared>();
                            combatMusicListener_Shared.UnitPrefabGuid = PrefabGUID.Empty;
                            character.Write(combatMusicListener_Shared);
                        }
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