using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using ProjectM.Physics;
using ProjectM.Scripting;
using System.Collections;
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

    public static bool hasInitialized = false;

    static MonoBehaviour MonoBehaviour;
    public static void Initialize()
    {
        if (hasInitialized) return;

        hasInitialized = true;

        _ = new PlayerService();
        _ = new LocalizationService();

        if (ConfigService.ExtraRecipes) RecipeUtilities.ExtraRecipes();
        if (ConfigService.StarterKit) ConfigUtilities.StarterKit();
        if (ConfigService.PrestigeSystem) BuffUtilities.PrestigeBuffs();
        if (ConfigService.SoftSynergies || ConfigService.HardSynergies) ConfigUtilities.CreateClassSpellCooldowns();
        if (ConfigService.ClientCompanion) _ = new EclipseService();

        if (ConfigService.LevelingSystem) DeathEventListenerSystemPatch.OnDeathEvent += LevelingSystem.OnUpdate;
        if (ConfigService.BloodSystem) DeathEventListenerSystemPatch.OnDeathEvent += BloodSystem.OnUpdate;
        if (ConfigService.ExpertiseSystem) DeathEventListenerSystemPatch.OnDeathEvent += WeaponSystem.OnUpdate;
        if (ConfigService.QuestSystem)
        {
            _ = new QuestService();
            DeathEventListenerSystemPatch.OnDeathEvent += QuestSystem.OnUpdate;
        }
        if (ConfigService.FamiliarSystem)
        {
            ConfigUtilities.FamiliarBans();
            DeathEventListenerSystemPatch.OnDeathEvent += FamiliarLevelingSystem.OnUpdate;
            DeathEventListenerSystemPatch.OnDeathEvent += FamiliarUnlockSystem.OnUpdate;
        }

        hasInitialized = true;
    }
    static World GetServerWorld()
    {
        return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
    }
    public static void StartCoroutine(IEnumerator routine)
    {
        if (MonoBehaviour == null)
        {
            MonoBehaviour = new GameObject("Bloodcraft").AddComponent<IgnorePhysicsDebugSystem>();
            UnityEngine.Object.DontDestroyOnLoad(MonoBehaviour.gameObject);
        }
        MonoBehaviour.StartCoroutine(routine.WrapToIl2Cpp());
    }

    /*
    static readonly ComponentType[] SCTComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<ScrollingCombatTextMessage>()),
    ];

    static readonly PrefabGUID SCTPrefab = new(-1661525964);
    static void SCTFixTest() // one off thing hopefully but leaving since those have a habit of coming back around
    {
        // -1661525964 SCT prefab
        EntityQuery sctQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = SCTComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        int counter = 0;
        try
        {
            Dictionary<string, Dictionary<string, int>> userTargetCounts = [];
            IEnumerable<Entity> prefabEntities = GetEntitiesEnumerable(sctQuery); // destroy errant SCT entities and log what's going on
            int entityCount = prefabEntities.Count();

            Core.Log.LogInfo(entityCount); // okay yeah definitely something with either familiar summons or just undead summons?
            if (entityCount > 5000)
            {
                foreach (Entity entity in prefabEntities)
                {
                    if (!entity.Has<PrefabGUID>()) continue;
                    PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                    if (prefabGUID.Equals(SCTPrefab) && !entity.Has<SpawnTag>()) // don't destroy the spawner prefab probably >_>
                    {
                        if (ServerGameManager.TryGetBuffer<SyncToUserBuffer>(entity, out var syncToUserBuffer))
                        {
                            if (!syncToUserBuffer.IsEmpty)
                            {
                                string userName = syncToUserBuffer[0].UserEntity.Read<User>().CharacterName.Value;

                                if (entity.TryGetComponent(out ScrollingCombatTextMessage scrollingCombatText))
                                {
                                    Entity target = scrollingCombatText.Target.GetSyncedEntityOrNull();
                                    if (target.Exists() && !target.IsPlayer())
                                    {
                                        string targetName = target.Read<PrefabGUID>().LookupName();

                                        if (!userTargetCounts.ContainsKey(userName))
                                        {
                                            userTargetCounts[userName] = [];
                                        }

                                        Dictionary<string, int> targetCounts = userTargetCounts[userName];

                                        if (!targetCounts.ContainsKey(targetName))
                                        {
                                            targetCounts[targetName] = 0;
                                        }

                                        targetCounts[targetName]++;
                                        DestroyUtility.Destroy(EntityManager, target);
                                    }
                                }
                            }
                        }
                        DestroyUtility.Destroy(EntityManager, entity);
                        counter++;
                    }
                }
                Log.LogWarning($"Cleared {counter} SCT entities");
            }

            foreach (var userEntry in userTargetCounts)
            {
                string userName = userEntry.Key;
                var targetCounts = userEntry.Value;

                Log.LogInfo($"User: {userName}");
                foreach (var targetEntry in targetCounts)
                {
                    string targetName = targetEntry.Key;
                    int count = targetEntry.Value;
                    Log.LogInfo($"\tTarget: {targetName}, Count: {count}");
                }
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }
        finally
        {
            sctQuery.Dispose();
        }
    }

    static readonly ComponentType[] PrefabGUIDComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
    ];

    static readonly PrefabGUID WitheredPrefab = new(-1584807109);
    static void UnitFixTest() // one off thing hopefully but leaving since those have a habit of coming back around
    {
        // -1661525964 SCT prefab
        EntityQuery prefabQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = PrefabGUIDComponent,
            Options = EntityQueryOptions.IncludeAll
        });

        try
        {
            Dictionary<string, Dictionary<string, int>> userTargetCounts = [];
            IEnumerable<Entity> prefabEntities = GetEntitiesEnumerable(prefabQuery);
            int entityCount = prefabEntities.Count();

            int counter = 0;
            //Core.Log.LogInfo(entityCount); 
            foreach (Entity entity in prefabEntities)
            {
                if (entity.TryGetComponent(out PrefabGUID prefab) && prefab.Equals(WitheredPrefab) && entity.Has<BlockFeedBuff>() && entity.TryGetComponent(out EntityOwner owner) && owner.Owner.Exists())
                {
                    StatChangeUtility.KillOrDestroyEntity(EntityManager, entity, owner.Owner, owner.Owner, ServerTime, StatChangeReason.Default, true);
                    counter++;
                }
            }
            Log.LogWarning($"Destroyed {counter} withered units");
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }
        finally
        {
            prefabQuery.Dispose();
        }
    }
    */
}