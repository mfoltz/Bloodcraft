using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using static Bloodcraft.Utilities;
using EnterShapeshiftEvent = ProjectM.Network.EnterShapeshiftEvent;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BuffSpawnSystemPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService; 
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;

    static readonly GameModeType GameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

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
    static readonly PrefabGUID holyBeamPowerBuff = new(-1584595113);
    static readonly PrefabGUID solarus = new(-740796338);
    static readonly PrefabGUID generalStunDebuffFX = new(796254181);
    static readonly PrefabGUID generalStunDebuff = new(355774169);
    static readonly PrefabGUID meredithVBlood = new(850622034);
    static readonly PrefabGUID feedExecute = new(366323518);
    static readonly PrefabGUID insideBuff = new(-1930363607);
    static readonly PrefabGUID minionDeathBuff = new(2086395440);

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
                if (!entity.GetBuffTarget().Exists()) continue;

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                string prefabName = prefabGUID.LookupName().ToLower();

                Core.Log.LogInfo(prefabName); // check if spawn buff for minions from fam with player as owner

                Entity player;
                
                if (prefabName.Contains("holybubble"))
                {
                    Entity character = entity.GetBuffTarget();

                    if (character.Read<PrefabGUID>().Equals(solarus) && !ServerGameManager.HasBuff(character, holyBeamPowerBuff))
                    {
                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = holyBeamPowerBuff,
                        };

                        FromCharacter fromCharacter = new()
                        {
                            Character = character,
                            User = character
                        };

                        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        if (ServerGameManager.TryGetBuff(character, holyBeamPowerBuff.ToIdentifier(), out Entity buff))
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
                    continue;
                }

                if (prefabName.Contains("emote_onaggro") && entity.GetBuffTarget().TryGetFollowedPlayer(out player))
                {
                    ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (!GetPlayerBool(steamId, "VBloodEmotes"))
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                    continue;
                }

                if (entity.GetBuffTarget().TryGetFollowedPlayer(out player))
                {
                    Entity familiar = FindPlayerFamiliar(player);
                    if (familiar.Exists())
                    {
                        if (prefabGUID.Equals(draculaReturnHide))
                        {
                            DestroyUtility.CreateDestroyEvent(EntityManager, entity, DestroyReason.Default, DestroyDebugReason.None);
                        }

                        if (prefabGUID.Equals(draculaFinal))
                        {
                            ApplyBuffDebugEvent applyBuffDebugEvent = new()
                            {
                                BuffPrefabGUID = new(-31099041), // Buff_Vampire_Dracula_SpellPhase
                            };

                            FromCharacter fromCharacter = new()
                            {
                                Character = familiar,
                                User = player.Read<PlayerCharacter>().UserEntity,
                            };
                            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                        }

                        if (prefabGUID.Equals(swordBuff))
                        {
                            if (ServerGameManager.TryGetBuff(familiar, highlordSwordBuff.ToIdentifier(), out Entity swordPermabuff))
                            {
                                if (swordPermabuff.Has<AmplifyBuff>()) swordPermabuff.Remove<AmplifyBuff>();
                            }
                        }

                        if (entity.Has<EntityOwner>() && entity.Read<EntityOwner>().Owner.Has<PlayerCharacter>()) // if from player and targeting familiar, destroy buff entity
                        {
                            DestroyUtility.Destroy(EntityManager, entity);
                            continue;
                        }
                    }
                } // cassius, drac, other weird boss phase stuff

                if (ConfigService.FamiliarSystem && (prefabGUID.Equals(combatStance) || prefabGUID.Equals(combatBuff)))
                {
                    if (entity.GetBuffTarget().TryGetPlayer(out player))
                    {
                        Entity familiar = FindPlayerFamiliar(player);
                        if (EntityManager.Exists(familiar))
                        {
                            ReturnFamiliar(player, familiar);
                        }
                    }
                    continue;
                } // return familiar when entering combat if far enough away

                if (ConfigService.ProfessionSystem && prefabName.Contains("consumable") && entity.GetBuffTarget().TryGetPlayer(out player))
                {
                    IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID, "alchemy");
                    ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    int level = handler.GetProfessionData(steamId).Key;

                    if (ConfigService.PotionStacking)
                    {
                        if (entity.Has<RemoveBuffOnGameplayEvent>()) entity.Remove<RemoveBuffOnGameplayEvent>();
                        if (entity.Has<RemoveBuffOnGameplayEventEntry>()) entity.Remove<RemoveBuffOnGameplayEventEntry>();
                    }

                    if (entity.Has<LifeTime>())
                    {
                        LifeTime lifeTime = entity.Read<LifeTime>();
                        if (lifeTime.Duration != -1) lifeTime.Duration *= (float)(1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                        entity.Write(lifeTime);
                    }

                    if (entity.Has<ModifyUnitStatBuff_DOTS>())
                    {
                        var buffer = entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                            statBuff.Value *= (float)(1 + (float)level / (float)ConfigService.MaxProfessionLevel);
                            buffer[i] = statBuff;
                        }
                    }
                } // alchemy bonuses/potion stacking

                if (ConfigService.FamiliarSystem && prefabName.Contains("consumable") && entity.GetBuffTarget().TryGetPlayer(out player))
                {
                    Entity familiar = FindPlayerFamiliar(player);
                    if (familiar != Entity.Null)
                    {
                        ApplyBuffDebugEvent applyBuffDebugEvent = new()
                        {
                            BuffPrefabGUID = prefabGUID,
                        };

                        FromCharacter fromCharacter = new()
                        {
                            Character = familiar,
                            User = familiar
                        };

                        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
                    }
                    continue;
                } // familiar potion sharing

                if (ConfigService.FamiliarSystem && prefabGUID.Equals(phasing) && entity.GetBuffTarget().TryGetPlayer(out player)) // teleport familiar to player after waygate
                {
                    Entity familiar = FindPlayerFamiliar(player);
                    if (familiar.Exists())
                    {
                        ReturnFamiliar(player, familiar);
                    }
                    else if (player.GetSteamId().TryGetFamiliarActives(out var data))
                    {
                        if (data.Familiar.Exists())
                        {
                            ReturnFamiliar(player, familiar);
                        }
                    }
                    continue;
                }

                if (prefabGUID.Equals(feedExecute) && entity.GetBuffTarget().TryGetPlayer(out player)) // feed execute kills
                {
                    Entity died = entity.Read<SpellTarget>().Target._Entity;
                    Entity userEntity = player.Read<PlayerCharacter>().UserEntity;
                    if (ConfigService.BloodSystem) BloodSystem.UpdateLegacy(player, died);
                    if (ConfigService.ExpertiseSystem) WeaponSystem.UpdateExpertise(player, died);
                    if (ConfigService.LevelingSystem) LevelingSystem.UpdateLeveling(player, died);
                    if (ConfigService.FamiliarSystem)
                    {
                        FamiliarLevelingSystem.UpdateFamiliar(player, died);
                        FamiliarUnlockSystem.HandleUnitUnlock(player, died);
                    }
                    if (ConfigService.QuestSystem)
                    {
                        QuestSystem.UpdateQuests(player, userEntity, died.Read<PrefabGUID>());
                    }
                    continue; // not needed right now since last if block but probably adding more later and don't want to forget to do this
                }
            }
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

                    Entity familiar = FindPlayerFamiliar(character);
                    if (familiar.Exists() && !familiar.Disabled())
                    {
                        EmoteSystemPatch.CallDismiss(userEntity, character, steamId);
                    }
                }
            }
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

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();

                if (ConfigService.FamiliarSystem && prefabGUID.Equals(combatBuff))
                {
                    if (entity.GetBuffTarget().TryGetPlayer(out Entity player))
                    {
                        Entity familiar = FindPlayerFamiliar(player);
                        if (familiar.Exists())
                        {
                            player.With((ref CombatMusicListener_Shared shared) =>
                            {
                                shared.UnitPrefabGuid = PrefabGUID.Empty;
                            });
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }
}