using Cobalt.Hooks;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Sanguimancy.BloodStats;

namespace Cobalt.Commands
{
    public static class SanguimancyCommands
    {
        [Command(name: "getSanguimancyProgress", shortHand: "gsp", adminOnly: false, usage: ".gsp", description: "Display your current sanguimancy progress.")]
        public static void GetMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var mastery))
            {
                ctx.Reply($"You are level <color=white>{mastery.Key}</color> in <color=red>sanguimancy</color>.");
            }
            else
            {
                ctx.Reply("You haven't gained any sanguimancy  ");
            }
        }

        [Command(name: "logBloodlineProgress", shortHand: "lbp", adminOnly: false, usage: ".lbp", description: "Toggles bloodline progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["BloodLogging"] = !bools["BloodLogging"];
            }
            ctx.Reply($"Sanguimancy progress logging is now {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "chooseBloodStat", shortHand: "cbs", adminOnly: false, usage: ".cbs <Stat>", description: "Choose a bloodline stat to enhance based on your mastery.")]
        public static void SetBloodlineStatCommand(ChatCommandContext ctx, string statChoice)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            string statType = statChoice.ToLower();
            // try to parse statType from choice string
            if (!Enum.TryParse<BloodStatManager.BloodStatType>(statType, true, out _))
            {
                ctx.Reply("Invalid bloodStat type.");
                return;
            }
            if (!Core.DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var _))
            {
                List<string> bloodStats = [];
                Core.DataStructures.PlayerBloodChoices[steamId] = bloodStats;
            }

            if (PlayerBloodUtilities.ChooseStat(steamId, statType))
            {
                ctx.Reply($"Stat {statType} has been chosen for Sanguimancy.");
                Core.DataStructures.SavePlayerBloodChoices();
            }
            else
            {
                ctx.Reply("You have already chosen two stats for this weapon.");
            }
        }

        [Command(name: "resetBloodStats", shortHand: "rbs", adminOnly: false, usage: ".rbs", description: "Reset the stat choices for a player's bloodline stats.")]
        public static void ResetBloodlineStatsCommand(ChatCommandContext ctx)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            Entity character = ctx.Event.SenderCharacterEntity;
            if (!Core.DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var stats))
            {
                ctx.Reply("No blood choices found for this SteamID.");
                return;
            }
            UnitStatsOverride.RemoveBloodBonuses(character);
            stats.Clear();
            Core.DataStructures.SavePlayerBloodChoices();
            ctx.Reply($"Blood stat choices reset.");
        }
    }
}