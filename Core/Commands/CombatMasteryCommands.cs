using Cobalt.Hooks;
using Cobalt.Systems.Weapon;
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
            WeaponMasterySystem.WeaponType weaponType = WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon);
            string weaponString = weaponType.ToString();
            if (DataStructures.weaponMasteryMap.TryGetValue(weaponString, out var masteryDictionary) && masteryDictionary.TryGetValue(steamID, out var mastery))
            {
                ctx.Reply($"You are level <color=white>{mastery.Key}</color> in {weaponType}.");
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

        [Command(name: "chooseWeaponStat", shortHand: "sws", adminOnly: true, usage: ".cws <Stat>", description: "Choose a weapon stat to enhance based on your weapon mastery.")]
        public static void ChooseWeaponStat(ChatCommandContext ctx, string statChoice)
        {
            string statType = statChoice.ToLower();
            // If not, try parsing it from the string representation
            if (!Enum.TryParse<WeaponStatManager.WeaponStatType>(statType, true, out _))
            {
                ctx.Reply("Invalid stat type.");
                return;
            }
            WeaponStatsSystem.WeaponStatManager.WeaponStatType statTypeChoice = Enum.Parse<WeaponStatsSystem.WeaponStatManager.WeaponStatType>(statType, true);
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();

            // Ensure that there is a dictionary for the player's stats
            if (!DataStructures.PlayerWeaponStatChoices.TryGetValue(steamID, out var weaponsStats))
            {
                weaponsStats = [];
                DataStructures.PlayerWeaponStatChoices[steamID] = weaponsStats;
            }
            string weaponType = WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon).ToString();
            // Ensure that there are stats registered for the specific weapon
            if (!DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var stats))
            {
                stats = [];
                DataStructures.PlayerWeaponStats[steamID] = stats;
            }
            if (!stats.TryGetValue(weaponType, out var weaponStats))
            {
                weaponStats = [];
                stats[weaponType] = weaponStats;
            }
            // Choose a stat for the specific weapon stats instance
            if (PlayerWeaponUtilities.ChooseStat(steamID, weaponType, statType))
            {
                ctx.Reply($"Stat {statType} has been chosen for {weaponType}.");
                DataStructures.SavePlayerWeaponChoices();
            }
            else
            {
                ctx.Reply("You have already chosen two stats for this weapon.");
            }
        }

        [Command(name: "resetWeaponStats", shortHand: "rws", adminOnly: true, usage: ".rws", description: "Reset the stat choices for a player's currently equipped weapon stats.")]
        public static void ResetWeaponStats(ChatCommandContext ctx)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();
            string weaponType = WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon).ToString();
            if (DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponsStats) && weaponsStats.TryGetValue(weaponType.ToString(), out var stats))
            {
                UnitStatsOverride.RemoveStatBonuses(character, weaponType);
                stats.Clear();
                PlayerWeaponUtilities.ResetChosenStats(steamID, weaponType.ToString());
                DataStructures.SavePlayerWeaponStats();
                DataStructures.SavePlayerWeaponChoices();
                ctx.Reply("Your weapon stats have been reset for the currently equipped weapon.");
            }
            else
            {
                ctx.Reply("No stats to reset for this weapon.");
            }
        }

        [Command(name: "setWeaponMastery", shortHand: "swm", adminOnly: true, usage: ".swm [Weapon] [Level]", description: "Sets your weapon mastery level.")]
        public static void MasterySetCommand(ChatCommandContext ctx, string weaponType, int level)
        {
            if (level < 0 || level > WeaponMasterySystem.MaxCombatMasteryLevel)
            {
                ctx.Reply($"Level must be between 0 and {WeaponMasterySystem.MaxCombatMasteryLevel}.");
                return;
            }
            ulong steamId = ctx.Event.User.PlatformId;
            if (!DataStructures.weaponMasteryMap.TryGetValue(weaponType, out var masteryDict))
            {
                ctx.Reply("Invalid weapon type.");
                return;
            }
            if (!masteryDict.TryGetValue(steamId, out var _))
            {
                ctx.Reply("No existing mastery data found for this weapon.");
                return;
            }

            // Update mastery level and XP
            var xpData = new KeyValuePair<int, float>(level, WeaponMasterySystem.ConvertLevelToXp(level));
            masteryDict[steamId] = xpData;
            if (DataStructures.saveActions.TryGetValue(weaponType, out var saveAction))
            {
                saveAction();
                ctx.Reply($"Mastery level for {weaponType} set to {level}.");
            }
            else
            {
                ctx.Reply("Failed to save mastery data. No save action found.");
            }
            ctx.Reply($"Mastery level for {weaponType} set to {level}.");
        }
    }
}