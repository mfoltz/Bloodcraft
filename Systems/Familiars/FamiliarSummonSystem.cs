using Bloodcraft.Services;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Entities;
using Unity.Mathematics;
using static Bloodcraft.Services.DataService.FamiliarPersistence;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Systems.Familiars;
internal static class FamiliarSummonSystem
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static readonly GameDifficulty _gameDifficulty = SystemService.ServerGameSettingsSystem.Settings.GameDifficulty;
    // static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _familiarCombat = ConfigService.FamiliarCombat;
    static readonly bool _familiarPrestige = ConfigService.FamiliarPrestige;

    // static readonly WaitForSeconds _delay = new(1f);

    public const float FAMILIAR_LIFETIME = 240f;
    const float SPAWN_BUFF_LIFETIME = 1.25f; // want to make as long as possible before another circle spawns, 1.25f is good try 1.5f? 1.5f sliiightly too long

    static readonly int _maxFamiliarLevel = ConfigService.MaxFamiliarLevel;
    static readonly float _familiarPrestigeStatMultiplier = ConfigService.FamiliarPrestigeStatMultiplier;
    static readonly float _vBloodDamageMultiplier = ConfigService.VBloodDamageMultiplier;

    public static Entity _unitTeamSingleton = Entity.Null;

    static readonly PrefabGUID _invulnerableBuff = new(-480024072);
    static readonly PrefabGUID _spawnBuff = new(-1782768874);        // AB_Undead_BishopOfShadows_Idle_Buff
    static readonly PrefabGUID _hideSpawnBuff = new(-205058219);      // Buff_General_Spawn_Unit_Medium
    static readonly PrefabGUID _distanceCheckBuff = new(1269197489); // AB_Vampire_CrimsonIronMaiden_DistanceChecker_Buff
    static readonly PrefabGUID _solarusFinalStageBuff = new(2144624015);
    static readonly PrefabGUID _targetSwallowedBuff = new(-915145807);

    static readonly PrefabGUID _divineAngel = new(-1737346940);
    static readonly PrefabGUID _solarus = new(-740796338);

    static readonly PrefabGUID _familiarServant = new(51737727);

    static readonly PrefabGUID _ignoredFaction = new(-1430861195);
    static readonly PrefabGUID _playerFaction = new(1106458752);
    static readonly PrefabGUID _legionFaction = new(-772044125);
    static readonly PrefabGUID _cursedFaction = new(1522496317);

    static readonly List<PrefabGUID> _teamFactions =
    [
        _legionFaction,
        _cursedFaction
    ];
    public enum FamiliarStatType
    {
        MaxHealth,
        AttackSpeed,        // Cast animation speed
        PrimaryAttackSpeed, // Primary attack speed
        PhysicalPower,
        PhysicalCritChance,
        PhysicalCritDamage,
        SpellPower,
        SpellCritChance,
        SpellCritDamage,
        HealingReceived,
        PhysicalResistance,
        SpellResistance,
        DamageReduction,
        ShieldAbsorb,
        MovementSpeed
    }

    public static readonly Dictionary<FamiliarStatType, float> FamiliarStatValues = new()
    {
        {FamiliarStatType.MaxHealth, 1f},
        {FamiliarStatType.AttackSpeed, 1f},
        {FamiliarStatType.PhysicalCritChance, 0.2f},
        {FamiliarStatType.SpellCritChance, 0.2f},
        {FamiliarStatType.HealingReceived, 0.5f},
        {FamiliarStatType.PhysicalResistance, 0.2f},
        {FamiliarStatType.SpellResistance, 0.2f},
        {FamiliarStatType.ShieldAbsorb, 1f}
    };

    public static readonly List<FamiliarStatType> FamiliarPrestigeStats =
    [
        FamiliarStatType.PhysicalCritChance,
        FamiliarStatType.SpellCritChance,
        FamiliarStatType.HealingReceived,
        FamiliarStatType.PhysicalResistance,
        FamiliarStatType.SpellResistance,
        FamiliarStatType.ShieldAbsorb
    ];

    public static readonly ConcurrentDictionary<ulong, List<PrefabGUID>> PlayerBattleGroups = [];
    public static readonly ConcurrentDictionary<ulong, List<Entity>> PlayerBattleFamiliars = [];
    public static void InstantiateFamiliarImmediate(User user, Entity playerCharacter, int famKey)
    {
        PrefabGUID familiarId = new(famKey);
        Entity familiar = ServerGameManager.InstantiateEntityImmediate(playerCharacter, familiarId);
        
        // if (SpawnTransformSystemOnSpawnPatch.UnitPrefabGuidsToModify.Contains(familiarId)) SpawnTransformSystemOnSpawnPatch.FamiliarsToSkip.Add(familiar);

        HandleBindingImmediate(playerCharacter, user, familiar, familiarId, playerCharacter.GetPosition());
    }
    public static void InstantiateFamiliarDeferred(User user, Entity playerCharacter, int famKey) // good idea in theory but a lot of effort to remake this entirely for ECB instead of EM, will reconsider in future
    {
        PrefabGUID familiarId = new(famKey);

        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();
        Entity familiar = ServerGameManager.InstantiateEntityDeferred(playerCharacter, familiarId);

        // if (SpawnTransformSystemOnSpawnPatch.UnitPrefabGuidsToModify.Contains(familiarId)) SpawnTransformSystemOnSpawnPatch._familiarsToSkip.Add(familiar); can't add this while deferred, maybe check for owner in patch and see if player to verify shard bearers and such

        HandleBindingDeferred(entityCommandBuffer, playerCharacter, user, familiar, familiarId, playerCharacter.GetPosition());
    }
    static void HandleBindingImmediate(Entity playerCharacter, User user, Entity familiar, PrefabGUID familiarId, float3 position)
    {
        ulong steamId = user.PlatformId;
        
        if (familiar.Exists() && playerCharacter.TryGetTeamEntity(out Entity teamReference))
        {
            familiar.SetTeam(teamReference);
            familiar.SetPosition(position);

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
    public static void SummonFamiliarForBattle(Entity playerCharacter, User user, PrefabGUID familiarId, float3 position, int teamIndex)
    {
        ulong steamId = user.PlatformId;
        Entity familiar = ServerGameManager.InstantiateEntityImmediate(playerCharacter, familiarId);

        if (!PlayerBattleFamiliars.ContainsKey(steamId)) PlayerBattleFamiliars[steamId] = [];

        PlayerBattleGroups[steamId].Remove(familiarId);
        PlayerBattleFamiliars[steamId].Add(familiar);

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
    }
    public static bool HandleFamiliarImmediate(User user, Entity playerCharacter, Entity familiar)
    {
        ulong steamId = user.PlatformId;

        try
        {
            int famKey = familiar.GetPrefabGuidHash();

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);
            int level = famData.FamiliarLevels.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(1, ConvertLevelToXp(1));
                famData.FamiliarLevels[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, famData);
            }

            if (ModifyFamiliarImmediate(user, steamId, famKey, playerCharacter, familiar, level))
            {
                playerCharacter.TryApplyBuffWithLifeTime(_spawnBuff, SPAWN_BUFF_LIFETIME);
                familiar.TryApplyBuff(_hideSpawnBuff);

                // if (HandleFamiliarServantImmediate(familiar)) Utilities.Familiars.EquipFamiliar(steamId, familiar, famKey);

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
    static bool HandleFamiliarServantImmediate(Entity familiar)
    {
        Entity servant = ServerGameManager.InstantiateEntityImmediate(familiar, _familiarServant);

        if (servant.Exists() && servant.TryApplyAndGetBuffWithOwner(familiar, _targetSwallowedBuff, out Entity buffEntity))
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
    public static bool HandleFamiliarDeferred(User user, Entity playerCharacter, Entity familiar) // use commandBuffer instead of entityManager
    {
        ulong steamId = user.PlatformId;

        try
        {
            // int level = familiar.GetUnitLevel();
            int famKey = familiar.GetPrefabGuidHash();

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);
            int level = famData.FamiliarLevels.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(1, ConvertLevelToXp(1));
                famData.FamiliarLevels[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, famData);
            }

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
    public static bool HandleFamiliarForBattle(Entity playerCharacter, User user, Entity familiar, float3 position, int teamIndex)
    {
        ulong steamId = user.PlatformId;

        try
        {
            //int level = familiar.GetUnitLevel();
            int famKey = familiar.GetPrefabGuidHash();

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperienceData(steamId);
            int level = famData.FamiliarLevels.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
                famData.FamiliarLevels[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperienceData(steamId, famData);
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
    public static bool ModifyFamiliarImmediate(User user, ulong steamId, int familiarId, Entity playerCharacter, Entity familiar, int level)
    {
        try
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);

            ModifyFollowerFactionMinion(playerCharacter, familiar);
            ModifyUnitStats(familiar, level, steamId, familiarId);
            ModifyConvertable(familiar);
            ModifyCollision(familiar);
            ModifyDropTable(familiar);
            PreventDisableFamiliar(familiar);

            if (!_familiarCombat) DisableCombat(familiar);

            if (Misc.PlayerBoolsManager.TryGetPlayerBool(steamId, "FamiliarVisual", out bool value))
            {
                FamiliarBuffsData data = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);
                if (data.FamiliarBuffs.ContainsKey(familiarId))
                {
                    PrefabGUID shinyBuff = new(data.FamiliarBuffs[familiarId].First());
                    Buffs.HandleShinyBuff(familiar, shinyBuff);
                }
            }

            ManualAggroHandling(familiar); // seems generally better than normal game handling for minions even on PvE

            /* don't think I need this right now per se since BloodyPoint teleports are handled but may want to revisit for other edge-cases
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
            */

            return true;
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error during familiar modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    public static bool ModifyFamiliarForBattle(User user, ulong steamId, int famKey, Entity player, Entity familiar, int level, int teamIndex)
    {
        try
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);

            ModifyTeamFactionAggro(familiar, teamIndex);
            ModifyDamageStatsForBattle(familiar, level, steamId, famKey);
            ModifyConvertable(familiar);
            ModifyCollision(familiar);
            ModifyDropTable(familiar);
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

            if (Misc.PlayerBoolsManager.GetPlayerBool(steamId, "FamiliarVisual"))
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
    static void DisableCombat(Entity familiar)
    {
        familiar.SetFaction(_ignoredFaction);

        familiar.DisableAggroable();
        familiar.DisableAggroable();

        familiar.TryApplyBuff(_invulnerableBuff);
    }
    static void ModifyTeamFactionAggro(Entity familiar, int factionIndex)
    {
        if (familiar.Has<FactionReference>())
        {
            PrefabGUID factionPrefabGUID = _teamFactions[factionIndex];
            // Core.Log.LogInfo($"Setting FactionReference - {factionPrefabGUID.GetPrefabName()} | {familiar.GetPrefabGuid().GetPrefabName()}");

            familiar.SetFaction(factionPrefabGUID);
        }

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

        if (familiar.Has<Team>() && _unitTeamSingleton.Exists())
        {
            // Core.Log.LogInfo($"Setting Team/TeamReference - {_unitTeamSingleton.GetPrefabGuid().GetPrefabName()} | {familiar.GetPrefabGuid().GetPrefabName()}");

            familiar.With((ref Team team) =>
            {
                team.Value = 2;
            });

            familiar.With((ref TeamReference teamReference) =>
            {
                teamReference.Value._Value = _unitTeamSingleton;
            });
        }

        if (!familiar.Has<BlockFeedBuff>()) familiar.Add<BlockFeedBuff>(); // general marker for fams
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
    public static void ModifyBloodSource(Entity familiar, int level)
    {
        BloodConsumeSource bloodConsumeSource = familiar.Read<BloodConsumeSource>();
        bloodConsumeSource.BloodQuality = level / (float)_maxFamiliarLevel * 100;
        bloodConsumeSource.CanBeConsumed = false;
        familiar.Write(bloodConsumeSource);
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

        HandleGeneralSnipping(familiar, familiarId);
    }
    public static void ModifyUnitStats(Entity familiar, int level, ulong steamId, int famKey)
    {
        float scalingFactor = 0.1f + (level / (float)_maxFamiliarLevel) * 0.9f; // Calculate scaling factor for power and such
        float healthScalingFactor = 1.0f + ((level - 1) / (float)_maxFamiliarLevel) * 4.0f; // Calculate scaling factor for max health

        if (level == _maxFamiliarLevel) healthScalingFactor = 5.0f;

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (_familiarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestiges.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        // traits should be % added bonus on top of whatever their normal scaled stats are? still need to think about equipment but yeah that should mostly work

        // get base stats from original unit prefab then apply scaling
        PrefabGUID familiarId = familiar.Read<PrefabGUID>();

        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[familiarId];
        UnitStats unitStats = original.Read<UnitStats>();

        UnitStats familiarStats = familiar.Read<UnitStats>();
        familiarStats.PhysicalPower._Value = unitStats.PhysicalPower._Value * scalingFactor * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
        familiarStats.SpellPower._Value = unitStats.SpellPower._Value * scalingFactor * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);

        foreach (FamiliarStatType stat in stats)
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

        HandleGeneralSnipping(familiar, familiarId);
    }
    static void PreventDisableFamiliar(Entity familiar)
    {
        // one of the relics from the inception of familiars, might be better off not doing this in the long run
        // but hasn't caused issues (that I'm aware of, at least) and need to get out of current rabbit hole before entering another so leaving note for later >_>

        ModifiableBool modifiableBool = new() { _Value = false };
        CanPreventDisableWhenNoPlayersInRange canPreventDisable = new() { CanDisable = modifiableBool };

        EntityManager.AddComponentData(familiar, canPreventDisable);
    }
    static void ModifyConvertable(Entity familiar)
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
    static void ModifyCollision(Entity familiar)
    {
        DynamicCollision collision = familiar.Read<DynamicCollision>();
        collision.AgainstPlayers.RadiusOverride = -1f;
        collision.AgainstPlayers.HardnessThreshold._Value = 0f;
        collision.AgainstPlayers.PushStrengthMax._Value = 0f;
        collision.AgainstPlayers.PushStrengthMin._Value = 0f;
        collision.AgainstPlayers.RadiusVariation = 0f;
        familiar.Write(collision);
    }
    static void ModifyDropTable(Entity familiar)
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
    static void ManualAggroHandling(Entity familiar)
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
    static void HandleGeneralSnipping(Entity familiar, PrefabGUID familiarId)
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

        if (familiar.Has<Immortal>())
        {
            familiar.Remove<Immortal>();

            if (familiarId.Equals(_solarus) && familiar.TryGetBuffer<ApplyBuffOnGameplayEvent>(out var buffer) && !buffer.IsEmpty)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    var item = buffer[i];

                    if (item.Buff0.Equals(_solarusFinalStageBuff))
                    {
                        item.Buff0 = PrefabGUID.Empty;
                        buffer[i] = item;

                        break;
                    }
                }
            }
        }
    }
}
