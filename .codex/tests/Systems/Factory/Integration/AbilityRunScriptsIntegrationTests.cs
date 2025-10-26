using Bloodcraft.Tests.Systems.Factory;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Xunit;

namespace Bloodcraft.Tests.Systems.Factory.Integration;

/// <summary>
/// Exercises <see cref="AbilityRunScriptsWork"/> inside a managed Unity world to verify runtime wiring.
/// </summary>
public sealed class AbilityRunScriptsIntegrationTests
{
    [Fact]
    public void Update_ProcessesEventsAndRefreshesLookups()
    {
        if (!IsUnityWorldAvailable())
            return;

        var shapeshiftAbilityGroup = new PrefabGUID(0x1234);
        var waypointAbilityGroup = new PrefabGUID(0x9876);
        var vanishBuff = new PrefabGUID(0x4321);

        bool cooldownInvoked = false;
        EntityHandle? cooldownCharacter = null;
        PrefabGUID? cooldownGroup = null;
        float? cooldownValue = null;
        EntityHandle? autoCallFamiliar = null;
        EntityHandle? dismissalCharacter = null;
        EntityHandle? dismissalFamiliar = null;
        ulong? dismissalSteamId = null;
        EntityHandle playerHandle = default;
        EntityHandle familiarHandle = default;

        using var harness = new FactoryWorldHarness<AbilityRunScriptsWork>(() => new AbilityRunScriptsWork(
            cooldownSetter: (character, group, cooldown) =>
            {
                cooldownInvoked = true;
                cooldownCharacter = character;
                cooldownGroup = group;
                cooldownValue = cooldown;
            },
            shapeshiftResolver: (PrefabGUID group, out float cooldown) =>
            {
                if (group.Equals(shapeshiftAbilityGroup))
                {
                    cooldown = 30f;
                    return true;
                }

                cooldown = 0f;
                return false;
            },
            hasActiveFamiliar: steamId => steamId == 123UL,
            hasDismissedFamiliar: _ => false,
            familiarResolver: player => player == playerHandle ? familiarHandle : null,
            familiarBuffChecker: (_, __) => false,
            familiarAutoCallRegistrar: (_, familiar) => autoCallFamiliar = familiar,
            familiarDismissalDelegate: (player, familiar, _, steamId) =>
            {
                dismissalCharacter = player;
                dismissalFamiliar = familiar;
                dismissalSteamId = steamId;
            },
            vanishBuff: vanishBuff,
            waypointAbilityGroups: new[] { waypointAbilityGroup }));

        var manager = harness.EntityManager;
        Entity characterEntity = manager.CreateEntity();
        Entity playerEntity = manager.CreateEntity();
        Entity familiarEntity = manager.CreateEntity();
        Entity postCastEntity = manager.CreateEntity();
        Entity castStartedEntity = manager.CreateEntity();

        manager.AddComponentData(postCastEntity, default(AbilityPostCastEndedEvent));
        manager.AddComponentData(castStartedEntity, default(AbilityCastStartedEvent));

        var characterHandle = new EntityHandle(characterEntity.Index);
        playerHandle = new EntityHandle(playerEntity.Index);
        familiarHandle = new EntityHandle(familiarEntity.Index);
        var postCastHandle = new EntityHandle(postCastEntity.Index);
        var castStartedHandle = new EntityHandle(castStartedEntity.Index);

        var work = harness.Work;
        work.AddPostCastEvent(new AbilityRunScriptsWork.PostCastEndedEventData(
            postCastHandle,
            characterHandle,
            shapeshiftAbilityGroup,
            HasVBloodAbility: false,
            PlayerCharacter: playerHandle,
            CharacterIsExoForm: true));
        work.AddCastStartedEvent(new AbilityRunScriptsWork.CastStartedEventData(
            castStartedHandle,
            characterHandle,
            waypointAbilityGroup,
            playerHandle,
            SteamId: 123UL,
            User: null));

        harness.Update();

        Assert.True(work.RuntimeRefreshExecuted);
        Assert.NotEmpty(harness.System.RefreshActions);

        Assert.True(cooldownInvoked);
        Assert.Equal(characterHandle, cooldownCharacter);
        Assert.Equal(shapeshiftAbilityGroup, cooldownGroup);
        Assert.Equal(30f, cooldownValue);

        Assert.Equal(familiarHandle, autoCallFamiliar);
        Assert.Equal(playerHandle, dismissalCharacter);
        Assert.Equal(familiarHandle, dismissalFamiliar);
        Assert.Equal(123UL, dismissalSteamId);

        _ = harness.System.GetComponentLookup<AbilityPostCastEndedEvent>(isReadOnly: true);
        Assert.True(manager.HasComponent<AbilityPostCastEndedEvent>(postCastEntity));
    }

    static bool IsUnityWorldAvailable()
    {
        try
        {
            var world = new World("AbilityRunScriptsIntegrationTests.Probe");
            try
            {
                return true;
            }
            finally
            {
                world.Dispose();
            }
        }
        catch (TypeInitializationException exception) when (ContainsDllNotFound(exception))
        {
            return false;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    static bool ContainsDllNotFound(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is DllNotFoundException)
                return true;
        }

        return false;
    }
}
