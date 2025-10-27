using System;
using System.Collections.Generic;
using ProjectM;
using Unity.Entities;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the familiar imprisonment cleanup patch.
/// </summary>
public sealed class FamiliarImprisonmentWork : ISystemWork
{
    /// <summary>
    /// Delegate invoked when a component should be removed from the familiar target.
    /// </summary>
    /// <param name="target">Entity losing the component.</param>
    /// <param name="componentType">Component type being removed.</param>
    public delegate void ComponentRemovalAction(EntityHandle target, Type componentType);

    /// <summary>
    /// Delegate invoked when the familiar should be destroyed.
    /// </summary>
    /// <param name="target">Entity being destroyed.</param>
    public delegate void DestroyAction(EntityHandle target);

    /// <summary>
    /// Represents cached data associated with an imprisoned buff entry.
    /// </summary>
    /// <param name="Buff">Entity representing the buff instance.</param>
    /// <param name="Target">Entity targeted by the buff.</param>
    /// <param name="BuffHasBuffComponent">Indicates whether the buff entity has a <see cref="Buff"/> component.</param>
    /// <param name="TargetExists">Indicates whether the buff target exists.</param>
    /// <param name="TargetHasCharmSource">Indicates whether the target has a <see cref="CharmSource"/> component.</param>
    /// <param name="TargetHasBlockFeedBuff">Indicates whether the target has a <see cref="BlockFeedBuff"/> component.</param>
    /// <param name="TargetHasDisabled">Indicates whether the target has a <see cref="Disabled"/> component.</param>
    /// <param name="TargetHasMinion">Indicates whether the target has a <see cref="Minion"/> component.</param>
    public readonly record struct ImprisonedEntry(
        EntityHandle Buff,
        EntityHandle Target,
        bool BuffHasBuffComponent = true,
        bool TargetExists = true,
        bool TargetHasCharmSource = false,
        bool TargetHasBlockFeedBuff = false,
        bool TargetHasDisabled = false,
        bool TargetHasMinion = false);

    static QueryDescription CreateImprisonedQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<Buff>();
        builder.AddAllReadOnly<ImprisonedBuff>();
        builder.WithOptions(EntityQueryOptions.IncludeDisabled);
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription imprisonedQuery = CreateImprisonedQuery();

    readonly ComponentRemovalAction? componentRemovalHook;
    readonly DestroyAction? destroyHook;

    Dictionary<EntityHandle, ImprisonedEntry>? imprisonedEntries;
    List<EntityHandle>? entryOrder;
    List<(EntityHandle Entity, Type ComponentType)>? removalRecords;
    List<EntityHandle>? destroyedTargets;

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarImprisonmentWork"/> class.
    /// </summary>
    public FamiliarImprisonmentWork()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarImprisonmentWork"/> class.
    /// </summary>
    /// <param name="componentRemovalHook">Optional hook invoked when a component should be removed.</param>
    /// <param name="destroyHook">Optional hook invoked when the familiar should be destroyed.</param>
    public FamiliarImprisonmentWork(
        ComponentRemovalAction? componentRemovalHook,
        DestroyAction? destroyHook)
    {
        this.componentRemovalHook = componentRemovalHook;
        this.destroyHook = destroyHook;
        imprisonedEntries = null;
        entryOrder = null;
        removalRecords = null;
        destroyedTargets = null;
    }

    Dictionary<EntityHandle, ImprisonedEntry> EntryMap => imprisonedEntries ??= new();

    List<EntityHandle> EntryOrderList => entryOrder ??= new();

    List<(EntityHandle Entity, Type ComponentType)> RemovalRecordsList => removalRecords ??= new();

    List<EntityHandle> DestroyedTargetsList => destroyedTargets ??= new();

    /// <summary>
    /// Gets the imprisoned buff query used for iteration.
    /// </summary>
    public QueryDescription ImprisonedQuery => imprisonedQuery;

    /// <summary>
    /// Gets the cached imprisoned entries keyed by buff entity.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, ImprisonedEntry> ImprisonedEntries => EntryMap;

    /// <summary>
    /// Gets the iteration order of imprisoned buff entities.
    /// </summary>
    public IReadOnlyList<EntityHandle> ImprisonedOrder => EntryOrderList;

    /// <summary>
    /// Gets the recorded component removals executed during processing.
    /// </summary>
    public IReadOnlyList<(EntityHandle Entity, Type ComponentType)> RemovedComponents => RemovalRecordsList;

    /// <summary>
    /// Gets the familiar targets scheduled for destruction.
    /// </summary>
    public IReadOnlyList<EntityHandle> DestroyedTargets => DestroyedTargetsList;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<Buff>();
        builder.AddAllReadOnly<ImprisonedBuff>();
        builder.WithOptions(EntityQueryOptions.IncludeDisabled);
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetComponentLookup<Buff>(isReadOnly: true);
            _ = facade.GetComponentLookup<CharmSource>(isReadOnly: true);
            _ = facade.GetComponentLookup<BlockFeedBuff>(isReadOnly: false);
            _ = facade.GetComponentLookup<Disabled>(isReadOnly: false);
            _ = facade.GetComponentLookup<Minion>(isReadOnly: false);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        EnsureTrackingCollections();
        context.ForEachEntity(imprisonedQuery, ProcessImprisonedBuff);
    }

    void ProcessImprisonedBuff(EntityHandle buff)
    {
        if (!EntryMap.TryGetValue(buff, out var entry))
        {
            return;
        }

        if (!entry.BuffHasBuffComponent)
        {
            return;
        }

        if (!entry.TargetExists)
        {
            return;
        }

        if (entry.TargetHasCharmSource)
        {
            return;
        }

        if (!entry.TargetHasBlockFeedBuff)
        {
            return;
        }

        if (entry.TargetHasDisabled)
        {
            RecordComponentRemoval(entry.Target, typeof(Disabled));
        }

        if (entry.TargetHasMinion)
        {
            RecordComponentRemoval(entry.Target, typeof(Minion));
        }

        RecordComponentRemoval(entry.Target, typeof(BlockFeedBuff));
        RecordDestruction(entry.Target);
    }

    void RecordComponentRemoval(EntityHandle target, Type componentType)
    {
        RemovalRecordsList.Add((target, componentType));
        componentRemovalHook?.Invoke(target, componentType);
    }

    void RecordDestruction(EntityHandle target)
    {
        DestroyedTargetsList.Add(target);
        destroyHook?.Invoke(target);
    }

    void EnsureTrackingCollections()
    {
        _ = RemovalRecordsList;
        _ = DestroyedTargetsList;
    }

    /// <summary>
    /// Adds an imprisoned buff entry to be processed during the next tick.
    /// </summary>
    /// <param name="entry">Entry to add.</param>
    public void AddImprisonedEntry(ImprisonedEntry entry)
    {
        if (!EntryMap.ContainsKey(entry.Buff))
        {
            EntryOrderList.Add(entry.Buff);
        }

        EntryMap[entry.Buff] = entry;
    }

    /// <summary>
    /// Attempts to retrieve an imprisoned buff entry by buff entity handle.
    /// </summary>
    /// <param name="buff">Buff entity handle.</param>
    /// <param name="entry">Retrieved entry when available.</param>
    public bool TryGetImprisonedEntry(EntityHandle buff, out ImprisonedEntry entry)
    {
        return EntryMap.TryGetValue(buff, out entry);
    }
}
