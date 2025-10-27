using System;
using System.Collections.Generic;
using Bloodcraft.Services;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using ProjectM;
using Stunlock.Core;
using System.Reflection;
using Unity.Entities;
#nullable enable

namespace Bloodcraft.Patches;

[HarmonyPatch]
public static class WorldBootstrapPatch
{
    static readonly MethodInfo _getOrCreate = typeof(World)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .First(m =>
            m.Name == nameof(World.GetOrCreateSystemManaged) &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 0
        );

    const string QuestTargetSystemTypeName = "Bloodcraft.Systems.Quests.QuestTargetSystem, Bloodcraft";
    const string PrimalWarEventSystemTypeName = "Bloodcraft.Systems.PrimalWarEventSystem, Bloodcraft";

    [HarmonyPatch(typeof(WorldBootstrapUtilities), nameof(WorldBootstrapUtilities.AddSystemsToWorld))]
    [HarmonyPrefix]
    public static void Prefix(World world, WorldBootstrap worldConfig, WorldSystemConfig worldSystemConfig)
    {
        try
        {
            if (world.Name.Equals("Server"))
            {
                ExecuteServerBootstrap(world);
            }
        }
        catch (Exception e)
        {
            Plugin.LogInstance.LogError($"[WorldBootstrap_Server.AddSystemsToWorld] Exception: {e}");
        }
    }

    static void ExecuteServerBootstrap(World world)
    {
        var updateGroup = TestHooks.CreateUpdateGroup?.Invoke(world)
            ?? world.GetOrCreateSystemManaged<UpdateGroup>();

        Type primalSystemType = ResolvePrimalSystemType();
        bool elitePrimalRiftsEnabled = ShouldRegisterPrimalRifts();

        foreach (Type type in EnumerateSystemTypes(elitePrimalRiftsEnabled, primalSystemType))
        {
            if (TestHooks.RegisterSystem is { } register)
            {
                register(world, updateGroup, type);
            }
            else
            {
                RegisterAndAddSystem(world, updateGroup, type);
            }
        }

        if (TestHooks.SortSystems is { } sort)
        {
            sort(updateGroup);
        }
        else
        {
            updateGroup.SortSystems();
        }
    }

    static IReadOnlyList<Type> EnumerateSystemTypes(bool elitePrimalRiftsEnabled, Type primalSystemType)
    {
        var results = new List<Type>();

        foreach (Type type in ResolveSystemTypes())
        {
            if (!elitePrimalRiftsEnabled && type == primalSystemType)
                continue;

            results.Add(type);
        }

        return results;
    }

    static IEnumerable<Type> ResolveSystemTypes()
    {
        if (TestHooks.GetSystemTypes is { } provider)
        {
            var systemTypes = provider();
            return systemTypes ?? Array.Empty<Type>();
        }

        return DefaultSystemTypes();
    }

    static IEnumerable<Type> DefaultSystemTypes()
    {
        yield return ResolveType(QuestTargetSystemTypeName);
        yield return ResolveType(PrimalWarEventSystemTypeName);
    }

    static Type ResolvePrimalSystemType()
    {
        if (TestHooks.PrimalSystemType is { } overrideType)
        {
            return overrideType;
        }

        return ResolveType(PrimalWarEventSystemTypeName);
    }

    static Type ResolveType(string qualifiedTypeName)
    {
        return Type.GetType(qualifiedTypeName, throwOnError: true)
            ?? throw new InvalidOperationException($"Unable to resolve type '{qualifiedTypeName}'.");
    }

    static bool ShouldRegisterPrimalRifts()
    {
        if (TestHooks.ElitePrimalRiftsProvider is { } provider)
        {
            return provider();
        }

        return ConfigService.ElitePrimalRifts;
    }

    static void RegisterAndAddSystem(this World world, UpdateGroup group, Type systemType)
    {
        ClassInjector.RegisterTypeInIl2Cpp(systemType);

        var getOrCreate = _getOrCreate.MakeGenericMethod(systemType);
        var instance = getOrCreate.Invoke(world, null)
            ?? throw new InvalidOperationException($"Failed to create system '{systemType}'.");
        var systemInstance = (ComponentSystemBase)instance;

        group.AddSystemToUpdateList(systemInstance);
    }

    internal static class TestHooks
    {
        internal static Func<World, UpdateGroup>? CreateUpdateGroup;
        internal static Action<World, UpdateGroup, Type>? RegisterSystem;
        internal static Action<UpdateGroup>? SortSystems;
        internal static Func<bool>? ElitePrimalRiftsProvider;
        internal static Func<IEnumerable<Type>>? GetSystemTypes;
        internal static Type? PrimalSystemType;

        internal static void Reset()
        {
            CreateUpdateGroup = null;
            RegisterSystem = null;
            SortSystems = null;
            ElitePrimalRiftsProvider = null;
            GetSystemTypes = null;
            PrimalSystemType = null;
        }

        internal static bool EvaluateElitePrimalRifts()
        {
            return ShouldRegisterPrimalRifts();
        }

        internal static IReadOnlyList<Type> EnumerateSystemsForTests(bool elitePrimalRiftsEnabled)
        {
            var primalSystemType = ResolvePrimalSystemType();
            return EnumerateSystemTypes(elitePrimalRiftsEnabled, primalSystemType);
        }
    }
}
