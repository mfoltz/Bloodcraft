using System;
using Bloodcraft.Interfaces;

namespace Bloodcraft.Services;

/// <summary>
/// Coordinates the bootstrap logic for resetting elite shard bearer entities.
/// </summary>
internal static class EliteShardBearerBootstrapper
{
    /// <summary>
    /// Initializes the shard bearer reset workflow when enabled.
    /// </summary>
    /// <param name="isEnabled">The feature flag indicating whether shard bearers should be reset.</param>
    /// <param name="contextFactory">Supplies the context required for shard bearer operations.</param>
    /// <param name="resetServiceFactory">Creates the reset service using the supplied context and logger.</param>
    /// <param name="logWarning">The delegate used to record warning messages.</param>
    public static void Initialize(
        bool isEnabled,
        Func<IVBloodEntityContext> contextFactory,
        Func<IVBloodEntityContext, Action<string>, ShardBearerResetService> resetServiceFactory,
        Action<string> logWarning)
    {
        if (!isEnabled)
        {
            return;
        }

        if (contextFactory is null)
        {
            throw new ArgumentNullException(nameof(contextFactory));
        }

        if (resetServiceFactory is null)
        {
            throw new ArgumentNullException(nameof(resetServiceFactory));
        }

        if (logWarning is null)
        {
            throw new ArgumentNullException(nameof(logWarning));
        }

        IVBloodEntityContext context = contextFactory()
            ?? throw new InvalidOperationException("The context factory returned null.");

        ShardBearerResetService resetService = resetServiceFactory(context, logWarning)
            ?? throw new InvalidOperationException("The reset service factory returned null.");

        resetService.ResetShardBearers();
    }
}
