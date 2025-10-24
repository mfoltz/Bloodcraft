using System;
using System.Runtime.CompilerServices;
using Bloodcraft.Tests.Stubs;
using Il2CppInterop.Runtime.Startup;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Applies assembly-wide test hooks.
/// </summary>
public static class TestModuleInitializers
{
    /// <summary>
    /// Applies the prefab GUID shim before any tests execute.
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        Il2CppInteropRuntime.Create(new RuntimeConfiguration
        {
            UnityVersion = new Version(1, 0),
            DetourProvider = new NullDetourProvider()
        });
        PrefabGuidTestShim.EnsurePatched();
    }
}
