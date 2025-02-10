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
    const float SPAWN_BUFF_LIFETIME = 1.25f;

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
    static readonly PrefabGUID _spawnBuff = new(-1782768874);        // AB_Undead_BishopOfShadows_Idle_Buff
    //static readonly PrefabGUID _hideSpawnBuff = new(-205058219);     // Buff_General_Spawn_Unit_Fast_WarEvent
    static readonly PrefabGUID _hideSpawnBuff = new(-133411573);     // Buff_General_Spawn_Unit_Medium
    static readonly PrefabGUID _distanceCheckBuff = new(1269197489); // AB_Vampire_CrimsonIronMaiden_DistanceChecker_Buff
    static readonly PrefabGUID _solarusFinalBuff = new(2144624015);
    static readonly PrefabGUID _healAngelGroup = new(-552723816);
    static readonly PrefabGUID _swallowedBuff = new(-915145807);

    static readonly PrefabGUID _divineAngel = new(-1737346940);
    static readonly PrefabGUID _solarus = new(-740796338);
    static readonly PrefabGUID _servant = new(51737727);

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
    static readonly PrefabGUID _teamMarkerBuff = new(-746290494);

    static readonly PrefabGUID _mapIconAlliedPlayer = new(-892362184);
    static readonly PrefabGUID _mapIconLocalPlayer = new(-1323817571);
    static readonly PrefabGUID _mapIconCharmed = new(-1491648886);
    static readonly PrefabGUID _mapIconBuff = new(-1476191492);

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
        {FamiliarStatType.MovementSpeed, 0.5f},
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
    public static IEnumerator InstantiateFamiliar(User user, Entity playerCharacter, int famKey, bool battle = false, int teamIndex = -1, float3 position = default, bool allies = false)
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
            // playerCharacter.TryApplyBuffWithLifeTime(_spawnBuff, SPAWN_BUFF_LIFETIME);
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
    public static bool HandleModifications(User user, Entity playerCharacter, Entity familiar, bool battle = false, int teamIndex = -1, bool allies = false)
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
    public static bool ModifyFamiliar(User user, ulong steamId, int famKey, Entity playerCharacter, Entity familiar, int level, bool battle = false, int teamIndex = -1, bool allies = false)
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
            /*
            if (familiar.TryApplyAndGetBuff(_teamMarkerBuff, out Entity buffEntity) && buffEntity.Has<ModifyMovementSpeedBuff>())
            {
                familiar.With((ref ModifyMovementSpeedBuff modifyMovementSpeedBuff) =>
                {
                    modifyMovementSpeedBuff.MoveSpeed = 1f;
                });
            }
            */

            if (familiar.TryApplyAndGetBuff(_teamMarkerBuff, out Entity buffEntity) && buffEntity.Has<LifeTime>())
            {
                familiar.With((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 9999f;
                });
            }
        }
    }
    static void ModifyFollowerFactionMinion(Entity player, Entity familiar)
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
                follower.Followed._Value = player;
                follower.ModeModifiable._Value = 0;
            });
        }

        if (!familiar.Has<Minion>())
        {
            familiar.Add<Minion>();
            familiar.With((ref Minion minion) => minion.MasterDeathAction = MinionMasterDeathAction.Kill);
        }

        if (familiar.Has<EntityOwner>())
        {
            familiar.Write(new EntityOwner { Owner = player });
        }

        if (!familiar.Has<BlockFeedBuff>()) familiar.Add<BlockFeedBuff>();

        var followerBuffer = player.ReadBuffer<FollowerBuffer>();
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

        /*
        if (_familiarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestigeData(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            List<int> prestigeStatEnums = prestigeData.Value.Cast<int>().ToList(); //  *slaps on bandaid* not ideal but really need to move past this for now >_>

            familiarPrestigeStats = prestigeStatEnums
                .Select(index => FamiliarPrestigeStats[index])
                .ToList();
        }
        */

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

        familiarUnitStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * powerFactor; // scaling these with prestige not a great idea in retrospect, but holding off on changing that till adding equipment and perma boost items or whatever
        familiarUnitStats.SpellPower._Value = unitStats.SpellPower._Value * powerFactor;
        
        // familiarStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * scalingFactor;
        // familiarStats.SpellPower._Value = unitStats.SpellPower._Value * scalingFactor;

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
            // okay only changing hardness threshold might have stopped undesirable behaviour without familiars getting peopel stuck in corners, as funny as the latter was and former is xP
            // dynamicCollision.AgainstPlayers.RadiusOverride = 0.1f;
            dynamicCollision.AgainstPlayers.HardnessThreshold._Value = 0.1f;
            // dynamicCollision.AgainstPlayers.PushStrengthMax._Value = 0.2f;
            // dynamicCollision.AgainstPlayers.PushStrengthMin._Value = 0.1f;
            // dynamicCollision.AgainstPlayers.RadiusVariation = 0.1f;
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
                if (buffer[i].SpawnPrefab.GetPrefabName().Contains("pilot", StringComparison.OrdinalIgnoreCase))
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
        if (!battle)
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
        else if (GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
        {
            int famKey = familiarId.GuidHash;
            FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffsData(steamId);

            if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                PrefabGUID shinyBuff = new(buffsData.FamiliarBuffs[famKey].First());
                Buffs.ModifyShinyBuff(familiar, shinyBuff);
            }
        }
    }
    static void HandleMapIcon(Entity playerCharacter, Entity familiar)
    {
        if (ServerGameManager.TryInstantiateBuffEntityImmediate(playerCharacter, familiar, _mapIconBuff, out Entity buffEntity))
        {
            var buffer = buffEntity.ReadBuffer<AttachMapIconsToEntity>();

            AttachMapIconsToEntity attachMapIconsToEntity = buffer[0];
            attachMapIconsToEntity.Prefab = _mapIconAlliedPlayer; // would prefer unique or at least diff colored indicator but will have to do for now

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
}

/*
    // if (HandleServantBinding(familiar)) Utilities.Familiars.EquipFamiliar(steamId, familiar, famKey);

    public static bool ModifyFamiliarForBattle(User user, ulong steamId, int famKey, Entity player, Entity familiar, int level, int teamIndex)
    {
        try
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);

            ModifyTeamFactionAggro(familiar, teamIndex);
            ModifyUnitStats(familiar, level, steamId, famKey, true);
            RemoveConvertable(familiar);
            ModifyCollision(familiar);
            RemoveDropTable(familiar);
            PreventDisableFamiliar(familiar);
            familiar.NothingLivesForever();

            if (BattleService.Matchmaker.MatchPairs.TryGetMatch(steamId, out var matchPair))
            {
                ulong pairedId = matchPair.Item1 == steamId ? matchPair.Item2 : matchPair.Item1;

                if (PlayerBattleFamiliars.ContainsKey(pairedId))
                {
                    int pairedIndex = PlayerBattleFamiliars[pairedId].Count - 1;
                    Entity enemy = PlayerBattleFamiliars[pairedId][pairedIndex];

                    if (enemy.Exists()) Utilities.Familiars.FaceYourEnemy(familiar, enemy); // will need to possibly refactor logic for this
                }
            }
            else
            {
                Core.Log.LogWarning($"Couldn't find MatchPair to get team indices for familiar battle!");
            }

            if (GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
            {
                FamiliarBuffsData data = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);

                if (data.FamiliarBuffs.ContainsKey(famKey))
                {
                    PrefabGUID visualBuff = new(data.FamiliarBuffs[famKey].First());
                    Buffs.HandleShinyBuff(familiar, visualBuff);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error during familiar battle modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    public static bool HandleFamiliarForBattle(Entity playerCharacter, User user, Entity familiar, float3 position, int teamIndex)
    {
        ulong steamId = user.PlatformId;

        try
        {
            int famKey = familiar.GetPrefabGuidHash();

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            int level = famData.FamiliarLevels.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
                famData.FamiliarLevels[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperience(steamId, famData);
            }

            familiar.SetPosition(position);

            if (ModifyFamiliarForBattle(user, steamId, famKey, playerCharacter, familiar, level, teamIndex))
            {
                familiar.TryApplyBuff(_hideSpawnBuff);

                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    public static void SummonFamiliarForBattle(Entity playerCharacter, User user, PrefabGUID familiarId, float3 position, int teamIndex)
    {
        ulong steamId = user.PlatformId;
        Entity familiar = ServerGameManager.InstantiateEntityImmediate(playerCharacter, familiarId);

        if (!PlayerBattleFamiliars.ContainsKey(steamId)) PlayerBattleFamiliars[steamId] = [];

        PlayerBattleGroups[steamId].Remove(familiarId);
        PlayerBattleFamiliars[steamId].Add(familiar);

        if (HandleFamiliarForBattle(playerCharacter, user, familiar, position, teamIndex))
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

    if (HandleFamiliarForBattle(playerCharacter, user, familiar, position, teamIndex))
    {
        if (BattleService.Matchmaker.MatchPairs.TryGetMatch(steamId, out var matchPair))
        {
            ulong pairedId = (matchPair.Item1 == steamId) ? matchPair.Item2 : matchPair.Item1;

            if (PlayerBattleGroups.ContainsKey(pairedId) && PlayerBattleGroups[pairedId].Count == 0)
            {
                BattleService.BattleCountdownRoutine((pairedId, steamId)).Start();
            }
        }
    }

    if (HandleFamiliarForBattle(playerCharacter, user, familiar, position, teamIndex))
    {
        if (PlayerBattleGroups[steamId].Count == 0)
        {
            if (BattleService.Matchmaker.MatchPairs.TryGetMatch(steamId, out var matchPair))
            {
                ulong pairedId = matchPair.Item1 == steamId ? matchPair.Item2 : matchPair.Item1;

                BattleService.BattleCountdownRoutine((steamId, pairedId)).Start();
            }
        }
    }

    try
    {
        if (familiar.TryApplyAndGetBuffWithOwner(playerCharacter, _distanceCheckBuff, out Entity buffEntity))
        {
            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = -1f;
                lifeTime.EndAction = LifeTimeEndAction.None;
            });

            buffEntity.With((ref UpdateTranslationWithOffset updateTranslation) =>
            {
                updateTranslation.TranslationOffset = playerCharacter.GetOffset(familiar);
            });

            if (buffEntity.TryGetBuffer<CreateGameplayEventOnDistanceReached>(out var createEventBuffer) && !createEventBuffer.IsEmpty)
            {
                CreateGameplayEventOnDistanceReached distanceReachedEvent = createEventBuffer[0];

                distanceReachedEvent.DistanceSqThreshold = 250f;
                distanceReachedEvent.TriggerWhen = CreateGameplayEventOnDistanceReachedTriggerWhen.Farther;

                createEventBuffer[0] = distanceReachedEvent;
            }

            // no idea how to modify further with any reasonable confidence in the result, will be lucky enough for this to work as it is
            if (buffEntity.TryGetBuffer<GameplayEventIdMapping>(out var eventIdBuffer) && !eventIdBuffer.IsEmpty)
            {
                GameplayEventIdMapping eventIdMapping = eventIdBuffer[0];

                eventIdMapping.TriggerCooldown = 5f;
                eventIdMapping.TriggerMultipleTimes = true;
                eventIdMapping.MaxTriggers = 50;

                eventIdBuffer[0] = eventIdMapping;
            }

            if (buffEntity.TryGetBuffer<GameplayEventListeners>(out var listenerBuffer) && !listenerBuffer.IsEmpty)
            {
                GameplayEventListeners eventListener = listenerBuffer[0];

                // eventListener.GameplayEventType = GameplayEventTypeEnum.

            }

            if (buffEntity.TryGetBuffer<DestroyOnGameplayEvent>(out var destroyEventBuffer) && !destroyEventBuffer.IsEmpty)
            {
                DestroyOnGameplayEvent destroyOnEvent = destroyEventBuffer[0];

                // destroyOnEvent.Who = DestroyOnGameplayEventWho.Self;
                // destroyOnEvent.Type = DestroyOnGameplayEventType.Remove;
            }
        }
    }
    catch (Exception ex)
    {
        Core.Log.LogWarning($"Error applying distance check buff to familiar: {ex}");
    }

    static bool HandleFamiliarServantImmediate(Entity familiar)
    {
        Entity servant = ServerGameManager.InstantiateEntityImmediate(familiar, _servant);

        if (servant.Exists() && servant.TryApplyAndGetBuffWithOwner(familiar, _swallowedBuff, out Entity buffEntity))
        {
            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 0f;
                lifeTime.EndAction = LifeTimeEndAction.None;
            });

            if (!Utilities.Familiars.FamiliarServantMap.ContainsKey(familiar)) Utilities.Familiars.FamiliarServantMap.TryAdd(familiar, servant);

            return true;
        }

        return false;
    }
    public static void InstantiateFamiliarDeferred(User user, Entity playerCharacter, int famKey) // good idea but a lot of effort to remake this entire flow for ECB instead of EM, will reconsider in the future
    {
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        PrefabGUID familiarId = new(famKey);
        Entity familiar = ServerGameManager.InstantiateEntityDeferred(playerCharacter, familiarId);

        HandleBindingDeferred(entityCommandBuffer, playerCharacter, user, familiar, familiarId, playerCharacter.GetPosition());
    }
    static void HandleBindingDeferred(EntityCommandBuffer entityCommandBuffer, Entity playerCharacter, User user, Entity familiar, PrefabGUID familiarId, float3 position) // use commandBuffer instead of entityManager
    {
        ulong steamId = user.PlatformId;

        if (playerCharacter.TryGetTeamEntity(out Entity teamReference))
        {
            // familiar.SetTeam(teamReference);
            // familiar.SetComponent<>

            familiar.SetTeam(entityCommandBuffer, teamReference);
            familiar.SetPosition(entityCommandBuffer, position);
            familiar.SetFaction(entityCommandBuffer, _playerFaction);

            if (HandleFamiliarImmediate(user, playerCharacter, familiar))
            {
                string colorCode = "<color=#FF69B4>";
                FamiliarBuffsData buffsData = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);

                if (buffsData.FamiliarBuffs.ContainsKey(familiarId.GuidHash))
                {
                    if (FamiliarUnlockSystem.ShinyBuffColorHexMap.TryGetValue(new(buffsData.FamiliarBuffs[familiarId.GuidHash].First()), out var hexColor))
                    {
                        colorCode = $"<color={hexColor}>";
                    }
                }

                string message = buffsData.FamiliarBuffs.ContainsKey(familiarId.GuidHash) ? $"<color=green>{familiarId.GetLocalizedName()}</color>{colorCode}*</color> <color=#00FFFF>bound</color>!" : $"<color=green>{familiarId.GetLocalizedName()}</color> <color=#00FFFF>bound</color>!";
                LocalizationService.HandleServerReply(EntityManager, user, message);
            }
            else
            {
                familiar.Destroy();
                LocalizationService.HandleServerReply(EntityManager, user, $"Failed to bind familiar...");
            }
        }
        else
        {
            Core.Log.LogWarning($"Familiar playback incomplete!");
        }
    }

    public static bool HandleFamiliarDeferred(User user, Entity playerCharacter, Entity familiar) // use commandBuffer instead of entityManager
    {
        ulong steamId = user.PlatformId;

        try
        {
            // int level = familiar.GetUnitLevel();
            int famKey = familiar.GetPrefabGuidHash();

            FamiliarExperienceData familiarExperience = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            int level = familiarExperience.FamiliarLevels.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level <= BASE_LEVEL)
            {
                level = BASE_LEVEL;
                xpData = new(BASE_LEVEL, ConvertLevelToXp(BASE_LEVEL));

                familiarExperience.FamiliarLevels[famKey] = xpData;
                FamiliarExperienceManager.SaveFamiliarExperience(steamId, familiarExperience);
            }

            Core.Log.LogInfo($"{familiar.GetPrefabGuid().GetPrefabName()} - {level}");

            if (ModifyFamiliarImmediate(user, steamId, famKey, playerCharacter, familiar, level))
            {
                playerCharacter.TryApplyBuffWithLifeTime(_spawnBuff, SPAWN_BUFF_LIFETIME);
                familiar.TryApplyBuff(_hideSpawnBuff);

                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    public static void ModifyDamageStatsForBattle(Entity familiar, int level, ulong steamId, int famKey)
    {
        float scalingFactor = 0.25f + (level / (float)_maxFamiliarLevel) * 0.75f; // Calculate scaling factor for power and such
        float healthScalingFactor = 1.0f + ((level - 1) / (float)_maxFamiliarLevel) * 1.5f; // Calculate scaling factor for max health

        if (level == _maxFamiliarLevel) healthScalingFactor = 2.5f;

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (_familiarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestiges.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        PrefabGUID familiarId = familiar.Read<PrefabGUID>();

        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[familiarId];
        UnitStats unitStats = original.Read<UnitStats>();

        UnitStats familiarStats = familiar.Read<UnitStats>();
        familiarStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * scalingFactor * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
        familiarStats.SpellPower._Value = unitStats.SpellPower._Value * scalingFactor * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);

        foreach (FamiliarStatType stat in stats) // replicate this with list of stats but from traits and respective bonus %s to add?
        {
            switch (stat)
            {
                case FamiliarStatType.PhysicalCritChance:
                    familiarStats.PhysicalCriticalStrikeChance._Value = FamiliarStatValues[FamiliarStatType.PhysicalCritChance] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.SpellCritChance:
                    familiarStats.SpellCriticalStrikeChance._Value = FamiliarStatValues[FamiliarStatType.SpellCritChance] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.HealingReceived:
                    familiarStats.HealingReceived._Value = FamiliarStatValues[FamiliarStatType.HealingReceived] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.PhysicalResistance:
                    familiarStats.PhysicalResistance._Value = FamiliarStatValues[FamiliarStatType.PhysicalResistance] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.SpellResistance:
                    familiarStats.SpellResistance._Value = FamiliarStatValues[FamiliarStatType.SpellResistance] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
                case FamiliarStatType.ShieldAbsorb:
                    familiarStats.ShieldAbsorbModifier._Value = unitStats.ShieldAbsorbModifier._Value + FamiliarStatValues[FamiliarStatType.ShieldAbsorb] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
            }
        }

        familiar.Write(familiarStats);

        familiar.With((ref UnitLevel unitLevel) =>
        {
            unitLevel.Level._Value = level;
            unitLevel.HideLevel = false;
        });

        int baseHealth = 500;
        if (_gameDifficulty.Equals(GameDifficulty.Hard))
        {
            baseHealth = 750;
        }

        familiar.With((ref Health health) =>
        {
            health.MaxHealth._Value = baseHealth * healthScalingFactor;
            health.Value = health.MaxHealth._Value;
        });

        if (_vBloodDamageMultiplier != 1f)
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
*/