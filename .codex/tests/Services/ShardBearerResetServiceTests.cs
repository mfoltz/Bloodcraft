using System;
using System.Collections.Generic;
using System.Linq;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Tests.Stubs;
using Bloodcraft.Tests;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Services;

public sealed class ShardBearerResetServiceTests : TestHost
{
    [Fact]
    public void ResetShardBearers_DestroysConfiguredShardBearers()
    {
        var context = FakeVBloodEntityContext.FromPrefabs(new PrefabGUID?[]
        {
            PrefabGUIDs.CHAR_Manticore_VBlood,
            new PrefabGUID(999)
        });

        var warnings = new List<string>();
        var service = new ShardBearerResetService(context, warnings.Add, new[] { PrefabGUIDs.CHAR_Manticore_VBlood });

        service.ResetShardBearers();

        FakeVBloodEntityRow shardBearerRow = context.Rows.First(row => row.PrefabGuid == PrefabGUIDs.CHAR_Manticore_VBlood);
        FakeVBloodEntityRow otherRow = context.Rows.First(row => row.PrefabGuid == new PrefabGUID(999));

        Assert.True(shardBearerRow.Destroyed);
        Assert.False(otherRow.Destroyed);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ResetShardBearers_IgnoresEntitiesWithoutPrefabGuid()
    {
        var context = FakeVBloodEntityContext.FromPrefabs(new PrefabGUID?[] { null });
        var warnings = new List<string>();
        var service = new ShardBearerResetService(context, warnings.Add);

        service.ResetShardBearers();

        Assert.False(context.Rows.Single().Destroyed);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ResetShardBearers_SucceedsWithEmptyWorld()
    {
        var context = FakeVBloodEntityContext.FromPrefabs(Array.Empty<PrefabGUID?>());
        var warnings = new List<string>();
        var service = new ShardBearerResetService(context, warnings.Add);

        service.ResetShardBearers();

        Assert.Empty(context.Rows);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ResetShardBearers_DoesNotThrowWhenContextFails()
    {
        var context = FakeVBloodEntityContext.FromPrefabs(Array.Empty<PrefabGUID?>());
        context.ThrowOnEnumerate = true;
        var warnings = new List<string>();
        var service = new ShardBearerResetService(context, warnings.Add);

        service.ResetShardBearers();

        Assert.Empty(context.Rows);
        Assert.Single(warnings, warning => warning.Contains("error", StringComparison.OrdinalIgnoreCase));
    }
}
