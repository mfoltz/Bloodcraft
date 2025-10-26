using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class CraftingProgressionWorkTests
{
    [Fact]
    public void DescribeQuery_RequiresInventoryChangedEvent()
    {
        var description = FactoryTestUtilities.DescribeQuery<CraftingProgressionWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(InventoryChangedEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void Queries_ExposeForgeWorkstationAndPrisonCompositions()
    {
        var work = new CraftingProgressionWork();

        var forgeQuery = work.ForgeProgressQuery;
        Assert.Collection(
            forgeQuery.All,
            requirement =>
            {
                Assert.Equal(typeof(Forge_Shared), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(UserOwner), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });
        Assert.Empty(forgeQuery.Any);
        Assert.Empty(forgeQuery.None);
        Assert.Equal(EntityQueryOptions.None, forgeQuery.Options);
        Assert.True(forgeQuery.RequireForUpdate);

        var workstationQuery = work.WorkstationProgressQuery;
        Assert.Collection(
            workstationQuery.All,
            requirement =>
            {
                Assert.Equal(typeof(CastleWorkstation), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(QueuedWorkstationCraftAction), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });
        Assert.Empty(workstationQuery.Any);
        Assert.Empty(workstationQuery.None);
        Assert.Equal(EntityQueryOptions.None, workstationQuery.Options);
        Assert.True(workstationQuery.RequireForUpdate);

        var prisonQuery = work.PrisonCraftingQuery;
        Assert.Collection(
            prisonQuery.All,
            requirement =>
            {
                Assert.Equal(typeof(CastleWorkstation), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(PrisonCell), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(QueuedWorkstationCraftAction), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });
        Assert.Empty(prisonQuery.Any);
        Assert.Empty(prisonQuery.None);
        Assert.Equal(EntityQueryOptions.None, prisonQuery.Options);
        Assert.True(prisonQuery.RequireForUpdate);
    }

    [Fact]
    public void OnCreate_RegistersLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<CraftingProgressionWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Equal(1, registrar.EntityTypeHandleRequests);
        Assert.Equal(1, registrar.EntityStorageInfoLookupRequests);

        Assert.Collection(
            registrar.ComponentLookups,
            lookup =>
            {
                Assert.Equal(typeof(InventoryConnection), lookup.ElementType);
                Assert.True(lookup.IsReadOnly);
            },
            lookup =>
            {
                Assert.Equal(typeof(UserOwner), lookup.ElementType);
                Assert.True(lookup.IsReadOnly);
            },
            lookup =>
            {
                Assert.Equal(typeof(CastleWorkstation), lookup.ElementType);
                Assert.True(lookup.IsReadOnly);
            },
            lookup =>
            {
                Assert.Equal(typeof(Forge_Shared), lookup.ElementType);
                Assert.True(lookup.IsReadOnly);
            },
            lookup =>
            {
                Assert.Equal(typeof(PrisonCell), lookup.ElementType);
                Assert.True(lookup.IsReadOnly);
            });

        Assert.Collection(
            registrar.BufferLookups,
            lookup =>
            {
                Assert.Equal(typeof(QueuedWorkstationCraftAction), lookup.ElementType);
                Assert.True(lookup.IsReadOnly);
            },
            lookup =>
            {
                Assert.Equal(typeof(SyncToUserBuffer), lookup.ElementType);
                Assert.True(lookup.IsReadOnly);
            });

        Assert.Empty(registrar.ComponentTypeHandles);
        Assert.Empty(registrar.BufferTypeHandles);
    }

    [Fact]
    public void OnUpdate_PropagatesValidatedJobsToDelegates()
    {
        var stationHandle = new EntityHandle(50);
        var clanEntity = new EntityHandle(60);
        var ownerUser = new EntityHandle(70);
        var inventoryHandle = new EntityHandle(80);
        var itemEntity = new EntityHandle(90);
        var forgeEventHandle = new EntityHandle(1);
        var inventoryEventHandle = new EntityHandle(2);
        var ownerSteamId = 111UL;
        var clanMateSteamId = 222UL;
        var questCalls = new List<(ulong SteamId, PrefabGUID Item, int Amount)>();
        var professionCalls = new List<(ulong SteamId, PrefabGUID Item, float Experience)>();
        PrefabGUID itemPrefab = default;

        var pendingJobs = new Dictionary<(ulong SteamId, EntityHandle Station), IDictionary<PrefabGUID, int>>
        {
            [(ownerSteamId, stationHandle)] = new Dictionary<PrefabGUID, int>
            {
                [itemPrefab] = 1,
            },
        };

        var validatedJobs = new Dictionary<(ulong SteamId, EntityHandle Station), IDictionary<PrefabGUID, int>>();

        IEnumerable<CraftingProgressionWork.ClanMemberData>? ClanResolver(CraftingProgressionWork.ClanContext context)
        {
            return new[]
            {
                new CraftingProgressionWork.ClanMemberData(context.OwnerSteamId, context.OwnerUser),
                new CraftingProgressionWork.ClanMemberData(clanMateSteamId, new EntityHandle(500)),
            };
        }

        IDictionary<PrefabGUID, int>? PendingSource(ulong steamId, EntityHandle station)
        {
            pendingJobs.TryGetValue((steamId, station), out var jobs);
            return jobs;
        }

        IDictionary<PrefabGUID, int>? ValidatedSource(ulong steamId, EntityHandle station)
        {
            if (!validatedJobs.TryGetValue((steamId, station), out var jobs))
            {
                jobs = new Dictionary<PrefabGUID, int>();
                validatedJobs[(steamId, station)] = jobs;
            }

            return jobs;
        }

        var work = new CraftingProgressionWork(
            clanMemberSource: ClanResolver,
            pendingJobsSource: PendingSource,
            validatedJobsSource: ValidatedSource,
            questProgress: (steamId, item, amount) => questCalls.Add((steamId, item, amount)),
            professionProgress: (steamId, item, experience) => professionCalls.Add((steamId, item, experience)));

        work.AddForgeEvent(new CraftingProgressionWork.CraftingStationEventData(
            EventEntity: forgeEventHandle,
            Station: stationHandle,
            SteamId: ownerSteamId,
            ItemPrefab: itemPrefab,
            Completed: true));

        work.AddInventoryEvent(new CraftingProgressionWork.InventoryObtainedEventData(
            EventEntity: inventoryEventHandle,
            Inventory: inventoryHandle,
            InventoryOwner: stationHandle,
            ClanEntity: clanEntity,
            OwnerUser: ownerUser,
            ItemPrefab: itemPrefab,
            ItemEntity: itemEntity,
            Amount: 1,
            OwnerSteamId: ownerSteamId,
            ProfessionExperience: 75f));

        var registrar = new RecordingRegistrar();
        var requestedQueries = new List<QueryDescription>();

        var context = FactoryTestUtilities.CreateContext(
            registrar,
            withTempEntities: (query, action) =>
            {
                requestedQueries.Add(query);

                if (ReferenceEquals(query, work.ForgeProgressQuery))
                {
                    action(work.GetForgeEventHandles());
                }
                else if (ReferenceEquals(query, work.WorkstationProgressQuery))
                {
                    action(work.GetWorkstationEventHandles());
                }
                else if (ReferenceEquals(query, work.PrisonCraftingQuery))
                {
                    action(work.GetPrisonEventHandles());
                }
                else if (ReferenceEquals(query, work.InventoryObtainedQuery))
                {
                    action(work.GetInventoryEventHandles());
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.ForgeProgressQuery, requestedQueries);
        Assert.Contains(work.WorkstationProgressQuery, requestedQueries);
        Assert.Contains(work.PrisonCraftingQuery, requestedQueries);
        Assert.Contains(work.InventoryObtainedQuery, requestedQueries);

        var pending = PendingSource(ownerSteamId, stationHandle);
        Assert.NotNull(pending);
        Assert.False(pending!.ContainsKey(itemPrefab));

        var validated = ValidatedSource(ownerSteamId, stationHandle);
        Assert.NotNull(validated);
        Assert.False(validated!.ContainsKey(itemPrefab));

        Assert.Equal(new[]
        {
            (ownerSteamId, itemPrefab, 1),
        }, questCalls);

        Assert.Equal(new[]
        {
            (ownerSteamId, itemPrefab, 75f),
        }, professionCalls);
    }
}
