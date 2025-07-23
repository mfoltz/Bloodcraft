using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class BuffSystemSpawnPatches
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static ModificationsRegistry ModificationsRegistry => SystemService.ModificationSystem.Registry;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _eliteShardBearers = ConfigService.EliteShardBearers;
    static readonly bool _legacies = ConfigService.LegacySystem;
    static readonly bool _expertise = ConfigService.ExpertiseSystem;
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _trueImmortal = ConfigService.TrueImmortal;
    static readonly bool _classes = ConfigService.ClassSystem;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _potionStacking = ConfigService.PotionStacking;
    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _alchemy = !Profession.Alchemy.IsDisabled();

    const float FAMILIAR_TRAVEL_DURATION = 7.5f;
    const float MINION_LIFETIME = 30f;
    const int MAX_PROFESSION_LEVEL = ProfessionSystem.MAX_PROFESSION_LEVEL;

    static readonly PrefabGUID _fallenAngel = PrefabGUIDs.CHAR_Paladin_FallenAngel;
    static readonly PrefabGUID _solarus = PrefabGUIDs.CHAR_ChurchOfLight_Paladin_VBlood;

    static readonly PrefabGUID _holyBubbleBuff = Buffs.HolyBubbleBuff;
    static readonly PrefabGUID _gateBossFeedCompleteBuff = Buffs.GateBossFeedCompleteBuff;
    static readonly PrefabGUID _holyBeamPowerBuff = Buffs.HolyBeamPowerBuff;
    static readonly PrefabGUID _pvpProtectedBuff = Buffs.PvPProtectedBuff;
    static readonly PrefabGUID _pveCombatBuff = Buffs.PvECombatBuff;
    static readonly PrefabGUID _pvpCombatBuff = Buffs.PvPCombatBuff;
    static readonly PrefabGUID _phasingBuff = Buffs.PhasingBuff;
    static readonly PrefabGUID _witchPigTransformationBuff = Buffs.WitchPigTransformationBuff;
    static readonly PrefabGUID _wranglerPotionBuff = Buffs.WranglerPotionBuff;
    static readonly PrefabGUID _highlordGroundSwordBossBuff = Buffs.HighlordGroundSwordBossBuff;
    static readonly PrefabGUID _bloodCurseBuff = Buffs.DraculaBloodCurseBuff;
    static readonly PrefabGUID _inkCrawlerDeathBuff = Buffs.InkCrawlerDeathBuff;
    static readonly PrefabGUID _targetSwallowedBuff = Buffs.TargetSwallowedBuff;
    static readonly PrefabGUID _combatStanceBuff = Buffs.CombatStanceBuff;
    static readonly PrefabGUID _evolvedVampireBuff = Buffs.EvolvedVampireBuff;
    static readonly PrefabGUID _draculaReturnHideBuff = Buffs.DraculaReturnHideBuff;
    static readonly PrefabGUID _batLandingTravel = new(-371745443);

    static readonly EntityQuery _query = QueryService.BuffSpawnServerQuery;

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Core._initialized) return;

        using NativeAccessor<Entity> entities = _query.ToEntityArrayAccessor();
        using NativeAccessor<PrefabGUID> prefabGuids = _query.ToComponentDataArrayAccessor<PrefabGUID>();
        using NativeAccessor<Buff> buffs = _query.ToComponentDataArrayAccessor<Buff>();

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);
        ComponentLookup<BlockFeedBuff> blockFeedBuffLookup = __instance.GetComponentLookup<BlockFeedBuff>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;
                PrefabGUID buffPrefabGuid = prefabGuids[i];

                string prefabName = buffPrefabGuid.GetPrefabName();

                // if (!prefabName.Contains("AntennaBuff")) Core.Log.LogWarning($"[BuffSystem_Spawn_Server] - {buffTarget.GetPrefabGuid().GetPrefabName()} | {prefabName}");

                if (!buffTarget.Exists()) continue;
                
                bool isPlayerTarget = playerCharacterLookup.HasComponent(buffTarget);
                ulong steamId = isPlayerTarget ? buffTarget.GetSteamId() : 0;

                int buffType = GetBuffType(buffPrefabGuid.GuidHash, prefabName);

                switch (buffType)
                {
                    case 1 when _eliteShardBearers && buffTarget.GetPrefabGuid().Equals(_solarus): // Elite Solarus Final Phase
                        if (!buffTarget.Has<BlockFeedBuff>() && !buffTarget.HasBuff(_holyBeamPowerBuff))
                        {
                            if (buffTarget.TryApplyAndGetBuff(_holyBeamPowerBuff, out buffEntity))
                            {
                                if (buffEntity.Has<LifeTime>())
                                {
                                    buffEntity.With((ref LifeTime lifeTime) =>
                                    {
                                        lifeTime.Duration = 0f;
                                        lifeTime.EndAction = LifeTimeEndAction.None;
                                    });
                                }
                            }
                        }
                        break;
                    case 2 when _trueImmortal && isPlayerTarget: // ExoForm Immortal Handling
                        if (buffEntity.Has<SpellTarget>())
                        {
                            Entity spellTarget = buffEntity.GetSpellTarget();

                            if (!spellTarget.IsVBloodOrGateBoss())
                            {
                                Shapeshifts.TrueImmortal(buffEntity, buffTarget);
                            }
                        }
                        break;
                    case 3 when isPlayerTarget: // Familiar and player has PvE Combat Buff
                        Entity familiar = Entity.Null;
                        if (buffTarget.HasBuff(_evolvedVampireBuff)) buffEntity.Remove<SetOwnerRotateTowardsMouse>();
                        if (_familiars && steamId.HasActiveFamiliar())
                        {
                            familiar = Familiars.GetActiveFamiliar(buffTarget);
                            // Core.Log.LogWarning($"[BuffSystem_Spawn_Server] SyncingAggro - {familiar.Exists()}");
                            Familiars.HandleFamiliarEnteringCombat(buffTarget, familiar);
                            Familiars.SyncAggro(buffTarget, familiar);
                        }
                        break;
                    case 4 when _familiars && isPlayerTarget: // Familiars and player has PvP Combat Buff
                        if (steamId.HasActiveFamiliar())
                        {
                            familiar = Familiars.GetActiveFamiliar(buffTarget);

                            if (!_familiarPvP)
                            {
                                User combatUser = buffTarget.GetUser();
                                Familiars.DismissFamiliar(buffTarget, familiar, combatUser, steamId);
                            }
                            else
                            {
                                Familiars.HandleFamiliarEnteringCombat(buffTarget, familiar);
                            }
                        }
                        break;
                    case 5 when _familiars && isPlayerTarget: // Vampiric curse (BloodyPoint)
                        if (!buffEntity.Has<GameplayEventListeners>())
                        {
                            familiar = Familiars.GetActiveFamiliar(buffTarget);
                            if (familiar.Exists())
                            {
                                if (familiar.TryApplyAndGetBuffWithOwner(buffTarget, _targetSwallowedBuff, out buffEntity))
                                {
                                    if (buffEntity.Has<LifeTime>())
                                    {
                                        buffEntity.With((ref LifeTime lifeTime) => lifeTime.Duration = FAMILIAR_TRAVEL_DURATION);
                                    }
                                }
                            }
                        }
                        break;
                    case 7 when _familiars && buffTarget.IsVBloodOrGateBoss(): // Witch pig transformation buff
                        buffEntity.Destroy();
                        break;
                    case 8 when isPlayerTarget:
                        if (_familiars && steamId.HasDismissedFamiliar() && Familiars.AutoCallMap.TryRemove(buffTarget, out familiar))
                        {
                            Familiars.CallFamiliar(buffTarget, familiar, buffTarget.GetUser(), steamId);
                        }
                        if (_legacies || _expertise)
                        {
                            Buffs.RefreshStats(buffTarget);
                        }
                        break;
                    case 9 when _familiars:
                        if (buffTarget.TryGetFollowedPlayer(out Entity playerCharacter))
                        {
                            familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (familiar.Exists() && familiar.TryGetBuff(_highlordGroundSwordBossBuff, out buffEntity))
                            {
                                buffEntity.AddWith((ref AmplifyBuff amplifyBuff) =>
                                {
                                    amplifyBuff.AmplifyModifier = -0.75f;
                                });
                                
                                buffEntity.AddWith((ref LifeTime lifeTime) =>
                                {
                                    lifeTime.Duration = MINION_LIFETIME;
                                    lifeTime.EndAction = LifeTimeEndAction.Destroy;
                                });
                            }
                        }
                        break;
                    case 10 when _familiars:
                        if (buffTarget.TryGetFollowedPlayer(out playerCharacter))
                        {
                            if (buffEntity.Has<LifeTime>())
                            {
                                buffEntity.With((ref LifeTime lifeTime) =>
                                {
                                    lifeTime.Duration = 30f;
                                    lifeTime.EndAction = LifeTimeEndAction.Destroy;
                                });
                            }
                        }
                        break;
                    case 11:
                        if (isPlayerTarget)
                        {
                            if (_potionStacking && !prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
                            {
                                if (buffEntity.Has<RemoveBuffOnGameplayEvent>()) buffEntity.Remove<RemoveBuffOnGameplayEvent>();
                                if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>()) buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
                            }

                            if (!_alchemy) continue;

                            if (_professions)
                            {
                                IProfession handler = ProfessionFactory.GetProfession(buffPrefabGuid);

                                int level = handler.GetProfessionData(steamId).Key;
                                float bonus = 1 + level / (float)MAX_PROFESSION_LEVEL;

                                if (buffEntity.Has<LifeTime>())
                                {
                                    LifeTime lifeTime = buffEntity.Read<LifeTime>();
                                    if (lifeTime.Duration != -1) lifeTime.Duration *= bonus;
                                    buffEntity.Write(lifeTime);
                                }

                                if (!prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase) && buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var statBuffer) && !statBuffer.IsEmpty)
                                {
                                    for (int j = 0; j < statBuffer.Length; j++)
                                    {
                                        ModifyUnitStatBuff_DOTS statBuff = statBuffer[j];
                                        statBuff.Value *= 1 + level / (float)MAX_PROFESSION_LEVEL;

                                        statBuffer[j] = statBuff;
                                    }
                                }

                                if (buffEntity.Has<HealOnGameplayEvent>() && buffEntity.TryGetBuffer<CreateGameplayEventsOnTick>(out var tickBuffer) && !tickBuffer.IsEmpty)
                                {
                                    CreateGameplayEventsOnTick eventsOnTick = tickBuffer[0];
                                    eventsOnTick.MaxTicks = (int)(eventsOnTick.MaxTicks * bonus);

                                    tickBuffer[0] = eventsOnTick;
                                }
                            }

                            if (_familiars && !buffPrefabGuid.Equals(_wranglerPotionBuff))
                            {
                                familiar = Familiars.GetActiveFamiliar(buffTarget);

                                if (familiar.Exists())
                                {
                                    if (buffEntity.Has<HealOnGameplayEvent>() && familiar.IsDisabled())
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        familiar.TryApplyBuff(buffPrefabGuid);
                                    }
                                }
                            }
                        }
                        else if (buffTarget.TryGetFollowedPlayer(out playerCharacter))
                        {
                            if (_potionStacking && !prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
                            {
                                if (buffEntity.Has<RemoveBuffOnGameplayEvent>()) buffEntity.Remove<RemoveBuffOnGameplayEvent>();
                                if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>()) buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
                            }

                            if (_familiars && !buffPrefabGuid.Equals(_wranglerPotionBuff))
                            {
                                familiar = Familiars.GetActiveFamiliar(playerCharacter);

                                if (familiar.Exists())
                                {
                                    if (buffEntity.Has<HealOnGameplayEvent>() && familiar.IsDisabled())
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        familiar.TryApplyBuff(buffPrefabGuid);
                                    }
                                }
                            }
                        }
                        break;
                    case 12 when _familiars:
                        if (buffTarget.TryGetFollowedPlayer(out playerCharacter) && !GetPlayerBool(playerCharacter.GetSteamId(), VBLOOD_EMOTES_KEY))
                        {
                            buffEntity.Destroy();
                        }
                        break;
                    case 13 when _familiars && isPlayerTarget:
                        familiar = Familiars.GetActiveFamiliar(buffTarget);
                        if (familiar.Exists())
                        {
                            familiar.TryApplyBuff(buffPrefabGuid);
                        }
                        break;
                    case 14:
                        if (isPlayerTarget)
                        {
                            if (buffTarget.HasBuff(_evolvedVampireBuff)) buffEntity.Remove<SetOwnerRotateTowardsMouse>();
                            else if (_familiars && steamId.HasActiveFamiliar())
                            {
                                familiar = Familiars.GetActiveFamiliar(buffTarget);
                                // Core.Log.LogWarning($"[BuffSystem_Spawn_Server] SyncingAggro - {familiar.Exists()}");
                                Familiars.SyncAggro(buffTarget, familiar);
                            }
                        }
                        /*
                        else if (blockFeedBuffLookup.HasComponent(buffTarget))
                        {

                        }
                        */
                        break;
                    case 15 when _familiars:
                        if (buffTarget.IsFollowingPlayer())
                        {
                            buffEntity.Destroy();
                        }
                        break;
                    case 16 when _familiars && isPlayerTarget:
                        User batUser = buffTarget.GetUser();
                        if (Familiars.AutoCallMap.TryRemove(buffTarget, out familiar) && familiar.Exists())
                        {
                            Familiars.CallFamiliar(buffTarget, familiar, batUser, steamId);
                        }
                        break;
                    default:
                        if (isPlayerTarget && !buffTarget.IsDueling())
                        {
                            Entity owner = buffEntity.GetOwner();
                            bool isPlayerOwner = owner.IsPlayer();

                            if (_gameMode.Equals(GameModeType.PvE))
                            {
                                if (isPlayerOwner && !owner.Equals(buffTarget))
                                {
                                    PreventDebuff(buffEntity);
                                }
                                else if (_familiars)
                                {
                                    if (owner.IsFollowingPlayer())
                                    {
                                        PreventDebuff(buffEntity);
                                    }
                                    else if (owner.GetOwner().IsFollowingPlayer())
                                    {
                                        PreventDebuff(buffEntity);
                                    }
                                }
                            }
                            else if (_gameMode.Equals(GameModeType.PvP) && buffTarget.HasBuff(_pvpProtectedBuff))
                            {
                                if (isPlayerOwner && !owner.Equals(buffTarget))
                                {
                                    PreventDebuff(buffEntity);
                                }
                                else if (_familiars)
                                {
                                    if (owner.IsFollowingPlayer())
                                    {
                                        PreventDebuff(buffEntity);
                                    }
                                    else if (owner.GetOwner().IsFollowingPlayer())
                                    {
                                        PreventDebuff(buffEntity);
                                    }
                                }
                            }
                        }

                        break;
                    }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[BuffSystem_Spawn_Server] - Exception: {e}");
        }
    }
    static int GetBuffType(int prefabGuid, string prefabName = "")
    {
        string lowerName = prefabName.ToLower();

        if (lowerName.Contains("consumable") || lowerName.Contains("elixir"))
            return 11;
        else if (lowerName.Contains("emote_onaggro"))
            return 12;
        else if (lowerName.Contains("userelic"))
            return 13;
        else
        {
            // technically more performant but this is even less readable than the if else if chain x_x and hard to keep up with changes to hashes if needed
            return prefabGuid switch
            {
                358972271 => 1,  // Elite Solarus Final Phase
                -354622715 => 2,  // TrueImmortal
                581443919 => 3,  // PvE Combat
                697095869 => 4,  // Familiar PvP
                -89195359 => 5,  // Vampiric curse
                1356064917 => 7,  // Witch pig transformation
                -79611032 => 8,  // Phasing
                -6635580 => 9,  // Familiar highlord sword
                -1584595113 => 10, // Familiar castleman holy buff
                -952067173 => 3, // Combat stance
                404387047 => 15, // Dracula return hide
                -371745443 => 16, // bat landing
                _ => 0
            };
        }
    }
    static void PreventDebuff(Entity buffEntity)
    {
        if (buffEntity.TryGetComponent(out Buff buff) && buff.BuffEffectType.Equals(BuffEffectType.Debuff))
        {
            buffEntity.Destroy();
        }
    }
    static readonly Dictionary<PrefabGUID, Action<BuffSpawnContext>> _buffSpawnActions = new()
    {
        { _gateBossFeedCompleteBuff,   HandleEliteSolarusFinalPhase },   // case 1
        { _bloodCurseBuff,             HandleTrueImmortal         },     // case 2
        { _pveCombatBuff,              HandlePveCombatBuff        },     // case 3
        { _pvpCombatBuff,              HandlePvpCombatBuff        },     // case 4
        { _combatStanceBuff,           HandleCombatStanceBuff     },     // case 14 (re‑used logic)
        { _witchPigTransformationBuff, HandleWitchPigTransformation},    // case 7
        { _phasingBuff,                HandlePhasingBuff          },     // case 8 (also 14 part‑A)
        { _highlordGroundSwordBossBuff,HandleHighlordSwordBuff    },     // case 9
        { _inkCrawlerDeathBuff,        HandleInkCrawlerDeathBuff  },     // case 10
        { _draculaReturnHideBuff,      HandleDraculaReturnHide    }      // case 15
    };
    readonly struct BuffSpawnContext
    {
        public Entity BuffEntity { get; init; }
        public Entity BuffTarget { get; init; }
        public bool TargetIsPlayer { get; init; }
        public ulong SteamId { get; init; }
        public ComponentLookup<PlayerCharacter> PlayerLookup { get; init; }
        public ComponentLookup<BlockFeedBuff> BlockFeedLookup { get; init; }
    }
    static void HandleConsumable(Entity buffEntity, Entity target, PrefabGUID buffGuid, string prefabName, bool targetIsPlayer, ulong steamId)
    {
        if (targetIsPlayer)
        {
            ApplyConsumableToPlayer(buffEntity, target, buffGuid, prefabName, steamId);
        }
        else if (target.TryGetFollowedPlayer(out Entity playerChar))
        {
            ApplyConsumableToFollower(buffEntity, playerChar, prefabName);
        }
    }
    static void HandleAggroEmote(Entity buffEntity, Entity target)
    {
        if (_familiars && target.TryGetFollowedPlayer(out Entity player) && !GetPlayerBool(player.GetSteamId(), VBLOOD_EMOTES_KEY))
        {
            buffEntity.Destroy();
        }
    }
    static void HandleUseRelic(Entity buffEntity, Entity target, bool targetIsPlayer)
    {
        if (_familiars && targetIsPlayer)
        {
            Entity familiar = Familiars.GetActiveFamiliar(target);

            if (familiar.Exists())
            {
                familiar.TryApplyBuff(buffEntity.GetPrefabGuid());
            }
        }
    }
    static void HandleEliteSolarusFinalPhase(BuffSpawnContext buff)
    {
        if (!_eliteShardBearers) return;
        else if (!buff.BuffTarget.GetPrefabGuid().Equals(_solarus)) return;

        if (!buff.BuffTarget.HasBuff(_holyBeamPowerBuff) && !buff.BlockFeedLookup.HasComponent(buff.BuffTarget))
        {
            if (buff.BuffTarget.TryApplyAndGetBuff(_holyBeamPowerBuff, out Entity buffEntity))
            {
                buffEntity.HasWith((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 0f;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                });
            }
        }
    }
    static void HandleTrueImmortal(BuffSpawnContext buff)
    {
        if (!_trueImmortal || !buff.TargetIsPlayer) return;

        Entity spellTarget = buff.BuffEntity.GetSpellTarget();

        if (!spellTarget.IsVBloodOrGateBoss())
        {
            Shapeshifts.TrueImmortal(buff.BuffEntity, buff.BuffTarget);
        }
    }
    static void HandlePveCombatBuff(BuffSpawnContext buff)
    {
        // original case 3
        if (!buff.TargetIsPlayer) return;

        Entity familiar;
        if (buff.BuffTarget.HasBuff(_evolvedVampireBuff))
        {
            buff.BuffEntity.Remove<SetOwnerRotateTowardsMouse>();
        }

        if (_familiars && buff.SteamId.HasActiveFamiliar())
        {
            familiar = Familiars.GetActiveFamiliar(buff.BuffTarget);
            Familiars.HandleFamiliarEnteringCombat(buff.BuffTarget, familiar);
            Familiars.SyncAggro(buff.BuffTarget, familiar);
        }
    }
    static void HandlePvpCombatBuff(BuffSpawnContext buff)
    {
        // original case 4 – familiar dismissal in PvP
        if (!_familiars || !buff.TargetIsPlayer) return;

        Entity familiar;
        if (buff.SteamId.HasActiveFamiliar())
        {
            familiar = Familiars.GetActiveFamiliar(buff.BuffTarget);

            if (!_familiarPvP)
            {
                User user = buff.BuffTarget.GetUser();
                Familiars.DismissFamiliar(buff.BuffTarget, familiar, user, buff.SteamId);
            }
            else
            {
                Familiars.HandleFamiliarEnteringCombat(buff.BuffTarget, familiar);
            }
        }
    }
    static void HandleWitchPigTransformation(BuffSpawnContext buff)
    {
        if (_familiars && buff.BuffTarget.IsVBloodOrGateBoss())
        {
            buff.BuffEntity.Destroy();
        }
    }
    static void HandlePhasingBuff(BuffSpawnContext buff)
    {
        if (!buff.TargetIsPlayer) return;

        if (_familiars && buff.SteamId.HasDismissedFamiliar() && Familiars.AutoCallMap.TryRemove(buff.BuffTarget, out Entity familiar))
        {
            Familiars.CallFamiliar(buff.BuffTarget, familiar, buff.BuffTarget.GetUser(), buff.SteamId);
        }

        if (_legacies || _expertise)
        {
            Buffs.RefreshStats(buff.BuffTarget);
        }
    }
    static void HandleCombatStanceBuff(BuffSpawnContext buff)
    {
        if (!buff.TargetIsPlayer) return;

        Entity familiar;
        if (buff.BuffTarget.HasBuff(_evolvedVampireBuff))
        {
            buff.BuffEntity.Remove<SetOwnerRotateTowardsMouse>();
        }
        else if (_familiars && buff.SteamId.HasActiveFamiliar())
        {
            familiar = Familiars.GetActiveFamiliar(buff.BuffTarget);
            Familiars.SyncAggro(buff.BuffTarget, familiar);
        }
    }
    static void HandleHighlordSwordBuff(BuffSpawnContext buff)
    {
        if (!_familiars) return;

        if (buff.BuffTarget.TryGetFollowedPlayer(out Entity playerChar))
        {
            Entity familiar = Familiars.GetActiveFamiliar(playerChar);
            if (familiar.Exists() && familiar.TryGetBuff(_highlordGroundSwordBossBuff, out Entity buffEntity))
            {
                buffEntity.AddWith((ref AmplifyBuff amplify) => amplify.AmplifyModifier = -0.75f);
                buffEntity.AddWith((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = MINION_LIFETIME;
                    lifeTime.EndAction = LifeTimeEndAction.Destroy;
                });
            }
        }
    }
    static void HandleInkCrawlerDeathBuff(BuffSpawnContext buff)
    {
        if (!_familiars) return;

        if (buff.BuffTarget.TryGetFollowedPlayer(out Entity playerChar))
        {
            buff.BuffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 30f;
                lifeTime.EndAction = LifeTimeEndAction.Destroy;
            });
        }
    }
    static void HandleDraculaReturnHide(BuffSpawnContext ctx)
    {
        if (!_familiars) return;

        if (ctx.BuffTarget.IsFollowingPlayer())
        {
            ctx.BuffEntity.Destroy();
        }
    }
    static void ApplyConsumableToPlayer(Entity buffEntity, Entity player, PrefabGUID buffGuid, string prefabName, ulong steamId)
    {
        if (_potionStacking && !prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
        {
            buffEntity.Remove<RemoveBuffOnGameplayEvent>();
            buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
        }

        if (_professions)
        {
            IProfession alchemyHandler = ProfessionFactory.GetProfession(buffGuid);
            int level = alchemyHandler.GetProfessionLevel(steamId);
            float duration = 1 + level / (float)MAX_PROFESSION_LEVEL;

            buffEntity.With((ref LifeTime lifeTime) =>
            {
                if (!lifeTime.EndAction.Equals(LifeTimeEndAction.None))
                {
                    lifeTime.Duration *= duration;
                }
            });

            if (!prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase) &&
                buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var statBuffer) && !statBuffer.IsEmpty)
            {
                for (int j = 0; j < statBuffer.Length; ++j)
                {
                    var statBuff = statBuffer[j];
                    statBuff.Value *= duration;
                    statBuffer[j] = statBuff;
                }
            }

            if (buffEntity.Has<HealOnGameplayEvent>() && buffEntity.TryGetBuffer<CreateGameplayEventsOnTick>(out var tickBuffer) && !tickBuffer.IsEmpty)
            {
                var eventOnTick = tickBuffer[0];
                eventOnTick.MaxTicks = (int)(eventOnTick.MaxTicks * duration);
                tickBuffer[0] = eventOnTick;
            }
        }

        if (_familiars && !buffGuid.Equals(_wranglerPotionBuff))
        {
            Entity familiar = Familiars.GetActiveFamiliar(player);
            if (familiar.Exists())
            {
                if (buffEntity.Has<HealOnGameplayEvent>() && familiar.IsDisabled())
                    return; // skip if disabled and heal‑based consumable

                familiar.TryApplyBuff(buffGuid);
            }
        }
    }
    static void ApplyConsumableToFollower(Entity buffEntity, Entity playerCharacter, string prefabName)
    {
        if (_potionStacking && !prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
        {
            buffEntity.Remove<RemoveBuffOnGameplayEvent>();
            buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
        }

        if (_familiars && !buffEntity.GetPrefabGuid().Equals(_wranglerPotionBuff))
        {
            Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);
            if (familiar.Exists())
            {
                if (buffEntity.Has<HealOnGameplayEvent>() && familiar.IsDisabled())
                    return;

                familiar.TryApplyBuff(buffEntity.GetPrefabGuid());
            }
        }
    }
    static void TryPreventDebuff(Entity buffEntity, Entity target, bool targetIsPlayer)
    {
        if (!targetIsPlayer) return;

        Entity owner = buffEntity.GetOwner();
        bool ownerPlayer = owner.IsPlayer();

        bool prevent = false;

        if (_gameMode == GameModeType.PvE)
        {
            prevent = ownerPlayer && !owner.Equals(target);
        }
        else if (_gameMode == GameModeType.PvP && target.HasBuff(_pvpProtectedBuff))
        {
            prevent = ownerPlayer && !owner.Equals(target);
        }

        if (!prevent && _familiars)
        {
            if (owner.IsFollowingPlayer() || owner.GetOwner().IsFollowingPlayer())
                prevent = true;
        }

        if (prevent)
        {
            PreventDebuff(buffEntity);
        }
    }
}
