using System;
using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using static Bloodcraft.Systems.Familiars.FamiliarBindingSystem;
using static Bloodcraft.Utilities.Progression;
using FamiliarExperienceData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarExperienceManager.FamiliarExperienceData;
using FamiliarBuffsData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBuffsManager.FamiliarBuffsData;
using FamiliarPrestigeData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarPrestigeManager.FamiliarPrestigeData;
using FamiliarBattleGroupsData = Bloodcraft.Services.DataService.FamiliarPersistence.FamiliarBattleGroupsManager.FamiliarBattleGroupsData;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition that mirrors the familiar binding orchestration flow.
/// </summary>
internal struct DropTableBuffer
{
}

/// <summary>
/// Provides a test work definition that mirrors the familiar binding orchestration flow.
/// </summary>
internal sealed class FamiliarBindingWork : ISystemWork
{
    /// <summary>
    /// Delegate used to load familiar experience data for a player.
    /// </summary>
    /// <param name="steamId">Player identifier whose data should be retrieved.</param>
    public delegate FamiliarExperienceData ExperienceLoader(ulong steamId);

    /// <summary>
    /// Delegate used to persist familiar experience data for a player.
    /// </summary>
    /// <param name="steamId">Player identifier whose data should be saved.</param>
    /// <param name="data">Experience payload being persisted.</param>
    public delegate void ExperienceSaver(ulong steamId, FamiliarExperienceData data);

    /// <summary>
    /// Delegate used to load familiar buff data for a player.
    /// </summary>
    /// <param name="steamId">Player identifier whose buff data should be retrieved.</param>
    public delegate FamiliarBuffsData BuffLoader(ulong steamId);

    /// <summary>
    /// Delegate used to apply persisted familiar equipment and stat packages.
    /// </summary>
    /// <param name="steamId">Player identifier owning the familiar.</param>
    /// <param name="servant">Servant entity paired with the familiar.</param>
    /// <param name="familiar">Bound familiar entity.</param>
    /// <param name="familiarId">Identifier of the familiar prefab.</param>
    public delegate void EquipmentBinder(ulong steamId, EntityHandle servant, EntityHandle familiar, int familiarId);

    /// <summary>
    /// Delegate used to refresh familiar statistics once persistence data has been resolved.
    /// </summary>
    /// <param name="familiar">Bound familiar entity.</param>
    /// <param name="level">Resolved familiar level.</param>
    /// <param name="steamId">Owning player identifier.</param>
    /// <param name="familiarKey">Identifier of the familiar prefab.</param>
    /// <param name="battle">Indicates whether the binding originated from a battle.</param>
    public delegate void StatRefreshDelegate(EntityHandle familiar, int level, ulong steamId, int familiarKey, bool battle);

    /// <summary>
    /// Delegate used to resolve battle matchmaking pairs for familiars.
    /// </summary>
    /// <param name="steamId">Owning player identifier.</param>
    /// <param name="matchPair">Resolved match pair when available.</param>
    /// <returns><c>true</c> when the player participates in a queued match.</returns>
    public delegate bool BattleMatchResolver(ulong steamId, out (ulong PlayerOne, ulong PlayerTwo) matchPair);

    /// <summary>
    /// Delegate used to trigger battle countdown routines once both familiars are ready.
    /// </summary>
    /// <param name="matchPair">Match pair entering combat.</param>
    public delegate void BattleCountdownDelegate((ulong PlayerOne, ulong PlayerTwo) matchPair);

    /// <summary>
    /// Delegate used to apply shiny buffs captured in persistence.
    /// </summary>
    /// <param name="familiar">Familiar receiving the buff.</param>
    /// <param name="buffPrefabHash">Buff prefab hash to apply.</param>
    public delegate void BuffApplicator(EntityHandle familiar, int buffPrefabHash);

    /// <summary>
    /// Tag component describing familiar gate interactions.
    /// </summary>
    public readonly record struct FamiliarBindingGate;

    /// <summary>
    /// Tag component describing familiar binding requests.
    /// </summary>
    public readonly record struct FamiliarBindingRequest;

    /// <summary>
    /// Represents cached data for a gate request.
    /// </summary>
    /// <param name="EventEntity">Entity representing the gate interaction.</param>
    /// <param name="PlayerCharacter">Player character attempting to bind.</param>
    /// <param name="UserEntity">User entity associated with the player.</param>
    /// <param name="FamiliarPrefabHash">Prefab hash identifier being bound.</param>
    /// <param name="Battle">Indicates whether the binding occurs inside the arena.</param>
    /// <param name="TeamIndex">Resolved battle team index.</param>
    /// <param name="Allies">Indicates whether allied battle rules apply.</param>
    public readonly record struct GateEventData(
        EntityHandle EventEntity,
        EntityHandle PlayerCharacter,
        EntityHandle UserEntity,
        int FamiliarPrefabHash,
        bool Battle,
        int TeamIndex,
        bool Allies);

    /// <summary>
    /// Represents cached data for a binding request.
    /// </summary>
    /// <param name="EventEntity">Entity representing the binding operation.</param>
    /// <param name="PlayerCharacter">Player character owning the familiar.</param>
    /// <param name="Familiar">Familiar entity being bound.</param>
    /// <param name="Servant">Servant entity paired with the familiar.</param>
    /// <param name="FamiliarPrefabHash">Prefab hash identifier being bound.</param>
    /// <param name="FamiliarKey">Hash associated with the familiar prefab.</param>
    /// <param name="SteamId">Owning player identifier.</param>
    /// <param name="Battle">Indicates whether the binding occurs inside the arena.</param>
    /// <param name="TeamIndex">Resolved battle team index.</param>
    /// <param name="Allies">Indicates whether allied battle rules apply.</param>
    public readonly record struct BindingEventData(
        EntityHandle EventEntity,
        EntityHandle PlayerCharacter,
        EntityHandle Familiar,
        EntityHandle Servant,
        int FamiliarPrefabHash,
        int FamiliarKey,
        ulong SteamId,
        bool Battle,
        int TeamIndex,
        bool Allies);

    static QueryDescription CreateGateQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<FamiliarBindingGate>();
        builder.AddAllReadOnly<ProjectM.Network.FromCharacter>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateBindingQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<FamiliarBindingRequest>();
        builder.AddAllReadOnly<ProjectM.Network.FromCharacter>();
        builder.AddAllReadOnly<User>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly QueryDescription gateQuery = CreateGateQuery();
    static readonly QueryDescription bindingQuery = CreateBindingQuery();

    readonly ExperienceLoader? experienceLoader;
    readonly ExperienceSaver? experienceSaver;
    readonly BuffLoader? buffLoader;
    readonly EquipmentBinder? equipmentBinder;
    readonly StatRefreshDelegate? statRefresher;
    readonly BattleMatchResolver? matchResolver;
    readonly BattleCountdownDelegate? battleCountdown;
    readonly BuffApplicator? buffApplicator;

    Dictionary<EntityHandle, GateEventData>? gateEvents;
    Dictionary<EntityHandle, BindingEventData>? bindingEvents;
    List<GateEventData>? processedGateEvents;
    List<BindingEventData>? processedBindingEvents;

    readonly Dictionary<ulong, List<int>> playerBattleGroups;
    readonly Dictionary<ulong, List<EntityHandle>> playerBattleFamiliars;

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarBindingWork"/> class.
    /// </summary>
    public FamiliarBindingWork()
        : this(null, null, null, null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FamiliarBindingWork"/> class.
    /// </summary>
    /// <param name="experienceLoader">Optional experience loader delegate.</param>
    /// <param name="experienceSaver">Optional experience saver delegate.</param>
    /// <param name="buffLoader">Optional buff loader delegate.</param>
    /// <param name="equipmentBinder">Optional equipment binder delegate.</param>
    /// <param name="statRefresher">Optional stat refresh delegate.</param>
    /// <param name="matchResolver">Optional battle match resolver delegate.</param>
    /// <param name="battleCountdown">Optional battle countdown delegate.</param>
    /// <param name="buffApplicator">Optional shiny buff applicator delegate.</param>
    public FamiliarBindingWork(
        ExperienceLoader? experienceLoader,
        ExperienceSaver? experienceSaver,
        BuffLoader? buffLoader,
        EquipmentBinder? equipmentBinder,
        StatRefreshDelegate? statRefresher,
        BattleMatchResolver? matchResolver,
        BattleCountdownDelegate? battleCountdown,
        BuffApplicator? buffApplicator)
    {
        this.experienceLoader = experienceLoader;
        this.experienceSaver = experienceSaver;
        this.buffLoader = buffLoader;
        this.equipmentBinder = equipmentBinder;
        this.statRefresher = statRefresher;
        this.matchResolver = matchResolver;
        this.battleCountdown = battleCountdown;
        this.buffApplicator = buffApplicator;

        gateEvents = null;
        bindingEvents = null;
        processedGateEvents = null;
        processedBindingEvents = null;

        playerBattleGroups = new();
        playerBattleFamiliars = new();
    }

    /// <summary>
    /// Gets the query used to inspect familiar gate interactions.
    /// </summary>
    public QueryDescription GateQuery => gateQuery;

    /// <summary>
    /// Gets the query used to inspect familiar binding requests.
    /// </summary>
    public QueryDescription BindingQuery => bindingQuery;

    /// <summary>
    /// Gets the recorded gate events processed by the work.
    /// </summary>
    public IReadOnlyList<GateEventData> ProcessedGateEvents => processedGateEvents != null ? processedGateEvents : Array.Empty<GateEventData>();

    /// <summary>
    /// Gets the recorded binding events processed by the work.
    /// </summary>
    public IReadOnlyList<BindingEventData> ProcessedBindingEvents => processedBindingEvents != null ? processedBindingEvents : Array.Empty<BindingEventData>();

    /// <summary>
    /// Gets the battle group dictionary mirroring <see cref="Systems.Familiars.FamiliarBindingSystem.PlayerBattleGroups"/>.
    /// </summary>
    public IDictionary<ulong, List<int>> PlayerBattleGroups => playerBattleGroups;

    /// <summary>
    /// Gets the battle familiar dictionary mirroring <see cref="Systems.Familiars.FamiliarBindingSystem.PlayerBattleFamiliars"/>.
    /// </summary>
    public IDictionary<ulong, List<EntityHandle>> PlayerBattleFamiliars => playerBattleFamiliars;

    /// <summary>
    /// Gets the configured experience loader delegate.
    /// </summary>
    public ExperienceLoader? ExperienceLoaderDelegate => experienceLoader;

    /// <summary>
    /// Gets the configured experience saver delegate.
    /// </summary>
    public ExperienceSaver? ExperienceSaverDelegate => experienceSaver;

    /// <summary>
    /// Gets the configured buff loader delegate.
    /// </summary>
    public BuffLoader? BuffLoaderDelegate => buffLoader;

    /// <summary>
    /// Gets the configured equipment binder delegate.
    /// </summary>
    public EquipmentBinder? EquipmentBinderDelegate => equipmentBinder;

    /// <summary>
    /// Gets the configured stat refresh delegate.
    /// </summary>
    public StatRefreshDelegate? StatRefresherDelegate => statRefresher;

    /// <summary>
    /// Gets the configured battle resolver delegate.
    /// </summary>
    public BattleMatchResolver? BattleMatchResolverDelegate => matchResolver;

    /// <summary>
    /// Gets the configured battle countdown delegate.
    /// </summary>
    public BattleCountdownDelegate? BattleCountdownHandler => battleCountdown;

    /// <summary>
    /// Gets the configured shiny buff applicator delegate.
    /// </summary>
    public BuffApplicator? BuffApplicatorDelegate => buffApplicator;

    /// <summary>
    /// Adds a gate event to the processing cache.
    /// </summary>
    public void AddGateEvent(GateEventData eventData)
    {
        (gateEvents ??= new()).Add(eventData.EventEntity, eventData);
    }

    /// <summary>
    /// Adds a binding event to the processing cache.
    /// </summary>
    public void AddBindingEvent(BindingEventData eventData)
    {
        (bindingEvents ??= new()).Add(eventData.EventEntity, eventData);
    }

    /// <summary>
    /// Gets the cached gate event handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetGateEventHandles() => GetHandles(gateEvents);

    /// <summary>
    /// Gets the cached binding event handles.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetBindingEventHandles() => GetHandles(bindingEvents);

    static IReadOnlyList<EntityHandle> GetHandles<T>(Dictionary<EntityHandle, T>? source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<EntityHandle>();
        }

        var handles = new EntityHandle[source.Count];
        var index = 0;
        foreach (var key in source.Keys)
        {
            handles[index++] = key;
        }

        return handles;
    }

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<ProjectM.Network.FromCharacter>();
        builder.AddAny(ComponentRequirements.ReadOnly<FamiliarBindingGate>());
        builder.AddAny(ComponentRequirements.ReadOnly<FamiliarBindingRequest>());
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        var registrar = context.Registrar;

        registrar.Register(static (ISystemFacade facade) =>
        {
            _ = facade.GetComponentLookup<UnitStats>();
            _ = facade.GetComponentLookup<AbilityBar_Shared>();
            _ = facade.GetComponentLookup<AiMoveSpeeds>();
            _ = facade.GetComponentLookup<UnitLevel>();
            _ = facade.GetComponentLookup<Health>();

            _ = facade.GetComponentLookup<FactionReference>();
            _ = facade.GetComponentLookup<Team>();
            _ = facade.GetComponentLookup<TeamReference>();
            _ = facade.GetComponentLookup<Follower>();
            _ = facade.GetComponentLookup<Minion>();
            _ = facade.GetComponentLookup<EntityOwner>();

            _ = facade.GetComponentLookup<BlockFeedBuff>();
            _ = facade.GetComponentLookup<Buff>();
            _ = facade.GetComponentLookup<DynamicCollision>();
            _ = facade.GetComponentLookup<ServantConvertable>();
            _ = facade.GetComponentLookup<CharmSource>();
            _ = facade.GetComponentLookup<CanPreventDisableWhenNoPlayersInRange>();
            _ = facade.GetBufferLookup<DropTableBuffer>();
            _ = facade.GetBufferLookup<AttachMapIconsToEntity>();

            _ = facade.GetComponentLookup<FamiliarExperienceData>();
            _ = facade.GetComponentLookup<FamiliarBuffsData>();
            _ = facade.GetComponentLookup<FamiliarPrestigeData>();
            _ = facade.GetComponentLookup<FamiliarBattleGroupsData>();
        });
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        context.WithTempEntities(gateQuery, ProcessGateEvents);
        context.WithTempEntities(bindingQuery, ProcessBindingEvents);
    }

    void ProcessGateEvents(IReadOnlyList<EntityHandle> handles)
    {
        if (handles == null || gateEvents == null)
            return;

        foreach (var handle in handles)
        {
            if (!gateEvents.TryGetValue(handle, out var eventData))
                continue;

            (processedGateEvents ??= new()).Add(eventData);
            gateEvents.Remove(handle);
        }
    }

    void ProcessBindingEvents(IReadOnlyList<EntityHandle> handles)
    {
        if (handles == null || bindingEvents == null)
            return;

        foreach (var handle in handles)
        {
            if (!bindingEvents.TryGetValue(handle, out var eventData))
                continue;

            (processedBindingEvents ??= new()).Add(eventData);
            bindingEvents.Remove(handle);

            var experienceData = LoadExperience(eventData.SteamId);
            int familiarKey = eventData.FamiliarKey;
            if (!experienceData.FamiliarExperience.TryGetValue(familiarKey, out var xpData))
            {
                xpData = new(BASE_LEVEL, ConvertLevelToXp(BASE_LEVEL));
                experienceData.FamiliarExperience[familiarKey] = xpData;
                SaveExperience(eventData.SteamId, experienceData);
            }

            int level = xpData.Key;
            if (level < BASE_LEVEL)
            {
                level = BASE_LEVEL;
                experienceData.FamiliarExperience[familiarKey] = new(level, ConvertLevelToXp(level));
                SaveExperience(eventData.SteamId, experienceData);
            }

            RefreshStats(eventData.Familiar, level, eventData.SteamId, familiarKey, eventData.Battle);

            var buffsData = LoadBuffs(eventData.SteamId);
            ApplyBuffs(eventData.Familiar, buffsData, familiarKey);

            if (eventData.Battle)
            {
                RecordBattleFamiliar(eventData.SteamId, eventData.Familiar);
                UpdateBattleGroups(eventData.SteamId, familiarKey);

                if (matchResolver != null && matchResolver(eventData.SteamId, out var matchPair))
                {
                    battleCountdown?.Invoke(matchPair);
                }
            }
            else
            {
                equipmentBinder?.Invoke(eventData.SteamId, eventData.Servant, eventData.Familiar, familiarKey);
            }
        }
    }

    FamiliarExperienceData LoadExperience(ulong steamId)
    {
        return experienceLoader?.Invoke(steamId) ?? new FamiliarExperienceData();
    }

    void SaveExperience(ulong steamId, FamiliarExperienceData data)
    {
        experienceSaver?.Invoke(steamId, data);
    }

    FamiliarBuffsData LoadBuffs(ulong steamId)
    {
        return buffLoader?.Invoke(steamId) ?? new FamiliarBuffsData();
    }

    void ApplyBuffs(EntityHandle familiar, FamiliarBuffsData data, int familiarKey)
    {
        if (buffApplicator == null)
            return;

        if (!data.FamiliarBuffs.TryGetValue(familiarKey, out var buffList) || buffList == null)
            return;

        foreach (var buff in buffList)
        {
            buffApplicator(familiar, buff);
        }
    }

    void RefreshStats(EntityHandle familiar, int level, ulong steamId, int familiarKey, bool battle)
    {
        statRefresher?.Invoke(familiar, level, steamId, familiarKey, battle);
    }

    void RecordBattleFamiliar(ulong steamId, EntityHandle familiar)
    {
        if (!playerBattleFamiliars.TryGetValue(steamId, out var familiars) || familiars == null)
        {
            familiars = new List<EntityHandle>();
            playerBattleFamiliars[steamId] = familiars;
        }

        familiars.Add(familiar);
    }

    void UpdateBattleGroups(ulong steamId, int familiarKey)
    {
        if (!playerBattleGroups.TryGetValue(steamId, out var group) || group == null)
            return;

        group.Remove(familiarKey);
    }
}
