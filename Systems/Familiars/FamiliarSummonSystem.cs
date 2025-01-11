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
    static DebugEventsSystem DebugEventsSystem => SystemService.DebugEventsSystem;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static EntityCommandBufferSystem EntityCommandBufferSystem => SystemService.EntityCommandBufferSystem;

    static readonly GameDifficulty _gameDifficulty = SystemService.ServerGameSettingsSystem.Settings.GameDifficulty;
    // static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly bool _familiarCombat = ConfigService.FamiliarCombat;
    static readonly bool _familiarPrestige = ConfigService.FamiliarPrestige;

    // static readonly WaitForSeconds _delay = new(1f);

    public const float FAMILIAR_LIFETIME = 240f;
    const float SPAWN_BUFF_LIFETIME = 2.5f;

    static readonly int _maxFamiliarLevel = ConfigService.MaxFamiliarLevel;
    static readonly float _familiarPrestigeStatMultiplier = ConfigService.FamiliarPrestigeStatMultiplier;
    static readonly float _vBloodDamageMultiplier = ConfigService.VBloodDamageMultiplier;

    public static Entity _unitTeamSingleton = Entity.Null;

    static readonly PrefabGUID _invulnerableBuff = new(-480024072);
    static readonly PrefabGUID _hideStaffBuff = new(2053361366);
    static readonly PrefabGUID _spawnBuff = new(-1782768874); // AB_Undead_BishopOfShadows_Idle_Buff
    static readonly PrefabGUID _hideSpawnBuff = new(396339796);

    static readonly PrefabGUID _divineAngel = new(-1737346940);

    static readonly PrefabGUID _ignoredFaction = new(-1430861195);
    static readonly PrefabGUID _playerFaction = new(1106458752);
    static readonly PrefabGUID _legionFaction = new(-772044125);
    static readonly PrefabGUID _cursedFaction = new(1522496317);

    static readonly List<PrefabGUID> _teamFactions =
    [
        _legionFaction,
        _cursedFaction
    ];

    public static readonly ConcurrentDictionary<ulong, List<PrefabGUID>> PlayerFamiliarBattleGroups = [];
    public static readonly ConcurrentDictionary<ulong, bool> PlayerSummoningForBattle = []; // can add this layer to normal binding if more validation is needed after moving away from binding bool in file
    public static readonly ConcurrentDictionary<ulong, List<Entity>> PlayerBattleFamiliars = [];
    public static void SummonFamiliar(Entity playerCharacter, Entity userEntity, int famKey)
    {
        PrefabGUID familiarId = new(famKey);
        User user = userEntity.Read<User>();
        Entity familiar = ServerGameManager.InstantiateEntityImmediate(playerCharacter, familiarId);

        HandleFamiliarV2(playerCharacter, user, familiar, familiarId, playerCharacter.GetPosition());

        /*
ulong steamId = user.PlatformId;
int index = user.Index;
EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

if (PlayerBindingValidation.ContainsKey(steamId))
{
    PlayerBindingValidation[steamId] = new(famKey);
}
else PlayerBindingValidation.TryAdd(steamId, new(famKey));

FromCharacter fromCharacter = new() { Character = character, User = userEntity };

SpawnDebugEvent debugEvent = new()
{
    PrefabGuid = new(famKey),
    Control = false,
    Roam = false,
    Team = SpawnDebugEvent.TeamEnum.Ally,
    Level = 1,
    Position = character.ReadRO<LocalToWorld>().Position,
    DyeIndex = 0
};

DebugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);     

if (PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(familiarId, out Entity prefabEntity))
{
    Entity familiar = entityCommandBuffer.Instantiate(prefabEntity);
}
else
{
    Core.Log.LogWarning($"Failed to find prefab entity - {familiarId}");
}
*/
    }
    static void HandleFamiliarV2(Entity playerCharacter, User user, Entity familiar, PrefabGUID familiarId, float3 position)
    {
        ulong steamId = user.PlatformId;
        
        if (familiar.Exists() && playerCharacter.TryGetTeamEntity(out Entity teamReference))
        {
            familiar.SetTeam(teamReference);
            familiar.SetPosition(position);

            if (HandleFamiliar(user, playerCharacter, familiar))
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
    public static void SummonFamiliarForBattle(Entity playerCharacter, Entity userEntity, PrefabGUID familiarId, float3 position, int teamIndex)
    {
        User user = userEntity.Read<User>();
        Entity familiar = ServerGameManager.InstantiateEntityImmediate(playerCharacter, familiarId);

        HandleFamiliarForBattle(playerCharacter, user, familiar, position, teamIndex);

        /*
        EntityCommandBuffer entityCommandBuffer = EntityCommandBufferSystem.CreateCommandBuffer();

        User user = userEntity.Read<User>();
        int index = user.Index;

        FromCharacter fromCharacter = new() { Character = character, User = userEntity };

        SpawnDebugEvent debugEvent = new()
        {
            PrefabGuid = familiarPrefabGUID,
            Control = false,
            Roam = false,
            Team = SpawnDebugEvent.TeamEnum.Ally,
            Level = 1,
            Position = position,
            DyeIndex = 0
        };

        DebugEventsSystem.SpawnDebugEvent(index, ref debugEvent, entityCommandBuffer, ref fromCharacter);
        */
    }
    public static bool HandleFamiliar(User user, Entity player, Entity familiar)
    {
        try
        {
            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            int level = unitLevel.Level._Value;
            int famKey = familiar.Read<PrefabGUID>().GuidHash;
            ulong steamId = user.PlatformId;

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            level = famData.FamiliarExperience.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(1, ConvertLevelToXp(1));
                famData.FamiliarExperience[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperience(steamId, famData);
            }

            if (ModifyFamiliar(user, steamId, famKey, player, familiar, level))
            {
                SpawnFamiliarBuff(player);
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
        try
        {
            ulong steamId = user.PlatformId;

            UnitLevel unitLevel = familiar.Read<UnitLevel>();
            int level = unitLevel.Level._Value;
            int famKey = familiar.Read<PrefabGUID>().GuidHash;

            FamiliarExperienceData famData = FamiliarExperienceManager.LoadFamiliarExperience(steamId);
            level = famData.FamiliarExperience.TryGetValue(famKey, out var xpData) ? xpData.Key : 0;

            if (level == 0)
            {
                level = 1;

                KeyValuePair<int, float> newXP = new(level, ConvertLevelToXp(level));
                famData.FamiliarExperience[famKey] = newXP;

                FamiliarExperienceManager.SaveFamiliarExperience(steamId, famData);
            }

            familiar.SetPosition(position);

            if (ModifyFamiliarForBattle(user, steamId, famKey, playerCharacter, familiar, level, teamIndex))
            {
                // Utilities.Familiars.FaceYourEnemy

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
    public static bool ModifyFamiliar(User user, ulong steamId, int famKey, Entity player, Entity familiar, int level)
    {
        try
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);

            ModifyFollowerFactionMinion(player, familiar);
            ModifyDamageStats(familiar, level, steamId, famKey);
            ModifyConvertable(familiar);
            ModifyCollision(familiar);
            ModifyDropTable(familiar);
            PreventDisableFamiliar(familiar);

            if (!_familiarCombat) DisableCombat(player, familiar);

            if (Misc.PlayerBoolsManager.TryGetPlayerBool(steamId, "FamiliarVisual", out bool value))
            {
                FamiliarBuffsData data = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);
                if (data.FamiliarBuffs.ContainsKey(famKey))
                {
                    PrefabGUID visualBuff = new(data.FamiliarBuffs[famKey][0]);
                    Buffs.HandleVisual(familiar, visualBuff);
                }
            }

            //if (_gameMode.Equals(GameModeType.PvP)) 
            ManualAggroHandling(familiar); // seems generally better than normal game handling when they're considered minions even for PvE

            return true;
        }
        catch (Exception ex)
        {
            Core.Log.LogWarning($"Error during familiar battle modifications for {user.CharacterName.Value}: {ex}");

            return false;
        }
    }
    public static bool ModifyFamiliarForBattle(User user, ulong steamId, int famKey, Entity player, Entity familiar, int level, int factionIndex)
    {
        try
        {
            if (familiar.Has<BloodConsumeSource>()) ModifyBloodSource(familiar, level);

            ModifyTeamFactionAggro(familiar, factionIndex);
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

                    if (enemy.Exists()) Utilities.Familiars.FaceYourEnemy(familiar, enemy);
                }
            }
            else
            {
                Core.Log.LogWarning($"SetDirectionAndFaction contained {steamId} but couldn't find MatchPair for battle!");
            }

            if (Misc.PlayerBoolsManager.GetPlayerBool(steamId, "FamiliarVisual"))
            {
                FamiliarBuffsData data = FamiliarBuffsManager.LoadFamiliarBuffs(steamId);

                if (data.FamiliarBuffs.ContainsKey(famKey))
                {
                    PrefabGUID visualBuff = new(data.FamiliarBuffs[famKey][0]);
                    Buffs.HandleVisual(familiar, visualBuff);
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
    static void SpawnFamiliarBuff(Entity playerCharacter)
    {
        if (playerCharacter.TryApplyAndGetBuff(_spawnBuff, out Entity buffEntity))
        {
            if (!buffEntity.Has<LifeTime>()) buffEntity.Add<LifeTime>();

            buffEntity.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = SPAWN_BUFF_LIFETIME;
                lifeTime.EndAction = LifeTimeEndAction.Destroy;
            });
        }
    }
    static void DisableCombat(Entity player, Entity familiar)
    {
        FactionReference factionReference = familiar.Read<FactionReference>();
        factionReference.FactionGuid._Value = _ignoredFaction;
        familiar.Write(factionReference);

        AggroConsumer aggroConsumer = familiar.Read<AggroConsumer>();
        aggroConsumer.Active._Value = false;
        familiar.Write(aggroConsumer);

        Aggroable aggroable = familiar.Read<Aggroable>();
        aggroable.Value._Value = false;
        aggroable.DistanceFactor._Value = 0f;
        aggroable.AggroFactor._Value = 0f;
        familiar.Write(aggroable);

        ApplyBuffDebugEvent applyBuffDebugEvent = new()
        {
            BuffPrefabGUID = _invulnerableBuff,
        };

        FromCharacter fromCharacter = new()
        {
            Character = familiar,
            User = player.Read<PlayerCharacter>().UserEntity,
        };

        DebugEventsSystem.ApplyBuff(fromCharacter, applyBuffDebugEvent);
        if (ServerGameManager.TryGetBuff(familiar, _invulnerableBuff.ToIdentifier(), out Entity invlunerableBuff))
        {
            if (invlunerableBuff.Has<LifeTime>())
            {
                var lifetime = invlunerableBuff.Read<LifeTime>();
                lifetime.Duration = -1;
                lifetime.EndAction = LifeTimeEndAction.None;
                invlunerableBuff.Write(lifetime);
            }
        }
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
    public enum FamiliarStatType
    {
        PhysicalCritChance,
        SpellCritChance,
        HealingReceived,
        PhysicalResistance,
        SpellResistance,
        CCReduction,
        ShieldAbsorb
    }
    public static readonly Dictionary<FamiliarStatType, float> FamiliarStatValues = new()
    {
        {FamiliarStatType.PhysicalCritChance, 0.2f},
        {FamiliarStatType.SpellCritChance, 0.2f},
        {FamiliarStatType.HealingReceived, 0.5f},
        {FamiliarStatType.PhysicalResistance, 0.2f},
        {FamiliarStatType.SpellResistance, 0.2f},
        {FamiliarStatType.CCReduction, 0.5f},
        {FamiliarStatType.ShieldAbsorb, 1f}
    };
    public static void ModifyDamageStatsForBattle(Entity familiar, int level, ulong steamId, int famKey)
    {
        float scalingFactor = 0.25f + (level / (float)_maxFamiliarLevel) * 0.75f; // Calculate scaling factor for power and such
        float healthScalingFactor = 1.0f + ((level - 1) / (float)_maxFamiliarLevel) * 1.5f; // Calculate scaling factor for max health

        if (level == _maxFamiliarLevel) healthScalingFactor = 2.5f;

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (_familiarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        PrefabGUID prefabGUID = familiar.Read<PrefabGUID>();

        if (prefabGUID.GuidHash.Equals(1945956671) && familiar.TryGetBuff(_hideStaffBuff, out Entity buffEntity))
        {
            DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
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
                case FamiliarStatType.CCReduction:
                    familiarStats.CCReduction._Value = (int)(FamiliarStatValues[FamiliarStatType.CCReduction] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier));
                    break;
                case FamiliarStatType.ShieldAbsorb:
                    familiarStats.ShieldAbsorbModifier._Value = unitStats.ShieldAbsorbModifier._Value + FamiliarStatValues[FamiliarStatType.ShieldAbsorb] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
            }
        }

        familiar.Write(familiarStats);

        UnitLevel unitLevel = familiar.Read<UnitLevel>();
        unitLevel.Level._Value = level;
        unitLevel.HideLevel = false;
        familiar.Write(unitLevel);

        Health familiarHealth = familiar.Read<Health>();
        int baseHealth = 500;

        if (_gameDifficulty.Equals(GameDifficulty.Hard))
        {
            baseHealth = 750;
        }

        familiarHealth.MaxHealth._Value = baseHealth * healthScalingFactor;
        familiarHealth.Value = familiarHealth.MaxHealth._Value;
        familiar.Write(familiarHealth);

        if (_vBloodDamageMultiplier != 1f)
        {
            DamageCategoryStats damageCategoryStats = familiar.Read<DamageCategoryStats>();

            if (damageCategoryStats.DamageVsVBloods._Value != _vBloodDamageMultiplier)
            {
                damageCategoryStats.DamageVsVBloods._Value *= _vBloodDamageMultiplier;
                familiar.Write(damageCategoryStats);
            }
        }

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

        if (prefabGUID.Equals(_divineAngel) && familiar.Has<Script_ApplyBuffUnderHealthThreshold_DataServer>())
        {
            familiar.With((ref Script_ApplyBuffUnderHealthThreshold_DataServer script_ApplyBuffUnderHealthThreshold_DataServer) =>
            {
                script_ApplyBuffUnderHealthThreshold_DataServer.NewBuffEntity = PrefabGUID.Empty;
            });
        }

        if (familiar.Has<Immortal>())
        {
            familiar.Remove<Immortal>();

            if (!familiar.Has<ApplyBuffOnGameplayEvent>()) return;

            var buffer = familiar.ReadBuffer<ApplyBuffOnGameplayEvent>();
            for (int i = 0; i < buffer.Length; i++)
            {
                var item = buffer[i];
                if (item.Buff0.GuidHash.Equals(2144624015)) // no bubble for Solarus
                {
                    item.Buff0 = new(0);
                    buffer[i] = item;

                    break;
                }
            }
        }
    }
    public static void ModifyDamageStats(Entity familiar, int level, ulong steamId, int famKey)
    {
        float scalingFactor = 0.1f + (level / (float)_maxFamiliarLevel) * 0.9f; // Calculate scaling factor for power and such
        float healthScalingFactor = 1.0f + ((level - 1) / (float)_maxFamiliarLevel) * 4.0f; // Calculate scaling factor for max health

        if (level == _maxFamiliarLevel) healthScalingFactor = 5.0f;

        int prestigeLevel = 0;
        List<FamiliarStatType> stats = [];

        if (_familiarPrestige && FamiliarPrestigeManager.LoadFamiliarPrestige(steamId).FamiliarPrestige.TryGetValue(famKey, out var prestigeData) && prestigeData.Key > 0)
        {
            prestigeLevel = prestigeData.Key;
            stats = prestigeData.Value;
        }

        // get base stats from original unit prefab then apply scaling
        PrefabGUID prefabGUID = familiar.Read<PrefabGUID>();

        if (prefabGUID.GuidHash.Equals(1945956671) && familiar.TryGetBuff(_hideStaffBuff, out Entity buffEntity))
        {
            DestroyUtility.Destroy(EntityManager, buffEntity, DestroyDebugReason.TryRemoveBuff);
        }

        Entity original = PrefabCollectionSystem._PrefabGuidToEntityMap[prefabGUID];
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
                case FamiliarStatType.CCReduction:
                    familiarStats.CCReduction._Value = (int)(FamiliarStatValues[FamiliarStatType.CCReduction] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier));
                    break;
                case FamiliarStatType.ShieldAbsorb:
                    familiarStats.ShieldAbsorbModifier._Value = unitStats.ShieldAbsorbModifier._Value + FamiliarStatValues[FamiliarStatType.ShieldAbsorb] * (1 + prestigeLevel * _familiarPrestigeStatMultiplier);
                    break;
            }
        }

        familiar.Write(familiarStats);

        UnitLevel unitLevel = familiar.Read<UnitLevel>();
        unitLevel.Level._Value = level;
        unitLevel.HideLevel = false;
        familiar.Write(unitLevel);

        Health familiarHealth = familiar.Read<Health>();
        int baseHealth = 500;

        if (_gameDifficulty.Equals(GameDifficulty.Hard))
        {
            baseHealth = 750;
        }

        familiarHealth.MaxHealth._Value = baseHealth * healthScalingFactor;
        familiarHealth.Value = familiarHealth.MaxHealth._Value;
        familiar.Write(familiarHealth);

        if (_vBloodDamageMultiplier != 1f)
        {
            DamageCategoryStats damageCategoryStats = familiar.Read<DamageCategoryStats>();

            if (damageCategoryStats.DamageVsVBloods._Value != _vBloodDamageMultiplier)
            {
                damageCategoryStats.DamageVsVBloods._Value *= _vBloodDamageMultiplier;
                familiar.Write(damageCategoryStats);
            }
        }

        if (familiar.Has<SpawnPrefabOnGameplayEvent>()) // stop pilots spawning from gloomrot mechs
        {
            var buffer = familiar.ReadBuffer<SpawnPrefabOnGameplayEvent>();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpawnPrefab.GetPrefabName().ToLower().Contains("pilot"))
                {
                    familiar.Remove<SpawnPrefabOnGameplayEvent>();
                }
            }
        }

        if (prefabGUID.Equals(_divineAngel) && familiar.TryGetComponent(out Script_ApplyBuffUnderHealthThreshold_DataServer script_ApplyBuffUnderHealthThreshold_DataServer))
        {
            script_ApplyBuffUnderHealthThreshold_DataServer.NewBuffEntity = PrefabGUID.Empty;
            familiar.Write(script_ApplyBuffUnderHealthThreshold_DataServer);
        }

        if (familiar.Has<Immortal>())
        {
            familiar.Remove<Immortal>();

            if (!familiar.Has<ApplyBuffOnGameplayEvent>()) return;

            var buffer = familiar.ReadBuffer<ApplyBuffOnGameplayEvent>();
            for (int i = 0; i < buffer.Length; i++)
            {
                var item = buffer[i];
                if (item.Buff0.GuidHash.Equals(2144624015)) // no bubble for Solarus
                {
                    item.Buff0 = new(0);
                    buffer[i] = item;
                    break;
                }
            }
        }
    }
    static void PreventDisableFamiliar(Entity familiar)
    {
        // one of the relics from the inception of familiars, might be better off not doing this in the long run
        // but hasn't caused issues (that I'm aware of, at least :p) and need to get out of current rabbit hole before entering another so leaving note for later >_>

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
}
