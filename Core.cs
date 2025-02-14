using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Bloodcraft.Patches;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Bloodcraft;
internal static class Core
{
    public static World Server { get; } = GetServerWorld() ?? throw new Exception("There is no Server world (yet)...");
    public static EntityManager EntityManager => Server.EntityManager;
    public static ServerGameManager ServerGameManager => SystemService.ServerScriptMapper.GetServerGameManager();
    public static SystemService SystemService { get; } = new(Server);
    public static double ServerTime => ServerGameManager.ServerTime;
    public static ManualLogSource Log => Plugin.LogInstance;

    static MonoBehaviour _monoBehaviour;

    static readonly List<PrefabGUID> _returnBuffs =
    [
        new PrefabGUID(-560330878),  // ReturnBuff
        new PrefabGUID(2086395440),  // ReturnNoInvulnerableBuff
        new PrefabGUID(-1049988817), // ValenciaReturnBuff
        new PrefabGUID(-1377587236), // DraculaReturnBuff
        new PrefabGUID(-1448806401), // OtherDraculaReturnBuff
        new PrefabGUID(-1511222240), // WerewolfChieftainReturnBuff
        new PrefabGUID(-1773136595), // DominaReturnBuff
        new PrefabGUID(-1983671299), // AngramReturnBuff
        new PrefabGUID(1089939900),  // AdamReturnBuff
        new PrefabGUID(-1435372081)  // SolarusReturnBuff
    ];

    // static readonly bool _performance = ConfigService.PerformanceAuditing;

    static readonly PrefabGUID _fallenAngel = new(-76116724);

    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);
    static readonly PrefabGUID _mapIconCharmed = new(-1491648886);

    static readonly PrefabGUID _bearDashAbility = new(1873182450);
    static readonly PrefabGUID _wolfBiteAbility = new(-1262842180);

    static readonly PrefabGUID _wolfBiteCast = new(990205141);

    static readonly HashSet<PrefabGUID> _bearFormBuffs =
    [
        new(-1569370346), // AB_Shapeshift_Bear_Buff
        new(-858273386)   // AB_Shapeshift_Bear_Skin01_Buff
    ];

    static readonly HashSet<PrefabGUID> _soulShardDropTables =
    [
        new(-454715368),  // DT_Unit_Relic_Manticore_Unique
        new(-1629745461), // DT_Unit_Relic_Paladin_Unique
        new(492631484),   // DT_Unit_Relic_Monster_Unique
        new(-191917509)   // DT_Unit_Relic_Dracula_Unique
    ];

    const float DIRECTION_DURATION = 6f; // for making familiars on team two face correct direction until battle starts

    const string SANGUIS = "Sanguis";
    const string SANGUIS_DATA_CLASS = "Sanguis.Core+DataStructures";
    const string SANGUIS_DATA_PROPERTY = "PlayerTokens";
    const string SANGUIS_CONFIG_CLASS = "Sanguis.Plugin";
    const string SANGUIS_CONFIG_PROPERTY = "TokensPerMinute";
    const string SANGUIS_SAVE_METHOD = "SavePlayerTokens";
    public static byte[] OLD_SHARED_KEY { get; internal set; }
    public static byte[] NEW_SHARED_KEY { get; internal set; }

    public static bool _initialized = false;
    public static void Initialize()
    {
        if (_initialized) return;

        OLD_SHARED_KEY = Convert.FromBase64String(SecretManager.GetOldSharedKey()); // loading these last causes a bad format exception now... oh computers, you so fun
        NEW_SHARED_KEY = Convert.FromBase64String(SecretManager.GetNewSharedKey());

        // if (_performance) ServerPerformanceLogger();
        _ = new PlayerService();
        _ = new LocalizationService();

        if (ConfigService.ClientCompanion) _ = new EclipseService();

        if (ConfigService.ExtraRecipes) Recipes.ModifyRecipes();
        if (ConfigService.StarterKit) Configuration.StarterKitItems();
        if (ConfigService.PrestigeSystem) Buffs.PrestigeBuffs();
        if (ConfigService.SoftSynergies || ConfigService.HardSynergies)
        {
            Configuration.ClassPassiveBuffsMap();
            Configuration.ClassSpellCooldownMap();
            Classes.GenerateAbilityJewelMap();
        }

        if (ConfigService.LevelingSystem) DeathEventListenerSystemPatch.OnDeathEventHandler += LevelingSystem.OnUpdate;
        if (ConfigService.ExpertiseSystem) DeathEventListenerSystemPatch.OnDeathEventHandler += WeaponSystem.OnUpdate;
        if (ConfigService.QuestSystem)
        {
            _ = new QuestService();
            DeathEventListenerSystemPatch.OnDeathEventHandler += QuestSystem.OnUpdate;
        }
        if (ConfigService.FamiliarSystem)
        {
            Configuration.FamiliarBans();
            if (!ConfigService.LevelingSystem) DeathEventListenerSystemPatch.OnDeathEventHandler += FamiliarLevelingSystem.OnUpdate;
            DeathEventListenerSystemPatch.OnDeathEventHandler += FamiliarUnlockSystem.OnUpdate;
            //DetectSanguis(); want to nail the fun factor and make sure no glaring bugs before adding stakes
            _ = new BattleService();
        }
        if (ConfigService.ProfessionSystem)
        {
            // uhh definitely remember what I was going to put here? hrm
        }

        ModifyPrefabs();
        // MiscLogging();

        _initialized = true;
    }
    static World GetServerWorld()
    {
        return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (_monoBehaviour == null)
        {
            _monoBehaviour = new GameObject("Bloodcraft").AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(_monoBehaviour.gameObject);
        }

        _monoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }
    static void ModifyPrefabs()
    {
        if (ConfigService.FamiliarSystem)
        {
            foreach (PrefabGUID prefabGUID in _returnBuffs)
            {
                if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(prefabGUID, out Entity returnBuffPrefab))
                {
                    if (returnBuffPrefab.TryGetBuffer<HealOnGameplayEvent>(out var buffer))
                    {
                        HealOnGameplayEvent healOnGameplayEvent = buffer[0];
                        healOnGameplayEvent.showSCT = false;
                        buffer[0] = healOnGameplayEvent;
                    }
                }
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_defaultEmoteBuff, out Entity defaultEmotePrefab))
            {
                defaultEmotePrefab.With((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = DIRECTION_DURATION;
                });

                defaultEmotePrefab.With((ref ModifyRotation modifyRotation) =>
                {
                    modifyRotation.TargetDirectionType = TargetDirectionType.TowardsBuffOwner;
                    modifyRotation.SnapToDirection = true;
                    modifyRotation.Type = RotationModificationType.Set;
                });
            }

            /*
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_mapIconCharmed, out Entity mapIconCharmedPrefab))
            {
                mapIconCharmedPrefab.With((ref LocalTransform localTransform) =>
                {
                    localTransform.Scale = 0.25f;
                });
            }
            */
        }
        if (ConfigService.SoftSynergies || ConfigService.HardSynergies)
        {
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_fallenAngel, out Entity fallenAngelPrefab))
            {
                if (!fallenAngelPrefab.Has<BlockFeedBuff>()) fallenAngelPrefab.Add<BlockFeedBuff>();
            }
        }
        if (ConfigService.BearFormDash)
        {
            /*
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_wolfBiteCast, out Entity wolfBiteCastPrefab))
            {
                wolfBiteCastPrefab.With((ref AbilityState abilityState) =>
                {
                    AbilityTypeFlag abilityTypeFlags = abilityState.AbilityTypeFlag;

                    abilityTypeFlags |= AbilityTypeFlag.Interact;
                    abilityTypeFlags &= ~(AbilityTypeFlag.AbilityKit | AbilityTypeFlag.AbilityKit_BreakStealth);

                    abilityState.AbilityTypeFlag = abilityTypeFlags;
                });
            }
            */

            foreach (PrefabGUID bearFormBuff in _bearFormBuffs)
            {
                if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(bearFormBuff, out Entity bearFormPrefab)
                    && bearFormPrefab.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    ReplaceAbilityOnSlotBuff abilityOnSlotBuff = buffer[4];
                    abilityOnSlotBuff.NewGroupId = _bearDashAbility;

                    buffer[4] = abilityOnSlotBuff;
                }
            }

            /*
            foreach (PrefabGUID wolfFormBuff in _wolfFormBuffs)
            {
                if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(wolfFormBuff, out Entity wolfFormPrefab)
                    && wolfFormPrefab.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    ReplaceAbilityOnSlotBuff buff = new()
                    {
                        Target = ReplaceAbilityTarget.BuffTarget,
                        Slot = 0,
                        NewGroupId = _wolfBiteAbility,
                        CopyCooldown = true,
                        Priority = 99,
                        CastBlockType = GroupSlotModificationCastBlockType.WholeCast
                    };

                    buffer.Insert(0, buff);
                }
            }
            */
        }
        if (ConfigService.EliteShardBearers) // should probably just modify their stats on the base prefabs instead of in the spawnTransformSystem >_>
        {
            foreach (PrefabGUID soulShardDropTable in _soulShardDropTables)
            {
                if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(soulShardDropTable, out Entity soulShardDropTablePrefab)
                    && soulShardDropTablePrefab.TryGetBuffer<DropTableDataBuffer>(out var buffer) && !buffer.IsEmpty)
                {
                    DropTableDataBuffer dropTableDataBuffer = buffer[0];

                    dropTableDataBuffer.DropRate = 0.6f;
                    buffer.Add(dropTableDataBuffer);

                    dropTableDataBuffer.DropRate = 0.45f;
                    buffer.Add(dropTableDataBuffer);

                    dropTableDataBuffer.DropRate = 0.3f;
                    buffer.Add(dropTableDataBuffer);
                }
            }
        }
    }

    static readonly ComponentType[] _prefabGUIDComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
    ];

    const string STAT_MOD = "StatMod";
    static void GatherStatMods()
    {
        var namesToPrefabGuids = SystemService.PrefabCollectionSystem.NameToPrefabGuidDictionary;

        foreach (var kvp in namesToPrefabGuids)
        {
            if (kvp.Key.StartsWith(STAT_MOD))
            {
                JewelSpawnSystemPatch.StatMods.TryAdd(kvp.Key, kvp.Value);
            }
        }
    }
    static void DetectSanguis()
    {
        try
        {
            Assembly sanguis = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == SANGUIS);

            if (sanguis != null)
            {
                Type config = sanguis.GetType(SANGUIS_CONFIG_CLASS);
                Type data = sanguis.GetType(SANGUIS_DATA_CLASS);

                if (config != null && data != null)
                {
                    PropertyInfo configProperty = config.GetProperty(SANGUIS_CONFIG_PROPERTY, BindingFlags.Static | BindingFlags.Public);
                    PropertyInfo dataProperty = data.GetProperty(SANGUIS_DATA_PROPERTY, BindingFlags.Static | BindingFlags.Public);

                    if (configProperty != null && dataProperty != null)
                    {
                        MethodInfo saveTokens = data.GetMethod(SANGUIS_SAVE_METHOD, BindingFlags.Static | BindingFlags.Public);
                        Dictionary<ulong, (int Tokens, (DateTime Start, DateTime DailyLogin) TimeData)> playerTokens = (Dictionary<ulong, (int, (DateTime, DateTime))>)(dataProperty?.GetValue(null) ?? new());
                        int tokensTransferred = (int)(configProperty?.GetValue(null) ?? 0);

                        if (saveTokens != null && playerTokens.Any() && tokensTransferred > 0)
                        {
                            BattleService._awardSanguis = true;
                            BattleService._tokensProperty = dataProperty;
                            BattleService._tokensTransferred = tokensTransferred;
                            BattleService._saveTokens = saveTokens;
                            BattleService._playerTokens = playerTokens;

                            Log.LogInfo($"{SANGUIS} registered for familiar battle rewards!");
                        }
                        else
                        {
                            Log.LogWarning($"Couldn't get {SANGUIS_SAVE_METHOD} | {SANGUIS_CONFIG_PROPERTY} from {SANGUIS}!");
                        }
                    }
                    else
                    {
                        Log.LogWarning($"Couldn't get {SANGUIS_DATA_PROPERTY} | {SANGUIS_CONFIG_PROPERTY} from {SANGUIS}!");
                    }
                }
                else
                {
                    Log.LogWarning($"Couldn't get {SANGUIS_DATA_CLASS} | {SANGUIS_CONFIG_CLASS} from {SANGUIS}!");
                }
            }
            else
            {
                Log.LogInfo($"{SANGUIS} not registered for familiar battle rewards!");
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Error during {SANGUIS} registration: {ex.Message}");
        }
    }
    public static void LogEntity(World world, Entity entity)
    {
        Il2CppSystem.Text.StringBuilder sb = new();

        try
        {
            EntityDebuggingUtility.DumpEntity(world, entity, true, sb);
            Log.LogInfo($"Entity Dump:\n{sb.ToString()}");
        }
        catch (Exception e)
        {
            Log.LogWarning($"Error dumping entity: {e.Message}");
        }
    }

    /*
    static readonly string _performanceAuditPath = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "PerformanceAudits");
    static int _queries;
    static string _logFilePath;
    static TimeSpan _lastCpuTime = TimeSpan.Zero;

    // Generate a unique log file name per session
    static void ServerPerformanceLogger()
    {
        if (!Directory.Exists(_performanceAuditPath))
        {
            Directory.CreateDirectory(_performanceAuditPath);
        }

        string timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");

        _queries = EntityManager.CalculateAliveEntityQueryCount();
        _logFilePath = Path.Combine(_performanceAuditPath, $"PerformanceAudit_{timestamp}.txt");
    }
    public static void LogPerformanceStats()
    {
        Process currentProcess = Process.GetCurrentProcess();

        float memoryUsedGB = currentProcess.WorkingSet64 / (1024f * 1024f * 1024f);
        float totalMemoryGB = GetTotalMemoryGB();

        TimeSpan cpuTime = currentProcess.TotalProcessorTime;
        float cpuUsage = 0;

        if (_lastCpuTime != TimeSpan.Zero)
        {
            TimeSpan cpuDelta = cpuTime - _lastCpuTime;
            cpuUsage = (float)(cpuDelta.TotalMilliseconds / 1000.0); // Approximate percentage
        }

        _lastCpuTime = cpuTime;

        int queryCount = GetEntityQueryCount();
        string logEntry = $"[{DateTime.UtcNow:HH:mm:ss}] RAM: {memoryUsedGB:F2} GB / {totalMemoryGB:F2} GB | CPU: {cpuUsage:F2}% | EntityQueries: {queryCount} (+{(queryCount > _queries ? queryCount - _queries: queryCount)})";

        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
    }
    static float GetTotalMemoryGB()
    {
        return SystemInfo.systemMemorySize / 1024f;
    }
    static int GetEntityQueryCount()
    {
        return EntityManager.CalculateAliveEntityQueryCount();
    }
    
    static readonly ComponentType[] _prefabGUIDComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
    ];

    static readonly ComponentType[] _dealDamageOnGameplayEventComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<DealDamageOnGameplayEvent>()),
    ];
    static void MiscLogging()
    {
        if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(new(1649578802), out Entity prefabEntity))
        {
            LogEntity(Server, prefabEntity);
        }
    }
    static void LogEntities()
    {
        ComponentType[] _cleanupComponent =
        [
            ComponentType.ReadOnly(Il2CppType.Of<CleanupEntity>()),
        ];

        EntityQuery entitiesQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            // None = Array.Empty<ComponentType>(),  // No excluded components
            // All = Array.Empty<ComponentType>(),  // No required components
            Any = _cleanupComponent,  // No optional components
            Options = EntityQueryOptions.IncludeAll
        });

        NativeArray<Entity> entities = entitiesQuery.ToEntityArray(Allocator.TempJob);
        Log.LogInfo($"CleanupEntities - ({entities.Length})");

        int minimum = 400;
        Entity source = Entity.Null;

        if (entities.Length >= minimum)
        {
            entities.Dispose();
            entitiesQuery.Dispose();
            return;
        }

        try
        {
            int needed = minimum - entities.Length;
            Entity sweeper;

            for (int i = 0; i < needed; i++)
            {
                sweeper = EntityManager.CreateEntity();

                sweeper.Add<CleanupEntity>();
                sweeper.Add<Simulate>();
            }

            Log.LogInfo($"Restored {needed} cleanup entities!");
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }
        finally
        {
            entities.Dispose();
            entitiesQuery.Dispose();
        }
    }
    static void LogWorlds()
    {
        try
        {
            foreach (World world in World._All_k__BackingField)
            {
                if (world != null)
                {
                    Log.LogInfo($"World: {world.Name}");
                    var systems = world.m_Systems;

                    foreach (var system in systems)
                    {
                        Log.LogInfo($"System: {system.GetIl2CppType().Name}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }

        try
        {
            foreach (World world in World.s_AllWorlds)
            {
                if (world != null)
                {
                    Log.LogInfo($"World: {world.Name}");
                    var systems = world.m_Systems;

                    foreach (var system in systems)
                    {
                        Log.LogInfo($"System: {system.GetIl2CppType().Name}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }

        try
        {
            World world = World._DefaultGameObjectInjectionWorld_k__BackingField;

            if (world != null)
            {
                Log.LogInfo($"World: {world.Name}");
                var systems = world.m_Systems;

                foreach (var system in systems)
                {
                    Log.LogInfo($"System: {system.GetIl2CppType().Name}");
                }
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }
    }
    static void LogActiveServerSystems()
    {
        Log.LogInfo("ACTIVE SERVER SYSTEMS");

        foreach (var kvp in Server.m_SystemLookup)
        {
            Il2CppSystem.Type systemType = kvp.Key;
            ComponentSystemBase systemBase = kvp.Value;

            if (systemBase.EntityQueries.Length == 0)
            {
                Log.LogInfo("=============================");
                Log.LogInfo($"{systemType.FullName}: No queries!");

                continue;
            }

            Log.LogInfo("=============================");
            Log.LogInfo(systemType.FullName);

            foreach (EntityQuery query in systemBase.EntityQueries)
            {
                EntityQueryDesc entityQueryDesc = query.GetEntityQueryDesc();
                Log.LogInfo($" All: {string.Join(",", entityQueryDesc.All)}");
                Log.LogInfo($" Any: {string.Join(",", entityQueryDesc.Any)}");
                Log.LogInfo($" Absent: {string.Join(",", entityQueryDesc.Absent)}");
                Log.LogInfo($" None: {string.Join(",", entityQueryDesc.None)}");
            }
        }

        Log.LogInfo("END ACTIVE SERVER SYSTEMS");
    }
    static void LogAllServerSystems()
    {
        Log.LogInfo("ALL SERVER SYSTEMS");

        foreach (ComponentSystemBase systemBase in Server.m_Systems)
        {
            Il2CppSystem.Type systemType = systemBase.GetIl2CppType();

            if (systemBase.EntityQueries.Length == 0)
            {
                Log.LogInfo("=============================");
                Log.LogInfo($"{systemType.FullName}: No queries!");

                continue;
            }

            Log.LogInfo("=============================");
            Log.LogInfo(systemType.FullName);

            foreach (EntityQuery query in systemBase.EntityQueries)
            {
                EntityQueryDesc entityQueryDesc = query.GetEntityQueryDesc();
                Log.LogInfo($" All: {string.Join(",", entityQueryDesc.All)}");
                Log.LogInfo($" Any: {string.Join(",", entityQueryDesc.Any)}");
                Log.LogInfo($" Absent: {string.Join(",", entityQueryDesc.Absent)}");
                Log.LogInfo($" None: {string.Join(",", entityQueryDesc.None)}");
            }
        }

        Log.LogInfo("END ALL SERVER SYSTEMS");
    }
    static void LogDealDamageOnGameplayEvents()
    {
        EntityQuery dealDamageOnGameplayEventQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _dealDamageOnGameplayEventComponent,
            Options = EntityQueryOptions.IncludeAll
        });

        NativeArray<Entity> entities = dealDamageOnGameplayEventQuery.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.TryGetBuffer<DealDamageOnGameplayEvent>(out var buffer))
                {
                    Log.LogInfo(entity.GetPrefabGuid().GetPrefabName());

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        DealDamageOnGameplayEvent dealDamageOnGameplayEvent = buffer[i];
                        Log.LogInfo($"  DealDamageOnGameplayEvent - MainType: {dealDamageOnGameplayEvent.Parameters.MainType} | MainFactor: {dealDamageOnGameplayEvent.Parameters.MainFactor} | RawDamagePercent: {dealDamageOnGameplayEvent.Parameters.RawDamagePercent} | RawDamageValue: {dealDamageOnGameplayEvent.Parameters.RawDamageValue}");
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            dealDamageOnGameplayEventQuery.Dispose();
        }
    }
    */
}
