using Bloodcraft.Services;

namespace Bloodcraft.Utilities;

internal static class PlayerUtilities
{
    public static bool GetPlayerBool(ulong steamId, string boolKey)
    {
        return steamId.TryGetPlayerBools(out var bools) && bools[boolKey];
    }
    public static void SetPlayerBool(ulong steamId, string boolKey, bool value)
    {
        if (steamId.TryGetPlayerBools(out var bools))
        {
            bools[boolKey] = value;
            steamId.SetPlayerBools(bools);
        }
    }
    public static void TogglePlayerBool(ulong steamId, string boolKey)
    {
        if (steamId.TryGetPlayerBools(out var bools))
        {
            bools[boolKey] = !bools[boolKey];
            steamId.SetPlayerBools(bools);
        }
    }
}
