using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Collections;
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
    static readonly bool _leveling = ConfigService.LevelingSystem;
    static readonly bool _legacies = ConfigService.BloodSystem;
    static readonly bool _professions = ConfigService.ProfessionSystem;
    static readonly bool _potionStacking = ConfigService.PotionStacking;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _exoFormImmortal = ConfigService.ExoPrestiging && ConfigService.TrueImmortal;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly int _maxProfessionLevel = ConfigService.MaxProfessionLevel;
    static readonly int _maxLevel = ConfigService.MaxLevel;

    const float FAMILIAR_TRAVEL_DURATION = 7.5f;
    const float MINION_LIFETIME = 30f;

    static readonly PrefabGUID _fallenAngel = new(-76116724);
    static readonly PrefabGUID _solarus = new(-740796338);

    static readonly PrefabGUID _draculaReturnBuff = new(404387047);
    static readonly PrefabGUID _vampiricCurse = new(-89195359); // Buff_Vampire_Dracula_BloodCurse
    static readonly PrefabGUID _channelHealBuff = new(478901515);
    static readonly PrefabGUID _highlordSwordSpawnBuff = new(-6635580);
    static readonly PrefabGUID _highlordSwordPermaBuff = new(-916946628);
    static readonly PrefabGUID _holyBeamPowerBuff = new(-1584595113);
    static readonly PrefabGUID _pvpProtectedBuff = new(1111481396);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _pveCombatBuff = new(581443919);
    static readonly PrefabGUID _phasingBuff = new(-79611032);
    static readonly PrefabGUID _spawnMutantBiteBuff = new(-651661301);
    static readonly PrefabGUID _feedBiteAbortTriggerBuff = new(366323518);
    static readonly PrefabGUID _mutantFromBiteBloodBuff = new(-491525099);
    static readonly PrefabGUID _witchPigTransformationBuff = new(1356064917);
    static readonly PrefabGUID _wranglerPotionBuff = new(387154469);
    static readonly PrefabGUID _inkCrawlerDeathBuff = new(1273155981);
    static readonly PrefabGUID _gateBossFeedCompleteBuff = new(-354622715);
    static readonly PrefabGUID _pristineHeartFeedBuff = new(-180761359);

    static readonly PrefabGUID _swallowAbilityGroup = new(1292896032);
    static readonly PrefabGUID _targetSwallowedBuff = new(-915145807);
    static readonly PrefabGUID _hasToadSwallowedBuff = new(1457576969);
    static readonly PrefabGUID _hasGolemSwallowedBuff = new(1303687336);

    static readonly PrefabGUID _traderFactionT01 = new(30052367);

    public static readonly ConcurrentDictionary<ulong, int> DeathMageMutantTriggerCounts = [];
    public static readonly ConcurrentQueue<Entity> DeathMagePlayerAngelSpawnOrder = [];

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = __instance._Query.ToEntityArray(Allocator.Temp);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGuid)) continue;

                Entity buffTarget = entity.GetBuffTarget();
                if (!buffTarget.Exists()) continue;

                string prefabName = prefabGuid.GetPrefabName();

                if (_eliteShardBearers && prefabName.Contains("holybubble", StringComparison.OrdinalIgnoreCase) && buffTarget.GetPrefabGuid().Equals(_solarus)) // holy mortar effect for Solarus when eliteShardBearers active
                {
                    if (!buffTarget.HasBuff(_holyBeamPowerBuff))
                    {
                        if (Buffs.TryApplyBuff(buffTarget, _holyBeamPowerBuff) && buffTarget.TryGetBuff(_holyBeamPowerBuff, out Entity buff))
                        {
                            if (buff.Has<LifeTime>())
                            {
                                buff.With((ref LifeTime lifeTime) =>
                                {
                                    lifeTime.Duration = -1;
                                    lifeTime.EndAction = LifeTimeEndAction.None;
                                });
                            }
                        }
                    }
                }
                else if (_exoFormImmortal && prefabGuid.Equals(_gateBossFeedCompleteBuff) && buffTarget.TryGetPlayer(out Entity playerCharacter))
                {
                    if (entity.Has<SpellTarget>())
                    {
                        Entity spellTarget = entity.GetSpellTarget();

                        if (spellTarget.Exists() && !spellTarget.IsPlayer()) continue;
                        else
                        {
                            ExoForm.HandleExoImmortal(entity, playerCharacter);
                        }
                    }
                }
                else if (_classes && (prefabGuid.Equals(_feedBiteAbortTriggerBuff) || prefabGuid.Equals(_spawnMutantBiteBuff)))
                {
                    if (buffTarget.TryGetPlayer(out playerCharacter))
                    {
                        ulong steamId = playerCharacter.GetSteamId();

                        Classes.HandleDeathMageBiteTriggerBuffSpawnServer(playerCharacter, steamId);
                    }
                    else if (buffTarget.GetPrefabGuid().Equals(_fallenAngel) && DeathMagePlayerAngelSpawnOrder.TryDequeue(out playerCharacter))
                    {
                        Classes.ModifyFallenAngelForDeathMage(buffTarget, playerCharacter);
                    }
                }
                else if (_familiars && prefabGuid.Equals(_witchPigTransformationBuff) && buffTarget.Has<VBloodUnit>())
                {
                    entity.Destroy();
                }
                else if (_familiars && prefabGuid.Equals(_phasingBuff) && buffTarget.TryGetPlayer(out playerCharacter))
                {
                    User user = playerCharacter.GetUser();
                    ulong steamId = user.PlatformId;

                    if (steamId.TryGetFamiliarActives(out var data) && Familiars.AutoCallMap.TryRemove(playerCharacter, out Entity familiar) && familiar.Exists())
                    {
                        Familiars.CallFamiliar(playerCharacter, familiar, user, steamId, data);
                    }
                }
                else if (_familiars && prefabGuid.Equals(_pveCombatBuff))
                {
                    if (buffTarget.TryGetPlayer(out playerCharacter))
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            familiar.With((ref Follower follower) =>
                            {
                                follower.ModeModifiable._Value = 1;
                            });

                            Familiars.TryReturnFamiliar(playerCharacter, familiar);
                        }
                    }
                }
                else if (_familiars && prefabGuid.Equals(_pvpCombatBuff))
                {
                    if (buffTarget.TryGetPlayer(out playerCharacter))
                    {
                        User user = playerCharacter.GetUser();
                        ulong steamId = user.PlatformId;

                        Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            familiar.With((ref Follower follower) =>
                            {
                                follower.ModeModifiable._Value = 1;
                            });

                            Familiars.TryReturnFamiliar(playerCharacter, familiar);

                            if (!_familiarPvP) Familiars.UnbindFamiliar(user, playerCharacter);
                        }
                    }
                }
                else if (_familiars && prefabGuid.Equals(_vampiricCurse) && !entity.Has<GameplayEventListeners>()) // bring familiar with player for BloodyPoint teleports, but in a cheeky way ;P
                {
                    if (buffTarget.TryGetPlayer(out playerCharacter) && entityOwner.Owner.Equals(playerCharacter))
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            // player.CastAbility(familiar, _swallowAbilityGroup); see if works without this

                            if (familiar.TryApplyAndGetBuffWithOwner(playerCharacter, _targetSwallowedBuff, out Entity buffEntity))
                            {
                                if (buffEntity.Has<LifeTime>())
                                {
                                    buffEntity.With((ref LifeTime lifeTime) =>
                                    {
                                        lifeTime.Duration = FAMILIAR_TRAVEL_DURATION;
                                    });
                                }
                            }
                        }
                    }
                }
                else if (_familiars && prefabName.Contains("emote_onaggro", StringComparison.OrdinalIgnoreCase) && buffTarget.TryGetFollowedPlayer(out playerCharacter))
                {
                    ulong steamId = playerCharacter.GetSteamId();

                    if (!GetPlayerBool(steamId, "VBloodEmotes"))
                    {
                        entity.Destroy();
                    }
                }
                else if (prefabName.Contains("consumable", StringComparison.OrdinalIgnoreCase) && buffTarget.TryGetPlayer(out playerCharacter)) // alchemy bonuses/potion stacking/familiar sharing
                {
                    ulong steamId = playerCharacter.GetSteamId();

                    if (_potionStacking && !prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase)) // stack t01/t02 potion effects except for holy, gets whack with damage reduction
                    {
                        if (entity.Has<RemoveBuffOnGameplayEvent>()) entity.Remove<RemoveBuffOnGameplayEvent>();
                        if (entity.Has<RemoveBuffOnGameplayEventEntry>()) entity.Remove<RemoveBuffOnGameplayEventEntry>();
                    }

                    if (_familiars && !prefabGuid.Equals(_wranglerPotionBuff))
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            familiar.TryApplyBuff(prefabGuid);
                        }
                    }

                    if (_professions)
                    {
                        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGuid, "alchemy");

                        int level = handler.GetProfessionData(steamId).Key;
                        float bonus = 1 + level / (float)_maxProfessionLevel;

                        if (entity.Has<LifeTime>())
                        {
                            LifeTime lifeTime = entity.Read<LifeTime>();
                            if (lifeTime.Duration != -1) lifeTime.Duration *= bonus;
                            entity.Write(lifeTime);
                        }

                        if (entity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var statBuffer) && !statBuffer.IsEmpty)
                        {
                            for (int i = 0; i < statBuffer.Length; i++)
                            {
                                ModifyUnitStatBuff_DOTS statBuff = statBuffer[i];
                                statBuff.Value *= 1 + level / (float)_maxProfessionLevel;

                                statBuffer[i] = statBuff;
                            }
                        }

                        if (entity.Has<HealOnGameplayEvent>() && entity.TryGetBuffer<CreateGameplayEventsOnTick>(out var tickBuffer) && !tickBuffer.IsEmpty)
                        {
                            CreateGameplayEventsOnTick eventsOnTick = tickBuffer[0];
                            eventsOnTick.MaxTicks = (int)(eventsOnTick.MaxTicks * bonus);

                            tickBuffer[0] = eventsOnTick;
                        }
                    }
                }
                else if (_familiars && buffTarget.TryGetFollowedPlayer(out playerCharacter)) // cassius, drac, other weird boss phase stuff. ultimately checking for specific prefabs, chain with the above and just check at the end
                {
                    Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

                    if (familiar.Exists())
                    {
                        if (prefabGuid.Equals(_draculaReturnBuff))
                        {
                            entity.Destroy();
                        }
                        else if (prefabGuid.Equals(_highlordSwordSpawnBuff))
                        {
                            if (familiar.TryGetBuff(_highlordSwordPermaBuff, out Entity buffEntity))
                            {
                                if (buffEntity.Has<AmplifyBuff>()) buffEntity.Remove<AmplifyBuff>();
                                if (!buffTarget.HasBuff(_inkCrawlerDeathBuff))
                                {
                                    Familiars.NothingLivesForever(buffTarget, MINION_LIFETIME);
                                }
                            }
                        }
                        else if (prefabGuid.Equals(_holyBeamPowerBuff))
                        {
                            if (entity.Has<LifeTime>())
                            {
                                entity.With((ref LifeTime lifeTime) =>
                                {
                                    lifeTime.Duration = 30f;
                                    lifeTime.EndAction = LifeTimeEndAction.Destroy;
                                });
                            }
                        }
                        /*
                        else if (prefabGuid.Equals(_hasToadSwallowedBuff))
                        {
                            if (entity.Has<LifeTime>())
                            {
                                entity.With((ref LifeTime lifeTime) =>
                                {
                                    lifeTime.Duration = 0f;
                                    lifeTime.EndAction = LifeTimeEndAction.None;
                                });
                            }

                            Core.Log.LogInfo($"HasToadSwallowedBuff - {familiar.GetPrefabGuid().GetPrefabName()} | {buffTarget.GetPrefabGuid().GetPrefabName()}");

                            if (entity.Has<ForceCastOnGameplayEvent>()) entity.Remove<ForceCastOnGameplayEvent>();
                            if (entity.Has<CreateGameplayEventsOnDestroy>()) entity.Remove<CreateGameplayEventsOnDestroy>();
                            if (entity.Has<GameplayEventIdMapping>()) entity.Remove<GameplayEventIdMapping>();
                            if (entity.Has<GameplayEventListeners>()) entity.Remove<GameplayEventListeners>();

                            // entity.Destroy();
                        }
                        else if (prefabGuid.Equals(_hasGolemSwallowedBuff))
                        {
                            if (entity.Has<LifeTime>())
                            {
                                entity.With((ref LifeTime lifeTime) =>
                                {
                                    lifeTime.Duration = 0f;
                                    lifeTime.EndAction = LifeTimeEndAction.None;
                                });
                            }
                        }
                        */
                    }
                }
                else if (_gameMode.Equals(GameModeType.PvE) && buffTarget.IsPlayer())
                {
                    Entity owner = entityOwner.Owner;

                    if (owner.IsPlayer() && !owner.Equals(buffTarget))
                    {
                        PreventDebuff(entity);
                    }
                    else if (_familiars)
                    {
                        if (owner.IsFollowingPlayer())
                        {
                            PreventDebuff(entity);
                        }
                        else if (owner.TryGetComponent(out entityOwner) && entityOwner.Owner.IsFollowingPlayer())
                        {
                            PreventDebuff(entity);
                        }
                    }
                }
                else if (_gameMode.Equals(GameModeType.PvP) && buffTarget.IsPlayer())
                {
                    Entity owner = entityOwner.Owner;
                    bool pvpProtected = buffTarget.HasBuff(_pvpProtectedBuff);

                    if (owner.IsPlayer() && !owner.Equals(buffTarget) && pvpProtected)
                    {
                        PreventDebuff(entity);
                    }
                    else if (_familiars && pvpProtected)
                    {
                        if (owner.IsFollowingPlayer())
                        {
                            PreventDebuff(entity);
                        }
                        else if (owner.TryGetComponent(out entityOwner) && entityOwner.Owner.IsFollowingPlayer())
                        {
                            PreventDebuff(entity);
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
    static void PreventDebuff(Entity buffEntity)
    {
        if (buffEntity.TryGetComponent(out Buff buff) && buff.BuffEffectType.Equals(BuffEffectType.Debuff))
        {
            buffEntity.Destroy();
        }
    }

    // Methods for refactoring... eventually >_>
    static void HandleEliteSolarusFinalPhase(Entity character)
    {
        if (character.Read<PrefabGUID>().Equals(_solarus) && !ServerGameManager.HasBuff(character, _holyBeamPowerBuff))
        {
            ApplyBuffDebugEvent applyBuffDebugEvent = new()
            {
                BuffPrefabGUID = _holyBeamPowerBuff,
            };

            FromCharacter fromCharacter = new()
            {
                Character = character,
                User = character
            };

            DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
            if (ServerGameManager.TryGetBuff(character, _holyBeamPowerBuff.ToIdentifier(), out Entity buff))
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
    static void HandleWaygatePhasing(Entity buffTarget)
    {
        if (buffTarget.TryGetPlayer(out Entity playerCharacter)) // teleport familiar to player after waygate and autoCall if was out before
        {
            User user = playerCharacter.GetUser();
            ulong steamId = user.PlatformId;

            if (steamId.TryGetFamiliarActives(out var data) && Familiars.AutoCallMap.TryRemove(playerCharacter, out Entity familiar) && familiar.Exists())
            {
                Familiars.CallFamiliar(playerCharacter, familiar, user, steamId, data);
            }
        }
    }
    static void HandlePvECombat(Entity buffTarget)
    {
        if (buffTarget.TryGetPlayer(out Entity playerCharacter))
        {
            Entity familiar = Familiars.FindPlayerFamiliar(playerCharacter);

            if (EntityManager.Exists(familiar))
            {
                Familiars.TryReturnFamiliar(playerCharacter, familiar);
            }
        }
    }
}