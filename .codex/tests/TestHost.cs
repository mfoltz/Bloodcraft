using Bloodcraft.Tests.Support;
using PlayerDictionaries = Bloodcraft.Services.DataService.PlayerDictionaries;

namespace Bloodcraft.Tests;

/// <summary>
/// Provides a shared initialization surface for the test suite. Derived tests can
/// override <see cref="ResetState"/> to clear static caches or set up ambient state
/// before each fixture runs.
/// </summary>
public abstract class TestHost : IDisposable
{
    protected TestHost()
    {
        ResetState();
    }

    /// <summary>
    /// Resets any ambient state that could leak between tests.
    /// </summary>
    protected virtual void ResetState()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
    }

    /// <summary>
    /// Captures the current player-related data caches and restores them when the returned
    /// <see cref="IDisposable"/> is disposed. Useful for tests that need to mutate shared state such as
    /// <see cref="Bloodcraft.Services.PlayerDictionaries._playerExperience"/>.
    /// </summary>
    /// <returns>A scope object that restores the cached data on disposal.</returns>
    protected static DataStateScope CapturePlayerData() => new();

    /// <summary>
    /// Seeds a specific player's experience entry for the lifetime of the returned scope. All tracked
    /// <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey, TValue}"/> instances are restored when the scope is
    /// disposed to ensure subsequent tests observe a clean slate.
    /// </summary>
    /// <param name="playerId">The Steam ID or entity ID of the player to configure.</param>
    /// <param name="level">The level that should be associated with the player.</param>
    /// <param name="experiencePoints">The fractional experience progress toward the next level.</param>
    /// <returns>An <see cref="IDisposable"/> scope that resets all player caches after use.</returns>
    protected static IDisposable WithPlayerExperience(ulong playerId, int level, float experiencePoints = 0f)
    {
        var scope = new DataStateScope();
        PlayerDictionaries._playerExperience[playerId] = new KeyValuePair<int, float>(level, experiencePoints);
        return scope;
    }

    /// <summary>
    /// Applies the provided configuration overrides and resets the cached <see cref="Lazy{T}"/> values
    /// exposed by <see cref="Bloodcraft.Services.ConfigService"/>. The original configuration is restored when the returned
    /// scope is disposed.
    /// </summary>
    /// <param name="overrides">The configuration keys and values to apply while the scope is active.</param>
    /// <returns>A <see cref="ConfigOverrideScope"/> that should be disposed when the overrides are no longer required.</returns>
    protected static ConfigOverrideScope WithConfigOverrides(params (string Key, object Value)[] overrides)
    {
        var payload = overrides.Select(entry => new KeyValuePair<string, object>(entry.Key, entry.Value));
        return new ConfigOverrideScope(payload);
    }

    /// <summary>
    /// Allows derived fixtures to release resources once the test suite completes.
    /// </summary>
    public virtual void Dispose()
    {
    }
}
