using Bloodcraft.Systems;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using ProjectM;
using Stunlock.Core;
using System.Reflection;
using Unity.Entities;
using static Bloodcraft.Plugin;

namespace Bloodcraft;

[HarmonyPatch]
internal static class VSystemManager
{
    static HashSet<Type> AdditionalSystems { get; } =
    [
        typeof(PrimalWarEventSystem),
        typeof(QuestTargetSystem),
        typeof(ServantUpgradeSystem),
        //typeof(FamiliarServantDeathContainerSystem),
    ];

    static MethodInfo GetOrCreateSystemManaged { get; } = typeof(World)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .First(m =>
            m.Name == nameof(World.GetOrCreateSystemManaged) &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 0
        );

    static void AddSystem(World world, ComponentSystemGroup systemGroup, Type systemType)
    {
        ClassInjector.RegisterTypeInIl2Cpp(systemType);
        var getOrCreate = GetOrCreateSystemManaged.MakeGenericMethod(systemType);

        ComponentSystemBase systemInstance = (ComponentSystemBase)getOrCreate.Invoke(world, null);
        systemGroup.AddSystemToUpdateList(systemInstance);
    }

    [HarmonyPatch(typeof(WorldBootstrapUtilities), nameof(WorldBootstrapUtilities.AddSystemsToWorld))]
    [HarmonyPrefix]
    static void Prefix(World world, WorldBootstrap worldConfig, WorldSystemConfig worldSystemConfig)
    {
        try
        {
            if (world.IsServerWorld())
            {
                var updateGroup = world.GetOrCreateSystemManaged<UpdateGroup>();

                foreach (var system in AdditionalSystems)
                {
                    AddSystem(world, updateGroup, system);
                }

                updateGroup.SortSystems();
            }
        }
        catch (Exception ex)
        {
            MiniBehaviour.LogSource.LogWarning($"{ex}");
        }
    }
}
