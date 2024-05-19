using Cobalt.Hooks;
using Cobalt.Systems.Expertise;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Expertise.WeaponStats;
using static Cobalt.Systems.Sanguimancy.BloodStats;

namespace Cobalt.Commands
{
    public static class ExpertiseCommands
    {
        [Command(name: "getExpertiseProgress", shortHand: "gep", adminOnly: false, usage: ".gep", description: "Display your current Expertise progress.")]
        public static void GetExpertiseCommand(ChatCommandContext ctx)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weaponGuid = equipment.WeaponSlot.SlotEntity._Entity.Read<PrefabGUID>();
            ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(weaponGuid);

            IWeaponExpertiseHandler handler = WeaponExpertiseHandlerFactory.GetWeaponExpertiseHandler(weaponType);
            if (handler == null)
            {
                ctx.Reply($"No expertise handler found for {weaponType}.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            var ExpertiseData = handler.GetExperienceData(steamID);

            // ExpertiseData.Key represents the level, and ExpertiseData.Value represents the experience.
            if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
            {
                ctx.Reply($"Your expertise is <color=yellow>{ExpertiseData.Key}</color> (<color=white>{ExpertiseSystem.GetLevelProgress(steamID, handler)}%</color>) with {weaponType}.");
            }
            else
            {
                ctx.Reply($"You haven't gained any expertise for {weaponType} yet.");
            }
        }

        [Command(name: "logExpertiseProgress", shortHand: "lep", adminOnly: false, usage: ".lep", description: "Toggles Expertise progress logging.")]
        public static void LogExpertiseCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ExpertiseLogging"] = !bools["ExpertiseLogging"];
            }
            ctx.Reply($"Weapon expertise logging is now {(bools["ExpertiseLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "chooseWeaponStat", shortHand: "cws", adminOnly: false, usage: ".cws [Stat]", description: "Choose a weapon stat to enhance based on your weapon Expertise.")]
        public static void ChooseWeaponStat(ChatCommandContext ctx, string statType)
        {
            if (!Enum.TryParse<WeaponStatManager.WeaponStatType>(statType, true, out var weaponStatType))
            {
                ctx.Reply("Invalid weapon stat choice, use .lws to see options.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            Equipment equipment = character.Read<Equipment>();
            ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);
            Entity weaponEntity = Entity.Null;
            if (weaponType == ExpertiseSystem.WeaponType.Unarmed)
            {
                weaponEntity = equipment.UnarmedBuffInstance;
            }
            else
            {
                weaponEntity = equipment.WeaponSlot.SlotEntity._Entity;
            }
            // Ensure that there is a dictionary for the player's stats
            if (!Core.DataStructures.PlayerWeaponChoices.TryGetValue(steamID, out var weaponsStats))
            {
                weaponsStats = [];
                Core.DataStructures.PlayerWeaponChoices[steamID] = weaponsStats;
            }

            // Choose a stat for the specific weapon stats instance
            if (PlayerWeaponUtilities.ChooseStat(steamID, weaponType, weaponStatType))
            {
                ctx.Reply($"{statType} has been chosen for {weaponType} and will apply after reequiping.");
                //ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, weaponType, weaponEntity);
                Core.DataStructures.SavePlayerWeaponChoices();
            }
            else
            {
                ctx.Reply("You have already chosen two stats for this weapon.");
            }
        }

        [Command(name: "resetWeaponStats", shortHand: "rws", adminOnly: false, usage: ".rws", description: "Reset the stat choices for a player's currently equipped weapon stats.")]
        public static void ResetWeaponStats(ChatCommandContext ctx)
        {
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);

            //ModifyUnitStatBuffUtils.ResetWeaponModifications(weapon);
            PlayerWeaponUtilities.ResetChosenStats(steamID, weaponType);
            //Core.DataStructures.SavePlayerWeaponChoices();
            ctx.Reply("Your weapon stats have been reset for the currently equipped weapon.");
        }

        [Command(name: "setWeaponExpertise", shortHand: "swe", adminOnly: true, usage: ".swe [Weapon] [Level]", description: "Sets your weapon expertise level.")]
        public static void SetExpertiseCommand(ChatCommandContext ctx, string weapon, int level)
        {
            if (level < 0 || level > ExpertiseSystem.MaxWeaponExpertiseLevel)
            {
                ctx.Reply($"Level must be between 0 and {ExpertiseSystem.MaxWeaponExpertiseLevel}.");
                return;
            }
            if (!Enum.TryParse<ExpertiseSystem.WeaponType>(weapon, true, out var weaponType))
            {
                ctx.Reply("Invalid weapon type.");
            }
            var expertiseHandler = WeaponExpertiseHandlerFactory.GetWeaponExpertiseHandler(weaponType);
            if (expertiseHandler == null)
            {
                ctx.Reply("Invalid weapon type.");
                return;
            }

            ulong steamId = ctx.Event.User.PlatformId;
            //var xpData = ExpertiseHandler.GetExperienceData(steamId);
            Entity character = ctx.Event.SenderCharacterEntity;
            Equipment equipment = character.Read<Equipment>();
            // Update Expertise level and XP
            var xpData = new KeyValuePair<int, float>(level, ExpertiseSystem.ConvertLevelToXp(level));
            expertiseHandler.UpdateExperienceData(steamId, xpData);
            expertiseHandler.SaveChanges();
            GearOverride.SetWeaponItemLevel(equipment, level, Core.Server.EntityManager);

            ctx.Reply($"Expertise for {expertiseHandler.GetWeaponType()} set to {level}.");
        }

        [Command(name: "listWeaponStats", shortHand: "lws", adminOnly: false, usage: ".lws", description: "Lists weapon stat choices.")]
        public static void ListBloodStatsCommand(ChatCommandContext ctx)
        {
            string weaponStats = string.Join(", ", Enum.GetNames(typeof(WeaponStatManager.WeaponStatType)));
            ctx.Reply($"Available weapon stats: {weaponStats}");
        }
    }
}