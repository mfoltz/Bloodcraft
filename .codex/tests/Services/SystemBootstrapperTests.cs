using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Bloodcraft.Patches;
using Bloodcraft.Systems;
using Bloodcraft.Systems.Quests;
using ProjectM;
using Unity.Entities;
using Xunit;
#nullable enable

namespace Bloodcraft.Tests.Services;

public sealed class SystemBootstrapperTests : TestHost
{
    static readonly List<Type> registeredSystemTypes = new();

    [Fact]
    public void Prefix_WithElitePrimalRiftsDisabled_RegistersQuestTargetSystemOnly()
    {
        using var worldScope = CreateServerWorld();
        var world = worldScope.Instance;

        ConfigureTestHooks(elitePrimalRiftsEnabled: false);

        try
        {
            WorldBootstrapPatch.Prefix(world, null!, null!);

            Assert.Contains(typeof(QuestTargetSystem), registeredSystemTypes);
            Assert.DoesNotContain(typeof(PrimalWarEventSystem), registeredSystemTypes);
            Assert.Single(registeredSystemTypes);
        }
        finally
        {
            registeredSystemTypes.Clear();
            WorldBootstrapPatch.TestHooks.Reset();
        }
    }

    [Fact]
    public void Prefix_WithElitePrimalRiftsEnabled_RegistersQuestTargetAndPrimalWarSystems()
    {
        using var overrides = WithConfigOverrides(("ElitePrimalRifts", true));
        using var worldScope = CreateServerWorld();
        var world = worldScope.Instance;

        ConfigureTestHooks(elitePrimalRiftsEnabled: true);

        try
        {
            WorldBootstrapPatch.Prefix(world, null!, null!);

            Assert.Contains(typeof(QuestTargetSystem), registeredSystemTypes);
            Assert.Contains(typeof(PrimalWarEventSystem), registeredSystemTypes);
            Assert.Equal(2, registeredSystemTypes.Count);
        }
        finally
        {
            registeredSystemTypes.Clear();
            WorldBootstrapPatch.TestHooks.Reset();
        }
    }

    protected override void ResetState()
    {
        base.ResetState();
        registeredSystemTypes.Clear();
        WorldBootstrapPatch.TestHooks.Reset();
    }

    static WorldScope CreateServerWorld() => new();

    static void ConfigureTestHooks(bool elitePrimalRiftsEnabled)
    {
        WorldBootstrapPatch.TestHooks.CreateUpdateGroup = _ => CreateStubUpdateGroup();
        WorldBootstrapPatch.TestHooks.SortSystems = _ => { };
        WorldBootstrapPatch.TestHooks.RegisterSystem = (_, __, type) => registeredSystemTypes.Add(type);
        WorldBootstrapPatch.TestHooks.ElitePrimalRiftsProvider = () => elitePrimalRiftsEnabled;
    }

    static UpdateGroup CreateStubUpdateGroup()
    {
        return (UpdateGroup)FormatterServices.GetUninitializedObject(typeof(UpdateGroup));
    }

    static void SetFieldIfExists(object target, string fieldName, object? value)
    {
        FieldInfo? field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        field?.SetValue(target, value);
    }

    sealed class WorldScope : IDisposable
    {
        public WorldScope()
        {
            Instance = (World)FormatterServices.GetUninitializedObject(typeof(World));
            SetFieldIfExists(Instance, "<Name>k__BackingField", "Server");
            SetFieldIfExists(Instance, "m_Name", "Server");
        }

        public World Instance { get; }

        public void Dispose()
        {
            TryDisposeWorld(Instance);
        }
    }

    static void TryDisposeWorld(World world)
    {
        try
        {
            world.Dispose();
        }
        catch
        {
            // Ignored: the world is a test double created without invoking the Unity.Entities runtime.
        }
    }
}
