using Bloodcraft.Resources;
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
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarEquipmentManager;
using static Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarBindingSystem
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

    const int TEAM_ONE = 0;
    const int TEAM_TWO = 1;

    public const int BASE_LEVEL = 1;
    const int NORMAL_HEALTH = 500;
    const int HARD_HEALTH = 750;

    const float HEALTH_MIN = 1f;
    const float HEALTH_MAX = 4f;   // from 5f for equipment stats
    const float HEALTH_ADD = HEALTH_MAX - HEALTH_MIN;

    const float POWER_MIN = 0.1f;
    const float POWER_MAX = 0.75f; // from 1f for equipment stats
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

    public static Entity _unitTeamSingleton = Entity.Null;

    static readonly PrefabGUID _invulnerableBuff = Buffs.AdminInvulnerableBuff;
    static readonly PrefabGUID _hideSpawnBuff = PrefabGUIDs.Buff_General_Spawn_Unit_Fast_WarEvent;

    static readonly PrefabGUID _monsterFakePos = PrefabGUIDs.AB_Monster_HomePos_FakeTarget;
    static readonly PrefabGUID _charDivineAngel = PrefabGUIDs.CHAR_Paladin_DivineAngel;

    static readonly PrefabGUID _charVampireCultistServant = PrefabGUIDs.CHAR_Vampire_Cultist_Male_Servant;
    static readonly PrefabGUID _invisibleAndImmaterialBuff = Buffs.InvisibleAndImmaterialBuff;

    static readonly PrefabGUID _ignoredFaction = PrefabGUIDs.Faction_Ignored;
    static readonly PrefabGUID _playerFaction = PrefabGUIDs.Faction_Players;
    static readonly PrefabGUID _legionFaction = PrefabGUIDs.Faction_Legion;
    static readonly PrefabGUID _cursedFaction = PrefabGUIDs.Faction_Cursed;

    static readonly PrefabGUID _teamOneMarkerBuff = PrefabGUID.Empty;
    static readonly PrefabGUID _teamTwoMarkerBuff = PrefabGUIDs.AB_Undead_Infiltrator_MasterOfDisguise_HintBuff;

    static readonly PrefabGUID _mapIconAlliedPlayer = PrefabGUIDs.MapIcon_Player;
    static readonly PrefabGUID _mapIconBuff = PrefabGUIDs.AB_Interact_Mount_Target_LastOwner_BuffIcon;

    static readonly PrefabGUID _openTheCages = PrefabGUIDs.AB_WerewolfChieftain_OpenTheCages_AbilityGroup;
    static readonly PrefabGUID _charWerewolfChieftainVBlood = PrefabGUIDs.CHAR_WerewolfChieftain_Human;

    static readonly List<PrefabGUID> _teamFactions =
    [
        _legionFaction,
        _cursedFaction
    ];
    public enum FamiliarStatType
    {
        MaxHealth,
        PhysicalPower,
        SpellPower
        /*
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
        */
    }

    /*
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
    */

    public static readonly ConcurrentDictionary<ulong, List<PrefabGUID>> PlayerBattleGroups = [];
    public static readonly ConcurrentDictionary<ulong, List<Entity>> PlayerBattleFamiliars = [];
    public static IEnumerator InstantiateFamiliarRoutine(User user, Entity playerCharacter, int famKey, 
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

                    Entity servant = HandleFamiliarServant(familiar);
                    EquipFamiliar(steamId, familiarId.GuidHash, servant, familiar);

                    Utilities.Familiars.ActiveFamiliarManager.UpdateActiveFamiliarData(steamId, familiar, servant, familiarId.GuidHash);
                    ApplyFamiliarStatsRoutine(familiar).Start();

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
            FamiliarExperienceData familiarExperience = LoadFamiliarExperienceData(steamId);

            int famKey = familiar.GetGuidHash();
            int level = familiarExperience.FamiliarExperience.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level < BASE_LEVEL)
            {
                level = BASE_LEVEL;

                KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
                familiarExperience.FamiliarExperience[famKey] = newXP;

                SaveFamiliarExperienceData(steamId, familiarExperience);
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
                // HandleMountedUnit(playerCharacter, familiar, battle);

                if (BattleService.Matchmaker.MatchPairs.TryGetMatch(steamId, out var matchPair))
                {
                    ulong pairedId = matchPair.Item1 == steamId ? matchPair.Item2 : matchPair.Item1;

                    if (PlayerBattleFamiliars.ContainsKey(pairedId))
                    {
                        int pairedIndex = PlayerBattleFamiliars[pairedId].Count - 1;

                        if (pairedIndex >= 0)
                        {
                            Entity opposingFamiliar = PlayerBattleFamiliars[pairedId][pairedIndex];
                            if (opposingFamiliar.Exists()) Utilities.Familiars.FaceYourEnemy(familiar, opposingFamiliar);
                        }
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
                // HandleMountedUnit(playerCharacter, familiar);

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
        familiar.TryApplyBuff(_invulnerableBuff);
        familiar.SetFaction(_ignoredFaction);
        Utilities.Familiars.DisableAggro(familiar);
        Utilities.Familiars.DisableAggroable(familiar);
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
            /*
            if (familiar.TryApplyAndGetBuff(_teamOneMarkerBuff, out Entity buffEntity))
            {
                buffEntity.AddWith((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 0f;
                    lifeTime.EndAction = LifeTimeEndAction.None;
                });

                if (buffEntity.Has<AbilityProjectileFanOnGameplayEvent_DataServer>()) buffEntity.Remove<AbilityProjectileFanOnGameplayEvent_DataServer>();
            }
            */
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

        if (_familiarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestigeData(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData))
        {
            prestigeLevel = prestigeData;

            /*
            List<int> prestigeStatIndexes = prestigeData.Value; // Already stored as indexes in V2

            familiarPrestigeStats = [..prestigeStatIndexes
                .Where(index => index >= 0 && index < FamiliarPrestigeStats.Count) // Prevent out-of-range errors
                .Select(index => FamiliarPrestigeStats[index])];
            */
        }

        powerFactor += prestigeLevel * POWER_SKEW;
        float prestigeFactor = 1 + (prestigeLevel * _familiarPrestigeStatMultiplier);

        // so from gear ~750 health, 30-40 physical power, 30-40 spell power
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
        
        /*
        foreach (FamiliarStatType prestigeStat in familiarPrestigeStats)
        {
            switch (prestigeStat)
            {
                case FamiliarStatType.PhysicalCritChance:
                    familiarUnitStats.PhysicalCriticalStrikeChance._Value = FamiliarBaseStatValues[FamiliarStatType.PhysicalCritChance] * prestigeFactor;
                    break;
                case FamiliarStatType.SpellCritChance:
                    familiarUnitStats.a._Value = FamiliarBaseStatValues[FamiliarStatType.SpellCritChance] * prestigeFactor;
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
                    familiarAbilityBarShared.AbilityAttackSpeed._Value = abilityBar_Shared.AbilityAttackSpeed._Value * (1 + (FamiliarBaseStatValues[FamiliarStatType.CastSpeed] * prestigeFactor));
                    break;
                default:
                    break;
            }
        }
        */

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

        RemoveMisc(familiar, familiarId);
    }
    public static void ModifyBloodSource(Entity familiar, int level)
    {
        familiar.With((ref BloodConsumeSource bloodConsumeSource) =>
        {
            bloodConsumeSource.BloodQuality = level / (float)_maxFamiliarLevel * 100;
            bloodConsumeSource.CanBeConsumed = false;
        });
    }
    public static void PreventDisableFamiliar(Entity familiar)
    {
        familiar.AddWith((ref CanPreventDisableWhenNoPlayersInRange canPreventDisable) =>
        {
            canPreventDisable.CanDisable = new ModifiableBool(false);
        });
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
        
        // best values tried so far
        familiar.With((ref DynamicCollision dynamicCollision) =>
        {
            dynamicCollision.AgainstPlayers.RadiusOverride = -1f;
            dynamicCollision.AgainstPlayers.HardnessThreshold._Value = 0.1f;
            dynamicCollision.AgainstPlayers.PushStrengthMax._Value = 0f;
            dynamicCollision.AgainstPlayers.PushStrengthMin._Value = 0f;
        });
    }
    public static void RemoveDropTable(Entity familiar)
    {
        if (!familiar.Has<DropTableBuffer>()) return;

        var buffer = familiar.ReadBuffer<DropTableBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            var item = buffer[i];

            item.DropTableGuid = PrefabGUID.Empty;
            item.DropTrigger = DropTriggerType.OnSalvageDestroy;
            item.RelicType = RelicType.None;

            buffer[i] = item;
        }
    }
    static void ModifyAggro(Entity familiar)
    {
        /*
        familiar.With((ref AggroConsumer aggroConsumer) =>
        {
            aggroConsumer.ProximityRadius = 0f;
            aggroConsumer.ProximityWeight = 0f;
        });
        */

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
        if (familiar.Has<SpawnPrefabOnGameplayEvent>())
        {
            var buffer = familiar.ReadBuffer<SpawnPrefabOnGameplayEvent>();

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpawnPrefab.Equals(_monsterFakePos)) // bounds error console spam culprit?
                {
                    SpawnPrefabOnGameplayEvent spawnPrefabOnGameplayEvent = buffer[i];
                    spawnPrefabOnGameplayEvent.SpawnPrefab = PrefabGUID.Empty;
                    buffer[i] = spawnPrefabOnGameplayEvent;

                    break;
                }
                else if (buffer[i].SpawnPrefab.GetPrefabName().Contains("pilot", StringComparison.OrdinalIgnoreCase)) // don't want pilots spawning from spider tank familiars
                {
                    if (familiar.TryGetBuffer<MinionBuffer>(out var minions))
                    {
                        foreach (MinionBuffer minion in minions)
                        {
                            if (minion.Entity.Exists())
                            {
                                minion.Entity.Destroy();
                            }
                        }
                    }

                    break;
                }
            }
        }

        if (familiarId.Equals(_charDivineAngel) && familiar.Has<Script_ApplyBuffUnderHealthThreshold_DataServer>()) // don't want fallen angels spawning from divine angel familiars on death
        {
            familiar.With((ref Script_ApplyBuffUnderHealthThreshold_DataServer script_ApplyBuffUnderHealthThreshold_DataServer) =>
            {
                script_ApplyBuffUnderHealthThreshold_DataServer.NewBuffEntity = PrefabGUID.Empty;
            });
        }

        if (familiarId.Equals(_charWerewolfChieftainVBlood) && familiar.TryGetBuffer<AbilityGroupSlotBuffer>(out var abilityGroupBuffer)) // don't want wilfred familiars opening cages in werewolf village
        {
            for (int i = abilityGroupBuffer.Length - 1; i >= 0; i--)
            {
                AbilityGroupSlotBuffer entry = abilityGroupBuffer[i];

                if (entry.BaseAbilityGroupOnSlot.Equals(_openTheCages))
                {
                    // buffer.RemoveAt(i);
                    entry.GroupSlotEntity.GetEntityOnServer().Destroy();
                    break;
                }
            }
        }

        if (familiar.Has<Immortal>()) familiar.Remove<Immortal>();
    }
    static void HandleShiny(User user, ulong steamId, Entity familiar, PrefabGUID familiarId, bool battle = false)
    {
        if (battle && GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
        {
            int famKey = familiarId.GuidHash;
            FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);

            if (buffsData.FamiliarBuffs.ContainsKey(famKey))
            {
                PrefabGUID shinyBuff = new(buffsData.FamiliarBuffs[famKey].First());
                Buffs.HandleShinyBuff(familiar, shinyBuff);
            }
        }
        else if (GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
        {
            string colorCode = SHINY_DEFAULT;
            bool isShiny = false;

            if (GetPlayerBool(steamId, SHINY_FAMILIARS_KEY))
            {
                int famKey = familiarId.GuidHash;
                FamiliarBuffsData buffsData = LoadFamiliarBuffsData(steamId);

                if (buffsData.FamiliarBuffs.ContainsKey(famKey))
                {
                    PrefabGUID shinyBuff = new(buffsData.FamiliarBuffs[famKey].First());

                    HandleResistanceBuffForShiny(familiar);
                    Buffs.HandleShinyBuff(familiar, shinyBuff);

                    if (FamiliarUnlockSystem.ShinyBuffColorHexes.TryGetValue(new(buffsData.FamiliarBuffs[famKey].First()), out var hexColor))
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
    static void HandleResistanceBuffForShiny(Entity familiar)
    {
        string familiarName = familiar.GetPrefabGuid().GetPrefabName();

        if (!familiarName.Contains("golem", StringComparison.OrdinalIgnoreCase) && !familiarName.Contains("spidertank", StringComparison.OrdinalIgnoreCase)) return;
        else if (familiar.TryGetComponent(out BuffResistances buffResistances))
        {
            Entity resistanceBuff = buffResistances.SettingsEntity._Value;

            if (resistanceBuff.TryGetBuffer<BuffResistanceElement>(out var buffer) && !buffer.IsEmpty)
            {
                BuffResistanceElement buffResistanceElement = buffer[0];
                buffResistanceElement.BuffCategory = 536871313;
                buffer[0] = buffResistanceElement;
            }
        }
    }
    static void HandleMapIcon(Entity playerCharacter, Entity familiar)
    {
        if (ServerGameManager.TryInstantiateBuffEntityImmediate(playerCharacter, familiar, _mapIconBuff, out Entity buffEntity))
        {
            var buffer = buffEntity.ReadBuffer<AttachMapIconsToEntity>();

            AttachMapIconsToEntity attachMapIconsToEntity = buffer[0];
            attachMapIconsToEntity.Prefab = _mapIconAlliedPlayer;

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
    static void HandleMountedUnit(Entity playerCharacter, Entity familiar, bool battle = false)
    {
        if (!familiar.TryGetComponent(out UnitMounter unitMounter)) return;

        if (unitMounter.MountEntity.TryGetSyncedEntity(out Entity mount))
        {

        }
    }
    static Entity HandleFamiliarServant(Entity familiar)
    {
        Entity servant = ServerGameManager.InstantiateEntityImmediate(familiar, _charVampireCultistServant);

        if (servant.Exists())
        {
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

            InventoryUtilitiesServer.InstantiateInventory(EntityManager, servant, 0);
            // InventoryUtilitiesServer.InstantiateInventory(EntityManager, servant, 27);

            /*
            int index = SystemService.AttachParentIdSystem.GetFreeParentIndex();
            servant.AddWith((ref AttachParentId attachParentId) =>
            {
                attachParentId.Index = index;
            });

            if (!servant.TryGetBuffer<AttachedBuffer>(out var buffer))
            {
                buffer = servant.AddBuffer<AttachedBuffer>();
                Entity inventory = InventoryUtilities.TryGetInventoryEntity(EntityManager, servant, out inventory) ? inventory : Entity.Null;

                if (inventory.Exists())
                {
                    AttachedBuffer attachedBuffer = new()
                    {
                        Entity = inventory,
                        PrefabGuid = _externalInventory
                    };

                    buffer.Add(attachedBuffer);
                }
            }
            */

            servant.Add<BlockFeedBuff>();
            // servant.TryRemove<OpenDoors>();
            servant.Remove<ServantPowerConstants>();
            // servant.TryRemoveComponent<UnitLevel>(); // console spam - typeIndexInArchetype was -1 for NetworkComponentIndex: 20. networkSnapshotType: 331

            RemoveDropTable(servant);

            servant.AddWith((ref Follower follower) =>
            {
                follower.Followed._Value = familiar;
                follower.ModeModifiable._Value = 0;
                follower.Stationary._Value = true;
            });

            DisableFamiliarServantRoutine(servant).Start(); // doing this immediately is reverted by server idk

            return servant;
        }

        return Entity.Null;
    }
    static IEnumerator DisableFamiliarServantRoutine(Entity servant)
    {
        yield return _delay;
        servant.Add<Disabled>();
    }
    static IEnumerator ApplyFamiliarStatsRoutine(Entity familiar)
    {
        yield return _delay;
        Buffs.RefreshStats(familiar);
    }
}