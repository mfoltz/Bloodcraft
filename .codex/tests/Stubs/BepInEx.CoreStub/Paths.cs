using System.IO;

namespace BepInEx;

/// <summary>
/// Minimal stub of the BepInEx Paths API used by the test harness.
/// </summary>
public static class Paths
{
    /// <summary>
    /// Gets or sets the configuration root directory.
    /// </summary>
    public static string ConfigPath
    {
        get => configPath;
        set
        {
            Directory.CreateDirectory(value);
            configPath = value;
        }
    }

    static string configPath = Path.Combine(Path.GetTempPath(), "bepinex_stub");
}
