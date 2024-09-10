﻿using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

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
    static readonly PrefabGUID draculaEvolve = new(-2005193286);

    static readonly PrefabGUID swordBuff = new(-6635580);
    static readonly PrefabGUID highlordSwordBuff = new(-916946628);

    static readonly PrefabGUID dominateAbility = new(-1908054166);

    static readonly PrefabGUID pvpProtBuff = new(1111481396);
    static readonly PrefabGUID pvpVampire = new(697095869);

    static readonly PrefabGUID combatBuff = new(581443919);
    static readonly PrefabGUID combatStance = new(-952067173);

    static readonly PrefabGUID holyBeamPowerBuff = new(-1584595113);
    static readonly PrefabGUID Solarus = new(-740796338);

    static readonly PrefabGUID phasing = new(-79611032); // lol switch bodies with familiar? hmmm
    static readonly PrefabGUID feedExecute = new(366323518);
    static readonly PrefabGUID minionDeathBuff = new(2086395440);

    static readonly PrefabGUID castlemanCombatBuff = new(731266864);

    static readonly PrefabGUID modifyHUDTarget = new(-182838302);

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!Core.hasInitialized) return;

                if (!entity.Has<PrefabGUID>() || !entity.Has<Buff>()) continue;
                if (!entity.GetBuffTarget().Exists()) continue;

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                string prefabName = prefabGUID.LookupName().ToLower();
                Entity player = Entity.Null;

                // sections should be grouped appropriately to not interfere with each other
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
                }
                else if (ConfigService.EliteShardBearers && prefabName.Contains("holybubble")) // holy mortar effect for Solarus when eliteShardBearers active
                {
                    Entity character = entity.GetBuffTarget();

                    if (character.Read<PrefabGUID>().Equals(Solarus) && !ServerGameManager.HasBuff(character, holyBeamPowerBuff))
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
                }
                else if (ConfigService.FamiliarSystem && prefabGUID.Equals(phasing) && entity.GetBuffTarget().TryGetPlayer(out player)) // teleport familiar to player after waygate
                {
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
                    if (familiar.Exists())
                    {
                        FamiliarUtilities.ReturnFamiliar(player, familiar);
                    }
                    else if (player.GetSteamId().TryGetFamiliarActives(out var data))
                    {
                        if (data.Familiar.Exists())
                        {
                            FamiliarUtilities.ReturnFamiliar(player, familiar);
                        }
                    }
                }
                else if (ConfigService.FamiliarSystem && prefabName.Contains("emote_onaggro") && entity.GetBuffTarget().TryGetFollowedPlayer(out player))
                {
                    ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;
                    if (!PlayerUtilities.GetPlayerBool(steamId, "VBloodEmotes"))
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                }
                else if (ConfigService.FamiliarSystem && (prefabGUID.Equals(combatStance) || prefabGUID.Equals(combatBuff))) // return familiar when entering combat if far enough away
                {
                    if (entity.GetBuffTarget().TryGetPlayer(out player))
                    {
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
                        if (EntityManager.Exists(familiar))
                        {
                            FamiliarUtilities.ReturnFamiliar(player, familiar);
                        }
                    }
                }
                else if (prefabName.Contains("consumable") && entity.GetBuffTarget().TryGetPlayer(out player)) // alchemy bonuses/potion stacking/familiar sharing
                {
                    ulong steamId = player.Read<PlayerCharacter>().UserEntity.Read<User>().PlatformId;

                    if (ConfigService.PotionStacking) // stack t01/t02 potion effects
                    {
                        if (entity.Has<RemoveBuffOnGameplayEvent>()) entity.Remove<RemoveBuffOnGameplayEvent>();
                        if (entity.Has<RemoveBuffOnGameplayEventEntry>()) entity.Remove<RemoveBuffOnGameplayEventEntry>();
                    }

                    if (ConfigService.FamiliarSystem) // player->familiar potion sharing
                    {
                        Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);
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
                    }

                    if (ConfigService.ProfessionSystem) // apply alchemy bonuses
                    {
                        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID, "alchemy");
                        int level = handler.GetProfessionData(steamId).Key;

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
                    }
                }
                else if (ConfigService.FamiliarSystem && entity.GetBuffTarget().TryGetFollowedPlayer(out player)) // cassius, drac, other weird boss phase stuff. ultimately checking for specific prefabs, chain with the above and just check at the end
                {
                    Entity familiar = FamiliarUtilities.FindPlayerFamiliar(player);

                    if (familiar.Exists())
                    {
                        if (prefabGUID.Equals(draculaReturnHide))
                        {
                            DestroyUtility.CreateDestroyEvent(EntityManager, entity, DestroyReason.Default, DestroyDebugReason.None);
                        }
                        else if (prefabGUID.Equals(draculaFinal)) // need to double check if this actually forces drac final phase or not
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
                        else if (prefabGUID.Equals(swordBuff))
                        {
                            if (ServerGameManager.TryGetBuff(familiar, highlordSwordBuff.ToIdentifier(), out Entity swordPermabuff))
                            {
                                if (swordPermabuff.Has<AmplifyBuff>()) swordPermabuff.Remove<AmplifyBuff>();
                            }
                        }
                        else if (prefabGUID.Equals(castlemanCombatBuff))
                        {
                            if (entity.Has<Script_Castleman_AdaptLevel_DataShared>()) entity.Remove<Script_Castleman_AdaptLevel_DataShared>();
                        }
                        else if (prefabGUID.Equals(holyBeamPowerBuff))
                        {
                            if (entity.Has<LifeTime>()) entity.Write(new LifeTime { Duration = 30f, EndAction = LifeTimeEndAction.Destroy });
                        }
                        else if (prefabGUID.Equals(modifyHUDTarget))
                        {
                            BuffUtilities.
                                                        // strip unwanted components, just using this in hopes ModifyTargetHUD system will activate
                                                        HandleModifyTargetHUDBuff(entity);
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