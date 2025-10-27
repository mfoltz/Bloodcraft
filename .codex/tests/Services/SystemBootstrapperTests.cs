using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Bloodcraft.Systems;
using Bloodcraft.Systems.Quests;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using ProjectM;
using Unity.Entities;
using Xunit;
using PatchWorldBootstrap = Bloodcraft.Patches.WorldBootstrapPatch;

namespace Bloodcraft.Tests.Services;

public sealed class SystemBootstrapperTests : TestHost
{
    static bool elitePrimalRiftsEnabled;
    static readonly List<Type> registeredSystemTypes = new();

    readonly Harmony harmony;

    public SystemBootstrapperTests()
    {
        harmony = new Harmony($"Bloodcraft.Tests.SystemBootstrapperTests.{Guid.NewGuid():N}");
        PatchClassInjector();
        PatchElitePrimalRiftsAccessor();
        PatchRegisterAndAddSystem();
        PatchWorldDisposal();
        PatchUpdateGroupSorting();
        PatchUpdateGroupFactory();
    }

    [Fact]
    public void Prefix_WithElitePrimalRiftsDisabled_RegistersQuestTargetSystemOnly()
    {
        var world = CreateServerWorld();

        SetElitePrimalRifts(false);
        try
        {
            PatchWorldBootstrap.Prefix(world, null!, null!);

            Assert.Contains(typeof(QuestTargetSystem), registeredSystemTypes);
            Assert.DoesNotContain(typeof(PrimalWarEventSystem), registeredSystemTypes);
            Assert.Single(registeredSystemTypes);
        }
        finally
        {
            SetElitePrimalRifts(false);
            registeredSystemTypes.Clear();
            world.Dispose();
        }
    }

    [Fact]
    public void Prefix_WithElitePrimalRiftsEnabled_RegistersQuestTargetAndPrimalWarSystems()
    {
        using var overrides = WithConfigOverrides(("ElitePrimalRifts", true));
        var world = CreateServerWorld();

        SetElitePrimalRifts(true);
        try
        {
            PatchWorldBootstrap.Prefix(world, null!, null!);

            Assert.Contains(typeof(QuestTargetSystem), registeredSystemTypes);
            Assert.Contains(typeof(PrimalWarEventSystem), registeredSystemTypes);
            Assert.Equal(2, registeredSystemTypes.Count);
        }
        finally
        {
            SetElitePrimalRifts(false);
            registeredSystemTypes.Clear();
            world.Dispose();
        }
    }

    protected override void ResetState()
    {
        base.ResetState();
        SetElitePrimalRifts(false);
        registeredSystemTypes.Clear();
    }

    public override void Dispose()
    {
        harmony.UnpatchSelf();
        SetElitePrimalRifts(false);
        registeredSystemTypes.Clear();
        base.Dispose();
    }

    static World CreateServerWorld()
    {
        var world = (World)FormatterServices.GetUninitializedObject(typeof(World));

        SetFieldIfExists(world, "<Name>k__BackingField", "Server");
        SetFieldIfExists(world, "m_Name", "Server");

        return world;
    }

    void PatchClassInjector()
    {
        MethodInfo? registerMethod = typeof(ClassInjector).GetMethod(
            nameof(ClassInjector.RegisterTypeInIl2Cpp),
            BindingFlags.Public | BindingFlags.Static,
            new[] { typeof(Type) }
        );

        if (registerMethod == null)
        {
            throw new InvalidOperationException("Unable to locate ClassInjector.RegisterTypeInIl2Cpp(Type) method for test setup.");
        }

        var skipMethod = typeof(SystemBootstrapperTests).GetMethod(
            nameof(SkipRegisterTypeInIl2Cpp),
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (skipMethod == null)
        {
            throw new InvalidOperationException("Unable to locate SkipRegisterTypeInIl2Cpp helper method.");
        }

        harmony.Patch(registerMethod, prefix: new HarmonyMethod(skipMethod));
    }

    void PatchElitePrimalRiftsAccessor()
    {
        PropertyInfo? property = typeof(PatchWorldBootstrap).GetProperty(
            "ElitePrimalRifts",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        MethodInfo? getter = property?.GetMethod;
        if (getter == null)
        {
            throw new InvalidOperationException("Unable to locate ElitePrimalRifts getter on WorldBootstrapPatch.");
        }

        var overrideMethod = typeof(SystemBootstrapperTests).GetMethod(
            nameof(OverrideElitePrimalRifts),
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (overrideMethod == null)
        {
            throw new InvalidOperationException("Unable to locate ElitePrimalRifts override method.");
        }

        harmony.Patch(getter, prefix: new HarmonyMethod(overrideMethod));
    }

    void PatchRegisterAndAddSystem()
    {
        MethodInfo? registerMethod = typeof(PatchWorldBootstrap).GetMethod(
            "RegisterAndAddSystem",
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (registerMethod == null)
        {
            throw new InvalidOperationException("Unable to locate RegisterAndAddSystem on WorldBootstrapPatch.");
        }

        var prefix = typeof(SystemBootstrapperTests).GetMethod(
            nameof(RecordRegisteredSystem),
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (prefix == null)
        {
            throw new InvalidOperationException("Unable to locate register-system recording prefix.");
        }

        harmony.Patch(registerMethod, prefix: new HarmonyMethod(prefix));
    }

    void PatchWorldDisposal()
    {
        MethodInfo? disposeMethod = typeof(World).GetMethod(
            nameof(World.Dispose),
            BindingFlags.Instance | BindingFlags.Public
        );

        if (disposeMethod == null)
        {
            throw new InvalidOperationException("Unable to locate World.Dispose method for test setup.");
        }

        var prefix = typeof(SystemBootstrapperTests).GetMethod(
            nameof(SkipWorldDispose),
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (prefix == null)
        {
            throw new InvalidOperationException("Unable to locate World.Dispose override prefix.");
        }

        harmony.Patch(disposeMethod, prefix: new HarmonyMethod(prefix));
    }

    void PatchUpdateGroupSorting()
    {
        MethodInfo? sortMethod = typeof(UpdateGroup).GetMethod(
            "SortSystems",
            BindingFlags.Instance | BindingFlags.Public
        );

        if (sortMethod == null)
        {
            throw new InvalidOperationException("Unable to locate UpdateGroup.SortSystems method for test setup.");
        }

        var prefix = typeof(SystemBootstrapperTests).GetMethod(
            nameof(SkipSortSystems),
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (prefix == null)
        {
            throw new InvalidOperationException("Unable to locate UpdateGroup.SortSystems override prefix.");
        }

        harmony.Patch(sortMethod, prefix: new HarmonyMethod(prefix));
    }

    void PatchUpdateGroupFactory()
    {
        MethodInfo? definition = typeof(World)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(method =>
                method.Name == nameof(World.GetOrCreateSystemManaged)
                && method.IsGenericMethodDefinition
                && method.GetParameters().Length == 0
            );

        if (definition == null)
        {
            throw new InvalidOperationException("Unable to locate generic World.GetOrCreateSystemManaged() definition.");
        }

        MethodInfo method = definition.MakeGenericMethod(typeof(UpdateGroup));

        var prefix = typeof(SystemBootstrapperTests).GetMethod(
            nameof(CreateStubUpdateGroup),
            BindingFlags.Static | BindingFlags.NonPublic
        );

        if (prefix == null)
        {
            throw new InvalidOperationException("Unable to locate stub UpdateGroup creation prefix.");
        }

        harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    }

    static bool SkipRegisterTypeInIl2Cpp(Type _)
    {
        return false;
    }

    static void SetElitePrimalRifts(bool enabled)
    {
        elitePrimalRiftsEnabled = enabled;
    }

    static void SetFieldIfExists(object target, string fieldName, object? value)
    {
        FieldInfo? field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        field?.SetValue(target, value);
    }

    static bool OverrideElitePrimalRifts(ref bool __result)
    {
        __result = elitePrimalRiftsEnabled;
        return false;
    }

    static bool RecordRegisteredSystem(World _, UpdateGroup __, Type systemType)
    {
        registeredSystemTypes.Add(systemType);
        return false;
    }

    static bool SkipWorldDispose()
    {
        return false;
    }

    static bool SkipSortSystems()
    {
        return false;
    }

    static bool CreateStubUpdateGroup(World __instance, ref UpdateGroup __result)
    {
        __result = (UpdateGroup)FormatterServices.GetUninitializedObject(typeof(UpdateGroup));
        return false;
    }
}
