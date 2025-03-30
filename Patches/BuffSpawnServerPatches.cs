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
using static Bloodcraft.Utilities.EntityQueries;

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
    static readonly PrefabGUID _inkCrawlerDeathBuff = new(1273155981);
    static readonly PrefabGUID _targetSwallowedBuff = new(-915145807);
    static readonly PrefabGUID _combatStanceBuff = new(-952067173);
    static readonly PrefabGUID _exoFormBuff = new(-31099041);
    static readonly PrefabGUID _draculaReturnHideBuff = new(404387047);

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

        ComponentLookup<PlayerCharacter> playerCharacterLookup = __instance.GetComponentLookup<PlayerCharacter>(true);

        try
        {
            for (int i = 0; i < entities.Length; i++)
            {
                Entity buffEntity = entities[i];
                Entity buffTarget = buffs[i].Target;

                PrefabGUID buffPrefabGuid = prefabGuids[i];
                string prefabName = buffPrefabGuid.GetPrefabName();

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
                    case 2 when _exoFormImmortal && isPlayerTarget: // ExoForm Immortal Handling
                        if (buffEntity.Has<SpellTarget>())
                        {
                            Entity spellTarget = buffEntity.GetSpellTarget();

                            if (!spellTarget.IsVBloodOrGateBoss())
                            {
                                ExoForm.HandleExoImmortal(buffEntity, buffTarget);
                            }
                        }
                        break;
                    case 3 when _familiars && isPlayerTarget: // Familiar and player has PvE Combat Buff
                        Entity familiar = Familiars.GetActiveFamiliar(buffTarget);
                        if (familiar.Exists())
                        {
                            Familiars.HandleFamiliarEnteringCombat(buffTarget, familiar);
                        }
                        break;
                    case 4 when _familiars && isPlayerTarget: // Familiars and player has PvP Combat Buff
                        if (steamId.HasActiveFamiliar())
                        {
                            familiar = Familiars.GetActiveFamiliar(buffTarget);

                            if (!_familiarPvP)
                            {
                                User user = buffTarget.GetUser();
                                Familiars.DismissFamiliar(buffTarget, familiar, user, steamId);
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
                        buffEntity.TryDestroy();
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
                            buffEntity.TryDestroy();
                        }
                        break;
                    case 13 when _familiars && isPlayerTarget:
                        familiar = Familiars.GetActiveFamiliar(buffTarget);
                        if (familiar.Exists())
                        {
                            familiar.TryApplyBuff(buffPrefabGuid);
                        }
                        break;
                    case 14 when _exoForm && isPlayerTarget && buffTarget.HasBuff(_exoFormBuff):
                        if (buffEntity.Has<SetOwnerRotateTowardsMouse>()) buffEntity.Remove<SetOwnerRotateTowardsMouse>();
                        break;
                    case 15 when _familiars:
                        if (buffTarget.IsFollowingPlayer())
                        {
                            buffEntity.TryDestroy();
                        }
                        break;
                    default:
                        if (isPlayerTarget)
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
                1356064917 => 7,  // Witch pig transformation
                -79611032 => 8,  // Phasing
                -6635580 => 9,  // Familiar highlord sword
                -1584595113 => 10, // Familiar castleman holy buff
                -952067173 or 581443919 => 14, // Combat stance
                404387047 => 15, // Dracula return hide
                _ => 0
            };
        }
    }
    static void PreventDebuff(Entity buffEntity)
    {
        if (buffEntity.TryGetComponent(out Buff buff) && buff.BuffEffectType.Equals(BuffEffectType.Debuff))
        {
            buffEntity.TryDestroy();
        }
    }
}
