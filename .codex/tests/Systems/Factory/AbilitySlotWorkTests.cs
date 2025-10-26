using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Xunit;

using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class AbilitySlotWorkTests
{
    [Fact]
    public void DescribeQuery_RequiresOwnerDataAndBuffer()
    {
        var description = FactoryTestUtilities.DescribeQuery<AbilitySlotWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(EntityOwner), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(ReplaceAbilityOnSlotData), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            },
            requirement =>
            {
                Assert.Equal(typeof(ReplaceAbilityOnSlotBuff), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadWrite, requirement.AccessMode);
            });

        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void OnCreate_RegistersOwnerDataAndBufferLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<AbilitySlotWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.RegistrationCount);
        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Equal(4, registrar.ComponentLookups.Count);
        Assert.Contains(new LookupRequest(typeof(EntityOwner), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(ReplaceAbilityOnSlotData), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(WeaponLevel), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(PrefabGUID), true), registrar.ComponentLookups);

        Assert.Collection(
            registrar.BufferLookups,
            lookup =>
            {
                Assert.Equal(typeof(ReplaceAbilityOnSlotBuff), lookup.ElementType);
                Assert.False(lookup.IsReadOnly);
            });
    }

    [Fact]
    public void Tick_UnarmedEntitiesAddSlotReplacementsAndShiftSpell()
    {
        var requestedQueries = new List<QueryDescription>();
        var steamId = 123UL;
        var entity = new EntityHandle(5);
        var work = new AbilitySlotWork(
            enableUnarmedSlots: true,
            enableDuality: true,
            enableShiftSlot: false,
            spellSource: (ulong id, out AbilitySlotWork.SpellLoadout spells) =>
            {
                spells = new AbilitySlotWork.SpellLoadout(101, 202, 303);
                return id == steamId;
            },
            spellPersistence: null,
            flagSource: (id, key) => id == steamId && key == SHIFT_LOCK_KEY,
            prefabLookup: (_, _) => false);

        work.AddAbilityEntity(new AbilitySlotWork.AbilityEntityData(
            entity,
            new EntityHandle(6),
            steamId,
            default,
            "Player_Unarmed_FishingPole",
            HasWeaponLevel: true));

        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);
                foreach (var handle in work.GetAbilityHandles())
                {
                    action(handle);
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.AbilityQuery, requestedQueries);

        var buffer = work.GetAbilityBuffer(entity);
        Assert.Equal(3, buffer.Count);
        Assert.Contains(buffer, buff => buff.Slot == 1);
        Assert.Contains(buffer, buff => buff.Slot == 4);
        Assert.Contains(buffer, buff => buff.Slot == 3);

        var metadata = work.GetAbilityBufferMetadata(entity);
        Assert.Contains((1, 101), metadata);
        Assert.Contains((4, 202), metadata);
        Assert.Contains((3, 303), metadata);
    }

    [Fact]
    public void Tick_ShiftOnlyAddsReplacementWhenEnabled()
    {
        var requestedQueries = new List<QueryDescription>();
        var steamId = 555UL;
        int? requestedHash = null;

        var work = new AbilitySlotWork(
            enableUnarmedSlots: false,
            enableDuality: false,
            enableShiftSlot: true,
            spellSource: (ulong id, out AbilitySlotWork.SpellLoadout spells) =>
            {
                spells = new AbilitySlotWork.SpellLoadout(0, 0, 909);
                return id == steamId;
            },
            spellPersistence: null,
            flagSource: (id, key) => id == steamId && key == SHIFT_LOCK_KEY,
            prefabLookup: (_, hash) =>
            {
                requestedHash = hash;
                return false;
            });

        var entity = new EntityHandle(8);
        work.AddAbilityEntity(new AbilitySlotWork.AbilityEntityData(
            entity,
            new EntityHandle(9),
            steamId,
            default,
            "Weapon_Whip",
            HasWeaponLevel: true));

        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);
                foreach (var handle in work.GetAbilityHandles())
                {
                    action(handle);
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.AbilityQuery, requestedQueries);
        Assert.Equal(909, requestedHash);

        var buffer = work.GetAbilityBuffer(entity);
        Assert.Single(buffer);
        Assert.Equal(3, buffer[0].Slot);

        var metadata = work.GetAbilityBufferMetadata(entity);
        Assert.Equal(new[] { (3, 909) }, metadata);
    }

    [Fact]
    public void Tick_LockedSpellsPersistLoadoutWithOverrides()
    {
        var requestedQueries = new List<QueryDescription>();
        var steamId = 777UL;
        var persisted = new List<(ulong SteamId, AbilitySlotWork.SpellLoadout Spells)>();

        var work = new AbilitySlotWork(
            enableUnarmedSlots: false,
            enableDuality: false,
            enableShiftSlot: false,
            spellSource: (ulong id, out AbilitySlotWork.SpellLoadout spells) =>
            {
                spells = new AbilitySlotWork.SpellLoadout(11, 22, 33);
                return id == steamId;
            },
            spellPersistence: (id, spells) => persisted.Add((id, spells)),
            flagSource: (id, key) => id == steamId && key == SPELL_LOCK_KEY,
            prefabLookup: (_, _) => false);

        var entity = new EntityHandle(12);
        work.AddAbilityEntity(new AbilitySlotWork.AbilityEntityData(
            entity,
            new EntityHandle(13),
            steamId,
            default,
            "Ability_Generic",
            HasWeaponLevel: false));

        work.SetAbilityBuffer(
            entity,
            new[]
        {
            new ReplaceAbilityOnSlotBuff
            {
                Slot = 5,
                NewGroupId = default,
                CopyCooldown = true,
                Priority = 0,
            },
            new ReplaceAbilityOnSlotBuff
            {
                Slot = 6,
                NewGroupId = default,
                CopyCooldown = true,
                Priority = 0,
            },
        },
            metadata: new[]
        {
            (5, 444),
            (6, 555),
        });

        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);
                foreach (var handle in work.GetAbilityHandles())
                {
                    action(handle);
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.AbilityQuery, requestedQueries);

        Assert.Single(persisted);
        Assert.Equal(steamId, persisted[0].SteamId);
        Assert.Equal(444, persisted[0].Spells.FirstUnarmed);
        Assert.Equal(555, persisted[0].Spells.SecondUnarmed);
        Assert.Equal(33, persisted[0].Spells.ClassSpell);
    }

}
