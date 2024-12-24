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
    static World Server { get; } = GetServerWorld() ?? throw new Exception("There is no Server world (yet)...");
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

    static readonly PrefabGUID _fallenAngel = new(-76116724);
    static readonly PrefabGUID _defaultEmoteBuff = new(-988102043);

    const float DIRECTION_DURATION = 6f; // for making familiars for player two face correct direction until battle starts

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

        _ = new PlayerService();
        _ = new LocalizationService();

        if (ConfigService.ClientCompanion) _ = new EclipseService();

        if (ConfigService.ExtraRecipes) Recipes.AddExtraRecipes();
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
            Misc.InitializeChanceModifiers();
            _ = new BattleService();
        }

        ModifyBuffPrefabs();

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
    static void ModifyBuffPrefabs()
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
        }

        if (ConfigService.SoftSynergies || ConfigService.HardSynergies)
        {
            /*
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_spawnMutantBiteBuff, out Entity spawnMutantBiteBuffPrefab))
            {
                spawnMutantBiteBuffPrefab.With((ref LifeTime lifeTime) =>
                {
                    lifeTime.Duration = 10f;
                });
            }
            */

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(_fallenAngel, out Entity fallenAngelPrefab))
            {
                if (!fallenAngelPrefab.Has<BlockFeedBuff>()) fallenAngelPrefab.Add<BlockFeedBuff>();
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

    /*
    static readonly PrefabGUID _sctPrefab = new(-1661525964);

    static readonly ComponentType[] _prefabGUIDComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
    ];

    static readonly ComponentType[] _dealDamageOnGameplayEventComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<DealDamageOnGameplayEvent>()),
    ];
    static void LogEntities()
    {
        EntityQuery entitiesQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            None = Array.Empty<ComponentType>(),  // No excluded components
            All = Array.Empty<ComponentType>(),  // No required components
            Any = Array.Empty<ComponentType>(),  // No optional components
            Options = EntityQueryOptions.IncludeAll
        });

        NativeArray<Entity> entities = entitiesQuery.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
                if (entity.Exists())
                {
                    //PrefabClonerFactory.EntityComponentLogger.LogEntityDetails(Server, entity);
                }
            }
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
    static void LogSCTEntities()
    {
        EntityQuery prefabGUIDQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = _prefabGUIDComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        // Dictionary to track target counts per player, keyed by character prefab name
        Dictionary<string, Dictionary<string, Dictionary<string, int>>> playerOwnedTargetCounts = [];

        NativeArray<Entity> entities = prefabGUIDQuery.ToEntityArray(Allocator.TempJob);
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<SpawnTag>() && entity.TryGetComponent(out PrefabGUID prefabGUID))
                {
                    if (prefabGUID.Equals(_sctPrefab) && entity.TryGetComponent(out ScrollingCombatTextMessage scrollingCombatTextMessage))
                    {
                        Entity target = scrollingCombatTextMessage.Target.GetEntityOnServer();
                        PrefabGUID prefabSCT = scrollingCombatTextMessage.Type; // SCT prefab type

                        if (target.TryGetComponent(out EntityOwner entityOwner)
                            && entityOwner.Owner.IsPlayer()
                            && target.TryGetComponent(out PrefabGUID targetPrefabGUID))
                        {
                            User user = entityOwner.Owner.GetUser();
                            string characterPrefabName = targetPrefabGUID.GetPrefabName(); // Character prefab name
                            string sctPrefabName = prefabSCT.GetPrefabName(); // SCT prefab name

                            // Initialize data structures if not present
                            if (!playerOwnedTargetCounts.ContainsKey(user.CharacterName.Value))
                            {
                                playerOwnedTargetCounts[user.CharacterName.Value] = [];
                            }

                            if (!playerOwnedTargetCounts[user.CharacterName.Value].ContainsKey(characterPrefabName))
                            {
                                playerOwnedTargetCounts[user.CharacterName.Value][characterPrefabName] = [];
                            }

                            if (!playerOwnedTargetCounts[user.CharacterName.Value][characterPrefabName].ContainsKey(sctPrefabName))
                            {
                                playerOwnedTargetCounts[user.CharacterName.Value][characterPrefabName][sctPrefabName] = 1;
                            }
                            else
                            {
                                playerOwnedTargetCounts[user.CharacterName.Value][characterPrefabName][sctPrefabName]++;
                            }
                        }

                        DestroyUtility.Destroy(EntityManager, entity);
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            prefabGUIDQuery.Dispose();

            // Log the results
            foreach (var playerEntry in playerOwnedTargetCounts)
            {
                Log.LogWarning($"Player: {playerEntry.Key}");
                Log.LogWarning(new string('-', 20));

                foreach (var characterPrefabEntry in playerEntry.Value)
                {
                    Log.LogInfo($"  CharacterPrefabGUID: {characterPrefabEntry.Key}");
                    foreach (var sctEntry in characterPrefabEntry.Value)
                    {
                        Log.LogInfo($"    SCTPrefabName: {sctEntry.Key} | Count: {sctEntry.Value}");
                    }
                }

                Log.LogInfo(""); // Blank line for readability
            }
        }
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
                    Log.LogInfo(entity.GetPrefabGUID().GetPrefabName());

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
