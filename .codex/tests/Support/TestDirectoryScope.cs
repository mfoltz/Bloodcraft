using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Bloodcraft.Services;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a sandboxed directory for <see cref="Bloodcraft.Services.DataService.PlayerPersistence"/> file operations.
/// The scope temporarily rewrites entries in the private <c>_filePaths</c> map so tests can observe persistence behaviour
/// without touching the developer's local filesystem.
/// </summary>
public sealed class TestDirectoryScope : IDisposable
{
    readonly DirectoryInfo tempDirectory;
    readonly Dictionary<string, string> originalPaths;
    readonly Dictionary<string, string> remappedPaths;
    bool disposed;

    /// <summary>
    /// Gets the remapped file paths that now point at the isolated temporary directory.
    /// </summary>
    public IReadOnlyDictionary<string, string> Paths => remappedPaths;

    /// <summary>
    /// Creates a new scope that rewrites the specified persistence keys to live under a dedicated temporary directory.
    /// </summary>
    /// <param name="keys">The keys from <c>PlayerPersistence._filePaths</c> that should be redirected.</param>
    public TestDirectoryScope(params string[] keys)
    {
        ConfigDirectoryShim.EnsureInitialized();

        if (keys is null || keys.Length == 0)
        {
            throw new ArgumentException("At least one key must be provided to remap the persistence directory.", nameof(keys));
        }

        var filePaths = GetFilePathsDictionary();

        originalPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        remappedPaths = new Dictionary<string, string>(StringComparer.Ordinal);
        tempDirectory = CreateTemporaryDirectory();

        foreach (var key in keys)
        {
            if (!filePaths.TryGetValue(key, out var path))
            {
                throw new KeyNotFoundException($"The persistence map does not contain the key '{key}'.");
            }

            var replacement = Path.Combine(tempDirectory.FullName, Path.GetFileName(path));
            originalPaths[key] = path;
            remappedPaths[key] = replacement;
            filePaths[key] = replacement;
        }
    }

    static Dictionary<string, string> GetFilePathsDictionary()
    {
        ConfigDirectoryShim.EnsureInitialized();

        var persistenceType = typeof(DataService).GetNestedType("PlayerPersistence", BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("PlayerPersistence type could not be located.");
        var field = persistenceType.GetField("_filePaths", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("PlayerPersistence._filePaths field could not be located.");
        return (Dictionary<string, string>)field.GetValue(null)!;
    }

    static DirectoryInfo CreateTemporaryDirectory()
    {
        var factory = typeof(Directory).GetMethod("CreateTempSubdirectory", BindingFlags.Public | BindingFlags.Static, Type.DefaultBinder, Type.EmptyTypes, null);
        if (factory is not null)
        {
            return (DirectoryInfo)factory.Invoke(null, null)!;
        }

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        return Directory.CreateDirectory(path);
    }

    /// <summary>
    /// Restores the original file-path mappings and deletes the temporary directory.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        try
        {
            var filePaths = GetFilePathsDictionary();
            foreach (var mapping in originalPaths)
            {
                filePaths[mapping.Key] = mapping.Value;
            }
        }
        finally
        {
            try
            {
                if (tempDirectory.Exists)
                {
                    tempDirectory.Delete(recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures; test execution should not depend on file-system deletion semantics.
            }

            disposed = true;
        }
    }
}
