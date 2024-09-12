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
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Network;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Utilities.EntityUtilities;

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

        //SCTFixTest(); // only useful for removing SCT prefabs if for some reason they are being generated in mass

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

        try
        {
            List<Entity> processed = [];
            IEnumerable<Entity> prefabEntities = GetEntitiesEnumerable(prefabQuery); // find SCT prefab entities and destroy them
            foreach (Entity entity in prefabEntities)
            {
                if (!entity.Has<PrefabGUID>()) continue;

                PrefabGUID prefabGUID = entity.Read<PrefabGUID>();
                if (prefabGUID.Equals(SCTPrefab) && !entity.Has<SpawnTag>()) // don't destroy the spawner... or do >_>? check syncToUserBuffer first and see who these are for
                {
                    if (entity.Has<SyncToUserBuffer>())
                    {
                        var syncToUserBuffer = entity.ReadBuffer<SyncToUserBuffer>();
                        foreach (SyncToUserBuffer syncToUser in syncToUserBuffer)
                        {
                            if (syncToUser.UserEntity.Exists() && syncToUser.UserEntity.Has<User>() && !processed.Contains(syncToUser.UserEntity))
                            {
                                User user = syncToUser.UserEntity.Read<User>();
                                Entity localCharacter = user.LocalCharacter.GetEntityOnServer();

                                if (localCharacter.Has<FollowerBuffer>())
                                {
                                    var buffer = localCharacter.ReadBuffer<FollowerBuffer>();
                                    if (!buffer.IsEmpty)
                                    {
                                        foreach (FollowerBuffer follower in buffer)
                                        {
                                            Entity familiar = follower.Entity.GetEntityOnServer();
                                            if (familiar.Has<Disabled>()) familiar.Remove<Disabled>();
                                            EntityManager.DestroyEntity(familiar);
                                        }

                                        buffer.Clear();
                                        processed.Add(syncToUser.UserEntity);
                                    }
                                }
                            }
                        }
                    }
                    EntityManager.DestroyEntity(entity);
                    counter++;
                }
            }
        }
        catch (Exception e)
        {
            Log.LogError(e);
        }
        finally
        {
            Log.LogInfo($"SCTFixTest: {counter} SCT prefab entities destroyed!");
            prefabQuery.Dispose();
        }
    }
}