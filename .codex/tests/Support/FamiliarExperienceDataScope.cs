using System;
using System.IO;
using Bloodcraft.Services;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Ensures familiar experience persistence starts from a clean slate for each test case.
/// </summary>
public sealed class FamiliarExperienceDataScope : IDisposable
{
    readonly string filePath;
    bool disposed;

    /// <summary>
    /// Removes any existing familiar experience payload for the specified Steam ID.
    /// </summary>
    public FamiliarExperienceDataScope(ulong steamId)
    {
        filePath = Path.Combine(ConfigService.ConfigInitialization.DirectoryPaths[7], $"{steamId}_familiar_experience.json");
        DeleteIfExists();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        DeleteIfExists();
        disposed = true;
    }

    void DeleteIfExists()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
