using System.Collections.Generic;
using Stunlock.Core;

namespace Bloodcraft.Services
{
    public static class ConfigService
    {
        public static string BannedUnits = string.Empty;
        public static string BannedTypes = string.Empty;
        public static string QuestRewardAmounts = string.Empty;
        public static string QuestRewards = string.Empty;
        public static string KitQuantities = string.Empty;
        public static string KitPrefabs = string.Empty;
    }

    public static class LocalizationService
    {
        public static Dictionary<PrefabGUID, string> PrefabGuidNames { get; } = new();
    }
}

namespace Bloodcraft.Commands
{
    public static class MiscCommands
    {
        public static Dictionary<PrefabGUID, int> StarterKitItemPrefabGUIDs { get; } = new();
    }
}

namespace Bloodcraft.Patches
{
    public static class AbilityRunScriptsSystemPatch
    {
        public static void AddClassSpell(PrefabGUID spell, int index) { }
    }
}

namespace Bloodcraft.Systems.Familiars
{
    public static class FamiliarUnlockSystem
    {
        public static HashSet<PrefabGUID> ConfiguredPrefabGuidBans { get; } = new();
        public static HashSet<UnitCategory> ConfiguredCategoryBans { get; } = new();
    }

    public enum UnitCategory { }
}

namespace Bloodcraft.Systems.Quests
{
    public static class QuestSystem
    {
        public static Dictionary<PrefabGUID, int> QuestRewards { get; } = new();
    }
}

namespace Bloodcraft.Utilities
{
    public static class Classes
    {
        public static Dictionary<string, string> ClassSpellsMap { get; } = new();
    }
}

namespace Stunlock.Core
{
    public readonly struct PrefabGUID : System.IEquatable<PrefabGUID>
    {
        readonly int _hash;
        public PrefabGUID(int hash) => _hash = hash;
        public bool HasValue() => _hash != 0;
        public bool Equals(PrefabGUID other) => _hash == other._hash;
        public override bool Equals(object obj) => obj is PrefabGUID other && Equals(other);
        public override int GetHashCode() => _hash;
    }
}

namespace ProjectM
{
    public class PrefabCollectionSystem
    {
        public Dictionary<string, Stunlock.Core.PrefabGUID> SpawnableNameToPrefabGuidDictionary { get; } = new();
    }
}

namespace Bloodcraft
{
    public static class Core
    {
        public static SystemService SystemService { get; } = new();
        public static Logger Log { get; } = new();
    }

    public class SystemService
    {
        public ProjectM.PrefabCollectionSystem PrefabCollectionSystem { get; } = new();
    }

    public class Logger
    {
        public void LogWarning(string message) { }
    }
}
