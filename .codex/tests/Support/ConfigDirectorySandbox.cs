using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bloodcraft;
using Bloodcraft.Services;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a disposable sandbox that redirects configuration directory reads to an isolated temporary root.
/// </summary>
public sealed class ConfigDirectorySandbox : IDisposable
{
    static readonly FieldInfo DirectoryPathsField = typeof(ConfigService)
        .GetNestedType("ConfigInitialization", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
        ?.GetField("_directoryPaths", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Failed to locate ConfigInitialization._directoryPaths");

    static readonly object SyncRoot = new();
    static ConfigDirectorySandbox? assemblyScope;
    static bool processExitRegistered;
    static readonly FieldInfo? FieldAttributesField = typeof(FieldInfo)
        .GetField("m_fieldAttributes", BindingFlags.Instance | BindingFlags.NonPublic);

    readonly Lazy<List<string>> originalLazy;
    readonly DirectoryInfo sandboxRoot;
    readonly Lazy<List<string>> sandboxLazy;
    bool disposed;

    ConfigDirectorySandbox()
    {
        if (DirectoryPathsField.GetValue(null) is not Lazy<List<string>> capturedLazy)
        {
            throw new InvalidOperationException("ConfigInitialization._directoryPaths did not expose a Lazy<List<string>> value.");
        }

        originalLazy = capturedLazy;
        sandboxRoot = CreateSandboxRoot();
        var rootPath = sandboxRoot.FullName;

        sandboxLazy = new Lazy<List<string>>(() =>
        {
            string pluginRoot = EnsureDirectory(rootPath, MyPluginInfo.PLUGIN_NAME);
            return new List<string>
            {
                pluginRoot,
                EnsureDirectory(pluginRoot, "PlayerLeveling"),
                EnsureDirectory(pluginRoot, "Quests"),
                EnsureDirectory(pluginRoot, "WeaponExpertise"),
                EnsureDirectory(pluginRoot, "BloodLegacies"),
                EnsureDirectory(pluginRoot, "Professions"),
                EnsureDirectory(pluginRoot, "Familiars"),
                EnsureDirectory(pluginRoot, "Familiars", "FamiliarLeveling"),
                EnsureDirectory(pluginRoot, "Familiars", "FamiliarUnlocks"),
                EnsureDirectory(pluginRoot, "PlayerBools"),
                EnsureDirectory(pluginRoot, "Familiars", "FamiliarEquipment"),
                EnsureDirectory(pluginRoot, "Familiars", "FamiliarBattleGroups")
            };
        });

        AssignDirectoryPaths(sandboxLazy);
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

            AssignDirectoryPaths(originalLazy);

            if (ReferenceEquals(assemblyScope, this))
            {
                assemblyScope = null;
            }

            disposed = true;
        }

        TryDeleteSandboxRoot();
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

    static DirectoryInfo CreateSandboxRoot()
    {
        var directoryType = typeof(Directory);

        var prefixFactory = directoryType.GetMethod(
            "CreateTempSubdirectory",
            BindingFlags.Public | BindingFlags.Static,
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
            BindingFlags.Public | BindingFlags.Static,
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

    static void OnDomainShutdown(object? sender, EventArgs e)
    {
        ConfigDirectorySandbox? scope;
        lock (SyncRoot)
        {
            scope = assemblyScope;
            assemblyScope = null;
        }

        scope?.Dispose();
    }

    static void AssignDirectoryPaths(Lazy<List<string>> value)
    {
        if (FieldAttributesField != null)
        {
            var attributes = (FieldAttributes)FieldAttributesField.GetValue(DirectoryPathsField)!;
            if ((attributes & FieldAttributes.InitOnly) != 0)
            {
                try
                {
                    FieldAttributesField.SetValue(DirectoryPathsField, attributes & ~FieldAttributes.InitOnly);
                    DirectoryPathsField.SetValue(null, value);
                }
                finally
                {
                    FieldAttributesField.SetValue(DirectoryPathsField, attributes);
                }

                return;
            }
        }

        DirectoryPathsField.SetValue(null, value);
    }
}
