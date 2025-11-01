using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Unity.Entities;

namespace Bloodcraft.Factory;

/// <summary>
/// Describes the component and option requirements for an <see cref="EntityQuery"/>.
/// </summary>
public sealed class QueryDescriptor
{
    /// <summary>
    /// Specifies the access level required when matching components.
    /// </summary>
    public enum AccessMode
    {
        /// <summary>
        /// Indicates that the component is accessed in a read-only manner.
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Indicates that the component is accessed with read-write permissions.
        /// </summary>
        ReadWrite
    }

    readonly List<ComponentRequirement> _allComponents = new();
    readonly List<Il2CppSystem.Type> _anyComponents = new();
    readonly List<Il2CppSystem.Type> _noneComponents = new();

    EntityQueryOptions _options;
    bool? _requireForUpdate;

    /// <summary>
    /// Creates a new descriptor instance.
    /// </summary>
    public static QueryDescriptor Create() => new();

    /// <summary>
    /// Adds a component that must be present on matching entities.
    /// </summary>
    /// <typeparam name="T">Component type to include.</typeparam>
    /// <param name="accessMode">Access mode required for the component.</param>
    public QueryDescriptor WithAll<T>(AccessMode accessMode = AccessMode.ReadOnly)
    {
        _allComponents.Add(new ComponentRequirement(Il2CppType.Of<T>(), accessMode));
        return this;
    }

    /// <summary>
    /// Adds a component that can satisfy an "any" requirement.
    /// </summary>
    /// <typeparam name="T">Component type that can match.</typeparam>
    public QueryDescriptor WithAny<T>()
    {
        _anyComponents.Add(Il2CppType.Of<T>());
        return this;
    }

    /// <summary>
    /// Adds a component that must be absent on matching entities.
    /// </summary>
    /// <typeparam name="T">Component type to exclude.</typeparam>
    public QueryDescriptor WithNone<T>()
    {
        _noneComponents.Add(Il2CppType.Of<T>());
        return this;
    }

    /// <summary>
    /// Marks the query to include disabled entities.
    /// </summary>
    /// <param name="include">Whether disabled entities should be included.</param>
    public QueryDescriptor IncludeDisabled(bool include = true) =>
        include
            ? WithOptions(EntityQueryOptions.IncludeDisabled)
            : WithoutOptions(EntityQueryOptions.IncludeDisabled);

    /// <summary>
    /// Marks the query to include entities tagged as spawned.
    /// </summary>
    /// <param name="include">Whether spawn-tagged entities should be included.</param>
    public QueryDescriptor IncludeSpawnTag(bool include = true) =>
        include
            ? WithOptions(EntityQueryOptions.IncludeSpawnTag)
            : WithoutOptions(EntityQueryOptions.IncludeSpawnTag);

    /// <summary>
    /// Marks the query to include system entities.
    /// </summary>
    /// <param name="include">Whether system entities should be included.</param>
    public QueryDescriptor IncludeSystems(bool include = true) =>
        include
            ? WithOptions(EntityQueryOptions.IncludeSystems)
            : WithoutOptions(EntityQueryOptions.IncludeSystems);

    /// <summary>
    /// Adds the supplied <see cref="EntityQueryOptions"/> flags to the descriptor.
    /// </summary>
    /// <param name="options">Flags to merge into the descriptor.</param>
    public QueryDescriptor WithOptions(EntityQueryOptions options)
    {
        if (options == 0)
            return this;

        _options |= options;
        return this;
    }

    private QueryDescriptor WithoutOptions(EntityQueryOptions options)
    {
        if (options == 0)
            return this;

        _options &= ~options;
        return this;
    }

    /// <summary>
    /// Sets whether the resulting query should be required for update.
    /// </summary>
    /// <param name="require">Whether to require the query for update.</param>
    public QueryDescriptor RequireForUpdate(bool require = true)
    {
        _requireForUpdate = require;
        return this;
    }

    /// <summary>
    /// Applies the descriptor configuration to the supplied builder.
    /// </summary>
    /// <param name="builder">Builder receiving the configuration.</param>
    public void Configure(ref EntityQueryBuilder builder)
    {
        for (int i = 0; i < _allComponents.Count; ++i)
        {
            builder.AddAll(CreateComponentType(_allComponents[i]));
        }

        for (int i = 0; i < _anyComponents.Count; ++i)
        {
            builder.AddAny(ComponentType.ReadOnly(_anyComponents[i]));
        }

        for (int i = 0; i < _noneComponents.Count; ++i)
        {
            builder.AddNone(ComponentType.ReadOnly(_noneComponents[i]));
        }

        if (_options != 0)
        {
            builder.WithOptions(_options);
        }
    }

    internal bool TryGetRequireForUpdate(out bool require)
    {
        if (_requireForUpdate.HasValue)
        {
            require = _requireForUpdate.Value;
            return true;
        }

        require = default;
        return false;
    }

    static ComponentType CreateComponentType(ComponentRequirement requirement) =>
        requirement.AccessMode switch
        {
            AccessMode.ReadOnly => ComponentType.ReadOnly(requirement.Type),
            AccessMode.ReadWrite => ComponentType.ReadWrite(requirement.Type),
            _ => throw new ArgumentOutOfRangeException(nameof(requirement.AccessMode), requirement.AccessMode, "Unsupported access mode.")
        };

    readonly struct ComponentRequirement
    {
        public ComponentRequirement(Il2CppSystem.Type type, AccessMode accessMode)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            AccessMode = accessMode;
        }

        public Il2CppSystem.Type Type { get; }

        public AccessMode AccessMode { get; }
    }
}
