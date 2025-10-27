using System;
using System.Collections.Generic;
using System.IO;
using Bloodcraft;
using Bloodcraft.Services;
using BepInEx;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a disposable sandbox that redirects configuration directory reads to an isolated temporary root.
/// </summary>
public sealed class ConfigDirectorySandbox : IDisposable
{
    static readonly object SyncRoot = new();
    static readonly Stack<string> ConfigPathHistory = new();
    static readonly string DefaultConfigRoot = Paths.ConfigPath;

    static ConfigDirectorySandbox? assemblyScope;
    static bool processExitRegistered;

    readonly DirectoryInfo sandboxRoot;
    bool disposed;

    ConfigDirectorySandbox()
    {
        sandboxRoot = CreateSandboxRoot();
        InitializeDirectoryLayout(sandboxRoot.FullName);

        lock (SyncRoot)
        {
            ConfigPathHistory.Push(Paths.ConfigPath);
            Paths.ConfigPath = sandboxRoot.FullName;
        }
    }

    /// <summary>
    /// Installs a sandbox that remains active for the duration of the test assembly.
    /// Subsequent calls are ignored once the sandbox has been configured.
    /// </summary>
    public static void InstallForAssemblyLifetime()
    {
        lock (SyncRoot)
        {
            if (assemblyScope != null)
            {
                return;
            }

            assemblyScope = new ConfigDirectorySandbox();
            EnsureProcessExitHandlers();
        }
    }

    /// <summary>
    /// Creates a sandbox scope that should be disposed when the override is no longer needed.
    /// </summary>
    public static ConfigDirectorySandbox Install()
    {
        return new ConfigDirectorySandbox();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (SyncRoot)
        {
            if (disposed)
            {
                return;
            }

            string restorePath = ConfigPathHistory.Count > 0 ? ConfigPathHistory.Pop() : DefaultConfigRoot;
            Paths.ConfigPath = restorePath;

            if (ReferenceEquals(assemblyScope, this))
            {
                assemblyScope = null;
            }

            disposed = true;
        }

        TryDeleteSandboxRoot();
    }

    static void EnsureProcessExitHandlers()
    {
        if (processExitRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.ProcessExit += OnDomainShutdown;
        AppDomain.CurrentDomain.DomainUnload += OnDomainShutdown;
        processExitRegistered = true;
    }

    static void OnDomainShutdown(object? sender, EventArgs e)
    {
        lock (SyncRoot)
        {
            assemblyScope = null;
            ConfigPathHistory.Clear();
            Paths.ConfigPath = DefaultConfigRoot;
        }
    }

    static DirectoryInfo CreateSandboxRoot()
    {
        var directoryType = typeof(Directory);

        var prefixFactory = directoryType.GetMethod(
            "CreateTempSubdirectory",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null);

        if (prefixFactory != null
            && prefixFactory.Invoke(null, new object?[] { "bloodcraft_config_", null }) is DirectoryInfo prefixed)
        {
            return prefixed;
        }

        var parameterlessFactory = directoryType.GetMethod(
            "CreateTempSubdirectory",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        if (parameterlessFactory != null
            && parameterlessFactory.Invoke(null, Array.Empty<object?>()) is DirectoryInfo unprefixed)
        {
            return unprefixed;
        }

        string fallbackPath = Path.Combine(Path.GetTempPath(), $"bloodcraft_config_{Guid.NewGuid():N}");
        return Directory.CreateDirectory(fallbackPath);
    }

    static void InitializeDirectoryLayout(string rootPath)
    {
        string pluginRoot = EnsureDirectory(rootPath, MyPluginInfo.PLUGIN_NAME);
        _ = EnsureDirectory(pluginRoot, "PlayerLeveling");
        _ = EnsureDirectory(pluginRoot, "Quests");
        _ = EnsureDirectory(pluginRoot, "WeaponExpertise");
        _ = EnsureDirectory(pluginRoot, "BloodLegacies");
        _ = EnsureDirectory(pluginRoot, "Professions");
        _ = EnsureDirectory(pluginRoot, "Familiars");
        _ = EnsureDirectory(pluginRoot, "Familiars", "FamiliarLeveling");
        _ = EnsureDirectory(pluginRoot, "Familiars", "FamiliarUnlocks");
        _ = EnsureDirectory(pluginRoot, "PlayerBools");
        _ = EnsureDirectory(pluginRoot, "Familiars", "FamiliarEquipment");
        _ = EnsureDirectory(pluginRoot, "Familiars", "FamiliarBattleGroups");
    }

    static string EnsureDirectory(string root, params string[] segments)
    {
        string fullPath = root;
        foreach (string segment in segments)
        {
            fullPath = Path.Combine(fullPath, segment);
        }

        return Directory.CreateDirectory(fullPath).FullName;
    }

    void TryDeleteSandboxRoot()
    {
        try
        {
            sandboxRoot.Delete(recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
