using System.Collections.Generic;
using System.Linq;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Tests.Stubs;
using Stunlock.Core;

namespace Bloodcraft.Tests.Services;

public sealed class EliteShardBearerBootstrapperTests : TestHost
{
    [Fact]
    public void Initialize_WhenDisabled_SkipsContextAndFactory()
    {
        int contextFactoryInvocations = 0;
        int resetFactoryInvocations = 0;

        EliteShardBearerBootstrapper.Initialize(
            false,
            () =>
            {
                contextFactoryInvocations++;
                return FakeVBloodEntityContext.FromPrefabs(Array.Empty<PrefabGUID?>());
            },
            (context, logger) =>
            {
                resetFactoryInvocations++;
                return new ShardBearerResetService(context, logger);
            },
            _ => { });

        Assert.Equal(0, contextFactoryInvocations);
        Assert.Equal(0, resetFactoryInvocations);
    }

    [Fact]
    public void Initialize_WhenEnabled_ResetsShardBearers()
    {
        var context = FakeVBloodEntityContext.FromPrefabs(new PrefabGUID?[]
        {
            PrefabGUIDs.CHAR_Manticore_VBlood
        });

        int contextFactoryInvocations = 0;
        int resetFactoryInvocations = 0;
        var warnings = new List<string>();

        EliteShardBearerBootstrapper.Initialize(
            true,
            () =>
            {
                contextFactoryInvocations++;
                return context;
            },
            (ctx, logger) =>
            {
                resetFactoryInvocations++;
                Assert.Same(context, ctx);
                return new ShardBearerResetService(ctx, logger);
            },
            warnings.Add);

        Assert.Equal(1, contextFactoryInvocations);
        Assert.Equal(1, resetFactoryInvocations);
        Assert.True(context.Rows.Single().Destroyed);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Initialize_WhenResetFails_ReportsWarningThroughDelegate()
    {
        var context = FakeVBloodEntityContext.FromPrefabs(Array.Empty<PrefabGUID?>());
        context.ThrowOnEnumerate = true;

        int contextFactoryInvocations = 0;
        int resetFactoryInvocations = 0;
        var warnings = new List<string>();

        EliteShardBearerBootstrapper.Initialize(
            true,
            () =>
            {
                contextFactoryInvocations++;
                return context;
            },
            (ctx, logger) =>
            {
                resetFactoryInvocations++;
                Assert.Same(context, ctx);
                return new ShardBearerResetService(ctx, logger);
            },
            warnings.Add);

        Assert.Equal(1, contextFactoryInvocations);
        Assert.Equal(1, resetFactoryInvocations);
        Assert.Single(warnings);
        Assert.Contains("error", warnings.Single(), StringComparison.OrdinalIgnoreCase);
    }
}
