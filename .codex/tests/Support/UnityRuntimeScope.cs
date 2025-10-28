using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Bloodcraft;
using Bloodcraft.Services;
using HarmonyLib;
using ProjectM;
using Unity.Entities;
using Xunit;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a disposable scope that wires up a lightweight Unity runtime so static
/// constructors in the production assemblies can execute without touching IL2CPP
/// entry points. The scope registers a stub <c>World</c>, patches <see cref="Core"/>
/// accessors and injects simple stand-ins for the handful of runtime services that
/// the systems expect during initialization.
/// </summary>
public sealed class UnityRuntimeScope : IDisposable
{
    static readonly Harmony HarmonyInstance = new("Bloodcraft.Tests.Support.UnityRuntimeScope");
    static readonly object SyncRoot = new();

    static World? world;
    static SystemService? systemService;
    static ServerGameManager? serverGameManager;
    static object? serverGameSettingsSystem;
    static bool patched;
    static int activeScopes;

    bool disposed;

    /// <summary>
    /// Initializes the shimmed Unity runtime if this is the first active scope.
    /// </summary>
    public UnityRuntimeScope()
    {
        lock (SyncRoot)
        {
            if (activeScopes == 0)
            {
                InitializeRuntime();
            }

            activeScopes++;
        }
    }

    /// <summary>
    /// Tears down the shimmed runtime once the last scope exits.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        lock (SyncRoot)
        {
            if (activeScopes > 0)
            {
                activeScopes--;
                if (activeScopes == 0)
                {
                    TeardownRuntime();
                }
            }
        }

        disposed = true;
    }

    static void InitializeRuntime()
    {
        world = new World("Server");
        RegisterWorld(world);

        systemService = new SystemService(world);
        serverGameManager = new ServerGameManager();
        serverGameSettingsSystem = CreateServerGameSettingsSystem();

        ApplySystemServiceOverrides(systemService, serverGameSettingsSystem);
        ApplyPatches();
    }

    static void TeardownRuntime()
    {
        HarmonyInstance.UnpatchSelf();
        patched = false;

        if (systemService != null)
        {
            ResetSystemServiceOverrides(systemService);
            systemService = null;
        }

        serverGameManager = null;
        serverGameSettingsSystem = null;

        if (world != null)
        {
            UnregisterWorld(world);
            world.Dispose();
            world = null;
        }
    }

    static void ApplyPatches()
    {
        if (patched)
        {
            return;
        }

        PatchPropertyGetter(nameof(Core.Server), nameof(ServerGetterPrefix));
        PatchPropertyGetter(nameof(Core.SystemService), nameof(SystemServiceGetterPrefix));
        PatchPropertyGetter(nameof(Core.ServerGameManager), nameof(ServerGameManagerGetterPrefix));

        patched = true;
    }

    static void PatchPropertyGetter(string propertyName, string prefixName)
    {
        MethodInfo? getter = AccessTools.PropertyGetter(typeof(Core), propertyName);
        MethodInfo prefix = typeof(UnityRuntimeScope)
            .GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Failed to locate prefix '{prefixName}' for {propertyName} getter patch.");

        HarmonyInstance.Patch(getter, prefix: new HarmonyMethod(prefix));
    }

    static bool ServerGetterPrefix(ref World __result)
    {
        if (world == null)
        {
            throw new InvalidOperationException("Unity runtime scope has not been initialized.");
        }

        __result = world;
        return false;
    }

    static bool SystemServiceGetterPrefix(ref SystemService __result)
    {
        if (systemService == null)
        {
            throw new InvalidOperationException("Unity runtime scope has not been initialized.");
        }

        __result = systemService;
        return false;
    }

    static bool ServerGameManagerGetterPrefix(ref ServerGameManager __result)
    {
        if (serverGameManager == null)
        {
            throw new InvalidOperationException("Unity runtime scope has not been initialized.");
        }

        __result = serverGameManager;
        return false;
    }

    static void RegisterWorld(World serverWorld)
    {
        FieldInfo? allWorldsField = AccessTools.Field(typeof(World), "s_AllWorlds");
        if (allWorldsField?.GetValue(null) is List<World> worlds)
        {
            if (!worlds.Contains(serverWorld))
            {
                worlds.Add(serverWorld);
            }
        }

        World.DefaultGameObjectInjectionWorld = serverWorld;
    }

    static void UnregisterWorld(World serverWorld)
    {
        FieldInfo? allWorldsField = AccessTools.Field(typeof(World), "s_AllWorlds");
        if (allWorldsField?.GetValue(null) is List<World> worlds)
        {
            worlds.Remove(serverWorld);
        }

        if (ReferenceEquals(World.DefaultGameObjectInjectionWorld, serverWorld))
        {
            World.DefaultGameObjectInjectionWorld = null;
        }
    }

    static void ApplySystemServiceOverrides(SystemService service, object? settingsSystem)
    {
        if (settingsSystem == null)
        {
            return;
        }

        FieldInfo? settingsField = typeof(SystemService)
            .GetField("_serverGameSettingsSystem", BindingFlags.Instance | BindingFlags.NonPublic);
        settingsField?.SetValue(service, settingsSystem);
    }

    static void ResetSystemServiceOverrides(SystemService service)
    {
        FieldInfo? settingsField = typeof(SystemService)
            .GetField("_serverGameSettingsSystem", BindingFlags.Instance | BindingFlags.NonPublic);
        settingsField?.SetValue(service, null);
    }

    static object? CreateServerGameSettingsSystem()
    {
        Type? systemType = AccessTools.TypeByName("ProjectM.Gameplay.Systems.ServerGameSettingsSystem");
        if (systemType == null)
        {
            return null;
        }

        object instance = FormatterServices.GetUninitializedObject(systemType);
        object? settings = CreateGameSettingsPayload(systemType);

        if (settings != null)
        {
            AssignMember(systemType, instance, "Settings", settings);
            AssignMember(systemType, instance, "_Settings", settings);
        }

        return instance;
    }

    static object? CreateGameSettingsPayload(Type systemType)
    {
        Type? settingsType = systemType.GetField("Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.FieldType
            ?? systemType.GetProperty("Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.PropertyType
            ?? systemType.GetField("_Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.FieldType;

        if (settingsType == null)
        {
            return null;
        }

        object settings = FormatterServices.GetUninitializedObject(settingsType);

        AssignEnum(settings, "GameDifficulty", "ProjectM.GameDifficulty");
        AssignEnum(settings, "GameModeType", "ProjectM.GameModeType");
        AssignFloat(settings, "MaterialYieldModifier_Global", 1f);

        return settings;
    }

    static void AssignMember(Type declaringType, object instance, string memberName, object value)
    {
        FieldInfo? field = declaringType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(instance, value);
            return;
        }

        PropertyInfo? property = declaringType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(instance, value);
    }

    static void AssignEnum(object target, string memberName, string enumTypeName)
    {
        Type? enumType = AccessTools.TypeByName(enumTypeName);
        if (enumType == null)
        {
            return;
        }

        object enumValue = Enum.GetValues(enumType).GetValue(0) ?? Activator.CreateInstance(enumType)!;
        AssignValue(target, memberName, enumValue);
    }

    static void AssignFloat(object target, string memberName, float value)
    {
        AssignValue(target, memberName, value);
    }

    static void AssignValue(object target, string memberName, object value)
    {
        Type targetType = target.GetType();
        FieldInfo? field = targetType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
            return;
        }

        PropertyInfo? property = targetType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        property?.SetValue(target, value);
    }
}

/// <summary>
/// Provides a test collection that ensures the Unity runtime scope is active for all
/// participating test classes.
/// </summary>
public static class UnityRuntimeTestCollection
{
    /// <summary>
    /// The shared collection name used by Unity-dependent tests.
    /// </summary>
    public const string CollectionName = "Unity Runtime";
}

/// <summary>
/// Collection fixture that keeps the Unity runtime scope alive for the duration of the
/// annotated test collection.
/// </summary>
[CollectionDefinition(UnityRuntimeTestCollection.CollectionName, DisableParallelization = true)]
public sealed class UnityRuntimeCollectionDefinition : ICollectionFixture<UnityRuntimeScopeFixture>
{
}

/// <summary>
/// XUnit fixture that acquires a <see cref="UnityRuntimeScope"/> for the lifetime of the
/// test collection.
/// </summary>
public sealed class UnityRuntimeScopeFixture : IDisposable
{
    readonly UnityRuntimeScope scope = new();
    bool disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        scope.Dispose();
        disposed = true;
    }
}
