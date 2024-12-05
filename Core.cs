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
using ProjectM.Shared;
using ProjectM.Terrain;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
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

    static readonly List<PrefabGUID> ReturnBuffs =
    [
        new PrefabGUID(-560330878),  // ReturnBuff
        new PrefabGUID(2086395440),  // ReturnNoInvulnerableBuff
        new PrefabGUID(-1049988817), // ValenciaReturnBuff
        //new PrefabGUID(-1377587236), // DraculaReturnBuff
        //new PrefabGUID(-1448806401), // OtherDraculaReturnBuff
        new PrefabGUID(-1511222240), // WerewolfChieftainReturnBuff
        new PrefabGUID(-1773136595), // DominaReturnBuff
        new PrefabGUID(-1983671299), // AngramReturnBuff
        new PrefabGUID(1089939900),  // AdamReturnBuff
        new PrefabGUID(-1435372081)  // SolarusReturnBuff
    ];

    static readonly PrefabGUID SpawnMutantBiteBuff = new(-651661301);
    static readonly PrefabGUID FallenAngel = new(-76116724);
    public static byte[] OLD_SHARED_KEY { get; internal set; }
    public static byte[] NEW_SHARED_KEY { get; internal set; }

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

        //JobsUtility.JobScheduleParameters

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
        
        OLD_SHARED_KEY = Convert.FromBase64String(SecretManager.GetOldSharedKey());
        NEW_SHARED_KEY = Convert.FromBase64String(SecretManager.GetNewSharedKey());

        ModifyBuffPrefabs();
        //LogSCTPRefabs();

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
    static void ModifyBuffPrefabs()
    {
        foreach (PrefabGUID prefabGUID in ReturnBuffs)
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

        if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(SpawnMutantBiteBuff, out Entity spawnMutantBiteBuffPrefab))
        {
            spawnMutantBiteBuffPrefab.With((ref LifeTime lifeTime) =>
            {
                lifeTime.Duration = 10f;
            });
        }

        if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(FallenAngel, out Entity fallenAngelPrefab))
        {
            if (!fallenAngelPrefab.Has<BlockFeedBuff>()) fallenAngelPrefab.Add<BlockFeedBuff>();
        }
    }

    static readonly PrefabGUID SCTPrefab = new(-1661525964);

    static readonly ComponentType[] PrefabGUIDComponent =
    [
        ComponentType.ReadOnly(Il2CppType.Of<PrefabGUID>()),
    ];
    static void LogSCTPRefabs()
    {
        EntityQuery prefabGUIDQuery = EntityManager.CreateEntityQuery(new EntityQueryDesc
        {
            All = PrefabGUIDComponent,
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
                    if (prefabGUID.Equals(SCTPrefab) && entity.TryGetComponent(out ScrollingCombatTextMessage scrollingCombatTextMessage))
                    {
                        Entity target = scrollingCombatTextMessage.Target.GetEntityOnServer();
                        PrefabGUID prefabSCT = scrollingCombatTextMessage.Type; // SCT prefab type

                        if (target.TryGetComponent(out EntityOwner entityOwner)
                            && entityOwner.Owner.IsPlayer()
                            && target.TryGetComponent(out PrefabGUID targetPrefabGUID))
                        {
                            User user = entityOwner.Owner.GetUser();
                            string characterPrefabName = targetPrefabGUID.LookupName(); // Character prefab name
                            string sctPrefabName = prefabSCT.LookupName(); // SCT prefab name

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

                Log.LogInfo(""); // Blank line for better readability
            }
        }
    }
}
