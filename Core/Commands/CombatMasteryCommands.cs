using Cobalt.Systems.Weapon;
using LibCpp2IL.BinaryStructures;
using ProjectM;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Weapon.WeaponStatsSystem;

namespace Cobalt.Core.Commands
{
    public static class CombatMasteryCommands
    {
        [Command(name: "getMasteryProgress", shortHand: "gmp", adminOnly: false, usage: ".gmp", description: "Display your current mastery progress.")]
        public static void GetMasteryCommand(ChatCommandContext ctx)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();
            CombatMasterySystem.WeaponType weaponType = CombatMasterySystem.GetWeaponTypeFromPrefab(weapon);

            if (CombatMasterySystem.weaponMasteries.TryGetValue(weaponType, out var masteryDictionary) && masteryDictionary.TryGetValue(steamID, out var mastery))
            {
                ctx.Reply($"You have <color=white>{mastery.Value}</color> proficiency points for your {weaponType}.");
            }
            else
            {
                ctx.Reply("You haven't earned any mastery points for this weapon type yet.");
            }
        }

        [Command(name: "logMasteryProgress", shortHand: "lmp", adminOnly: false, usage: ".lmp", description: "Toggles mastery progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["CombatLogging"] = !bools["CombatLogging"];
            }
            ctx.Reply($"Combat mastery logging is now {(bools["CombatLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "setWeaponStat", shortHand: "sws", adminOnly: true, usage: ".sws <Stat>", description: "Choose a weapon stat to enhance based on your weapon mastery.")]
        public static void SetWeaponStat(ChatCommandContext ctx, string statChoice)
        {

            string statType = statChoice.ToLower();
            // If not, try parsing it from the string representation
            if (!Enum.TryParse<WeaponStatManager.WeaponFocusSystem.WeaponStatType>(statType, true, out _))
            {
                ctx.Reply("Invalid stat type.");
                return;
            }
            WeaponStatsSystem.WeaponStatManager.WeaponFocusSystem.WeaponStatType statTypeChoice = Enum.Parse<WeaponStatsSystem.WeaponStatManager.WeaponFocusSystem.WeaponStatType>(statType, true);
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();

            

            // Ensure that there is a dictionary for the player's stats
            if (!DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponsStats))
            {
                weaponsStats = [];
                DataStructures.PlayerWeaponStats[steamID] = weaponsStats;
            }

            // Ensure that there are stats registered for the specific weapon
            if (!weaponsStats.TryGetValue(weapon, out var stats))
            {
                stats = new PlayerWeaponStats();
                weaponsStats[weapon] = stats;
            }

            // Choose a stat for the specific weapon stats instance
            stats.ChooseStat(statTypeChoice);

            // Optionally, save or update external storage
            DataStructures.SavePlayerWeaponStats();
            ctx.Reply($"Stat {statType} has been chosen for your weapon.");
        }

        [Command(name: "resetWeaponStats", shortHand: "rws", adminOnly: true, usage: ".rws", description: "Reset the stat choices for a player's currently equipped weapon stats.")]
        public static void ResetWeaponStats(ChatCommandContext ctx)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();

            if (DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponsStats) && weaponsStats.TryGetValue(weapon, out var stats))
            {
                stats.ResetChosenStats();
                DataStructures.SavePlayerWeaponStats();
                ctx.Reply("Your weapon stats have been reset for the currently equipped weapon.");
            }
            else
            {
                ctx.Reply("No stats to reset for this weapon.");
            }
        }
    }
}