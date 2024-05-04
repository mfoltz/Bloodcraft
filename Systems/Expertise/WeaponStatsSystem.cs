using Cobalt.Core;

namespace Cobalt.Systems.Expertise
{
    public class WeaponStatsSystem
    {
        public class PlayerWeaponUtilities
        {
            public static bool ChooseStat(ulong steamId, string weaponType, string statType)
            {
                if (!DataStructures.PlayerWeaponChoices.ContainsKey(steamId))
                    DataStructures.PlayerWeaponChoices[steamId] = [];

                if (!DataStructures.PlayerWeaponChoices[steamId].ContainsKey(weaponType))
                    DataStructures.PlayerWeaponChoices[steamId][weaponType] = [];

                if (DataStructures.PlayerWeaponChoices[steamId][weaponType].Count >= 2)
                {
                    return false; // Only allow 2 stats to be chosen
                }

                DataStructures.PlayerWeaponChoices[steamId][weaponType].Add(statType);
                DataStructures.SavePlayerWeaponChoices();
                return true;
            }

            public static void ResetChosenStats(ulong steamId, string weaponType)
            {
                if (DataStructures.PlayerWeaponChoices.TryGetValue(steamId, out var weaponStatChoices) && weaponStatChoices.TryGetValue(weaponType, out var choices))
                {
                    choices.Clear();
                    DataStructures.SavePlayerWeaponChoices();
                }
            }
        }

        public class WeaponStatManager
        {
            public enum WeaponStatType
            {
                MaxHealth,
                AttackSpeed,
                PhysicalPower,
                SpellPower,
                PhysicalCritChance,
                PhysicalCritDamage,
                SpellCritChance,
                SpellCritDamage
            }

            public static readonly Dictionary<int, WeaponStatType> WeaponStatMap = new()
                {
                    { 0, WeaponStatType.MaxHealth },
                    { 1, WeaponStatType.AttackSpeed },
                    { 2, WeaponStatType.PhysicalPower },
                    { 3, WeaponStatType.SpellPower },
                    { 4, WeaponStatType.PhysicalCritChance },
                    { 5, WeaponStatType.PhysicalCritDamage },
                    { 6, WeaponStatType.SpellCritChance },
                    { 7, WeaponStatType.SpellCritDamage }
                };

            private static readonly Dictionary<WeaponStatType, float> baseCaps = new()
                {
                    {WeaponStatType.MaxHealth, 150},
                    {WeaponStatType.AttackSpeed, 0.15f},
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