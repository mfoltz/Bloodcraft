using System;
using static Bloodcraft.Utilities.Familiars.ActiveFamiliarManager;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides a guard that clears the active familiar cache before and after a test runs.
/// </summary>
public sealed class ActiveFamiliarResetScope : IDisposable
{
    readonly ulong steamId;
    bool disposed;

    /// <summary>
    /// Initializes the scope and clears any ambient familiar state for the provided player.
    /// </summary>
    public ActiveFamiliarResetScope(ulong steamId)
    {
        this.steamId = steamId;
        ResetActiveFamiliarData(steamId);
        EntityTestScope.RemoveFamiliarStub(steamId);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        ResetActiveFamiliarData(steamId);
        EntityTestScope.RemoveFamiliarStub(steamId);
        disposed = true;
    }
}
