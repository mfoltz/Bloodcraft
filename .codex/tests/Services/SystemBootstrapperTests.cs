using System;
using System.Collections.Generic;
using Bloodcraft.Patches;
using Unity.Entities;
using Xunit;
#nullable enable

namespace Bloodcraft.Tests.Services;

public sealed class SystemBootstrapperTests : TestHost
{
    [Fact]
    public void Prefix_WithElitePrimalRiftsDisabled_RegistersQuestTargetSystemOnly()
    {
        using var overrides = WithConfigOverrides(("ElitePrimalRifts", false));
        using var worldScope = CreateServerWorld();
        var world = worldScope.Instance;
        _ = world;

        ConfigureTestHooks(() => false);

        try
        {
            IReadOnlyList<Type> registeredTypes = WorldBootstrapPatch.TestHooks.EnumerateSystemsForTests(false);

            Assert.Contains(typeof(DummyQuestSystem), registeredTypes);
            Assert.DoesNotContain(typeof(DummyPrimalSystem), registeredTypes);
            Assert.Single(registeredTypes);
        }
        finally
        {
            WorldBootstrapPatch.TestHooks.Reset();
        }
    }

    [Fact]
    public void Prefix_WithElitePrimalRiftsEnabled_RegistersQuestTargetAndPrimalWarSystems()
    {
        using var overrides = WithConfigOverrides(("ElitePrimalRifts", true));
        using var worldScope = CreateServerWorld();
        var world = worldScope.Instance;
        _ = world;

        ConfigureTestHooks(() => true);

        try
        {
            bool elitePrimalRiftsEnabled = WorldBootstrapPatch.TestHooks.EvaluateElitePrimalRifts();
            IReadOnlyList<Type> registeredTypes = WorldBootstrapPatch.TestHooks.EnumerateSystemsForTests(elitePrimalRiftsEnabled);

            Assert.Contains(typeof(DummyQuestSystem), registeredTypes);
            Assert.Contains(typeof(DummyPrimalSystem), registeredTypes);
            Assert.Equal(2, registeredTypes.Count);
        }
        finally
        {
            WorldBootstrapPatch.TestHooks.Reset();
        }
    }

    protected override void ResetState()
    {
        base.ResetState();
        WorldBootstrapPatch.TestHooks.Reset();
    }

    static WorldScope CreateServerWorld() => new();

    static void ConfigureTestHooks(Func<bool>? elitePrimalRiftsProvider = null)
    {
        WorldBootstrapPatch.TestHooks.ElitePrimalRiftsProvider = elitePrimalRiftsProvider;
        WorldBootstrapPatch.TestHooks.GetSystemTypes = () => new[] { typeof(DummyQuestSystem), typeof(DummyPrimalSystem) };
        WorldBootstrapPatch.TestHooks.PrimalSystemType = typeof(DummyPrimalSystem);
    }

    sealed class WorldScope : IDisposable
    {
        public World? Instance { get; } = null;

        public void Dispose()
        {
        }
    }

    sealed class DummyQuestSystem { }

    sealed class DummyPrimalSystem { }
}
