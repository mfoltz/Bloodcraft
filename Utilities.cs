using Bloodcraft.Commands;
using Bloodcraft.SystemUtilities.Familiars;
using Stunlock.Core;

namespace Bloodcraft;
internal static class Utilities
{
    public static void StarterKit()
    {
        List<PrefabGUID> kitPrefabs = Core.ParseConfigString(Plugin.KitPrefabs.Value).Select(x => new PrefabGUID(x)).ToList();
        List<int> kitAmounts = [.. Core.ParseConfigString(Plugin.KitQuantities.Value)];
        MiscCommands.KitPrefabs = kitPrefabs.Zip(kitAmounts, (item, amount) => new { item, amount }).ToDictionary(x => x.item, x => x.amount);
    }
    public  static void FamiliarBans()
    {
        List<int> unitBans = Core.ParseConfigString(Plugin.BannedUnits.Value);
        List<string> typeBans = Plugin.BannedTypes.Value.Split(',').Select(s => s.Trim()).ToList();
        if (unitBans.Count > 0) FamiliarUnlockUtilities.ExemptPrefabs = unitBans;
        if (typeBans.Count > 0) FamiliarUnlockUtilities.ExemptTypes = typeBans;
    }
}
