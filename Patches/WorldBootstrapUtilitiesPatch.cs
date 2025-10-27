using System;
using System.Collections.Generic;
using Bloodcraft.Services;
using Bloodcraft.Systems;
using Bloodcraft.Systems.Quests;
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
    static readonly List<Type> _registerSystems =
    [
        typeof(QuestTargetSystem),
        typeof(PrimalWarEventSystem)
    ];

    static readonly MethodInfo _getOrCreate = typeof(World)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .First(m =>
            m.Name == nameof(World.GetOrCreateSystemManaged) &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 0
        );

    [HarmonyPatch(typeof(WorldBootstrapUtilities), nameof(WorldBootstrapUtilities.AddSystemsToWorld))]
    [HarmonyPrefix]
    public static void Prefix(World world, WorldBootstrap worldConfig, WorldSystemConfig worldSystemConfig)
    {
        try
        {
            if (world.Name.Equals("Server"))
            {
                var updateGroup = TestHooks.CreateUpdateGroup?.Invoke(world)
                    ?? world.GetOrCreateSystemManaged<UpdateGroup>();

                foreach (Type type in _registerSystems)
                {
                    if (!ShouldRegisterPrimalRifts() && type == typeof(PrimalWarEventSystem))
                        continue;

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
        }
        catch (Exception e)
        {
            Plugin.LogInstance.LogError($"[WorldBootstrap_Server.AddSystemsToWorld] Exception: {e}");
        }
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
        var systemInstance = (ComponentSystemBase)getOrCreate.Invoke(world, null);

        group.AddSystemToUpdateList(systemInstance);
    }

    internal static class TestHooks
    {
        internal static Func<World, UpdateGroup>? CreateUpdateGroup;
        internal static Action<World, UpdateGroup, Type>? RegisterSystem;
        internal static Action<UpdateGroup>? SortSystems;
        internal static Func<bool>? ElitePrimalRiftsProvider;

        internal static void Reset()
        {
            CreateUpdateGroup = null;
            RegisterSystem = null;
            SortSystems = null;
            ElitePrimalRiftsProvider = null;
        }
    }
}
