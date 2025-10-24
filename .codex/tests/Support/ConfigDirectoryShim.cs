using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using Bloodcraft.Services;
using HarmonyLib;

namespace Bloodcraft.Tests.Support;

static class ConfigDirectoryShim
{
    static readonly object SyncRoot = new();
    static readonly Harmony Harmony = new("Bloodcraft.Tests.ConfigDirectories");
    static bool patched;
    static List<string>? directories;
    static string? configRoot;

    public static void EnsureInitialized()
    {
        if (!patched)
        {
            InstallPatch();
        }

        _ = ConfigService.ConfigInitialization.DirectoryPaths;
    }

    static void InstallPatch()
    {
        PatchBepInExPaths();

        var getter = typeof(ConfigService.ConfigInitialization)
            .GetProperty("DirectoryPaths", BindingFlags.Public | BindingFlags.Static)!
            .GetMethod!;

        Harmony.Patch(getter, prefix: new HarmonyMethod(typeof(ConfigDirectoryShim).GetMethod(nameof(OverrideDirectoryPaths), BindingFlags.NonPublic | BindingFlags.Static)));
        patched = true;
    }

    static void PatchBepInExPaths()
    {
        var pathsType = typeof(Paths);
        var cctor = pathsType.TypeInitializer;
        if (cctor != null)
        {
            Harmony.Patch(cctor, prefix: new HarmonyMethod(typeof(ConfigDirectoryShim).GetMethod(nameof(SkipPathsInitializer), BindingFlags.NonPublic | BindingFlags.Static)));
        }

        var configGetter = AccessTools.PropertyGetter(pathsType, nameof(Paths.ConfigPath));
        if (configGetter != null)
        {
            Harmony.Patch(configGetter, prefix: new HarmonyMethod(typeof(ConfigDirectoryShim).GetMethod(nameof(GetConfigPath), BindingFlags.NonPublic | BindingFlags.Static)));
        }
    }

    static bool SkipPathsInitializer() => false;

    static bool GetConfigPath(ref string __result)
    {
        lock (SyncRoot)
        {
            if (directories is null)
            {
                var tuple = CreateDirectoriesInternal();
                configRoot = tuple.root.FullName;
                directories = tuple.directories;
            }
        }

        __result = configRoot;
        return false;
    }

    static bool OverrideDirectoryPaths(ref List<string> __result)
    {
        lock (SyncRoot)
        {
            if (directories is null)
            {
                var tuple = CreateDirectoriesInternal();
                configRoot ??= tuple.root.FullName;
                directories = tuple.directories;
            }
        }

        __result = directories;
        return false;
    }

    static (DirectoryInfo root, List<string> directories) CreateDirectoriesInternal()
    {
        var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Bloodcraft.Tests", Guid.NewGuid().ToString("N")));
        var directories = Enumerable.Range(0, 12)
            .Select(index =>
            {
                var path = Path.Combine(root.FullName, index.ToString(CultureInfo.InvariantCulture));
                Directory.CreateDirectory(path);
                return path;
            })
            .ToList();

        return (root, directories);
    }
}
