using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;

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
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly int _maxProfessionLevel = ConfigService.MaxProfessionLevel;
    static readonly int _maxLevel = ConfigService.MaxLevel;

    static readonly PrefabGUID _fallenAngel = new(-76116724);
    static readonly PrefabGUID _solarus = new(-740796338);

    static readonly PrefabGUID _draculaReturnBuff = new(404387047);
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
                if (!entity.TryGetComponent(out EntityOwner entityOwner) || !entityOwner.Owner.Exists() || !entity.TryGetComponent(out PrefabGUID prefabGUID)) continue;

                Entity buffTarget = entity.GetBuffTarget();
                if (!buffTarget.Exists()) continue;

                string prefabName = prefabGUID.GetPrefabName().ToLower();

                //int guidHash = prefabGUID.GuidHash;
                //Core.Log.LogInfo($"BuffSystem_Spawn_Server: {prefabName}");

                if (_eliteShardBearers && prefabName.Contains("holybubble") && buffTarget.GetPrefabGUID().Equals(_solarus)) // holy mortar effect for Solarus when eliteShardBearers active
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
                else if (_classes && (prefabGUID.Equals(_feedBiteAbortTriggerBuff) || prefabGUID.Equals(_spawnMutantBiteBuff)))
                {
                    if (buffTarget.TryGetPlayer(out Entity player))
                    {
                        ulong steamId = player.GetSteamId();

                        Classes.HandleDeathMageBiteTriggerBuffSpawnServer(player, steamId);
                    }
                    else if (buffTarget.GetPrefabGUID().Equals(_fallenAngel) && DeathMagePlayerAngelSpawnOrder.TryDequeue(out player))
                    {
                        Classes.ModifyFallenAngelTeam(entity, player);
                    }
                }
                else if (_familiars && prefabGUID.Equals(_witchPigTransformationBuff) && buffTarget.Has<VBloodUnit>())
                {
                    entity.Destroy();
                }
                else if (_familiars && prefabGUID.Equals(_phasingBuff) && buffTarget.TryGetPlayer(out Entity player))
                {
                    User user = player.GetUser();
                    ulong steamId = user.PlatformId;

                    if (steamId.TryGetFamiliarActives(out var data) && Familiars.AutoCallMap.TryRemove(player, out Entity familiar) && familiar.Exists())
                    {
                        Familiars.CallFamiliar(player, familiar, user, steamId, data);
                    }
                }
                else if (_familiars && prefabGUID.Equals(_pveCombatBuff)) // return familiar when entering combat if far enough away
                {
                    if (buffTarget.TryGetPlayer(out player))
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(player);

                        if (familiar.Exists())
                        {
                            familiar.With((ref Follower follower) =>
                            {
                                follower.ModeModifiable._Value = 1;
                            });

                            Familiars.TryReturnFamiliar(player, familiar);
                        }
                    }
                }
                else if (_familiars && prefabGUID.Equals(_pvpCombatBuff))
                {
                    if (buffTarget.TryGetPlayer(out player))
                    {
                        Entity userEntity = player.GetUserEntity();
                        ulong steamId = player.GetSteamId();

                        Entity familiar = Familiars.FindPlayerFamiliar(player);

                        if (familiar.Exists())
                        {
                            familiar.With((ref Follower follower) =>
                            {
                                follower.ModeModifiable._Value = 1;
                            });

                            Familiars.TryReturnFamiliar(player, familiar);
                            if (!_familiarPvP) Familiars.UnbindFamiliar(player, userEntity, steamId);
                        }
                    }
                }
                else if (_familiars && prefabName.Contains("emote_onaggro") && buffTarget.TryGetFollowedPlayer(out player))
                {
                    ulong steamId = player.GetSteamId();

                    if (!Misc.GetPlayerBool(steamId, "VBloodEmotes"))
                    {
                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                }
                else if (prefabName.Contains("consumable") && buffTarget.TryGetPlayer(out player)) // alchemy bonuses/potion stacking/familiar sharing
                {
                    ulong steamId = player.GetSteamId();

                    if (_potionStacking && !prefabName.Contains("holyresistance")) // stack t01/t02 potion effects
                    {
                        if (entity.Has<RemoveBuffOnGameplayEvent>()) entity.Remove<RemoveBuffOnGameplayEvent>();
                        if (entity.Has<RemoveBuffOnGameplayEventEntry>()) entity.Remove<RemoveBuffOnGameplayEventEntry>();
                    }

                    if (_familiars && !prefabGUID.Equals(_wranglerPotionBuff))
                    {
                        Entity familiar = Familiars.FindPlayerFamiliar(player);

                        if (familiar.Exists())
                        {
                            Buffs.TryApplyBuff(familiar, prefabGUID);
                        }
                    }

                    if (_professions) // apply alchemy bonuses
                    {
                        IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(prefabGUID, "alchemy");
                        int level = handler.GetProfessionData(steamId).Key;

                        if (entity.Has<LifeTime>())
                        {
                            LifeTime lifeTime = entity.ReadRO<LifeTime>();
                            if (lifeTime.Duration != -1) lifeTime.Duration *= 1 + level / (float)_maxProfessionLevel;
                            entity.Write(lifeTime);
                        }

                        if (entity.Has<ModifyUnitStatBuff_DOTS>())
                        {
                            var buffer = entity.ReadBuffer<ModifyUnitStatBuff_DOTS>();
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                ModifyUnitStatBuff_DOTS statBuff = buffer[i];
                                statBuff.Value *= 1 + level / (float)_maxProfessionLevel;
                                buffer[i] = statBuff;
                            }
                        }
                    }
                }
                else if (_familiars && buffTarget.TryGetFollowedPlayer(out player)) // cassius, drac, other weird boss phase stuff. ultimately checking for specific prefabs, chain with the above and just check at the end
                {
                    Entity familiar = Familiars.FindPlayerFamiliar(player);

                    if (familiar.Exists())
                    {
                        if (prefabGUID.Equals(_draculaReturnBuff))
                        {
                            DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (prefabGUID.Equals(_highlordSwordSpawnBuff))
                        {
                            if (ServerGameManager.TryGetBuff(familiar, _highlordSwordPermaBuff.ToIdentifier(), out Entity swordPermabuff))
                            {
                                if (swordPermabuff.Has<AmplifyBuff>()) swordPermabuff.Remove<AmplifyBuff>();
                            }
                        }
                        else if (prefabGUID.Equals(_holyBeamPowerBuff))
                        {
                            if (entity.Has<LifeTime>()) entity.Write(new LifeTime { Duration = 30f, EndAction = LifeTimeEndAction.Destroy });
                        }
                    }
                }
                else if (_gameMode.Equals(GameModeType.PvE) && buffTarget.IsPlayer())
                {
                    Entity owner = entityOwner.Owner;

                    if (owner.IsPlayer() && !owner.Equals(buffTarget))
                    {
                        Buff buff = entity.ReadRO<Buff>();
                        if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                    }
                    else if (_familiars)
                    {
                        if (owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.ReadRO<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (owner.TryGetComponent(out entityOwner) && entityOwner.Owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.ReadRO<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                    }
                }
                else if (_gameMode.Equals(GameModeType.PvP) && buffTarget.IsPlayer())
                {
                    Entity owner = entityOwner.Owner;
                    bool pvpProtected = buffTarget.HasBuff(_pvpProtectedBuff);

                    if (owner.IsPlayer() && !owner.Equals(buffTarget) && pvpProtected)
                    {
                        Buff buff = entity.ReadRO<Buff>();
                        if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                    }
                    else if (_familiars && pvpProtected)
                    {
                        if (owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.ReadRO<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
                        }
                        else if (owner.TryGetComponent(out entityOwner) && entityOwner.Owner.IsFollowingPlayer())
                        {
                            Buff buff = entity.ReadRO<Buff>();
                            if (buff.BuffEffectType.Equals(BuffEffectType.Debuff)) DestroyUtility.Destroy(EntityManager, entity);
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

    // Methods for refactoring eventually
    static void HandleEliteSolarusFinalPhase(Entity character)
    {
        if (character.ReadRO<PrefabGUID>().Equals(_solarus) && !ServerGameManager.HasBuff(character, _holyBeamPowerBuff))
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
                    var lifetime = buff.ReadRO<LifeTime>();
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
/* need to improve logic in this patch and finish switch statement but not tonight x_x
switch (guidHash)
{
    case -354622715:
        if (ConfigService.BloodSystem) HandleGateBossFeed(entity);
        break;
    case -79611032:
        if (ConfigService.FamiliarSystem) HandleWaygatePhasing(buffTarget);
        break;
    case 581443919:
        if (ConfigService.FamiliarSystem) HandlePvECombat(buffTarget);
        break;
}
*/