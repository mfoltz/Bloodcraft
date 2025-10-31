using System;
using Unity.Collections;
using Unity.Entities;

namespace Bloodcraft.Factory;

/// <summary>
/// Represents a managed wrapper around an <see cref="EntityQuery"/> with scoped utility helpers.
/// </summary>
public sealed class QueryHandle : IDisposable
{
    readonly Action<EntityQuery, Action<NativeArray<Entity>>> _withTempEntities;
    readonly Action<EntityQuery, Action<Entity>> _forEachEntity;
    readonly Action<EntityQuery, Action<NativeArray<ArchetypeChunk>>> _withTempChunks;
    readonly Action<EntityQuery, Action<ArchetypeChunk>> _forEachChunk;
    readonly bool _ownsQuery;

    EntityQuery _query;
    bool _isDisposed;

    internal QueryHandle(
        EntityQuery query,
        bool ownsQuery,
        Action<EntityQuery, Action<NativeArray<Entity>>> withTempEntities,
        Action<EntityQuery, Action<Entity>> forEachEntity,
        Action<EntityQuery, Action<NativeArray<ArchetypeChunk>>> withTempChunks,
        Action<EntityQuery, Action<ArchetypeChunk>> forEachChunk)
    {
        _query = query;
        _ownsQuery = ownsQuery;
        _withTempEntities = withTempEntities ?? throw new ArgumentNullException(nameof(withTempEntities));
        _forEachEntity = forEachEntity ?? throw new ArgumentNullException(nameof(forEachEntity));
        _withTempChunks = withTempChunks ?? throw new ArgumentNullException(nameof(withTempChunks));
        _forEachChunk = forEachChunk ?? throw new ArgumentNullException(nameof(forEachChunk));
    }

    /// <summary>
    /// Gets the wrapped <see cref="EntityQuery"/> instance.
    /// </summary>
    public EntityQuery Query
    {
        get
        {
            EnsureNotDisposed();
            return _query;
        }
    }

    /// <summary>
    /// Indicates whether the handle has been disposed.
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// Executes the supplied action with a temporary entity array derived from the query.
    /// </summary>
    /// <param name="action">Action executed with the temporary entity array.</param>
    public void WithTempEntities(Action<NativeArray<Entity>> action)
    {
        EnsureNotDisposed();
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _withTempEntities(_query, action);
    }

    /// <summary>
    /// Iterates each entity contained in the query.
    /// </summary>
    /// <param name="action">Action invoked for every entity.</param>
    public void ForEachEntity(Action<Entity> action)
    {
        EnsureNotDisposed();
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _forEachEntity(_query, action);
    }

    /// <summary>
    /// Executes the supplied action with a temporary chunk array derived from the query.
    /// </summary>
    /// <param name="action">Action executed with the temporary chunk array.</param>
    public void WithTempChunks(Action<NativeArray<ArchetypeChunk>> action)
    {
        EnsureNotDisposed();
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _withTempChunks(_query, action);
    }

    /// <summary>
    /// Iterates each chunk contained in the query.
    /// </summary>
    /// <param name="action">Action invoked for every chunk.</param>
    public void ForEachChunk(Action<ArchetypeChunk> action)
    {
        EnsureNotDisposed();
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        _forEachChunk(_query, action);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        if (_ownsQuery && _query != default)
        {
            _query.Dispose();
            _query = default;
        }
    }

    void EnsureNotDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(QueryHandle));
    }
}
