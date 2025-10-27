using System;
using ProjectM;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the <see cref="Patches.DeathEventListenerSystemPatch"/> setup behaviour.
/// </summary>
public sealed class DeathEventAggregationWork : ISystemWork
{
    static QueryDescription CreateDeathEventQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<DeathEvent>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription deathEventQuery = CreateDeathEventQuery();

    /// <summary>
    /// Gets the primary death-event query description used by the work.
    /// </summary>
    public QueryDescription DeathEventQuery => deathEventQuery;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<DeathEvent>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetEntityTypeHandle();
            _ = facade.GetEntityStorageInfoLookup();
            _ = facade.GetComponentLookup<Movement>(isReadOnly: true);
            _ = facade.GetComponentLookup<BlockFeedBuff>(isReadOnly: true);
            _ = facade.GetComponentLookup<Trader>(isReadOnly: true);
            _ = facade.GetComponentLookup<UnitLevel>(isReadOnly: true);
            _ = facade.GetComponentLookup<Minion>(isReadOnly: true);
            _ = facade.GetComponentLookup<VBloodConsumeSource>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
    }

    /// <summary>
    /// Determines whether the kill qualifies as a feed kill for legacy progression.
    /// </summary>
    /// <param name="deathEvent">Death event being evaluated.</param>
    public static bool IsFeedKill(in DeathEvent deathEvent)
    {
        return deathEvent.StatChangeReason.Equals(StatChangeReason.HandleGameplayEventsBase_11);
    }

    /// <summary>
    /// Determines whether the legacy system should process the death event.
    /// </summary>
    /// <param name="legaciesEnabled">Indicates whether the legacy system is enabled.</param>
    /// <param name="deathEvent">Death event being evaluated.</param>
    public static bool ShouldProcessLegacyProgression(bool legaciesEnabled, in DeathEvent deathEvent)
    {
        return legaciesEnabled && IsFeedKill(in deathEvent);
    }

    /// <summary>
    /// Determines whether an active familiar should be reset after a death.
    /// </summary>
    /// <param name="familiarsEnabled">Indicates whether the familiar system is enabled.</param>
    /// <param name="diedHasFollowedPlayer">Indicates whether the deceased entity was following a player.</param>
    /// <param name="playerHasActiveFamiliar">Indicates whether the player has an active familiar.</param>
    /// <param name="deceasedMatchesActiveFamiliar">Indicates whether the deceased entity matches the active familiar.</param>
    public static bool ShouldResetActiveFamiliar(
        bool familiarsEnabled,
        bool diedHasFollowedPlayer,
        bool playerHasActiveFamiliar,
        bool deceasedMatchesActiveFamiliar)
    {
        if (!familiarsEnabled || !diedHasFollowedPlayer)
        {
            return false;
        }

        return playerHasActiveFamiliar && deceasedMatchesActiveFamiliar;
    }

    /// <summary>
    /// Determines whether minion unlock rewards should be processed for the death.
    /// </summary>
    /// <param name="allowMinions">Indicates whether minion rewards are allowed.</param>
    /// <param name="isMinion">Indicates whether the deceased entity was a minion.</param>
    public static bool ShouldProcessMinionUnlock(bool allowMinions, bool isMinion)
    {
        return allowMinions && isMinion;
    }
}
