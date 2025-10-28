using System;
using System.Reflection;
using System.Runtime.Serialization;
using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using Unity.Entities;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a disposable scope that patches <see cref="Bloodcraft.Core"/> so tests can exercise
/// code paths that normally rely on Unity's DOTS runtime without spinning up the real ECS world.
/// </summary>
public sealed class UnityRuntimeScope : IDisposable
{
    static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.Support.UnityRuntimeScope");
    static readonly object SyncRoot = new();

    static World? cachedWorld;
    static EntityManager cachedEntityManager;
    static SystemService? cachedSystemService;
    static ServerGameManager? cachedServerGameManager;
    static ServerScriptMapper? cachedServerScriptMapper;
    static bool cachedEntityManagerInitialized;
    static bool patchesApplied;
    static int activeCount;

    bool disposed;

    /// <summary>
    /// Initializes the scope, installing Harmony patches and stub instances on first use.
    /// </summary>
    public UnityRuntimeScope()
    {
        lock (SyncRoot)
        {
            if (activeCount == 0)
            {
                EnsurePatches();
            }

            activeCount++;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (SyncRoot)
        {
            activeCount--;

            if (activeCount == 0)
            {
                HarmonyInstance.UnpatchSelf();
                patchesApplied = false;
            }
        }

        disposed = true;
    }

    static void EnsurePatches()
    {
        if (patchesApplied)
        {
            AssignCoreBackingFields();
            return;
        }

        PatchCoreTypeInitializer();
        PatchCoreGetter(nameof(Bloodcraft.Core.Server), nameof(CoreServerGetterPrefix));
        PatchCoreGetter(nameof(Bloodcraft.Core.SystemService), nameof(CoreSystemServiceGetterPrefix));
        PatchCoreGetter(nameof(Bloodcraft.Core.ServerGameManager), nameof(CoreServerGameManagerGetterPrefix));
        PatchCoreGetter(nameof(Bloodcraft.Core.EntityManager), nameof(CoreEntityManagerGetterPrefix));

        AssignCoreBackingFields();
        patchesApplied = true;
    }

    static void PatchCoreTypeInitializer()
    {
        MethodBase? typeInitializer = GetCoreTypeInitializer();
        if (typeInitializer == null)
        {
            return;
        }

        var prefix = new HarmonyMethod(typeof(UnityRuntimeScope).GetMethod(
            nameof(CoreTypeInitializerPrefix),
            BindingFlags.Static | BindingFlags.NonPublic));

        HarmonyInstance.Patch(typeInitializer, prefix: prefix);
    }

    static MethodBase? GetCoreTypeInitializer()
    {
        MethodInfo? accessToolsMethod = typeof(AccessTools).GetMethod(
            "TypeInitializer",
            BindingFlags.Public | BindingFlags.Static);

        if (accessToolsMethod != null)
        {
            if (accessToolsMethod.Invoke(null, new object[] { typeof(Bloodcraft.Core) }) is MethodBase methodBase)
            {
                return methodBase;
            }
        }

        return typeof(Bloodcraft.Core).TypeInitializer;
    }

    static void PatchCoreGetter(string propertyName, string prefixName)
    {
        var getter = AccessTools.PropertyGetter(typeof(Bloodcraft.Core), propertyName);
        if (getter == null)
        {
            return;
        }

        var prefix = new HarmonyMethod(typeof(UnityRuntimeScope).GetMethod(
            prefixName,
            BindingFlags.Static | BindingFlags.NonPublic));

        HarmonyInstance.Patch(getter, prefix: prefix);
    }

    static bool CoreTypeInitializerPrefix()
    {
        AssignCoreBackingFields();
        return false;
    }

    static void AssignCoreBackingFields()
    {
        SetCoreBackingField("Server", EnsureServerWorld());
        SetCoreBackingField("SystemService", EnsureSystemService());
        SetCoreBackingField("ServerGameManager", EnsureServerGameManager());
    }

    static bool CoreServerGetterPrefix(ref World __result)
    {
        __result = EnsureServerWorld();
        return false;
    }

    static bool CoreSystemServiceGetterPrefix(ref SystemService __result)
    {
        __result = EnsureSystemService();
        return false;
    }

    static bool CoreServerGameManagerGetterPrefix(ref ServerGameManager __result)
    {
        __result = EnsureServerGameManager();
        return false;
    }

    static bool CoreEntityManagerGetterPrefix(ref EntityManager __result)
    {
        __result = EnsureEntityManager();
        return false;
    }

    static void SetCoreBackingField(string propertyName, object value)
    {
        var field = typeof(Bloodcraft.Core).GetField($"<{propertyName}>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);

        field?.SetValue(null, value);
    }

    static World EnsureServerWorld()
    {
        lock (SyncRoot)
        {
            if (cachedWorld != null)
            {
                return cachedWorld;
            }

            var world = (World)FormatterServices.GetUninitializedObject(typeof(World));
            AssignWorldName(world, "Server");
            AssignWorldEntityManager(world, EnsureEntityManager());

            cachedWorld = world;
            return world;
        }
    }

    static void AssignWorldName(World world, string name)
    {
        var nameField = world.GetType().GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? world.GetType().GetField("m_Name", BindingFlags.Instance | BindingFlags.NonPublic);

        nameField?.SetValue(world, name);
    }

    static void AssignWorldEntityManager(World world, EntityManager entityManager)
    {
        var entityManagerField = world.GetType().GetField("m_EntityManager", BindingFlags.Instance | BindingFlags.NonPublic);
        if (entityManagerField != null)
        {
            entityManagerField.SetValue(world, entityManager);
        }
    }

    static EntityManager EnsureEntityManager()
    {
        lock (SyncRoot)
        {
            if (cachedEntityManagerInitialized)
            {
                return cachedEntityManager;
            }

            cachedEntityManager = (EntityManager)FormatterServices.GetUninitializedObject(typeof(EntityManager));
            cachedEntityManagerInitialized = true;
            return cachedEntityManager;
        }
    }

    static SystemService EnsureSystemService()
    {
        lock (SyncRoot)
        {
            if (cachedSystemService != null)
            {
                return cachedSystemService;
            }

            var service = (SystemService)FormatterServices.GetUninitializedObject(typeof(SystemService));
            SetInstanceField(service, "_world", EnsureServerWorld());
            SetInstanceField(service, "_serverScriptMapper", EnsureServerScriptMapper());

            cachedSystemService = service;
            return service;
        }
    }

    static ServerScriptMapper EnsureServerScriptMapper()
    {
        lock (SyncRoot)
        {
            if (cachedServerScriptMapper != null)
            {
                return cachedServerScriptMapper;
            }

            var mapper = new ServerScriptMapper
            {
                ServerGameManager = EnsureServerGameManager()
            };

            cachedServerScriptMapper = mapper;
            return mapper;
        }
    }

    static ServerGameManager EnsureServerGameManager()
    {
        lock (SyncRoot)
        {
            if (cachedServerGameManager != null)
            {
                return cachedServerGameManager;
            }

            cachedServerGameManager = (ServerGameManager)FormatterServices.GetUninitializedObject(typeof(ServerGameManager));
            return cachedServerGameManager;
        }
    }

    static void SetInstanceField(object instance, string fieldName, object value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field?.SetValue(instance, value);
    }
}
