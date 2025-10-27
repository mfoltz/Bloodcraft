using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory;

public sealed class AbilityRunScriptsWorkTests
{
    [Fact]
    public void DescribeQuery_ExposesPostCastEndedRequirement()
    {
        var description = FactoryTestUtilities.DescribeQuery<AbilityRunScriptsWork>();

        Assert.Collection(
            description.All,
            requirement =>
            {
                Assert.Equal(typeof(AbilityPostCastEndedEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(description.Any);
        Assert.Empty(description.None);
        Assert.Equal(EntityQueryOptions.None, description.Options);
        Assert.True(description.RequireForUpdate);
    }

    [Fact]
    public void CastStartedQuery_ExposesAbilityCastStartedRequirement()
    {
        var work = FactoryTestUtilities.CreateWork<AbilityRunScriptsWork>();
        var query = work.CastStartedQuery;

        Assert.Collection(
            query.All,
            requirement =>
            {
                Assert.Equal(typeof(AbilityCastStartedEvent), requirement.ElementType);
                Assert.Equal(ComponentAccessMode.ReadOnly, requirement.AccessMode);
            });

        Assert.Empty(query.Any);
        Assert.Empty(query.None);
        Assert.Equal(EntityQueryOptions.None, query.Options);
        Assert.True(query.RequireForUpdate);
    }

    [Fact]
    public void OnCreate_RegistersEventUserAndFamiliarLookups()
    {
        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(registrar);
        var work = FactoryTestUtilities.CreateWork<AbilityRunScriptsWork>();

        FactoryTestUtilities.OnCreate(work, context);

        Assert.Equal(1, registrar.RegistrationCount);
        Assert.Equal(1, registrar.FacadeRegistrationCount);
        Assert.Equal(0, registrar.SystemRegistrationCount);

        registrar.InvokeRegistrations();

        Assert.Contains(new LookupRequest(typeof(AbilityPostCastEndedEvent), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(AbilityCastStartedEvent), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(User), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(AbilityRunScriptsWork.FamiliarActivityChecker), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(AbilityRunScriptsWork.FamiliarDismissalChecker), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(AbilityRunScriptsWork.FamiliarResolver), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(AbilityRunScriptsWork.FamiliarBuffChecker), true), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(AbilityRunScriptsWork.FamiliarAutoCallRegistrar), false), registrar.ComponentLookups);
        Assert.Contains(new LookupRequest(typeof(AbilityRunScriptsWork.FamiliarDismissalDelegate), false), registrar.ComponentLookups);
    }

    [Fact]
    public void Tick_PostCastEventsUseShapeshiftCooldowns()
    {
        var requestedQueries = new List<QueryDescription>();
        var abilityGroup = new PrefabGUID(1200);
        var character = new EntityHandle(1);
        var player = new EntityHandle(2);
        var eventEntity = new EntityHandle(3);

        bool shapeshiftInvoked = false;
        EntityHandle? cooldownCharacter = null;
        int? cooldownAbilityGroupHash = null;
        float? cooldownValue = null;

        var work = new AbilityRunScriptsWork(
            cooldownSetter: (source, group, cooldown) =>
            {
                cooldownCharacter = source;
                cooldownAbilityGroupHash = AbilityRunScriptsWork.ToGuidHash(group);
                cooldownValue = cooldown;
            },
            shapeshiftResolver: (PrefabGUID group, out float cooldown) =>
            {
                shapeshiftInvoked = true;
                if (AbilityRunScriptsWork.ToGuidHash(group) == AbilityRunScriptsWork.ToGuidHash(abilityGroup))
                {
                    cooldown = 42f;
                    return true;
                }

                cooldown = 0f;
                return false;
            });

        work.AddPostCastEvent(new AbilityRunScriptsWork.PostCastEndedEventData(
            eventEntity,
            character,
            abilityGroup,
            HasVBloodAbility: false,
            PlayerCharacter: player,
            CharacterIsExoForm: true));

        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);
                if (query.Equals(work.PostCastEndedQuery))
                {
                    action(eventEntity);
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.PostCastEndedQuery, requestedQueries);
        Assert.Contains(work.CastStartedQuery, requestedQueries);
        Assert.True(shapeshiftInvoked);
        Assert.Equal(character, cooldownCharacter);
        Assert.Equal(AbilityRunScriptsWork.ToGuidHash(abilityGroup), cooldownAbilityGroupHash);
        Assert.Equal(42f, cooldownValue);
    }

    [Fact]
    public void Tick_PostCastEventsFallBackToClassCooldowns()
    {
        var requestedQueries = new List<QueryDescription>();
        var abilityGroup = new PrefabGUID(98765);
        var character = new EntityHandle(4);
        var player = new EntityHandle(5);
        var eventEntity = new EntityHandle(6);

        AbilityRunScriptsWork.AddClassSpell(abilityGroup, spellIndex: 2);

        int shapeshiftCalls = 0;
        var appliedCooldowns = new List<float>();
        var cooldownAbilityGroupHashes = new List<int>();

        var work = new AbilityRunScriptsWork(
            cooldownSetter: (source, group, cooldown) =>
            {
                Assert.Equal(character, source);
                cooldownAbilityGroupHashes.Add(AbilityRunScriptsWork.ToGuidHash(group));
                appliedCooldowns.Add(cooldown);
            },
            shapeshiftResolver: (PrefabGUID _, out float cooldown) =>
            {
                shapeshiftCalls++;
                cooldown = 0f;
                return false;
            });

        work.AddPostCastEvent(new AbilityRunScriptsWork.PostCastEndedEventData(
            eventEntity,
            character,
            abilityGroup,
            HasVBloodAbility: false,
            PlayerCharacter: player,
            CharacterIsExoForm: false));

        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);
                if (query.Equals(work.PostCastEndedQuery))
                {
                    action(eventEntity);
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Equal(0, shapeshiftCalls);
        Assert.Single(appliedCooldowns);
        Assert.Single(cooldownAbilityGroupHashes);
        Assert.Equal(AbilityRunScriptsWork.ToGuidHash(abilityGroup), cooldownAbilityGroupHashes.Single());
        Assert.Equal(24f, appliedCooldowns.Single());
        Assert.Contains(work.PostCastEndedQuery, requestedQueries);
        Assert.Contains(work.CastStartedQuery, requestedQueries);
    }

    [Fact]
    public void Tick_CastStartedEventsInvokeFamiliarDismissal()
    {
        var requestedQueries = new List<QueryDescription>();
        var abilityGroup = new PrefabGUID(2222);
        var vanishBuff = new PrefabGUID(7777);
        var character = new EntityHandle(10);
        var player = new EntityHandle(11);
        var familiar = new EntityHandle(12);
        var eventEntity = new EntityHandle(13);
        const ulong steamId = 555555UL;

        var autoCall = new List<(EntityHandle Player, EntityHandle Familiar)>();
        var dismissals = new List<(EntityHandle Player, EntityHandle Familiar, User? User, ulong SteamId)>();

        bool hasActiveChecked = false;
        bool hasDismissedChecked = false;
        bool buffChecked = false;
        bool resolverInvoked = false;

        var work = new AbilityRunScriptsWork(
            hasActiveFamiliar: id =>
            {
                hasActiveChecked = true;
                Assert.Equal(steamId, id);
                return true;
            },
            hasDismissedFamiliar: id =>
            {
                hasDismissedChecked = true;
                Assert.Equal(steamId, id);
                return false;
            },
            familiarResolver: playerCharacter =>
            {
                resolverInvoked = true;
                Assert.Equal(player, playerCharacter);
                return familiar;
            },
            familiarBuffChecker: (resolvedFamiliar, buff) =>
            {
                buffChecked = true;
                Assert.Equal(familiar, resolvedFamiliar);
                Assert.Equal(
                    AbilityRunScriptsWork.ToGuidHash(vanishBuff),
                    AbilityRunScriptsWork.ToGuidHash(buff));
                return false;
            },
            familiarAutoCallRegistrar: (playerCharacter, resolvedFamiliar) =>
            {
                autoCall.Add((playerCharacter, resolvedFamiliar));
            },
            familiarDismissalDelegate: (playerCharacter, resolvedFamiliar, resolvedUser, resolvedSteamId) =>
            {
                dismissals.Add((playerCharacter, resolvedFamiliar, resolvedUser, resolvedSteamId));
            },
            vanishBuff: vanishBuff);

        work.AddWaypointAbilityGroup(abilityGroup);

        work.AddCastStartedEvent(new AbilityRunScriptsWork.CastStartedEventData(
            eventEntity,
            character,
            abilityGroup,
            PlayerCharacter: player,
            SteamId: steamId,
            User: null));

        var registrar = new RecordingRegistrar();
        var context = FactoryTestUtilities.CreateContext(
            registrar,
            forEachEntity: (query, action) =>
            {
                requestedQueries.Add(query);
                if (query.Equals(work.CastStartedQuery))
                {
                    action(eventEntity);
                }
            });

        FactoryTestUtilities.OnCreate(work, context);
        FactoryTestUtilities.OnUpdate(work, context);

        Assert.Contains(work.PostCastEndedQuery, requestedQueries);
        Assert.Contains(work.CastStartedQuery, requestedQueries);

        Assert.True(hasActiveChecked);
        Assert.True(hasDismissedChecked);
        Assert.True(resolverInvoked);
        Assert.True(buffChecked);

        Assert.Single(autoCall);
        Assert.Equal((player, familiar), autoCall[0]);

        Assert.Single(dismissals);
        var dismissal = dismissals[0];
        Assert.Equal(player, dismissal.Player);
        Assert.Equal(familiar, dismissal.Familiar);
        Assert.Null(dismissal.User);
        Assert.Equal(steamId, dismissal.SteamId);
    }
}
