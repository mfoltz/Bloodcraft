using System;
using System.Collections.Generic;
using System.Linq;
using Bloodcraft.Interfaces;
using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Tests;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Xunit;

namespace Bloodcraft.Tests.Services;

public sealed class ShardBearerResetServiceTests : TestHost
{
    [Fact]
    public void ResetShardBearers_DestroysConfiguredShardBearers()
    {
        var context = new StubVBloodEntityContext(new PrefabGUID?[]
        {
            PrefabGUIDs.CHAR_Manticore_VBlood,
            new PrefabGUID(999)
        });

        var warnings = new List<string>();
        var service = new ShardBearerResetService(context, warnings.Add, new[] { PrefabGUIDs.CHAR_Manticore_VBlood });

        service.ResetShardBearers();

        Assert.Single(context.DestroyedPrefabs, PrefabGUIDs.CHAR_Manticore_VBlood);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ResetShardBearers_IgnoresEntitiesWithoutPrefabGuid()
    {
        var context = new StubVBloodEntityContext(new PrefabGUID?[] { null });
        var warnings = new List<string>();
        var service = new ShardBearerResetService(context, warnings.Add);

        service.ResetShardBearers();

        Assert.Empty(context.DestroyedPrefabs);
        Assert.Empty(warnings);
    }

    [Fact]
    public void ResetShardBearers_DoesNotThrowWhenContextFails()
    {
        var context = new StubVBloodEntityContext(Array.Empty<PrefabGUID?>())
        {
            ThrowOnEnumerate = true
        };
        var warnings = new List<string>();
        var service = new ShardBearerResetService(context, warnings.Add);

        service.ResetShardBearers();

        Assert.Empty(context.DestroyedPrefabs);
        Assert.Single(warnings);
    }

    sealed class StubVBloodEntityContext : IVBloodEntityContext
    {
        readonly List<PrefabGUID?> entries;
        int nextIndex;
        PrefabGUID? lastPrefabGuid;

        public StubVBloodEntityContext(IEnumerable<PrefabGUID?> prefabs)
        {
            entries = prefabs.ToList();
        }

        public bool ThrowOnEnumerate { get; init; }

        public List<PrefabGUID> DestroyedPrefabs { get; } = new();

        public IEnumerable<Entity> EnumerateVBloodEntities()
        {
            if (ThrowOnEnumerate)
            {
                throw new InvalidOperationException("Enumeration failed");
            }

            foreach (PrefabGUID? _ in entries)
            {
                yield return new Entity();
            }
        }

        public bool TryGetPrefabGuid(Entity entity, out PrefabGUID prefabGuid)
        {
            if (nextIndex >= entries.Count)
            {
                lastPrefabGuid = null;
                prefabGuid = default;
                return false;
            }

            PrefabGUID? value = entries[nextIndex++];
            if (value.HasValue)
            {
                lastPrefabGuid = value.Value;
                prefabGuid = value.Value;
                return true;
            }

            lastPrefabGuid = null;
            prefabGuid = default;
            return false;
        }

        public void Destroy(Entity entity)
        {
            if (lastPrefabGuid.HasValue)
            {
                DestroyedPrefabs.Add(lastPrefabGuid.Value);
            }
        }
    }
}
