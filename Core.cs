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

    static readonly PrefabGUID ReturnBuff = new(-560330878);
    static readonly PrefabGUID ReturnNoInvulnerableBuff = new(2086395440);
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

        ModifyReturnBuffPrefabs();
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
    static void ModifyReturnBuffPrefabs()
    {
        if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(ReturnBuff, out Entity buffEntity))
        {
            if (buffEntity.TryGetBuffer<HealOnGameplayEvent>(out var buffer))
            {
                HealOnGameplayEvent healOnGameplayEvent = buffer[0];
                healOnGameplayEvent.showSCT = false;

                buffer[0] = healOnGameplayEvent;
            }
        }

        if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(ReturnNoInvulnerableBuff, out buffEntity))
        {
            if (buffEntity.TryGetBuffer<HealOnGameplayEvent>(out var buffer))
            {
                HealOnGameplayEvent healOnGameplayEvent = buffer[0];
                healOnGameplayEvent.showSCT = false;

                buffer[0] = healOnGameplayEvent;
            }
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

        //int counter = 0;

        Dictionary<string, Dictionary<string, Dictionary<string, int>>> playerOwnedTargetCounts = new();

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
                                playerOwnedTargetCounts[user.CharacterName.Value] = new Dictionary<string, Dictionary<string, int>>();
                            }

                            if (!playerOwnedTargetCounts[user.CharacterName.Value].ContainsKey(characterPrefabName))
                            {
                                playerOwnedTargetCounts[user.CharacterName.Value][characterPrefabName] = new Dictionary<string, int>();
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
