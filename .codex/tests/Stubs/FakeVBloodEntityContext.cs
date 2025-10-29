using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Bloodcraft.Interfaces;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace Bloodcraft.Tests.Stubs;

/// <summary>
/// Provides a configurable <see cref="IVBloodEntityContext"/> for exercising entity workflows.
/// </summary>
public sealed class FakeVBloodEntityContext : IVBloodEntityContext
{
    readonly IReadOnlyList<FakeVBloodEntityRow> rows;
    readonly IReadOnlyDictionary<Entity, FakeVBloodEntityRow> lookup;

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeVBloodEntityContext"/> class.
    /// </summary>
    /// <param name="rows">The scripted rows to expose from the fake context.</param>
    public FakeVBloodEntityContext(IEnumerable<FakeVBloodEntityRow> rows)
    {
        if (rows is null)
        {
            throw new ArgumentNullException(nameof(rows));
        }

        this.rows = new ReadOnlyCollection<FakeVBloodEntityRow>(rows.Select(row =>
        {
            if (row is null)
            {
                throw new ArgumentException("Rows cannot contain null entries.", nameof(rows));
            }

            return row;
        }).ToList());

        lookup = this.rows.ToDictionary(row => row.Entity);
    }

    /// <summary>
    /// Gets a value indicating whether enumeration should throw.
    /// </summary>
    public bool ThrowOnEnumerate { get; set; }

    /// <summary>
    /// Gets the scripted rows exposed by this context.
    /// </summary>
    public IReadOnlyList<FakeVBloodEntityRow> Rows => rows;

    /// <inheritdoc />
    public IEnumerable<Entity> EnumerateVBloodEntities()
    {
        if (ThrowOnEnumerate)
        {
            throw new InvalidOperationException("Enumeration failed");
        }

        foreach (FakeVBloodEntityRow row in rows)
        {
            yield return row.Entity;
        }
    }

    /// <inheritdoc />
    public bool TryGetPrefabGuid(Entity entity, out PrefabGUID prefabGuid)
    {
        if (lookup.TryGetValue(entity, out FakeVBloodEntityRow? row))
        {
            if (row?.PrefabGuid is PrefabGUID prefab)
            {
                prefabGuid = prefab;
                return true;
            }
        }

        prefabGuid = default;
        return false;
    }

    /// <inheritdoc />
    public void Destroy(Entity entity)
    {
        if (lookup.TryGetValue(entity, out FakeVBloodEntityRow? row))
        {
            row?.MarkDestroyed();
        }
    }

    /// <summary>
    /// Creates a fake context from the provided prefab GUIDs.
    /// </summary>
    /// <param name="prefabs">The prefab GUIDs that should be exposed by the context.</param>
    /// <returns>A new <see cref="FakeVBloodEntityContext"/> containing the provided prefabs.</returns>
    public static FakeVBloodEntityContext FromPrefabs(IEnumerable<PrefabGUID?> prefabs)
    {
        if (prefabs is null)
        {
            throw new ArgumentNullException(nameof(prefabs));
        }

        FakeVBloodEntityRow[] rows = prefabs.Select(FakeVBloodEntityRow.FromPrefab).ToArray();
        return new FakeVBloodEntityContext(rows);
    }
}

/// <summary>
/// Represents a scripted VBlood entity row.
/// </summary>
public sealed class FakeVBloodEntityRow
{
    static int nextEntityIndex;

    FakeVBloodEntityRow(Entity entity, PrefabGUID? prefabGuid)
    {
        Entity = entity;
        PrefabGuid = prefabGuid;
    }

    /// <summary>
    /// Gets the entity associated with this row.
    /// </summary>
    public Entity Entity { get; }

    /// <summary>
    /// Gets the prefab GUID assigned to this row, if any.
    /// </summary>
    public PrefabGUID? PrefabGuid { get; }

    /// <summary>
    /// Gets a value indicating whether this row's entity was marked for destruction.
    /// </summary>
    public bool Destroyed { get; private set; }

    /// <summary>
    /// Creates a row for the provided prefab.
    /// </summary>
    /// <param name="prefabGuid">The prefab GUID to associate with the row.</param>
    /// <returns>A new <see cref="FakeVBloodEntityRow"/>.</returns>
    public static FakeVBloodEntityRow FromPrefab(PrefabGUID? prefabGuid)
    {
        int index = Interlocked.Increment(ref nextEntityIndex);
        var entity = new Entity { Index = index, Version = 1 };
        return new FakeVBloodEntityRow(entity, prefabGuid);
    }

    /// <summary>
    /// Marks the row as destroyed.
    /// </summary>
    internal void MarkDestroyed()
    {
        Destroyed = true;
    }
}
