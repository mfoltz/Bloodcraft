using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the ability run scripts Harmony patches.
/// </summary>
public partial class AbilityRunScriptsWork : ISystemWork
{

    /// <summary>
    /// Delegate used to resolve shapeshift cooldowns for ability groups.
    /// </summary>
    /// <param name="abilityGroup">Ability group associated with the shapeshift.</param>
    /// <param name="cooldownSeconds">Cooldown returned by the shapeshift registry.</param>
    /// <returns><c>true</c> when the cooldown could be resolved.</returns>
    public delegate bool ShapeshiftCooldownResolver(PrefabGUID abilityGroup, out float cooldownSeconds);

    /// <summary>
    /// Delegate used to apply ability group cooldowns.
    /// </summary>
    /// <param name="character">Character requesting the cooldown.</param>
    /// <param name="abilityGroup">Ability group receiving the cooldown.</param>
    /// <param name="cooldownSeconds">Cooldown value in seconds.</param>
    public delegate void AbilityCooldownSetter(EntityHandle character, PrefabGUID abilityGroup, float cooldownSeconds);

    /// <summary>
    /// Delegate used to determine whether a player has an active familiar.
    /// </summary>
    /// <param name="steamId">Steam identifier associated with the player.</param>
    /// <returns><c>true</c> when the player has an active familiar.</returns>
    public delegate bool FamiliarActivityChecker(ulong steamId);

    /// <summary>
    /// Delegate used to determine whether a familiar has already been dismissed.
    /// </summary>
    /// <param name="steamId">Steam identifier associated with the player.</param>
    /// <returns><c>true</c> when the familiar has been dismissed.</returns>
    public delegate bool FamiliarDismissalChecker(ulong steamId);

    /// <summary>
    /// Delegate used to resolve the active familiar for a player character.
    /// </summary>
    /// <param name="playerCharacter">Player character requesting the familiar.</param>
    /// <returns>The familiar entity handle when available.</returns>
    public delegate EntityHandle? FamiliarResolver(EntityHandle playerCharacter);

    /// <summary>
    /// Delegate used to inspect whether a familiar currently has a specified buff.
    /// </summary>
    /// <param name="familiar">Familiar entity handle.</param>
    /// <param name="buff">Buff prefab to check.</param>
    /// <returns><c>true</c> when the familiar has the buff.</returns>
    public delegate bool FamiliarBuffChecker(EntityHandle familiar, PrefabGUID buff);

    /// <summary>
    /// Delegate used to register familiars for auto-call behaviour.
    /// </summary>
    /// <param name="playerCharacter">Player character associated with the familiar.</param>
    /// <param name="familiar">Familiar entity handle.</param>
    public delegate void FamiliarAutoCallRegistrar(EntityHandle playerCharacter, EntityHandle familiar);

    /// <summary>
    /// Delegate used to dismiss the familiar when a waypoint cast starts.
    /// </summary>
    /// <param name="playerCharacter">Player character initiating the cast.</param>
    /// <param name="familiar">Familiar entity handle.</param>
    /// <param name="user">User owning the player character.</param>
    /// <param name="steamId">Steam identifier associated with the player.</param>
    public delegate void FamiliarDismissalDelegate(EntityHandle playerCharacter, EntityHandle familiar, User? user, ulong steamId);

    /// <summary>
    /// Represents cached data associated with an <see cref="AbilityPostCastEndedEvent"/> entity.
    /// </summary>
    /// <param name="EventEntity">Entity representing the event.</param>
    /// <param name="Character">Character that cast the ability.</param>
    /// <param name="AbilityGroup">Ability group executed by the character.</param>
    /// <param name="HasVBloodAbility">Indicates whether the ability group has VBlood data.</param>
    /// <param name="PlayerCharacter">Optional player character associated with the caster.</param>
    /// <param name="CharacterIsExoForm">Indicates whether the player is currently in an exo shapeshift.</param>
    public readonly record struct PostCastEndedEventData(
        EntityHandle EventEntity,
        EntityHandle Character,
        PrefabGUID AbilityGroup,
        bool HasVBloodAbility,
        EntityHandle? PlayerCharacter,
        bool CharacterIsExoForm,
        int AbilityGroupHash = 0);

    /// <summary>
    /// Represents cached data associated with an <see cref="AbilityCastStartedEvent"/> entity.
    /// </summary>
    /// <param name="EventEntity">Entity representing the event.</param>
    /// <param name="Character">Character starting the cast.</param>
    /// <param name="AbilityGroup">Ability group that is being cast.</param>
    /// <param name="PlayerCharacter">Optional player character associated with the caster.</param>
    /// <param name="SteamId">Steam identifier associated with the caster.</param>
    /// <param name="User">User associated with the caster.</param>
    public readonly record struct CastStartedEventData(
        EntityHandle EventEntity,
        EntityHandle Character,
        PrefabGUID AbilityGroup,
        EntityHandle? PlayerCharacter,
        ulong SteamId,
        User? User,
        int AbilityGroupHash = 0);

    const float CooldownFactor = 8f;

    static QueryDescription CreatePostCastEndedQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<AbilityPostCastEndedEvent>();
        return builder.Describe(requireForUpdate: true);
    }

    static QueryDescription CreateCastStartedQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<AbilityCastStartedEvent>();
        return builder.Describe(requireForUpdate: true);
    }

    static readonly Dictionary<int, int> classSpells = new();

    readonly AbilityCooldownSetter? cooldownSetter;
    readonly ShapeshiftCooldownResolver? shapeshiftResolver;
    readonly FamiliarActivityChecker? hasActiveFamiliar;
    readonly FamiliarDismissalChecker? hasDismissedFamiliar;
    readonly FamiliarResolver? familiarResolver;
    readonly FamiliarBuffChecker? familiarBuffChecker;
    readonly FamiliarAutoCallRegistrar? familiarAutoCallRegistrar;
    readonly FamiliarDismissalDelegate? familiarDismissalDelegate;
    readonly PrefabGUID vanishBuff;
    readonly HashSet<int> waypointAbilityGroups;

    Dictionary<EntityHandle, PostCastEndedEventData>? postCastEvents;
    List<EntityHandle>? postCastOrder;
    Dictionary<EntityHandle, CastStartedEventData>? castStartedEvents;
    List<EntityHandle>? castStartedOrder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbilityRunScriptsWork"/> struct.
    /// </summary>
    public AbilityRunScriptsWork()
    {
        cooldownSetter = null;
        shapeshiftResolver = null;
        hasActiveFamiliar = null;
        hasDismissedFamiliar = null;
        familiarResolver = null;
        familiarBuffChecker = null;
        familiarAutoCallRegistrar = null;
        familiarDismissalDelegate = null;
        vanishBuff = default;
        waypointAbilityGroups = new HashSet<int>();
        postCastEvents = null;
        postCastOrder = null;
        castStartedEvents = null;
        castStartedOrder = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbilityRunScriptsWork"/> struct using the provided delegates.
    /// </summary>
    /// <param name="cooldownSetter">Optional delegate invoked when an ability cooldown should be applied.</param>
    /// <param name="shapeshiftResolver">Optional delegate used to resolve shapeshift cooldowns.</param>
    /// <param name="hasActiveFamiliar">Optional delegate used to determine whether a familiar is active.</param>
    /// <param name="hasDismissedFamiliar">Optional delegate used to determine whether the familiar was dismissed.</param>
    /// <param name="familiarResolver">Optional delegate used to resolve the active familiar.</param>
    /// <param name="familiarBuffChecker">Optional delegate used to inspect familiar buffs.</param>
    /// <param name="familiarAutoCallRegistrar">Optional delegate used to register familiars for auto-call behaviour.</param>
    /// <param name="familiarDismissalDelegate">Optional delegate used to dismiss familiars.</param>
    /// <param name="vanishBuff">Prefab used when checking whether the familiar has the vanish buff.</param>
    /// <param name="waypointAbilityGroups">Optional collection of waypoint ability groups.</param>
    public AbilityRunScriptsWork(
        AbilityCooldownSetter? cooldownSetter = null,
        ShapeshiftCooldownResolver? shapeshiftResolver = null,
        FamiliarActivityChecker? hasActiveFamiliar = null,
        FamiliarDismissalChecker? hasDismissedFamiliar = null,
        FamiliarResolver? familiarResolver = null,
        FamiliarBuffChecker? familiarBuffChecker = null,
        FamiliarAutoCallRegistrar? familiarAutoCallRegistrar = null,
        FamiliarDismissalDelegate? familiarDismissalDelegate = null,
        PrefabGUID vanishBuff = default,
        IEnumerable<PrefabGUID>? waypointAbilityGroups = null)
        : this()
    {
        this.cooldownSetter = cooldownSetter;
        this.shapeshiftResolver = shapeshiftResolver;
        this.hasActiveFamiliar = hasActiveFamiliar;
        this.hasDismissedFamiliar = hasDismissedFamiliar;
        this.familiarResolver = familiarResolver;
        this.familiarBuffChecker = familiarBuffChecker;
        this.familiarAutoCallRegistrar = familiarAutoCallRegistrar;
        this.familiarDismissalDelegate = familiarDismissalDelegate;
        this.vanishBuff = vanishBuff;

        if (waypointAbilityGroups != null)
        {
            foreach (var abilityGroup in waypointAbilityGroups)
            {
                this.waypointAbilityGroups.Add(ToGuidHash(abilityGroup));
            }
        }
    }

    /// <summary>
    /// Converts a prefab guid into its integer hash without invoking IL2CPP helpers.
    /// </summary>
    /// <param name="prefabGuid">Prefab guid to convert.</param>
    /// <returns>Integer hash associated with the prefab guid.</returns>
    public static int ToGuidHash(PrefabGUID prefabGuid)
    {
        return MemoryMarshal.Cast<PrefabGUID, int>(MemoryMarshal.CreateReadOnlySpan(ref prefabGuid, 1))[0];
    }

    Dictionary<EntityHandle, PostCastEndedEventData> PostCastEventMap => postCastEvents ??= new();

    List<EntityHandle> PostCastEventOrder => postCastOrder ??= new();

    Dictionary<EntityHandle, CastStartedEventData> CastStartedEventMap => castStartedEvents ??= new();

    List<EntityHandle> CastStartedEventOrder => castStartedOrder ??= new();

    /// <summary>
    /// Provides a read-only view of the cached post-cast events.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, PostCastEndedEventData> PostCastEvents => PostCastEventMap;

    /// <summary>
    /// Provides a read-only view of the cached cast-started events.
    /// </summary>
    public IReadOnlyDictionary<EntityHandle, CastStartedEventData> CastStartedEvents => CastStartedEventMap;

    /// <summary>
    /// Provides the iteration order of cached post-cast events.
    /// </summary>
    public IReadOnlyList<EntityHandle> PostCastEventsOrder => PostCastEventOrder;

    /// <summary>
    /// Provides the iteration order of cached cast-started events.
    /// </summary>
    public IReadOnlyList<EntityHandle> CastStartedEventsOrder => CastStartedEventOrder;

    /// <summary>
    /// Provides the configured waypoint ability groups.
    /// </summary>
    public IReadOnlyCollection<int> WaypointAbilityGroups => waypointAbilityGroups;

    /// <summary>
    /// Provides the prefab used to identify the vanish buff.
    /// </summary>
    public PrefabGUID VanishBuff => vanishBuff;

    /// <summary>
    /// Gets the post-cast ended query description.
    /// </summary>
    public QueryDescription PostCastEndedQuery => CreatePostCastEndedQuery();

    /// <summary>
    /// Gets the cast-started query description.
    /// </summary>
    public QueryDescription CastStartedQuery => CreateCastStartedQuery();

    /// <summary>
    /// Provides a read-only view of the class spell cooldown configuration.
    /// </summary>
    public static IReadOnlyDictionary<int, int> ClassSpells => classSpells;

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<AbilityPostCastEndedEvent>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        context.Registrar.Register(RegisterRefreshLookups);
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        context.ForEachEntity(PostCastEndedQuery, ProcessPostCastEndedEvent);
        context.ForEachEntity(CastStartedQuery, ProcessCastStartedEvent);
    }

    void RegisterRefreshLookups(ISystemFacade facade)
    {
        if (facade == null)
            throw new ArgumentNullException(nameof(facade));

        _ = facade.GetComponentLookup<AbilityPostCastEndedEvent>(isReadOnly: true);
        _ = facade.GetComponentLookup<AbilityCastStartedEvent>(isReadOnly: true);
        _ = facade.GetComponentLookup<User>(isReadOnly: true);
        _ = facade.GetComponentLookup<FamiliarActivityChecker>(isReadOnly: true);
        _ = facade.GetComponentLookup<FamiliarDismissalChecker>(isReadOnly: true);
        _ = facade.GetComponentLookup<FamiliarResolver>(isReadOnly: true);
        _ = facade.GetComponentLookup<FamiliarBuffChecker>(isReadOnly: true);
        _ = facade.GetComponentLookup<FamiliarAutoCallRegistrar>();
        _ = facade.GetComponentLookup<FamiliarDismissalDelegate>();
    }

    /// <summary>
    /// Adds a post-cast ended event to the cached collection.
    /// </summary>
    /// <param name="eventData">Event data to add.</param>
    public void AddPostCastEvent(PostCastEndedEventData eventData)
    {
        int abilityGroupHash = eventData.AbilityGroupHash != 0
            ? eventData.AbilityGroupHash
            : ToGuidHash(eventData.AbilityGroup);

        var normalised = eventData with { AbilityGroupHash = abilityGroupHash };

        if (!PostCastEventMap.ContainsKey(eventData.EventEntity))
        {
            PostCastEventOrder.Add(eventData.EventEntity);
        }

        PostCastEventMap[eventData.EventEntity] = normalised;
    }

    /// <summary>
    /// Adds a cast-started event to the cached collection.
    /// </summary>
    /// <param name="eventData">Event data to add.</param>
    public void AddCastStartedEvent(CastStartedEventData eventData)
    {
        int abilityGroupHash = eventData.AbilityGroupHash != 0
            ? eventData.AbilityGroupHash
            : ToGuidHash(eventData.AbilityGroup);

        var normalised = eventData with { AbilityGroupHash = abilityGroupHash };

        if (!CastStartedEventMap.ContainsKey(eventData.EventEntity))
        {
            CastStartedEventOrder.Add(eventData.EventEntity);
        }

        CastStartedEventMap[eventData.EventEntity] = normalised;
    }

    /// <summary>
    /// Adds a waypoint ability group to the active set.
    /// </summary>
    /// <param name="abilityGroup">Ability group prefab to add.</param>
    public void AddWaypointAbilityGroup(PrefabGUID abilityGroup)
    {
        waypointAbilityGroups.Add(ToGuidHash(abilityGroup));
    }

    /// <summary>
    /// Adds a class spell entry mirroring the runtime patch behaviour.
    /// </summary>
    /// <param name="prefabGuid">Ability group associated with the spell.</param>
    /// <param name="spellIndex">Configured class spell index.</param>
    public static void AddClassSpell(PrefabGUID prefabGuid, int spellIndex)
    {
        classSpells.TryAdd(ToGuidHash(prefabGuid), spellIndex);
    }

    void ProcessPostCastEndedEvent(EntityHandle eventEntity)
    {
        if (postCastEvents == null)
        {
            return;
        }

        if (!postCastEvents.TryGetValue(eventEntity, out var eventData))
        {
            return;
        }

        if (eventData.HasVBloodAbility)
        {
            return;
        }

        if (eventData.PlayerCharacter is not EntityHandle)
        {
            return;
        }

        if (eventData.CharacterIsExoForm && shapeshiftResolver != null)
        {
            if (shapeshiftResolver(eventData.AbilityGroup, out float cooldownSeconds))
            {
                cooldownSetter?.Invoke(eventData.Character, eventData.AbilityGroup, cooldownSeconds);
                return;
            }
        }

        if (classSpells.TryGetValue(eventData.AbilityGroupHash, out int spellIndex))
        {
            float cooldownSeconds = spellIndex == 0 ? CooldownFactor : (spellIndex + 1) * CooldownFactor;
            cooldownSetter?.Invoke(eventData.Character, eventData.AbilityGroup, cooldownSeconds);
        }
    }

    void ProcessCastStartedEvent(EntityHandle eventEntity)
    {
        if (castStartedEvents == null)
        {
            return;
        }

        if (!castStartedEvents.TryGetValue(eventEntity, out var eventData))
        {
            return;
        }

        if (eventData.PlayerCharacter is not EntityHandle playerCharacter)
        {
            return;
        }

        if (!waypointAbilityGroups.Contains(eventData.AbilityGroupHash))
        {
            return;
        }

        if (hasActiveFamiliar == null || !hasActiveFamiliar(eventData.SteamId))
        {
            return;
        }

        if (hasDismissedFamiliar != null && hasDismissedFamiliar(eventData.SteamId))
        {
            return;
        }

        if (familiarResolver == null)
        {
            return;
        }

        EntityHandle? familiar = familiarResolver(playerCharacter);
        if (familiar is not EntityHandle familiarHandle)
        {
            return;
        }

        if (familiarBuffChecker != null && familiarBuffChecker(familiarHandle, vanishBuff))
        {
            return;
        }

        familiarAutoCallRegistrar?.Invoke(playerCharacter, familiarHandle);
        familiarDismissalDelegate?.Invoke(playerCharacter, familiarHandle, eventData.User, eventData.SteamId);
    }
}
