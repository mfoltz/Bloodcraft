using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Familiars;
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

    static MonoBehaviour MonoBehaviour;

    public static bool hasInitialized = false;
    public static void Initialize()
    {
        if (hasInitialized) return;

        _ = new PlayerService();
        _ = new LocalizationService();

        if (ConfigService.ClientCompanion) _ = new EclipseService();
        
        if (ConfigService.ExtraRecipes) RecipeUtilities.AddExtraRecipes();
        if (ConfigService.StarterKit) ConfigUtilities.StarterKitItems();
        if (ConfigService.PrestigeSystem) BuffUtilities.PrestigeBuffs();
        if (ConfigService.SoftSynergies || ConfigService.HardSynergies)
        {
            ConfigUtilities.ClassPassiveBuffsMap();
            ConfigUtilities.ClassSpellCooldownMap();
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
            ConfigUtilities.FamiliarBans();
            //DeathEventListenerSystemPatch.OnDeathEvent += FamiliarLevelingSystem.OnUpdate;
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

        //CleanSCTPRefabs();

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
    static void OverrideVBloodMenuLevels()
    {
        Il2CppSystem.Collections.Generic.List<VBloodUnitSetting> vBloodUnitSettings = SystemService.ServerGameSettingsSystem._Settings.VBloodUnitSettings;
        List<int> shardBearers = SpawnTransformSystemOnSpawnPatch.shardBearers.Select(x => x._Value).ToList();

        foreach (VBloodUnitSetting vBloodUnitSetting in vBloodUnitSettings)
        {
            if (shardBearers.Contains(vBloodUnitSetting.UnitId))
            {
                vBloodUnitSetting.UnitLevel = (byte)ConfigService.ShardBearerLevel;
            }
        }

        SystemService.ServerGameSettingsSystem._Settings.VBloodUnitSettings = vBloodUnitSettings;

        SystemService.ServerBootstrapSystem._SteamPlatformSystem.SetServerSettings(SystemService.ServerGameSettingsSystem._Settings.ToStruct(true));
        SystemService.ServerBootstrapSystem._SteamPlatformSystem.SetServerGameSettingsHash(SystemService.ServerGameSettingsSystem._Settings);

        SystemService.ServerBootstrapSystem.ReloadSettings();
    }
    */

    /*
    static readonly PrefabGUID SCTPrefab = new(-1661525964);

    static readonly ComponentType[] PrefabGUIDComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
    ];

    static readonly PrefabGUID UndeadLeader = new(-1365931036);
    static void CleanSCTPRefabs()
    {
        EntityQuery prefabGUIDQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = PrefabGUIDComponent,
            Options = EntityQueryOptions.IncludeDisabled
        });

        NativeArray<Entity> entities = prefabGUIDQuery.ToEntityArray(Allocator.TempJob);

        Entity Kriig = Entity.Null;
        int sctCounter = 0;
        int targetCounter = 0;
        try
        {
            foreach (Entity entity in entities)
            {
                if (!entity.Has<SpawnTag>() && entity.TryGetComponent(out PrefabGUID prefabGUID))
                {
                    if (prefabGUID.Equals(SCTPrefab))
                    {
                        ScrollingCombatTextMessage scrollingCombatTextMessage = entity.Read<ScrollingCombatTextMessage>();
                        Entity targetEntity = scrollingCombatTextMessage.Target.GetEntityOnServer();

                        if (targetEntity.Exists() && targetEntity.TryGetComponent(out PrefabGUID targetPrefabGUID) && targetEntity.TryGetComponent(out EntityOwner owner))
                        {
                            if (owner.Owner.Exists() && owner.Owner.IsPlayer())
                            {
                                //ownedPrefabGUIDs.Add(targetPrefabGUID);
                                //Log.LogInfo($"{targetPrefabGUID.LookupName()} | {owner.Owner.Read<PlayerCharacter>().Name.Value}");
                            }
                            else if (owner.Owner.Exists() && owner.Owner.TryGetComponent(out PrefabGUID ownerPrefabGUID))
                            {
                                //Log.LogInfo($"{targetPrefabGUID.LookupName()} | {ownerPrefabGUID.LookupName()}");
                            }

                            if (targetEntity.Has<VampireTag>()) continue;

                            DestroyUtility.Destroy(EntityManager, targetEntity);
                            targetCounter++;
                        }

                        if (entity.Has<VampireTag>()) continue;

                        DestroyUtility.Destroy(EntityManager, entity);
                        sctCounter++;
                    }
                    else if (prefabGUID.Equals(UndeadLeader))
                    {
                        entity.LogComponentTypes();
                        if (entity.Has<Disabled>())
                        {
                            entity.Remove<Disabled>();
                            if (entity.Has<DisableWhenNoPlayersInRange>()) entity.Remove<DisableWhenNoPlayersInRange>();
                            if (entity.Has<DisabledDueToNoPlayersInRange>()) entity.Remove<DisabledDueToNoPlayersInRange>();

                            Kriig = entity;
                            DestroyUtility.Destroy(EntityManager, entity);
                            //Log.LogInfo("Destroyed Undead Leader...");
                        }
                    }
                }
            }
        }
        finally
        {
            entities.Dispose();
            prefabGUIDQuery.Dispose();
            Log.LogWarning($"Destroyed {sctCounter} | {targetCounter} SCT prefab entities and targets...");

            if (Kriig.Exists())
            {
                EntityManager.DestroyEntity(Kriig);
                Log.LogInfo("Still exists, using alternate destroy method...");
            }
        }
    }
    */
}
