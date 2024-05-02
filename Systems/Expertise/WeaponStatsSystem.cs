using Bloodstone.API;
using Cobalt.Core;
using ProjectM;
using Steamworks;

namespace Cobalt.Systems.Weapon
{
    public class WeaponStatsSystem
    {
        public class PlayerWeaponUtilities
        {
            public static bool ChooseStat(ulong steamId, string weaponType, string statType)
            {
                if (!DataStructures.PlayerWeaponStatChoices.ContainsKey(steamId))
                    DataStructures.PlayerWeaponStatChoices[steamId] = [];

                if (!DataStructures.PlayerWeaponStatChoices[steamId].ContainsKey(weaponType))
                    DataStructures.PlayerWeaponStatChoices[steamId][weaponType] = [];

                if (DataStructures.PlayerWeaponStatChoices[steamId][weaponType].Count >= 2)
                {
                    return false; // Only allow 2 stats to be chosen
                }

                DataStructures.PlayerWeaponStatChoices[steamId][weaponType].Add(statType);
                DataStructures.SavePlayerWeaponChoices();
                return true;
            }

            public static void ResetChosenStats(ulong steamId, string weaponType)
            {
                if (DataStructures.PlayerWeaponStatChoices.TryGetValue(steamId, out var weaponStatChoices) && weaponStatChoices.TryGetValue(weaponType, out var choices))
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
                CastSpeed,
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
                    { 1, WeaponStatType.CastSpeed },
                    { 2, WeaponStatType.AttackSpeed },
                    { 3, WeaponStatType.PhysicalPower },
                    { 4, WeaponStatType.SpellPower },
                    { 5, WeaponStatType.PhysicalCritChance },
                    { 6, WeaponStatType.PhysicalCritDamage },
                    { 7, WeaponStatType.SpellCritChance },
                    { 8, WeaponStatType.SpellCritDamage }
                };

            private static readonly Dictionary<WeaponStatType, float> baseCaps = new()
                {
                    {WeaponStatType.MaxHealth, 150},
                    {WeaponStatType.CastSpeed, 0.15f},
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