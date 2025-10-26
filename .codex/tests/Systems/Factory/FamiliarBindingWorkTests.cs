using System.Collections.Generic;
using ProjectM;
using Xunit;
using FamiliarExperienceData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager.FamiliarExperienceData;
using FamiliarBuffsData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager.FamiliarBuffsData;
using FamiliarPrestigeData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager.FamiliarPrestigeData;
using FamiliarBattleGroupsData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBattleGroupsManager.FamiliarBattleGroupsData;
using static Bloodcraft.Systems.Familiars.FamiliarBindingSystem;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class FamiliarBindingWorkTests
{
    [Fact]
    public void DescribeQuery_DefinesGateAndBindingComponents()
    {
        var description = FactoryTestUtilities.DescribeQuery<FamiliarBindingWork>();

        Assert.Single(description.All);
        Assert.Equal(typeof(ProjectM.Network.FromCharacter), description.All[0].ElementType);
        Assert.Equal(ComponentAccessMode.ReadOnly, description.All[0].AccessMode);

        Assert.Collection(
            description.Any,
            requirement =>
            {
                Assert.Equal(typeof(FamiliarBindingWork.FamiliarBindingGate), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(FamiliarBindingWork.FamiliarBindingRequest), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void Queries_ExposeGateAndBindingDescriptors()
    {
        var work = FactoryTestUtilities.CreateWork<FamiliarBindingWork>();

        var gateQuery = work.GateQuery;
        Assert.True(gateQuery.RequireForUpdate);
        Assert.Collection(
            gateQuery.All,
            requirement =>
            {
                Assert.Equal(typeof(FamiliarBindingWork.FamiliarBindingGate), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(ProjectM.Network.FromCharacter), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });
        Assert.Empty(gateQuery.Any);
        Assert.Empty(gateQuery.None);

        var bindingQuery = work.BindingQuery;
        Assert.True(bindingQuery.RequireForUpdate);
        Assert.Collection(
            bindingQuery.All,
            requirement =>
            {
                Assert.Equal(typeof(FamiliarBindingWork.FamiliarBindingRequest), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(ProjectM.Network.FromCharacter), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(ProjectM.Network.User), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });
        Assert.Empty(bindingQuery.Any);
        Assert.Empty(bindingQuery.None);
    }

    [Fact]
    public void OnCreate_RegistersRequiredLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<FamiliarBindingWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(UnitStats) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(AbilityBar_Shared) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(AiMoveSpeeds) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(UnitLevel) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(Health) && !lookup.IsReadOnly);

        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(FactionReference) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(Team) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(TeamReference) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(Follower) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(Minion) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(EntityOwner) && !lookup.IsReadOnly);

        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(BlockFeedBuff) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(Buff) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(DynamicCollision) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(ServantConvertable) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(CharmSource) && !lookup.IsReadOnly);
        Assert.Contains(registrar.ComponentLookups, lookup => lookup.ElementType == typeof(CanPreventDisableWhenNoPlayersInRange) && !lookup.IsReadOnly);

        Assert.Contains(registrar.BufferLookups, lookup => lookup.ElementType == typeof(DropTableBuffer) && !lookup.IsReadOnly);
        Assert.Contains(registrar.BufferLookups, lookup => lookup.ElementType == typeof(AttachMapIconsToEntity) && !lookup.IsReadOnly);

        Assert.Contains(
            registrar.ComponentLookups,
            lookup => lookup.ElementType == typeof(FamiliarExperienceData));
        Assert.Contains(
            registrar.ComponentLookups,
            lookup => lookup.ElementType == typeof(FamiliarBuffsData));
        Assert.Contains(
            registrar.ComponentLookups,
            lookup => lookup.ElementType == typeof(FamiliarPrestigeData));
        Assert.Contains(
            registrar.ComponentLookups,
            lookup => lookup.ElementType == typeof(FamiliarBattleGroupsData));
    }

    [Fact]
    public void Tick_ProcessesBindingRequestsAndInvokesDelegates()
    {
        const ulong nonBattleId = 111UL;
        const ulong battleId = 222UL;
        const int nonBattleKey = 1001;
        const int battleKey = 2002;
        var experienceStore = new Dictionary<ulong, FamiliarExperienceData>
        {
            [nonBattleId] = new FamiliarExperienceData
            {
                FamiliarExperience = new Dictionary<int, KeyValuePair<int, float>>
                {
                    [nonBattleKey] = new KeyValuePair<int, float>(0, 0f)
                }
            },
            [battleId] = new FamiliarExperienceData
            {
                FamiliarExperience = new Dictionary<int, KeyValuePair<int, float>>
                {
                    [battleKey] = new KeyValuePair<int, float>(5, 123f)
                }
            }
        };

        var savedExperience = new List<(ulong SteamId, FamiliarExperienceData Data)>();

        FamiliarBindingWork.ExperienceLoader experienceLoader = id =>
        {
            return experienceStore.TryGetValue(id, out var data)
                ? data
                : new FamiliarExperienceData();
        };

        FamiliarBindingWork.ExperienceSaver experienceSaver = (id, data) =>
        {
            savedExperience.Add((id, data));
            experienceStore[id] = data;
        };

        var buffStore = new Dictionary<ulong, FamiliarBuffsData>
        {
            [nonBattleId] = new FamiliarBuffsData
            {
                FamiliarBuffs = new Dictionary<int, List<int>>
                {
                    [nonBattleKey] = new List<int> { 777 }
                }
            },
            [battleId] = new FamiliarBuffsData()
        };

        var buffLoaderCalls = new List<ulong>();
        FamiliarBindingWork.BuffLoader buffLoader = id =>
        {
            buffLoaderCalls.Add(id);
            return buffStore.TryGetValue(id, out var data)
                ? data
                : new FamiliarBuffsData();
        };

        var equipmentRecords = new List<(ulong SteamId, EntityHandle Servant, EntityHandle Familiar, int FamiliarId)>();
        FamiliarBindingWork.EquipmentBinder equipmentBinder = (steamId, servant, familiar, familiarId) =>
        {
            equipmentRecords.Add((steamId, servant, familiar, familiarId));
        };

        var statRecords = new List<(EntityHandle Familiar, int Level, ulong SteamId, int FamiliarKey, bool Battle)>();
        FamiliarBindingWork.StatRefreshDelegate statRefresher = (familiar, level, steamId, key, battle) =>
        {
            statRecords.Add((familiar, level, steamId, key, battle));
        };

        var matchRequests = new List<ulong>();
        FamiliarBindingWork.BattleMatchResolver matchResolver = (ulong steamId, out (ulong, ulong) matchPair) =>
        {
            matchRequests.Add(steamId);
            matchPair = (steamId, 333UL);
            return true;
        };

        var countdowns = new List<(ulong, ulong)>();
        FamiliarBindingWork.BattleCountdownDelegate countdownDelegate = matchPair => countdowns.Add(matchPair);

        var buffApplications = new List<(EntityHandle Familiar, int Buff)>();
        FamiliarBindingWork.BuffApplicator buffApplicator = (familiar, buff) => buffApplications.Add((familiar, buff));

        var work = new FamiliarBindingWork(
            experienceLoader: experienceLoader,
            experienceSaver: experienceSaver,
            buffLoader: buffLoader,
            equipmentBinder: equipmentBinder,
            statRefresher: statRefresher,
            matchResolver: matchResolver,
            battleCountdown: countdownDelegate,
            buffApplicator: buffApplicator);

        var nonBattleEvent = new FamiliarBindingWork.BindingEventData(
            EventEntity: new EntityHandle(1),
            PlayerCharacter: new EntityHandle(10),
            Familiar: new EntityHandle(100),
            Servant: new EntityHandle(200),
            FamiliarPrefabHash: nonBattleKey,
            FamiliarKey: nonBattleKey,
            SteamId: nonBattleId,
            Battle: false,
            TeamIndex: 0,
            Allies: false);

        var battleEvent = new FamiliarBindingWork.BindingEventData(
            EventEntity: new EntityHandle(2),
            PlayerCharacter: new EntityHandle(11),
            Familiar: new EntityHandle(101),
            Servant: new EntityHandle(0),
            FamiliarPrefabHash: battleKey,
            FamiliarKey: battleKey,
            SteamId: battleId,
            Battle: true,
            TeamIndex: 1,
            Allies: false);

        work.AddBindingEvent(nonBattleEvent);
        work.AddBindingEvent(battleEvent);

        work.PlayerBattleGroups[battleId] = new List<int> { battleKey };

        var requestedQueries = new List<QueryDescription>();
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            withTempEntities: (query, action) =>
            {
                requestedQueries.Add(query);
                if (ReferenceEquals(query, work.BindingQuery))
                {
                    action(work.GetBindingEventHandles());
                }
                else if (ReferenceEquals(query, work.GateQuery))
                {
                    action(Array.Empty<EntityHandle>());
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.GateQuery, requestedQueries);
        Assert.Contains(work.BindingQuery, requestedQueries);

        Assert.Equal(2, statRecords.Count);
        Assert.Contains(statRecords, record =>
            record.Familiar == nonBattleEvent.Familiar
            && record.Level == BASE_LEVEL
            && record.SteamId == nonBattleId
            && record.FamiliarKey == nonBattleKey
            && record.Battle == false);
        Assert.Contains(statRecords, record =>
            record.Familiar == battleEvent.Familiar
            && record.Level == 5
            && record.SteamId == battleId
            && record.FamiliarKey == battleKey
            && record.Battle);

        Assert.Single(equipmentRecords);
        Assert.Equal((nonBattleId, nonBattleEvent.Servant, nonBattleEvent.Familiar, nonBattleKey), equipmentRecords[0]);

        Assert.Single(matchRequests);
        Assert.Equal(battleId, matchRequests[0]);
        Assert.Single(countdowns);
        Assert.Equal((battleId, 333UL), countdowns[0]);

        Assert.NotEmpty(savedExperience);
        Assert.Contains(savedExperience, entry =>
            entry.SteamId == nonBattleId
            && entry.Data.FamiliarExperience.TryGetValue(nonBattleKey, out var xp)
            && xp.Key == BASE_LEVEL);

        Assert.Equal(new[] { nonBattleId, battleId }, buffLoaderCalls);
        Assert.Contains(buffApplications, application =>
            application.Familiar == nonBattleEvent.Familiar
            && application.Buff == 777);

        Assert.True(work.PlayerBattleFamiliars.TryGetValue(battleId, out var recordedFamiliars));
        Assert.Contains(battleEvent.Familiar, recordedFamiliars);
        Assert.True(work.PlayerBattleGroups.TryGetValue(battleId, out var battleGroup));
        Assert.Empty(battleGroup);
    }
}
