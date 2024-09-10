using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Bloodcraft.Services;
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

        //SCTFixTest(); only useful for removing SCT prefabs if for some reason they are being generated in mass and causing the server to crash

        _ = new PlayerService();
        _ = new LocalizationService();

        if (ConfigService.FamiliarSystem) ConfigUtilities.FamiliarBans();
        if (ConfigService.ExtraRecipes) RecipeUtilities.ExtraRecipes();
        if (ConfigService.StarterKit) ConfigUtilities.StarterKit();
        if (ConfigService.PrestigeSystem) BuffUtilities.PrestigeBuffs();
        if (ConfigService.QuestSystem) _ = new QuestService();
        if (ConfigService.ClientCompanion) _ = new EclipseService();

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
    static readonly ComponentType[] PrefabGUIDComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
    ];

    static readonly PrefabGUID SCTPrefab = new(-1661525964);
    
    static void SCTFixTest() // one off thing hopefully but leaving since those have a habit of coming back around
    {
        // -1661525964 SCT prefab

        EntityQuery prefabQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = PrefabGUIDComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        int counter = 0;
        IEnumerable<Entity> prefabEntities = GetEntitiesEnumerable(prefabQuery); // find SCT prefab entities and destroy them

        foreach (Entity entity in prefabEntities)
        {
            PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
            if (prefabGUID.Equals(SCTPrefab) && !entity.Has<SpawnTag>()) // don't destroy the spawner... or do >_>? check syncToUserBuffer first and see who these are for
            {
                if (entity.Has<SyncToUserBuffer>())
                {
                    var syncToUserBuffer = entity.ReadBuffer<SyncToUserBuffer>();
                    foreach (SyncToUserBuffer syncToUser in syncToUserBuffer)
                    {
                        if (syncToUser.UserEntity.Exists() && syncToUser.UserEntity.Has<User>())
                        {
                            Log.LogInfo($"SCTFixTest: {syncToUser.UserEntity} | {syncToUser.UserEntity.Read<User>().CharacterName.Value}");
                        }
                        else
                        {
                            Log.LogInfo($"SCTFixTest: userEntity does not exist in syncToUserBuffer... {syncToUser.UserEntity}");
                        }
                    }
                }
                EntityManager.DestroyEntity(entity);
                counter++;
            }
        }
        
        Log.LogInfo($"SCTFixTest: {counter} SCT prefab entities destroyed!");
        prefabQuery.Dispose();
    }
    */
}