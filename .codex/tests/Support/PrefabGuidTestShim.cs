using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Stunlock.Core;

namespace Bloodcraft.Tests.Support;

/// <summary>
/// Provides helpers for disabling the IL2CPP-dependent portions of <see cref="PrefabGUID"/> during tests.
/// </summary>
public static class PrefabGuidTestShim
{
    static readonly Harmony harmony = new("Bloodcraft.Tests.Support.PrefabGuidShim");
    static bool patched;

    /// <summary>
    /// Ensures the shim patches have been applied.
    /// </summary>
    public static void EnsurePatched()
    {
        if (patched)
        {
            return;
        }

        PatchPrefabGuid();
        patched = true;
    }

    static void PatchPrefabGuid()
    {
        MethodBase? cctor = typeof(PrefabGUID).TypeInitializer;
        if (cctor != null)
        {
            MethodInfo skipOriginal = typeof(PrefabGuidTestShim)
                .GetMethod(nameof(SkipOriginal), BindingFlags.NonPublic | BindingFlags.Static)!;
            var processor = harmony.CreateProcessor(cctor);
            processor.AddPrefix(new HarmonyMethod(skipOriginal));
            processor.Patch();
        }

        MethodInfo? getHashCode = AccessTools.Method(typeof(PrefabGUID), nameof(PrefabGUID.GetHashCode));
        if (getHashCode != null)
        {
            MethodInfo prefix = typeof(PrefabGuidTestShim)
                .GetMethod(nameof(PrefabGuidGetHashCodePrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
            var processor = harmony.CreateProcessor(getHashCode);
            processor.AddPrefix(new HarmonyMethod(prefix));
            processor.Patch();
        }

        MethodBase? ctor = AccessTools.Constructor(typeof(PrefabGUID), new[] { typeof(int) });
        if (ctor != null)
        {
            MethodInfo prefix = typeof(PrefabGuidTestShim)
                .GetMethod(nameof(PrefabGuidCtorPrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
            var processor = harmony.CreateProcessor(ctor);
            processor.AddPrefix(new HarmonyMethod(prefix));
            processor.Patch();
        }
    }

    static bool SkipOriginal()
    {
        return false;
    }

    static bool PrefabGuidGetHashCodePrefix(ref int __result)
    {
        __result = 0;
        return false;
    }

    static bool PrefabGuidCtorPrefix(ref PrefabGUID __instance, int guidHash)
    {
        Unsafe.As<PrefabGUID, int>(ref __instance) = guidHash;
        return false;
    }
}
