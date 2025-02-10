using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
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
    static readonly bool _exoForm = ConfigService.ExoPrestiging;
    static readonly bool _exoFormImmortal = ConfigService.ExoPrestiging && ConfigService.TrueImmortal;
    static readonly bool _classes = ConfigService.SoftSynergies || ConfigService.HardSynergies;
    static readonly bool _familiars = ConfigService.FamiliarSystem;
    static readonly bool _familiarPvP = ConfigService.FamiliarPvP;
    static readonly bool _potionStacking = ConfigService.PotionStacking;
    static readonly bool _professions = ConfigService.ProfessionSystem;

    const float FAMILIAR_TRAVEL_DURATION = 7.5f;
    const float MINION_LIFETIME = 30f;
    const int MAX_PROFESSION_LEVEL = 100;

    static readonly PrefabGUID _fallenAngel = new(-76116724);
    static readonly PrefabGUID _solarus = new(-740796338);
    static readonly PrefabGUID _holyBubbleBuff = new(358972271);
    static readonly PrefabGUID _draculaReturnBuff = new(404387047);
    static readonly PrefabGUID _gateBossFeedCompleteBuff = new(-354622715);
    static readonly PrefabGUID _holyBeamPowerBuff = new(-1584595113);
    static readonly PrefabGUID _pvpProtectedBuff = new(1111481396);
    static readonly PrefabGUID _pveCombatBuff = new(581443919);
    static readonly PrefabGUID _pvpCombatBuff = new(697095869);
    static readonly PrefabGUID _phasingBuff = new(-79611032);
    static readonly PrefabGUID _witchPigTransformationBuff = new(1356064917);
    static readonly PrefabGUID _wranglerPotionBuff = new(387154469);
    static readonly PrefabGUID _highlordSwordSpawnBuff = new(-6635580);
    static readonly PrefabGUID _highlordSwordBossPermaBuff = new(-916946628);
    static readonly PrefabGUID _vampiricCurse = new(-89195359);
    static readonly PrefabGUID _spawnMutantBiteBuff = new(-651661301);
    static readonly PrefabGUID _feedBiteAbortTriggerBuff = new(366323518);
    static readonly PrefabGUID _mutantFromBiteBloodBuff = new(-491525099);
    static readonly PrefabGUID _inkCrawlerDeathBuff = new(1273155981);
    static readonly PrefabGUID _targetSwallowedBuff = new(-915145807);
    static readonly PrefabGUID _combatStanceBuff = new(-952067173);
    static readonly PrefabGUID _exoFormBuff = new(-31099041);

    static readonly PrefabGUID _summonBuff = new(1947371829);

    public static readonly ConcurrentDictionary<ulong, int> DeathMageMutantTriggerCounts = new();
    public static readonly ConcurrentQueue<Entity> DeathMagePlayerAngelSpawnOrder = new();

    static readonly EntityQuery _query = QueryService.BuffSpawnServerQuery;

    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(BuffSystem_Spawn_Server __instance)
    {
        if (!Core._initialized) return;

        NativeArray<Entity> entities = _query.ToEntityArray(Allocator.Temp);
        NativeArray<PrefabGUID> prefabGuids = _query.ToComponentDataArray<PrefabGUID>(Allocator.Temp);
        NativeArray<Buff> buffs = _query.ToComponentDataArray<Buff>(Allocator.Temp);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;

                PrefabGUID buffPrefabGuid = prefabGuids[i];
                string prefabName = buffPrefabGuid.GetPrefabName();

                if (!buffTarget.Exists()) continue;

                bool isPlayerTarget = buffTarget.TryGetPlayer(out Entity playerCharacter);
                ulong steamId = isPlayerTarget ? playerCharacter.GetSteamId() : 0;

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
                    case 2 when _exoFormImmortal && isPlayerTarget: // ExoForm Immortal Handling
                        if (buffEntity.Has<SpellTarget>())
                        {
                            Entity spellTarget = buffEntity.GetSpellTarget();

                            if (!spellTarget.IsVBloodOrGateBoss())
                            {
                                ExoForm.HandleExoImmortal(buffEntity, playerCharacter);
                            }
                        }

                        break;
                    case 3 when _familiars: // Familiar PvE Combat Buff
                        Entity familiar = Familiars.GetActiveFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            familiar.With((ref Follower follower) => follower.ModeModifiable._Value = 1);
                            Familiars.TryReturnFamiliar(playerCharacter, familiar);
                        }

                        break;
                    case 4 when _familiars: // Familiar PvP Combat Buff
                        if (buffTarget.TryGetPlayer(out playerCharacter))
                        {
                            familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (familiar.Exists())
                            {
                                familiar.With((ref Follower follower) => follower.ModeModifiable._Value = 1);
                                Familiars.TryReturnFamiliar(playerCharacter, familiar);

                                if (!_familiarPvP) Familiars.UnbindFamiliar(playerCharacter.GetUser(), playerCharacter);
                            }
                        }

                        break;
                    case 5 when _familiars: // Vampiric curse (BloodyPoint)
                        if (!buffEntity.Has<GameplayEventListeners>() && buffTarget.TryGetPlayer(out playerCharacter))
                        {
                            familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (familiar.Exists())
                            {
                                if (familiar.TryApplyAndGetBuffWithOwner(playerCharacter, _targetSwallowedBuff, out buffEntity))
                                {
                                    if (buffEntity.Has<LifeTime>())
                                    {
                                        buffEntity.With((ref LifeTime lifeTime) => lifeTime.Duration = FAMILIAR_TRAVEL_DURATION);
                                    }
                                }
                            }
                        }

                        break;
                    case 6 when _classes: // Death mage second passive buff; should probably make some of those hard-coded and some optional, this was effort >_>
                        if (buffTarget.TryGetPlayer(out playerCharacter))
                        {
                            Classes.HandleDeathMageBiteTriggerBuffSpawnServer(playerCharacter, steamId);
                        }
                        else if (buffTarget.GetPrefabGuid().Equals(_fallenAngel) && DeathMagePlayerAngelSpawnOrder.TryDequeue(out playerCharacter))
                        {
                            Classes.ModifyFallenAngelForDeathMage(buffTarget, playerCharacter);
                        }

                        break;
                    case 7 when _familiars && buffTarget.IsVBloodOrGateBoss(): // Witch pig transformation buff
                        buffEntity.Destroy();

                        break;
                    case 8 when _familiars:
                        if (buffTarget.TryGetPlayer(out playerCharacter))
                        {
                            User user = playerCharacter.GetUser();

                            if (steamId.TryGetFamiliarActives(out var data) && Familiars.AutoCallMap.TryRemove(playerCharacter, out familiar) && familiar.Exists())
                            {
                                Familiars.CallFamiliar(playerCharacter, familiar, user, steamId, data);
                            }
                        }

                        break;
                    case 9 when _familiars:
                        if (buffTarget.TryGetFollowedPlayer(out playerCharacter))
                        {
                            familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (familiar.Exists() && familiar.TryGetBuff(_highlordSwordBossPermaBuff, out buffEntity))
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
                            familiar = Familiars.GetActiveFamiliar(playerCharacter);

                            if (familiar.Exists() && familiar.TryGetBuff(_holyBeamPowerBuff, out buffEntity))
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
                        }

                        break;
                    case 11:
                        if (buffTarget.IsPlayer())
                        {
                            if (_potionStacking && !prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
                            {
                                if (buffEntity.Has<RemoveBuffOnGameplayEvent>()) buffEntity.Remove<RemoveBuffOnGameplayEvent>();
                                if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>()) buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
                            }

                            if (_professions)
                            {
                                IProfessionHandler handler = ProfessionHandlerFactory.GetProfessionHandler(buffPrefabGuid, "alchemy");

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
                        else if (buffTarget.IsFollowingPlayer())
                        {
                            if (_potionStacking && !prefabName.Contains("holyresistance", StringComparison.OrdinalIgnoreCase))
                            {
                                if (buffEntity.Has<RemoveBuffOnGameplayEvent>()) buffEntity.Remove<RemoveBuffOnGameplayEvent>();
                                if (buffEntity.Has<RemoveBuffOnGameplayEventEntry>()) buffEntity.Remove<RemoveBuffOnGameplayEventEntry>();
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

                        break;
                    case 12 when _familiars:
                        if (!GetPlayerBool(steamId, VBLOOD_EMOTES_KEY))
                        {
                            buffEntity.Destroy();
                        }

                        break;
                    case 13 when _familiars:
                        familiar = Familiars.GetActiveFamiliar(playerCharacter);

                        if (familiar.Exists())
                        {
                            familiar.TryApplyBuff(buffPrefabGuid);
                        }

                        break;
                    case 14 when _exoForm && playerCharacter.HasBuff(_exoFormBuff):
                        if (buffEntity.Has<SetOwnerRotateTowardsMouse>()) buffEntity.Remove<SetOwnerRotateTowardsMouse>();

                        break;
                    default:
                        Entity owner = buffEntity.GetOwner();
                        bool isPlayerOwner = owner.IsPlayer();

                        if (_gameMode.Equals(GameModeType.PvE) && isPlayerTarget)
                        {
                            if (owner.IsPlayer() && !owner.Equals(buffTarget))
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
                        else if (_gameMode.Equals(GameModeType.PvP) && buffTarget.IsPlayer())
                        {
                            bool pvpProtected = buffTarget.HasBuff(_pvpProtectedBuff);

                            if (owner.IsPlayer() && !owner.Equals(buffTarget) && pvpProtected)
                            {
                                PreventDebuff(buffEntity);
                            }
                            else if (_familiars && pvpProtected)
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

                        break;
                    }
            }
        }
        finally
        {
            entities.Dispose();
            prefabGuids.Dispose();
            buffs.Dispose();
        }
    }
    static int GetBuffType(int prefabGuid, string prefabName = "")
    {
        string lowerName = prefabName.ToLower();

        if (lowerName.Contains("consumable"))
            return 11;
        else if (lowerName.Contains("emote_onaggro"))
            return 12;
        else if (lowerName.Contains("userelic"))
            return 13;
        else
        {
            return prefabGuid switch
            {
                358972271 => 1,  // Elite Solarus Final Phase
                -354622715 => 2,  // ExoForm Immortal
                581443919 => 3,  // Familiar PvE
                697095869 => 4,  // Familiar PvP
                -89195359 => 5,  // Vampiric curse
                -651661301 or 366323518 => 6,  // Death mage secondary case
                1356064917 => 7,  // Witch pig transformation
                -79611032 => 8,  // Phasing
                -6635580 => 9,  // Familiar highlord sword
                -1584595113 => 10, // Familiar castleman holy buff
                -952067173 or 581443919 => 14, // Combat stance
                _ => 0,  // Not found in the numeric set
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
}
