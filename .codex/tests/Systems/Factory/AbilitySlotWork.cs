using System;
using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Tests.Systems.Factory;

/// <summary>
/// Provides a test work definition mirroring the replace-ability-on-slot patch.
/// </summary>
public class AbilitySlotWork : ISystemWork
{
    /// <summary>
    /// Supplies the current spell loadout for a player.
    /// </summary>
    public delegate bool PlayerSpellSource(ulong steamId, out SpellLoadout spells);

    /// <summary>
    /// Persists an updated spell loadout for a player.
    /// </summary>
    public delegate void PlayerSpellPersistence(ulong steamId, SpellLoadout spells);

    /// <summary>
    /// Resolves player-specific boolean toggles (e.g., spell lock / shift lock).
    /// </summary>
    public delegate bool PlayerFlagSource(ulong steamId, string key);

    /// <summary>
    /// Determines whether a prefab represents a VBlood ability and should be ignored for shift replacement.
    /// The guid hash is surfaced alongside the prefab so tests can inspect the raw value without relying on
    /// IL2CPP initialisation.
    /// </summary>
    public delegate bool PrefabAbilityLookup(PrefabGUID prefabGuid, int guidHash);

    /// <summary>
    /// Represents the spell slots available for a player.
    /// </summary>
    public readonly record struct SpellLoadout(int FirstUnarmed, int SecondUnarmed, int ClassSpell);

    /// <summary>
    /// Describes cached information about an ability entity produced by the replace-ability system.
    /// </summary>
    public readonly record struct AbilityEntityData(
        EntityHandle Entity,
        EntityHandle OwnerCharacter,
        ulong OwnerSteamId,
        PrefabGUID Prefab,
        string PrefabName,
        bool HasWeaponLevel);

    readonly bool enableUnarmedSlots;
    readonly bool enableDuality;
    readonly bool enableShiftSlot;
    PlayerSpellSource? spellSource;
    PlayerSpellPersistence? spellPersistence;
    PlayerFlagSource? flagSource;
    PrefabAbilityLookup? prefabLookup;
    Dictionary<EntityHandle, AbilityEntityData>? entities;
    Dictionary<EntityHandle, List<ReplaceAbilityOnSlotBuff>>? buffers;
    Dictionary<EntityHandle, List<(int Slot, int GuidHash)>>? bufferMetadata;
    List<EntityHandle>? entityHandles;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbilitySlotWork"/> struct.
    /// </summary>
    public AbilitySlotWork(
        bool enableUnarmedSlots,
        bool enableDuality,
        bool enableShiftSlot,
        PlayerSpellSource? spellSource,
        PlayerSpellPersistence? spellPersistence,
        PlayerFlagSource? flagSource,
        PrefabAbilityLookup? prefabLookup)
    {
        this.enableUnarmedSlots = enableUnarmedSlots;
        this.enableDuality = enableDuality;
        this.enableShiftSlot = enableShiftSlot;
        this.spellSource = spellSource;
        this.spellPersistence = spellPersistence;
        this.flagSource = flagSource;
        this.prefabLookup = prefabLookup;
        entities = null;
        buffers = null;
        bufferMetadata = null;
        entityHandles = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbilitySlotWork"/> struct with default configuration.
    /// </summary>
    public AbilitySlotWork()
    {
        enableUnarmedSlots = false;
        enableDuality = false;
        enableShiftSlot = false;
        spellSource = null;
        spellPersistence = null;
        flagSource = null;
        prefabLookup = null;
        entities = null;
        buffers = null;
        bufferMetadata = null;
        entityHandles = null;
    }

    static QueryDescription CreateAbilityQuery()
    {
        var builder = new TestEntityQueryBuilder();
        builder.AddAllReadOnly<EntityOwner>();
        builder.AddAllReadOnly<ReplaceAbilityOnSlotData>();
        builder.AddAllReadWrite<ReplaceAbilityOnSlotBuff>();
        return builder.Describe(requireForUpdate: true);
    }

    /// <summary>
    /// Gets the query used to capture replace-ability entities.
    /// </summary>
    public QueryDescription AbilityQuery => CreateAbilityQuery();

    /// <inheritdoc />
    public void Build(TestEntityQueryBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.AddAllReadOnly<EntityOwner>();
        builder.AddAllReadOnly<ReplaceAbilityOnSlotData>();
        builder.AddAllReadWrite<ReplaceAbilityOnSlotBuff>();
    }

    /// <inheritdoc />
    public void OnCreate(SystemContext context)
    {
        context.Registrar.Register(RegisterRefreshLookups);
    }

    /// <inheritdoc />
    public void OnUpdate(SystemContext context)
    {
        context.ForEachEntity(AbilityQuery, ProcessEntity);
    }

    void RegisterRefreshLookups(ISystemFacade facade)
    {
        if (facade == null)
            throw new ArgumentNullException(nameof(facade));

        _ = facade.GetComponentLookup<EntityOwner>(isReadOnly: true);
        _ = facade.GetComponentLookup<ReplaceAbilityOnSlotData>(isReadOnly: true);
        _ = facade.GetComponentLookup<WeaponLevel>(isReadOnly: true);
        _ = facade.GetComponentLookup<PrefabGUID>(isReadOnly: true);
        _ = facade.GetBufferLookup<ReplaceAbilityOnSlotBuff>();
    }

    /// <summary>
    /// Adds an ability entity for later processing.
    /// </summary>
    public void AddAbilityEntity(AbilityEntityData data)
    {
        entities ??= new Dictionary<EntityHandle, AbilityEntityData>();
        entities[data.Entity] = data;

        buffers ??= new Dictionary<EntityHandle, List<ReplaceAbilityOnSlotBuff>>();
        if (!buffers.TryGetValue(data.Entity, out _))
        {
            buffers[data.Entity] = new List<ReplaceAbilityOnSlotBuff>();
        }

        bufferMetadata ??= new Dictionary<EntityHandle, List<(int Slot, int GuidHash)>>();
        if (!bufferMetadata.TryGetValue(data.Entity, out _))
        {
            bufferMetadata[data.Entity] = new List<(int Slot, int GuidHash)>();
        }

        entityHandles ??= new List<EntityHandle>();
        if (!entityHandles.Contains(data.Entity))
        {
            entityHandles.Add(data.Entity);
        }
    }

    /// <summary>
    /// Associates an initial buffer with an ability entity.
    /// </summary>
    public void SetAbilityBuffer(
        EntityHandle entity,
        IEnumerable<ReplaceAbilityOnSlotBuff> entries,
        IEnumerable<(int Slot, int GuidHash)>? metadata = null)
    {
        if (entries == null)
            throw new ArgumentNullException(nameof(entries));

        buffers ??= new Dictionary<EntityHandle, List<ReplaceAbilityOnSlotBuff>>();
        buffers[entity] = new List<ReplaceAbilityOnSlotBuff>(entries);

        if (metadata != null)
        {
            bufferMetadata ??= new Dictionary<EntityHandle, List<(int Slot, int GuidHash)>>();
            bufferMetadata[entity] = new List<(int Slot, int GuidHash)>(metadata);
        }
        else if (bufferMetadata != null && bufferMetadata.TryGetValue(entity, out var existingMetadata))
        {
            existingMetadata.Clear();
        }
    }

    /// <summary>
    /// Gets the handles currently tracked by the work instance.
    /// </summary>
    public IReadOnlyList<EntityHandle> GetAbilityHandles()
    {
        if (entityHandles == null)
            return Array.Empty<EntityHandle>();

        return entityHandles;
    }

    /// <summary>
    /// Gets the buffer associated with the entity, if present.
    /// </summary>
    public IReadOnlyList<ReplaceAbilityOnSlotBuff> GetAbilityBuffer(EntityHandle entity)
    {
        if (buffers != null && buffers.TryGetValue(entity, out var buffer))
            return buffer;

        return Array.Empty<ReplaceAbilityOnSlotBuff>();
    }

    /// <summary>
    /// Gets the tracked buffer metadata for the entity, if present.
    /// </summary>
    public IReadOnlyList<(int Slot, int GuidHash)> GetAbilityBufferMetadata(EntityHandle entity)
    {
        if (bufferMetadata != null && bufferMetadata.TryGetValue(entity, out var metadata))
            return metadata;

        return Array.Empty<(int Slot, int GuidHash)>();
    }

    void ProcessEntity(EntityHandle entity)
    {
        if (entities == null || !entities.TryGetValue(entity, out var data))
            return;
        if (spellSource == null || !spellSource(data.OwnerSteamId, out var spells))
            return;

        if (enableUnarmedSlots && IsUnarmedPrefab(data.PrefabName))
        {
            var buffer = GetOrCreateBuffer(entity);
            AddUnarmedSpells(entity, buffer, spells);
            AddShiftSpell(entity, buffer, data.OwnerSteamId, spells);
            return;
        }

        if (enableShiftSlot && IsWeaponPrefab(data.PrefabName))
        {
            var buffer = GetOrCreateBuffer(entity);
            AddShiftSpell(entity, buffer, data.OwnerSteamId, spells);
            return;
        }

        if (!data.HasWeaponLevel)
        {
            PersistLockedSpells(entity, data.OwnerSteamId, spells);
        }
    }

    static bool IsUnarmedPrefab(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return false;

        return prefabName.Contains("unarmed", StringComparison.CurrentCultureIgnoreCase)
            || prefabName.Contains("fishingpole", StringComparison.CurrentCultureIgnoreCase);
    }

    static bool IsWeaponPrefab(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return false;

        return prefabName.Contains("weapon", StringComparison.CurrentCultureIgnoreCase);
    }

    void AddUnarmedSpells(EntityHandle entity, List<ReplaceAbilityOnSlotBuff> buffer, SpellLoadout spells)
    {
        if (spells.FirstUnarmed != 0)
        {
            buffer.Add(new ReplaceAbilityOnSlotBuff
            {
                Slot = 1,
                NewGroupId = default,
                CopyCooldown = true,
                Priority = 0,
            });
            RecordBufferEntry(entity, 1, spells.FirstUnarmed);
        }

        if (enableDuality && spells.SecondUnarmed != 0)
        {
            buffer.Add(new ReplaceAbilityOnSlotBuff
            {
                Slot = 4,
                NewGroupId = default,
                CopyCooldown = true,
                Priority = 0,
            });
            RecordBufferEntry(entity, 4, spells.SecondUnarmed);
        }
    }

    void AddShiftSpell(EntityHandle entity, List<ReplaceAbilityOnSlotBuff> buffer, ulong steamId, SpellLoadout spells)
    {
        var shiftLock = GetFlag(steamId, SHIFT_LOCK_KEY);
        if (!shiftLock)
            return;

        if (spells.ClassSpell == 0)
            return;

        if (prefabLookup != null && prefabLookup(default, spells.ClassSpell))
            return;

        buffer.Add(new ReplaceAbilityOnSlotBuff
        {
            Slot = 3,
            NewGroupId = default,
            CopyCooldown = true,
            Priority = 0,
        });
        RecordBufferEntry(entity, 3, spells.ClassSpell);
    }

    void PersistLockedSpells(EntityHandle entity, ulong steamId, SpellLoadout spells)
    {
        if (spellPersistence == null)
            return;

        var finalSpells = spells;
        var lockSpells = GetFlag(steamId, SPELL_LOCK_KEY);
        var metadata = GetBufferMetadataInternal(entity);

        if (lockSpells && metadata != null)
        {
            foreach (var entry in metadata)
            {
                if (entry.Slot == 5)
                {
                    finalSpells = finalSpells with { FirstUnarmed = entry.GuidHash };
                }
                else if (entry.Slot == 6)
                {
                    finalSpells = finalSpells with { SecondUnarmed = entry.GuidHash };
                }
            }
        }

        spellPersistence(steamId, finalSpells);
    }

    bool GetFlag(ulong steamId, string key)
    {
        return flagSource != null && flagSource(steamId, key);
    }

    List<ReplaceAbilityOnSlotBuff> GetOrCreateBuffer(EntityHandle entity)
    {
        if (buffers == null || !buffers.TryGetValue(entity, out var buffer))
        {
            buffer = new List<ReplaceAbilityOnSlotBuff>();
            buffers ??= new Dictionary<EntityHandle, List<ReplaceAbilityOnSlotBuff>>();
            buffers[entity] = buffer;
        }

        return buffer;
    }

    void RecordBufferEntry(EntityHandle entity, int slot, int guidHash)
    {
        if (bufferMetadata == null)
            return;

        if (!bufferMetadata.TryGetValue(entity, out var metadata))
        {
            metadata = new List<(int Slot, int GuidHash)>();
            bufferMetadata[entity] = metadata;
        }

        metadata.Add((slot, guidHash));
    }

    List<(int Slot, int GuidHash)>? GetBufferMetadataInternal(EntityHandle entity)
    {
        if (bufferMetadata == null || !bufferMetadata.TryGetValue(entity, out var metadata))
            return null;

        return metadata;
    }
}
