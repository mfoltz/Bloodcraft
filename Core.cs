using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using ProjectM.Sequencer;
using ProjectM.Shared;
using Stunlock.Core;
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

    static MonoBehaviour MonoBehaviour;

    public static bool hasInitialized = false;
    public static void Initialize()
    {
        if (hasInitialized) return;

        hasInitialized = true;

        NetworkedSequences();

        _ = new PlayerService();
        _ = new LocalizationService();

        if (ConfigService.ClientCompanion) _ = new EclipseService();

        if (ConfigService.ExtraRecipes) RecipeUtilities.ExtraRecipes();
        if (ConfigService.StarterKit) ConfigUtilities.StarterKit();
        if (ConfigService.PrestigeSystem) BuffUtilities.PrestigeBuffs();
        if (ConfigService.SoftSynergies || ConfigService.HardSynergies)
        {
            ConfigUtilities.CreateClassSpellCooldowns();
            ClassUtilities.GenerateAbilityJewelMap();
        }

        if (ConfigService.LevelingSystem) DeathEventListenerSystemPatch.OnDeathEvent += LevelingSystem.OnUpdate;
        if (ConfigService.ExpertiseSystem) DeathEventListenerSystemPatch.OnDeathEvent += WeaponSystem.OnUpdate;
        if (ConfigService.QuestSystem)
        {
            _ = new QuestService();
            DeathEventListenerSystemPatch.OnDeathEvent += QuestSystem.OnUpdate;
        }
        if (ConfigService.FamiliarSystem)
        {
            //CleanUpFams();
            ConfigUtilities.FamiliarBans();
            DeathEventListenerSystemPatch.OnDeathEvent += FamiliarLevelingSystem.OnUpdate;
            DeathEventListenerSystemPatch.OnDeathEvent += FamiliarUnlockSystem.OnUpdate;
        }

        /*
        foreach (var kvp in Server.m_SystemLookup)
        {
            Il2CppSystem.Type systemType = kvp.Key;
            ComponentSystemBase systemBase = kvp.Value;
            if (systemBase.EntityQueries.Length == 0) continue;

            Core.Log.LogInfo("=============================");
            Core.Log.LogInfo(systemType.FullName);
            foreach (EntityQuery query in systemBase.EntityQueries)
            {
                EntityQueryDesc entityQueryDesc = query.GetEntityQueryDesc();
                Core.Log.LogInfo($" All: {string.Join(",", entityQueryDesc.All)}");
                Core.Log.LogInfo($" Any: {string.Join(",", entityQueryDesc.Any)}");
                Core.Log.LogInfo($" Absent: {string.Join(",", entityQueryDesc.Absent)}");
                Core.Log.LogInfo($" None: {string.Join(",", entityQueryDesc.None)}");
            }
            Core.Log.LogInfo("=============================");
        }
        */    

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

    static readonly ComponentType[] NetworkedSequenceComponent =
    {
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>())
    };

    static EntityQuery NetworkedSequenceQuery;

    static readonly PrefabGUID unholySkeletonWarrior = new(1604500740);
    static void NetworkedSequences()
    {
        // BlockFeedBuff, Disabled, TeamReference with UserTeam on entity, and see if name of prefab starts with CHAR?
        NetworkedSequenceQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = NetworkedSequenceComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        int primaryCounter = 0;
        int secondaryCounter = 0;
        try
        {
            IEnumerable<Entity> networkedSequences = EntityUtilities.GetEntitiesEnumerable(NetworkedSequenceQuery); // need to filter for active/dismissed familiars and not destroy them
            foreach (Entity entity in networkedSequences)
            {
                if (entity.TryGetComponent(out SpawnSequenceForEntity spawnSequenceForEntity))
                {
                    Entity secondaryTarget = spawnSequenceForEntity.SecondaryTarget.GetEntityOnServer().Exists() ? spawnSequenceForEntity.SecondaryTarget.GetEntityOnServer() : Entity.Null;

                    if (secondaryTarget.TryGetComponent(out PrefabGUID secondaryTargetPrefabGUID) && secondaryTargetPrefabGUID.Equals(unholySkeletonWarrior))
                    {
                        DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.None);
                        primaryCounter++;
                    }
                }
                else if (entity.TryGetComponent(out PrefabGUID targetPrefabGUID) && targetPrefabGUID.Equals(unholySkeletonWarrior))
                {
                    DestroyUtility.Destroy(EntityManager, entity, DestroyDebugReason.None);
                    secondaryCounter++;
                }
            }
        }
        finally
        {
            NetworkedSequenceQuery.Dispose();
            if (primaryCounter > 0 || secondaryCounter > 0) Core.Log.LogWarning($"Destroyed {primaryCounter} networked sequences with secondary targets of {unholySkeletonWarrior.LookupName()} and {secondaryCounter} entities with PrefabGUID {unholySkeletonWarrior.LookupName()}");
        }
    }
}
