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
        ConfigDirectorySandbox.InstallForAssemblyLifetime();
        PrefabGuidTestShim.EnsurePatched();
    }
}
