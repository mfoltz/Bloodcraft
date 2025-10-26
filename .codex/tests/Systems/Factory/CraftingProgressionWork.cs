using System;
using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the crafting progression patches.
/// </summary>
public sealed class CraftingProgressionWork : ISystemWork
{
    /// <summary>
    /// Supplies clan member details associated with a workstation owner.
    /// </summary>
    /// <param name="context">Context describing the owner and clan entity.</param>
    public delegate IEnumerable<ClanMemberData>? ClanMemberSource(ClanContext context);

    /// <summary>
    /// Supplies crafting job trackers for the specified player and workstation.
    /// </summary>
    /// <param name="steamId">Player identifier associated with the tracker.</param>
    /// <param name="station">Workstation handle owning the tracker.</param>
    public delegate IDictionary<PrefabGUID, int>? JobTrackerSource(ulong steamId, EntityHandle station);

    /// <summary>
    /// Delegate invoked when quest progress should be recorded.
    /// </summary>
    /// <param name="steamId">Player identifier receiving the progress.</param>
    /// <param name="item">Crafted item prefab.</param>
    /// <param name="amount">Number of crafted items.</param>
    public delegate void QuestProgressDelegate(ulong steamId, PrefabGUID item, int amount);

    /// <summary>
    /// Delegate invoked when profession experience should be recorded.
    /// </summary>
    /// <param name="steamId">Player identifier receiving the experience.</param>
    /// <param name="item">Crafted item prefab.</param>
    /// <param name="experience">Amount of profession experience to award.</param>
    public delegate void ProfessionProgressDelegate(ulong steamId, PrefabGUID item, float experience);

    /// <summary>
    /// Describes the contextual information supplied to clan member sources.
    /// </summary>
    /// <param name="ClanEntity">Clan entity associated with the owner.</param>
    /// <param name="OwnerSteamId">Steam identifier for the owner.</param>
    /// <param name="OwnerUser">User entity representing the owner.</param>
    public readonly record struct ClanContext(
        EntityHandle ClanEntity,
        ulong OwnerSteamId,
        EntityHandle OwnerUser);

    /// <summary>
    /// Represents a clan member associated with the crafting job validation pipeline.
    /// </summary>
    /// <param name="SteamId">Steam identifier for the member.</param>
    /// <param name="UserEntity">Entity handle representing the user.</param>
    public readonly record struct ClanMemberData(ulong SteamId, EntityHandle UserEntity);

    /// <summary>
    /// Represents cached data for an obtained inventory event.
    /// </summary>
    /// <param name="EventEntity">Entity handle associated with the event.</param>
    /// <param name="Inventory">Inventory entity that received the item.</param>
    /// <param name="InventoryOwner">Workstation entity owning the inventory.</param>
    /// <param name="ClanEntity">Clan entity associated with the owner.</param>
    /// <param name="OwnerUser">User entity representing the owner.</param>
    /// <param name="ItemPrefab">Prefab of the crafted item.</param>
    /// <param name="ItemEntity">Entity handle representing the crafted item.</param>
    /// <param name="Amount">Quantity obtained.</param>
    /// <param name="OwnerSteamId">Steam identifier for the owner.</param>
    /// <param name="ProfessionExperience">Profession experience associated with the craft.</param>
    public readonly record struct InventoryObtainedEventData(
        EntityHandle EventEntity,
        EntityHandle Inventory,
        EntityHandle InventoryOwner,
        EntityHandle ClanEntity,
        EntityHandle OwnerUser,
        PrefabGUID ItemPrefab,
        EntityHandle ItemEntity,
        int Amount,
        ulong OwnerSteamId,
        float ProfessionExperience);

    /// <summary>
    /// Represents cached data for a crafting station progress entry.
    /// </summary>
    /// <param name="EventEntity">Entity handle associated with the progress event.</param>
    /// <param name="Station">Station entity validating the craft.</param>
    /// <param name="SteamId">Steam identifier linked to the craft.</param>
    /// <param name="ItemPrefab">Item prefab being crafted.</param>
    /// <param name="Completed">Indicates whether the craft finished.</param>
    public readonly record struct CraftingStationEventData(
        EntityHandle EventEntity,
        EntityHandle Station,
        ulong SteamId,
        PrefabGUID ItemPrefab,
        bool Completed);

    static QueryDescription CreateInventoryQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<InventoryChangedEvent>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateForgeQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<Forge_Shared>();
        builder.AddAllReadOnly<UserOwner>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateWorkstationQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<CastleWorkstation>();
        builder.AddAllReadOnly<QueuedWorkstationCraftAction>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreatePrisonQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<CastleWorkstation>();
        builder.AddAllReadOnly<PrisonCell>();
        builder.AddAllReadOnly<QueuedWorkstationCraftAction>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription inventoryObtainedQuery = CreateInventoryQuery();
    static readonly QueryDescription forgeProgressQuery = CreateForgeQuery();
    static readonly QueryDescription workstationProgressQuery = CreateWorkstationQuery();
    static readonly QueryDescription prisonCraftingQuery = CreatePrisonQuery();

    ClanMemberSource? clanMemberSource;
    JobTrackerSource? pendingJobsSource;
    JobTrackerSource? validatedJobsSource;
    QuestProgressDelegate? questProgress;
    ProfessionProgressDelegate? professionProgress;

    Dictionary<EntityHandle, InventoryObtainedEventData>? inventoryEvents;
    Dictionary<EntityHandle, CraftingStationEventData>? forgeEvents;
    Dictionary<EntityHandle, CraftingStationEventData>? workstationEvents;
    Dictionary<EntityHandle, CraftingStationEventData>? prisonEvents;

    List<EntityHandle>? inventoryHandles;
    List<EntityHandle>? forgeHandles;
    List<EntityHandle>? workstationHandles;
    List<EntityHandle>? prisonHandles;
    List<ClanMemberData>? clanBuffer;

    /// <summary>
    /// Initializes a new instance of the <see cref="CraftingProgressionWork"/> class.
    /// </summary>
    public CraftingProgressionWork()
        : this(null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CraftingProgressionWork"/> class.
    /// </summary>
    /// <param name="clanMemberSource">Optional clan member source.</param>
    /// <param name="pendingJobsSource">Optional pending job source.</param>
    /// <param name="validatedJobsSource">Optional validated job source.</param>
    /// <param name="questProgress">Optional quest progress delegate.</param>
    /// <param name="professionProgress">Optional profession progress delegate.</param>
    public CraftingProgressionWork(
        ClanMemberSource? clanMemberSource = null,
        JobTrackerSource? pendingJobsSource = null,
        JobTrackerSource? validatedJobsSource = null,
        QuestProgressDelegate? questProgress = null,
        ProfessionProgressDelegate? professionProgress = null)
    {
        this.clanMemberSource = clanMemberSource;
        this.pendingJobsSource = pendingJobsSource;
        this.validatedJobsSource = validatedJobsSource;
        this.questProgress = questProgress;
        this.professionProgress = professionProgress;

        inventoryEvents = null;
        forgeEvents = null;
        workstationEvents = null;
        prisonEvents = null;

        inventoryHandles = null;
        forgeHandles = null;
        workstationHandles = null;
        prisonHandles = null;
        clanBuffer = null;
    }

    /// <summary>
    /// Gets the query describing inventory obtained events.
    /// </summary>
    public QueryDescription InventoryObtainedQuery => inventoryObtainedQuery;

    /// <summary>
    /// Gets the query describing forge progress entities.
    /// </summary>
    public QueryDescription ForgeProgressQuery => forgeProgressQuery;

    /// <summary>
    /// Gets the query describing workstation progress entities.
    /// </summary>
    public QueryDescription WorkstationProgressQuery => workstationProgressQuery;

    /// <summary>
    /// Gets the query describing prison crafting entities.
    /// </summary>
    public QueryDescription PrisonCraftingQuery => prisonCraftingQuery;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<InventoryChangedEvent>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetEntityTypeHandle();
            _ = facade.GetEntityStorageInfoLookup();
            _ = facade.GetComponentLookup<InventoryConnection>(isReadOnly: true);
            _ = facade.GetComponentLookup<UserOwner>(isReadOnly: true);
            _ = facade.GetComponentLookup<CastleWorkstation>(isReadOnly: true);
            _ = facade.GetComponentLookup<Forge_Shared>(isReadOnly: true);
            _ = facade.GetComponentLookup<PrisonCell>(isReadOnly: true);
            _ = facade.GetBufferLookup<QueuedWorkstationCraftAction>(isReadOnly: true);
            _ = facade.GetBufferLookup<SyncToUserBuffer>(isReadOnly: true);
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        context.WithTempEntities(forgeProgressQuery, ProcessForgeEvents);
        context.WithTempEntities(workstationProgressQuery, ProcessWorkstationEvents);
        context.WithTempEntities(prisonCraftingQuery, ProcessPrisonEvents);
        context.WithTempEntities(inventoryObtainedQuery, ProcessInventoryEvents);
    }

    /// <summary>
    /// Adds cached data for an inventory obtained event.
    /// </summary>
    /// <param name="eventData">Event data to cache.</param>
    public void AddInventoryEvent(InventoryObtainedEventData eventData)
    {
        inventoryEvents ??= new Dictionary<EntityHandle, InventoryObtainedEventData>();
        inventoryEvents[eventData.EventEntity] = eventData;
    }

    /// <summary>
    /// Adds cached data for a forge progress event.
    /// </summary>
    /// <param name="eventData">Event data to cache.</param>
    public void AddForgeEvent(CraftingStationEventData eventData)
    {
        forgeEvents ??= new Dictionary<EntityHandle, CraftingStationEventData>();
        forgeEvents[eventData.EventEntity] = eventData;
    }

    /// <summary>
    /// Adds cached data for a workstation progress event.
    /// </summary>
    /// <param name="eventData">Event data to cache.</param>
    public void AddWorkstationEvent(CraftingStationEventData eventData)
    {
        workstationEvents ??= new Dictionary<EntityHandle, CraftingStationEventData>();
        workstationEvents[eventData.EventEntity] = eventData;
    }

    /// <summary>
    /// Adds cached data for a prison progress event.
    /// </summary>
    /// <param name="eventData">Event data to cache.</param>
    public void AddPrisonEvent(CraftingStationEventData eventData)
    {
        prisonEvents ??= new Dictionary<EntityHandle, CraftingStationEventData>();
        prisonEvents[eventData.EventEntity] = eventData;
    }

    /// <summary>
    /// Gets the cached inventory obtained handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetInventoryEventHandles()
    {
        if (inventoryEvents == null || inventoryEvents.Count == 0)
            return Array.Empty<EntityHandle>();

        inventoryHandles ??= new List<EntityHandle>();
        inventoryHandles.Clear();
        foreach (var handle in inventoryEvents.Keys)
        {
            inventoryHandles.Add(handle);
        }

        return inventoryHandles;
    }

    /// <summary>
    /// Gets the cached forge progress handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetForgeEventHandles()
    {
        if (forgeEvents == null || forgeEvents.Count == 0)
            return Array.Empty<EntityHandle>();

        forgeHandles ??= new List<EntityHandle>();
        forgeHandles.Clear();
        foreach (var handle in forgeEvents.Keys)
        {
            forgeHandles.Add(handle);
        }

        return forgeHandles;
    }

    /// <summary>
    /// Gets the cached workstation progress handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetWorkstationEventHandles()
    {
        if (workstationEvents == null || workstationEvents.Count == 0)
            return Array.Empty<EntityHandle>();

        workstationHandles ??= new List<EntityHandle>();
        workstationHandles.Clear();
        foreach (var handle in workstationEvents.Keys)
        {
            workstationHandles.Add(handle);
        }

        return workstationHandles;
    }

    /// <summary>
    /// Gets the cached prison progress handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetPrisonEventHandles()
    {
        if (prisonEvents == null || prisonEvents.Count == 0)
            return Array.Empty<EntityHandle>();

        prisonHandles ??= new List<EntityHandle>();
        prisonHandles.Clear();
        foreach (var handle in prisonEvents.Keys)
        {
            prisonHandles.Add(handle);
        }

        return prisonHandles;
    }

    void ProcessForgeEvents(IReadOnlyList<EntityHandle> handles)
    {
        ProcessStations(handles, forgeEvents);
    }

    void ProcessWorkstationEvents(IReadOnlyList<EntityHandle> handles)
    {
        ProcessStations(handles, workstationEvents);
    }

    void ProcessPrisonEvents(IReadOnlyList<EntityHandle> handles)
    {
        ProcessStations(handles, prisonEvents);
    }

    void ProcessStations(IReadOnlyList<EntityHandle> handles, Dictionary<EntityHandle, CraftingStationEventData>? store)
    {
        if (handles == null || store == null)
            return;

        for (int i = 0; i < handles.Count; i++)
        {
            var handle = handles[i];
            if (!store.TryGetValue(handle, out var eventData))
                continue;

            if (!eventData.Completed)
                continue;

            if (pendingJobsSource == null || validatedJobsSource == null)
                continue;

            var pending = pendingJobsSource(eventData.SteamId, eventData.Station);
            if (pending == null)
                continue;

            if (!pending.TryGetValue(eventData.ItemPrefab, out var pendingCount) || pendingCount <= 0)
                continue;

            pendingCount--;
            if (pendingCount <= 0)
            {
                pending.Remove(eventData.ItemPrefab);
            }
            else
            {
                pending[eventData.ItemPrefab] = pendingCount;
            }

            var validated = validatedJobsSource(eventData.SteamId, eventData.Station);
            if (validated == null)
                continue;

            if (!validated.TryGetValue(eventData.ItemPrefab, out var validatedCount))
            {
                validatedCount = 0;
            }

            validated[eventData.ItemPrefab] = validatedCount + 1;
        }
    }

    void ProcessInventoryEvents(IReadOnlyList<EntityHandle> handles)
    {
        if (handles == null || inventoryEvents == null)
            return;

        for (int i = 0; i < handles.Count; i++)
        {
            var handle = handles[i];
            if (!inventoryEvents.TryGetValue(handle, out var eventData))
                continue;

            var members = ResolveClanMembers(eventData);
            if (members.Count == 0)
                continue;

            if (validatedJobsSource == null)
                continue;

            for (int memberIndex = 0; memberIndex < members.Count; memberIndex++)
            {
                var member = members[memberIndex];
                var validated = validatedJobsSource(member.SteamId, eventData.InventoryOwner);
                if (validated == null)
                    continue;

                if (!validated.TryGetValue(eventData.ItemPrefab, out var jobs) || jobs <= 0)
                    continue;

                jobs--;
                if (jobs <= 0)
                {
                    validated.Remove(eventData.ItemPrefab);
                }
                else
                {
                    validated[eventData.ItemPrefab] = jobs;
                }

                questProgress?.Invoke(member.SteamId, eventData.ItemPrefab, eventData.Amount);
                professionProgress?.Invoke(member.SteamId, eventData.ItemPrefab, eventData.ProfessionExperience);
            }
        }
    }

    IReadOnlyList<ClanMemberData> ResolveClanMembers(in InventoryObtainedEventData eventData)
    {
        clanBuffer ??= new List<ClanMemberData>();
        clanBuffer.Clear();

        if (clanMemberSource != null)
        {
            var context = new ClanContext(eventData.ClanEntity, eventData.OwnerSteamId, eventData.OwnerUser);
            var members = clanMemberSource(context);
            if (members != null)
            {
                foreach (var member in members)
                {
                    if (!ContainsClanMember(member.SteamId))
                    {
                        clanBuffer.Add(member);
                    }
                }
            }
        }

        if (!ContainsClanMember(eventData.OwnerSteamId))
        {
            clanBuffer.Add(new ClanMemberData(eventData.OwnerSteamId, eventData.OwnerUser));
        }

        return clanBuffer;
    }

    bool ContainsClanMember(ulong steamId)
    {
        if (clanBuffer == null)
            return false;

        for (int i = 0; i < clanBuffer.Count; i++)
        {
            if (clanBuffer[i].SteamId == steamId)
                return true;
        }

        return false;
    }
}
