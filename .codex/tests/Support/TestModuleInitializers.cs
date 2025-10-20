using System.Runtime.CompilerServices;

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
        PrefabGuidTestShim.EnsurePatched();
    }
}
