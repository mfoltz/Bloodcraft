namespace Cobalt.Systems.Expertise
{
    public class WeaponStats
    {
        public class PlayerWeaponUtilities
        {
            public static bool ChooseStat(ulong steamId, ExpertiseSystem.WeaponType weaponType, WeaponStatManager.WeaponStatType statType)
            {
                if (!Core.DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStats) || !weaponStats.TryGetValue(weaponType, out var choices))
                {
                    choices = [];
                    Core.DataStructures.PlayerWeaponChoices[steamId][weaponType] = choices;
                }

                if (Core.DataStructures.PlayerWeaponChoices[steamId][weaponType].Count >= 2)
                {
                    return false; // Only allow 2 stats to be chosen
                }

                Core.DataStructures.PlayerWeaponChoices[steamId][weaponType].Add(statType);
                Core.DataStructures.SavePlayerWeaponChoices();
                return true;
            }

            public static void ResetChosenStats(ulong steamId, ExpertiseSystem.WeaponType weaponType)
            {
                if (Core.DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStatChoices) && weaponStatChoices.TryGetValue(weaponType, out var choices))
                {
                    choices.Clear();
                    Core.DataStructures.SavePlayerWeaponChoices();
                }
            }
        }

        public class WeaponStatManager
        {
            public enum WeaponStatType
            {
                PhysicalPower,
                SpellPower,
                PhysicalCritChance,
                PhysicalCritDamage,
                SpellCritChance,
                SpellCritDamage
            }

            public static readonly Dictionary<int, WeaponStatType> WeaponStatMap = new()
                {
                    { 1, WeaponStatType.PhysicalPower },
                    { 2, WeaponStatType.SpellPower },
                    { 3, WeaponStatType.PhysicalCritChance },
                    { 4, WeaponStatType.PhysicalCritDamage },
                    { 5, WeaponStatType.SpellCritChance },
                    { 6, WeaponStatType.SpellCritDamage }
                };

            private static readonly Dictionary<WeaponStatType, float> baseCaps = new()
                {
                    {WeaponStatType.PhysicalPower, 15},
                    {WeaponStatType.SpellPower, 15},
                    {WeaponStatType.PhysicalCritChance, 0.15f},
                    {WeaponStatType.PhysicalCritDamage, 0.75f},
                    {WeaponStatType.SpellCritChance, 0.15f},
                    {WeaponStatType.SpellCritDamage, 0.75f}
                };

            public static Dictionary<WeaponStatType, float> BaseCaps
            {
                get => baseCaps;
            }
        }
    }
}