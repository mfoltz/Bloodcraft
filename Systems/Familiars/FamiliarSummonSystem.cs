using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarSummonSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;

    static readonly GameDifficulty _gameDifficulty = SystemService.ServerGameSettingsSystem.Settings.GameDifficulty;

    static readonly bool _familiarCombat = ConfigService.FamiliarCombat;
    static readonly bool _familiarPrestige = ConfigService.FamiliarPrestige;

    static readonly WaitForSeconds _delay = new(0.25f);

    public const float FAMILIAR_LIFETIME = 240f;

    public const int BASE_LEVEL = 1;
    const int NORMAL_HEALTH = 500;
    const int HARD_HEALTH = 750;

    const float HEALTH_MIN = 1f;
    const float HEALTH_MAX = 5f;
    const float HEALTH_ADD = HEALTH_MAX - HEALTH_MIN;

    const float POWER_MIN = 0.1f;
    const float POWER_MAX = 1f;
    const float POWER_ADD = POWER_MAX - POWER_MIN;
    const float POWER_SKEW = 0.1f;

    const float HEALTH_MIN_BATTLE = 1f;
    const float HEALTH_MAX_BATTLE = 2.5f;
    const float HEALTH_ADD_BATTLE = HEALTH_MAX_BATTLE - HEALTH_MIN_BATTLE;

    const float POWER_MIN_BATTLE = 0.25f;
    const float POWER_MAX_BATTLE = 1f;
    const float POWER_ADD_BATTLE = POWER_MAX_BATTLE - POWER_MIN_BATTLE;

    const string SHINY_DEFAULT = "<color=#FF69B4>";

    static readonly int _maxFamiliarLevel = ConfigService.MaxFamiliarLevel;
    static readonly float _familiarPrestigeStatMultiplier = ConfigService.FamiliarPrestigeStatMultiplier;
    static readonly float _vBloodDamageMultiplier = ConfigService.VBloodDamageMultiplier;

    public static Entity _unitTeamSingleton = Entity.Null;

    static readonly PrefabGUID _invulnerableBuff = new(-480024072);
    static readonly PrefabGUID _hideSpawnBuff = new(-133411573);     // Buff_General_Spawn_Unit_Fast_WarEvent
    // static readonly PrefabGUID _distanceCheckBuff = new(1269197489); // AB_Vampire_CrimsonIronMaiden_DistanceChecker_Buff
    static readonly PrefabGUID _monsterFakePos = new(1529452061);

    static readonly PrefabGUID _divineAngel = new(-1737346940);
    static readonly PrefabGUID _familiarServant = new(1193263017);
    static readonly PrefabGUID _invisibleAndImmaterialBuff = new(-1144825660);

    static readonly PrefabGUID _ignoredFaction = new(-1430861195);
    static readonly PrefabGUID _playerFaction = new(1106458752);
    static readonly PrefabGUID _legionFaction = new(-772044125);
    static readonly PrefabGUID _cursedFaction = new(1522496317);

    static readonly PrefabGUID _pristineHeart = new(-1413694594);
    static readonly PrefabGUID _radiantFibre = new(-182923609);
    static readonly PrefabGUID _resonator = new(-1629804427);
    static readonly PrefabGUID _document = new(1334469825);
    static readonly PrefabGUID _demonFragment = new(-77477508);

    // static readonly PrefabGUID _teamMarkerBuff = new(969375416);
    static readonly PrefabGUID _teamOneMarkerBuff = new();
    static readonly PrefabGUID _teamTwoMarkerBuff = new(-1887712500); // AB_Undead_Infiltrator_MasterOfDisguise_HintBuff

    static readonly PrefabGUID _mapIconAlliedPlayer = new(-892362184);
    // static readonly PrefabGUID _mapIconLocalPlayer = new(-1323817571);
    static readonly PrefabGUID _mapIconCharmed = new(-1491648886); // could use this with eclipse registered players and modify the sprite clientside, at least?
    static readonly PrefabGUID _mapIconBuff = new(-1476191492);

    static readonly PrefabGUID _externalInventory = new(1183666186);

    static readonly List<PrefabGUID> _teamFactions =
    [
        _legionFaction,
        _cursedFaction
    ];
    public enum FamiliarStatType
    {
        MaxHealth,
        PhysicalPower,
        SpellPower,
        PrimaryLifeLeech,
        PhysicalLifeLeech,
        SpellLifeLeech,
        PhysicalCritChance,
        PhysicalCritDamage,
        SpellCritChance,
        SpellCritDamage,
        HealingReceived,
        DamageReduction,
        PhysicalResistance,
        SpellResistance,
        MovementSpeed,
        CastSpeed
    }

    public static readonly Dictionary<FamiliarStatType, float> FamiliarBaseStatValues = new()
    {
        {FamiliarStatType.PhysicalCritChance, 0.2f},
        {FamiliarStatType.SpellCritChance, 0.2f},
        {FamiliarStatType.HealingReceived, 0.5f},
        {FamiliarStatType.PhysicalResistance, 0.2f},
        {FamiliarStatType.SpellResistance, 0.2f},
        {FamiliarStatType.MovementSpeed, 0.25f},
        {FamiliarStatType.CastSpeed, 0.25f}
    };

    public static readonly List<FamiliarStatType> FamiliarPrestigeStats =
    [
        FamiliarStatType.PhysicalCritChance,
        FamiliarStatType.SpellCritChance,
        FamiliarStatType.HealingReceived,
        FamiliarStatType.PhysicalResistance,
        FamiliarStatType.SpellResistance,
        FamiliarStatType.MovementSpeed,
        FamiliarStatType.CastSpeed
    ];

    /*
    public class FamiliarSoulBoost(FamiliarStatType primaryStat, float primaryValue, 
        FamiliarStatType secondaryStat, float secondaryValue, 
        FamiliarStatType tertiaryStat, float tertiaryValue)
    {
        public (FamiliarStatType, float) PrimaryStatType = new(primaryStat, primaryValue);
        public (FamiliarStatType, float) SecondaryStatType = new(secondaryStat, secondaryValue);
        public (FamiliarStatType, float) TertiaryStatType = new(tertiaryStat, tertiaryValue);
    }

    static readonly Dictionary<PrefabGUID, FamiliarSoulBoost> ItemBoostMap = new()
    {
        // { _demonFragment, new() },
    };
    */

    public static readonly ConcurrentDictionary<ulong, List<PrefabGUID>> PlayerBattleGroups = [];
    public static readonly ConcurrentDictionary<ulong, List<Entity>> PlayerBattleFamiliars = [];
    public static IEnumerator InstantiateFamiliar(User user, Entity playerCharacter, int famKey, 
        bool battle = false, int teamIndex = -1, float3 position = default, bool allies = false)
    {
        PrefabGUID familiarId = new(famKey);
        Entity familiar = ServerGameManager.InstantiateEntityImmediate(playerCharacter, familiarId);

        yield return _delay;

        if (battle)
        {
            ulong steamId = user.PlatformId;

            if (!PlayerBattleFamiliars.ContainsKey(steamId)) PlayerBattleFamiliars[steamId] = [];

            PlayerBattleFamiliars[steamId].Add(familiar);
            PlayerBattleGroups[steamId].Remove(familiarId);

            familiar.TryApplyBuff(_hideSpawnBuff);

            if (HandleBinding(user, playerCharacter, familiar, familiarId, position, battle, teamIndex, allies))
            {
                if (BattleService.Matchmaker.MatchPairs.TryGetMatch(steamId, out var matchPair))
                {
                    ulong pairedId = (matchPair.Item1 == steamId) ? matchPair.Item2 : matchPair.Item1;

                    if (PlayerBattleGroups[steamId].Count == 0 &&
                        PlayerBattleGroups[pairedId].Count == 0)
                    {
                        BattleService.BattleCountdownRoutine((steamId, pairedId)).Start();
                    }
                }
            }
        }
        else
        {
            familiar.TryApplyBuff(_hideSpawnBuff);

            HandleBinding(user, playerCharacter, familiar, familiarId, playerCharacter.GetPosition());
        }
    }
    static bool HandleBinding(User user, Entity playerCharacter, Entity familiar, PrefabGUID familiarId, float3 position, bool battle = false, int teamIndex = -1, bool allies = false)
    {
        ulong steamId = user.PlatformId;
        
        if (battle)
        {
            if (familiar.Exists())
            {
                familiar.SetPosition(position);

                if (HandleModifications(user, playerCharacter, familiar, battle, teamIndex, allies))
                {
                    HandleShiny(user, steamId, familiar, familiarId, battle);

                    return true;
                }
                else
                {
                    familiar.Destroy();
                    Core.Log.LogWarning($"Battle modifications failed, destroying familiar...");

                    return false;
                }
            }
            else
            {
                Core.Log.LogWarning($"Familiar doesn't exist after instantiation for battle!");

                return false;
            }
        }
        else
        {
            if (familiar.Exists() && playerCharacter.TryGetTeamEntity(out Entity teamEntity))
            {
                familiar.SetTeam(teamEntity);
                familiar.SetPosition(position);

                if (HandleModifications(user, playerCharacter, familiar))
                {
                    HandleShiny(user, steamId, familiar, familiarId);
                    HandleMapIcon(playerCharacter, familiar);

                    // Utilities.Familiars.EquipFamiliar(steamId, familiarId.GuidHash, HandleFamiliarServant(familiar));

                    return true;
                }
                else
                {
                    familiar.Destroy();
                    LocalizationService.HandleServerReply(EntityManager, user, $"Binding failed...");

                    return false;
                }
            }
            else
            {
                Core.Log.LogWarning($"Familiar doesn't exist after instantiation and/or couldn't get team entity from playerCharacter! ({familiar.Exists()} | {playerCharacter.TryGetComponent(out TeamReference teamReference) && teamReference.Value._Value.Exists()})");

                return false;
            }
        }
    }
    public static bool HandleModifications(User user, Entity playerCharacter, Entity familiar, 
        bool battle = false, int teamIndex = -1, bool allies = false)
    {
        ulong steamId = user.PlatformId;

        try
        {
            FamiliarExperienceData familiarExperience = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);

            int famKey = familiar.GetPrefabGuidHash();
            int level = familiarExperience.FamiliarExperience.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level < BASE_LEVEL)
            {
                level = BASE_LEVEL;

                KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
                familiarExperience.FamiliarExperience[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, familiarExperience);
            }

            if (battle)
            {
                if (ModifyFamiliar(user, steamId, famKey, playerCharacter, familiar, level, battle, teamIndex, allies))
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }
            else
            {
                if (ModifyFamiliar(user, steamId, famKey, playerCharacter, familiar, level))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    public static bool ModifyFamiliar(User user, ulong steamId, int famKey, Entity playerCharacter, Entity familiar, int level, 
        bool battle = false, int teamIndex = -1, bool allies = false)
    {
        try
        {            
            if (battle)
            {
                ModifyTeamFactionAggro(playerCharacter, familiar, teamIndex, allies);
                ModifyUnitStats(familiar, level, steamId, famKey, battle);
                RemoveConvertable(familiar);
                HandleBloodSource(familiar, level);
                ModifyCollision(familiar);
                RemoveDropTable(familiar);
                PreventDisableFamiliar(familiar);

                if (BattleService.Matchmaker.MatchPairs.TryGetMatch(steamId, out var matchPair))
                {
                    ulong pairedId = matchPair.Item1 == steamId ? matchPair.Item2 : matchPair.Item1;

                    if (PlayerBattleFamiliars.ContainsKey(pairedId))
                    {
                        int pairedIndex = PlayerBattleFamiliars[pairedId].Count - 1;
                        Entity opposingFamiliar = PlayerBattleFamiliars[pairedId][pairedIndex];

                        if (opposingFamiliar.Exists()) Utilities.Familiars.FaceYourEnemy(familiar, opposingFamiliar); // will need to possibly refactor logic for this
                    }
                }
                else
                {
                    Core.Log.LogWarning($"Couldn't find MatchPair to get team indices for familiar battle!");

                    return false;
                }

                familiar.NothingLivesForever();

                return true;
            }
            else
            {
                ModifyFollowerFactionMinion(playerCharacter, familiar);
                ModifyUnitStats(familiar, level, steamId, famKey);

                if (!_familiarCombat) DisableCombat(familiar);
                else
                {
                    ModifyAggro(familiar);
                }

                RemoveConvertable(familiar);
                HandleBloodSource(familiar, level);
                ModifyCollision(familiar);
                RemoveDropTable(familiar);
                PreventDisableFamiliar(familiar);

                return true;
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    static void DisableCombat(Entity familiar)
    {
        familiar.SetFaction(_ignoredFaction);
        familiar.DisableAggro();
        familiar.DisableAggroable();
        familiar.TryApplyBuff(_invulnerableBuff);
    }
    static void ModifyTeamFactionAggro(Entity playerCharacter, Entity familiar, int teamIndex, bool allies)
    {
        if (familiar.Has<AggroConsumer>())
        {
            familiar.With((ref AggroConsumer aggroConsumer) =>
            {
                aggroConsumer.Active._Value = false;
            });
        }

        if (familiar.Has<Aggroable>())
        {
            familiar.With((ref Aggroable aggroable) =>
            {
                aggroable.Value._Value = false;
            });
        }

        if (!allies && playerCharacter.TryGetTeamEntity(out Entity teamEntity))
        {
            familiar.SetTeam(teamEntity);
        }
        else if (familiar.Has<Team>() && _unitTeamSingleton.Exists())
        {
            if (familiar.Has<FactionReference>())
            {
                PrefabGUID factionPrefabGUID = _teamFactions[teamIndex];
                familiar.SetFaction(factionPrefabGUID);
            }

            familiar.With((ref Team team) =>
            {
                team.Value = 2;
            });

            familiar.With((ref TeamReference teamReference) =>
            {
                teamReference.Value._Value = _unitTeamSingleton;
            });
        }
        else
        {
            Core.Log.LogWarning($"Couldn't find valid team to use for a familiar about to battle!");
        }

        if (!familiar.Has<BlockFeedBuff>()) familiar.Add<BlockFeedBuff>(); // general marker for fams

        if (teamIndex.Equals(1))
        {
            familiar.TryApplyBuffWithLifeTimeNone(_teamTwoMarkerBuff);
        }
        else
        {
            if (familiar.TryApplyAndGetBuff(_teamOneMarkerBuff, out Entity buffEntity))
            {
                buffEntity.AddWith((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 0f;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                });

                if (buffEntity.Has<AbilityProjectileFanOnGameplayEvent_DataServer>()) buffEntity.Remove<AbilityProjectileFanOnGameplayEvent_DataServer>();
            }
        }
    }
    static void ModifyFollowerFactionMinion(Entity playerCharacter, Entity familiar)
    {
        if (familiar.Has<FactionReference>())
        {
            familiar.With((ref FactionReference factionReference) =>
            {
                factionReference.FactionGuid._Value = _playerFaction;
            });
        }

        if (familiar.Has<Follower>())
        {
            familiar.With((ref Follower follower) =>
            {
                follower.Followed._Value = playerCharacter;
                follower.ModeModifiable._Value = 0;
            });
        }

        if (!familiar.Has<Minion>())
        {
            familiar.AddWith((ref Minion minion) => minion.MasterDeathAction = MinionMasterDeathAction.Kill);
        }

        if (familiar.Has<EntityOwner>())
        {
            familiar.With((ref EntityOwner entityOwner) =>
            {
                entityOwner.Owner = playerCharacter;
            });
        }

        if (!familiar.Has<BlockFeedBuff>()) familiar.Add<BlockFeedBuff>();

        var followerBuffer = playerCharacter.ReadBuffer<FollowerBuffer>();
        followerBuffer.Add(new FollowerBuffer { Entity = NetworkedEntity.ServerEntity(familiar) });
    }
    public static void ModifyUnitStats(Entity familiar, int level, ulong steamId, int famKey, bool battle = false)
    {
        float levelScale = level / (float)_maxFamiliarLevel;

        float powerFactor = POWER_MIN + (levelScale * POWER_ADD);
        float healthFactor = HEALTH_MIN + (levelScale * HEALTH_ADD);

        if (battle)
        {
            powerFactor = POWER_MIN_BATTLE + (levelScale * POWER_ADD_BATTLE);
            healthFactor = HEALTH_MIN_BATTLE + (levelScale * HEALTH_ADD_BATTLE);
        }

        int prestigeLevel = 0;
        List<FamiliarStatType> familiarPrestigeStats = [];

        if (_familiarPrestige && FamiliarPrestigeManager_V2.LoadFamiliarPrestigeData_V2(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            List<int> prestigeStatIndexes = prestigeData.Value; // Already stored as indexes in V2

            familiarPrestigeStats = prestigeStatIndexes
                .Where(index => index >= 0 && index < FamiliarPrestigeStats.Count) // Prevent out-of-range errors
                .Select(index => FamiliarPrestigeStats[index])
                .ToList();
        }

        powerFactor += prestigeLevel * POWER_SKEW;
        float prestigeFactor = 1 + (prestigeLevel * _familiarPrestigeStatMultiplier);

        // traits should be % added bonus on top of whatever their normal scaled stats are? still need to think about equipment but yeah that should mostly work
        // for the love of god don't touch anything related to saving persistence without then immediately testing to see if it does anything bad ;_;

        PrefabGUID familiarId = familiar.GetPrefabGuid();
        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[familiarId];

        UnitStats unitStats = original.Read<UnitStats>();
        UnitStats familiarUnitStats = familiar.Read<UnitStats>();

        AbilityBar_Shared abilityBar_Shared = original.Read<AbilityBar_Shared>();
        AbilityBar_Shared familiarAbilityBarShared = familiar.Read<AbilityBar_Shared>();

        AiMoveSpeeds aiMoveSpeeds = original.Read<AiMoveSpeeds>();
        AiMoveSpeeds familiarAiMoveSpeeds = familiar.Read<AiMoveSpeeds>();

        familiarUnitStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * powerFactor; // scaling these with prestige not a great idea in retrospect, nerfed that a bit but they also start at higher base power per prestige then probably rebalancing when equipment stats come into play
        familiarUnitStats.SpellPower._Value = unitStats.SpellPower._Value * powerFactor;
        
        foreach (FamiliarStatType prestigeStat in familiarPrestigeStats)
        {
            switch (prestigeStat)
            {
                case FamiliarStatType.PhysicalCritChance:
                    familiarUnitStats.PhysicalCriticalStrikeChance._Value = FamiliarBaseStatValues[FamiliarStatType.PhysicalCritChance] * prestigeFactor;
                    break;
                case FamiliarStatType.SpellCritChance:
                    familiarUnitStats.SpellCriticalStrikeChance._Value = FamiliarBaseStatValues[FamiliarStatType.SpellCritChance] * prestigeFactor;
                    break;
                case FamiliarStatType.HealingReceived:
                    familiarUnitStats.HealingReceived._Value = FamiliarBaseStatValues[FamiliarStatType.HealingReceived] * prestigeFactor;
                    break;
                case FamiliarStatType.PhysicalResistance:
                    familiarUnitStats.PhysicalResistance._Value = FamiliarBaseStatValues[FamiliarStatType.PhysicalResistance] * prestigeFactor;
                    break;
                case FamiliarStatType.SpellResistance:
                    familiarUnitStats.SpellResistance._Value = FamiliarBaseStatValues[FamiliarStatType.SpellResistance] * prestigeFactor;
                    break;
                case FamiliarStatType.MovementSpeed:
                    familiarAiMoveSpeeds.Walk._Value = aiMoveSpeeds.Walk._Value * (1 + (FamiliarBaseStatValues[FamiliarStatType.MovementSpeed] * prestigeFactor));
                    familiarAiMoveSpeeds.Run._Value = aiMoveSpeeds.Run._Value * (1 + (FamiliarBaseStatValues[FamiliarStatType.MovementSpeed] * prestigeFactor));
                    break;
                case FamiliarStatType.CastSpeed:
                    familiarAbilityBarShared.AttackSpeed._Value = abilityBar_Shared.AttackSpeed._Value * (1 + (FamiliarBaseStatValues[FamiliarStatType.CastSpeed] * prestigeFactor));
                    break;
                default:
                    break;
            }
        }

        familiar.Write(familiarUnitStats);
        familiar.Write(familiarAbilityBarShared);
        familiar.Write(familiarAiMoveSpeeds);

        familiar.With((ref UnitLevel unitLevel) =>
        {
            unitLevel.Level._Value = level;
            unitLevel.HideLevel = false;
        });

        float maxHealth = _gameDifficulty.Equals(GameDifficulty.Hard) ? HARD_HEALTH : NORMAL_HEALTH;

        if (level.Equals(BASE_LEVEL))
        {
            maxHealth *= HEALTH_MIN;
        }
        else if (level.Equals(_maxFamiliarLevel))
        {
            maxHealth *= HEALTH_MAX;
        }
        else
        {
            maxHealth *= healthFactor;
        }
        
        familiar.With((ref Health health) =>
        {
            health.MaxHealth._Value = maxHealth;
            health.Value = maxHealth;
        });

        if (!battle && !_vBloodDamageMultiplier.Equals(1f))
        {
            DamageCategoryStats damageCategoryStats = familiar.Read<DamageCategoryStats>();

            if (damageCategoryStats.DamageVsVBloods._Value != _vBloodDamageMultiplier)
            {
                familiar.With((ref DamageCategoryStats damageCategoryStats) =>
                {
                    damageCategoryStats.DamageVsVBloods._Value *= _vBloodDamageMultiplier;
                });
            }
        }

        RemoveMisc(familiar, familiarId);
    }
    public static void ModifyBloodSource(Entity familiar, int level)
    {
        BloodConsumeSource bloodConsumeSource = familiar.Read<BloodConsumeSource>();
        bloodConsumeSource.BloodQuality = level / (float)_maxFamiliarLevel * 100;
        bloodConsumeSource.CanBeConsumed = false;
        familiar.Write(bloodConsumeSource);
    }
    static void PreventDisableFamiliar(Entity familiar)
    {
        // one of the relics from the inception of familiars, might be better off not doing this in the long run
        // but hasn't caused issues (that I'm aware of, at least) and need to get out of current rabbit hole before entering another so leaving note for later >_>

        ModifiableBool modifiableBool = new() { _Value = false };
        CanPreventDisableWhenNoPlayersInRange canPreventDisable = new() { CanDisable = modifiableBool };

        EntityManager.AddComponentData(familiar, canPreventDisable);
    }
    static void RemoveConvertable(Entity familiar)
    {
        if (familiar.Has<ServantConvertable>())
        {
            familiar.Remove<ServantConvertable>();
        }
        if (familiar.Has<CharmSource>())
        {
            familiar.Remove<CharmSource>();
        }
    }
    static void HandleBloodSource(Entity familiar, int level)
    {
        if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);
    }
    static void ModifyCollision(Entity familiar)
    {
        if (!familiar.Has<DynamicCollision>()) return;
        
        familiar.With((ref DynamicCollision dynamicCollision) =>
        {
            // okay only changing hardness threshold might have stopped undesirable behaviour without familiars getting people stuck in corners, as funny as the latter was and former is xP well sorta, hrm
            dynamicCollision.AgainstPlayers.RadiusOverride = 0.05f;
            dynamicCollision.AgainstPlayers.HardnessThreshold._Value = 0.05f;
            dynamicCollision.AgainstPlayers.PushStrengthMax._Value = 0.05f;
            dynamicCollision.AgainstPlayers.PushStrengthMin._Value = 0.05f;
            dynamicCollision.AgainstPlayers.RadiusVariation = 0.05f;
        });

        if (!familiar.Has<MapCollision>()) return;

        familiar.With((ref MapCollision mapCollision) =>
        {
            mapCollision.Radius = 0.25f;
        });

        if (!familiar.Has<CollisionRadius>()) return;

        familiar.With((ref CollisionRadius collisionRadius) =>
        {
            collisionRadius.Radius = 0.35f;
        });
    }
    static void RemoveDropTable(Entity familiar)
    {
        if (!familiar.Has<DropTableBuffer>()) return;
        var buffer = familiar.ReadBuffer<DropTableBuffer>();
        for (int i = 0; i < buffer.Length; i++)
        {
            var item = buffer[i];
            item.DropTrigger = DropTriggerType.OnSalvageDestroy;
            buffer[i] = item;
        }
    }
    static void ModifyAggro(Entity familiar)
    {
        familiar.With((ref AggroConsumer aggroConsumer) =>
        {
            aggroConsumer.ProximityRadius = 0f;
            aggroConsumer.ProximityWeight = 0f;
        });

        familiar.With((ref AlertModifiers alertModifiers) =>
        {
            alertModifiers.CircleRadiusFactor._Value = 0f;
            alertModifiers.ConeRadiusFactor._Value = 0f;
        });

        familiar.With((ref AggroModifiers aggroModifiers) =>
        {
            aggroModifiers.CircleRadiusFactor._Value = 0f;
            aggroModifiers.ConeRadiusFactor._Value = 0f;
        });

        familiar.With((ref GainAggroByVicinity gainAggroByVicinity) =>
        {
            gainAggroByVicinity.Value.AggroValue = 0f;
        });

        familiar.With((ref GainAlertByVicinity gainAlertByVicinity) =>
        {
            gainAlertByVicinity.Value.AggroValue = 0f;
        });
    }
    static void RemoveMisc(Entity familiar, PrefabGUID familiarId)
    {
        if (familiar.Has<SpawnPrefabOnGameplayEvent>()) // stop pilots spawning from gloomrot mechs
        {
            var buffer = familiar.ReadBuffer<SpawnPrefabOnGameplayEvent>();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpawnPrefab.Equals(_monsterFakePos))
                {
                    familiar.Remove<SpawnPrefabOnGameplayEvent>();
                }
                else if (buffer[i].SpawnPrefab.GetPrefabName().Contains("pilot", StringComparison.OrdinalIgnoreCase))
                {
                    familiar.Remove<SpawnPrefabOnGameplayEvent>();
                }
            }
        }

        if (familiarId.Equals(_divineAngel) && familiar.Has<Script_ApplyBuffUnderHealthThreshold_DataServer>())
        {
            familiar.With((ref Script_ApplyBuffUnderHealthThreshold_DataServer script_ApplyBuffUnderHealthThreshold_DataServer) =>
            {
                script_ApplyBuffUnderHealthThreshold_DataServer.NewBuffEntity = PrefabGUID.Empty;
            });
        }

        if (familiar.Has<Immortal>()) familiar.Remove<Immortal>();
    }
    static void HandleShiny(User user, ulong steamId, Entity familiar, PrefabGUID familiarId, bool battle = false)
    {
        if (battle && GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
        {
            int famKey = familiarId.GuidHash;
            FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffsData(steamId);

            if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                PrefabGUID shinyBuff = new(buffsData.FamiliarBuffs[famKey].First());
                Buffs.ModifyShinyBuff(familiar, shinyBuff);
            }
        }
        else if (GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
        {
            string colorCode = SHINY_DEFAULT;
            bool isShiny = false;

            if (GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
            {
                int famKey = familiarId.GuidHash;
                FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffsData(steamId);

                if (buffsData.FamiliarBuffs.ContainsKey(famKey))
                {
                    PrefabGUID shinyBuff = new(buffsData.FamiliarBuffs[famKey].First());
                    Buffs.ModifyShinyBuff(familiar, shinyBuff);

                    if (FamiliarUnlockSystem.ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[famKey].First()), out var hexColor))
                    {
                        colorCode = $"<color={hexColor}>";
                    }

                    isShiny = true;
                }
            }

            string message = isShiny ? $"<color=green>{familiarId.GetLocalizedName()}</color>{colorCode}*</color> <color=#00FFFF>bound</color>!" : $"<color=green>{familiarId.GetLocalizedName()}</color> <color=#00FFFF>bound</color>!";
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
    }
    static void HandleMapIcon(Entity playerCharacter, Entity familiar)
    {
        if (ServerGameManager.TryInstantiateBuffEntityImmediate(playerCharacter, familiar, _mapIconBuff, out Entity buffEntity))
        {
            var buffer = buffEntity.ReadBuffer<AttachMapIconsToEntity>();

            AttachMapIconsToEntity attachMapIconsToEntity = buffer[0];
            attachMapIconsToEntity.Prefab = _mapIconAlliedPlayer; // would prefer unique or at least diff colored indicator but will have to do for now
            // attachMapIconsToEntity.Prefab = _mapIconCharmed; // would prefer unique or at least diff colored indicator but will have to do for now

            buffer[0] = attachMapIconsToEntity;

            if (!buffEntity.TryGetAttached(out Entity attached) || !attached.Exists() || attached.Equals(playerCharacter))
            {
                buffEntity.Remove<Attached>();
                buffEntity.Write(new Attach(familiar));
            }
            
            if (!buffEntity.GetBuffTarget().Equals(familiar))
            {
                buffEntity.With((ref Buff buff) =>
                {
                    buff.Target = familiar;
                });
            }
        }
    }
    static Entity HandleFamiliarServant(Entity familiar)
    {
        Entity servant = ServerGameManager.InstantiateEntityImmediate(familiar, _familiarServant);

        if (servant.Exists())
        {
            Utilities.Familiars.FamiliarServantMap[familiar] = servant;

            servant.AddWith((ref GetTranslationOnUpdate updateTranslation) =>
            {
                updateTranslation.Source = GetTranslationSource.Creator;
            });

            servant.With((ref DynamicCollision dynamicCollision) =>
            {
                dynamicCollision.AgainstPlayers.HardnessThreshold._Value = 0f;
                dynamicCollision.AgainstPlayers.PushStrengthMax._Value = 0f;
                dynamicCollision.AgainstPlayers.PushStrengthMin._Value = 0f;
                dynamicCollision.AgainstPlayers.RadiusOverride = 0f;
                dynamicCollision.AgainstPlayers.RadiusVariation = 0f;

                dynamicCollision.AgainstUnits.HardnessThreshold._Value = 0f;
                dynamicCollision.AgainstUnits.PushStrengthMax._Value = 0f;
                dynamicCollision.AgainstUnits.PushStrengthMin._Value = 0f;
                dynamicCollision.AgainstUnits.RadiusOverride = 0f;
                dynamicCollision.AgainstUnits.RadiusVariation = 0f;

                dynamicCollision.Immobile = true;
            });

            if (servant.TryApplyAndGetBuff(_invisibleAndImmaterialBuff, out Entity buffEntity))
            {
                buffEntity.With((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 0f;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                });
            }

            servant.With((ref Interactable interactable) =>
            {
                interactable.Disabled = true;
            });
            
            servant.With((ref AiMoveSpeeds aiMoveSpeeds) =>
            {
                aiMoveSpeeds.Circle._Value = 0f;
                aiMoveSpeeds.Walk._Value = 0f;
                aiMoveSpeeds.Run._Value = 0f;
                aiMoveSpeeds.Return._Value = 0f;
            });
            
            InventoryUtilitiesServer.InstantiateInventory(EntityManager, servant, 27);
            return servant;
        }

        return Entity.Null;
    }
}

