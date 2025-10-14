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
    /// Allows derived fixtures to release resources once the test suite completes.
    /// </summary>
    public virtual void Dispose()
    {
    }
}
