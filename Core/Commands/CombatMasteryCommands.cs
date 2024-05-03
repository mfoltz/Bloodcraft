using Cobalt.Hooks;
using Cobalt.Systems.Weapon;
using Cobalt.Systems.WeaponMastery;
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
                ctx.Reply($"You haven't gained any expertise for {weaponType} yet. ");
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
                ctx.Reply("Invalid weaponStat type.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weapon = equipment.WeaponSlotEntity._Entity.Read<PrefabGUID>();

            // Ensure that there is a dictionary for the player's stats
            if (!DataStructures.PlayerWeaponChoices.TryGetValue(steamID, out var _))
            {
                Dictionary<string, List<string>> weaponsStats = [];
                DataStructures.PlayerWeaponChoices[steamID] = weaponsStats;
            }
            string weaponType = WeaponMasterySystem.GetWeaponTypeFromPrefab(weapon).ToString();
            // Ensure that there are stats registered for the specific weapon

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

            UnitStatsOverride.RemoveWeaponBonuses(character, weaponType);
            PlayerWeaponUtilities.ResetChosenStats(steamID, weaponType);
            //DataStructures.SavePlayerWeaponChoices();
            ctx.Reply("Your weapon stats have been reset for the currently equipped weapon.");
        }

        [Command(name: "setWeaponMastery", shortHand: "swm", adminOnly: true, usage: ".swm [Weapon] [Level]", description: "Sets your weapon mastery level.")]
        public static void MasterySetCommand(ChatCommandContext ctx, string weaponType, int level)
        {
            if (level < 0 || level > WeaponMasterySystem.MaxCombatMasteryLevel)
            {
                ctx.Reply($"Level must be between 0 and {WeaponMasterySystem.MaxCombatMasteryLevel}.");
                return;
            }

            var masteryHandler = WeaponMasteryHandlerFactory.GetWeaponMasteryHandler(weaponType);
            if (masteryHandler == null)
            {
                ctx.Reply("Invalid weapon type.");
                return;
            }

            ulong steamId = ctx.Event.User.PlatformId;
            var xpData = masteryHandler.GetExperienceData(steamId);
            if (xpData.Key == 0 && xpData.Value == 0) // Assumes that default KeyValuePair is returned when not found
            {
                ctx.Reply("No existing mastery data found for this weapon.");
                return;
            }

            // Update mastery level and XP
            xpData = new KeyValuePair<int, float>(level, WeaponMasterySystem.ConvertLevelToXp(level));
            masteryHandler.UpdateExperienceData(steamId, xpData);
            masteryHandler.SaveChanges();
            ctx.Reply($"Mastery level for {masteryHandler.GetWeaponType()} set to {level}.");
        }
    }
}