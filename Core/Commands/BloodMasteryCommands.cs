using Cobalt.Systems.Bloodline;
using VampireCommandFramework;
using static Cobalt.Systems.Bloodline.BloodMasteryStatsSystem;

namespace Cobalt.Core.Commands
{
    public static class BloodMasteryCommands
    {
        [Command(name: "getBloodlineProgress", shortHand: "gbp", adminOnly: false, usage: ".gbp", description: "Display your current bloodline progress.")]
        public static void GetMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (DataStructures.PlayerBloodMastery.TryGetValue(SteamID, out var mastery))
            {
                ctx.Reply($"You have <color=white>{mastery.Value}</color> blood mastery points.");
            }
            else
            {
                ctx.Reply("You haven't earned any blood points yet.");
            }
        }

        [Command(name: "logBloodlineProgress", shortHand: "lbp", adminOnly: false, usage: ".lbp", description: "Toggles bloodline progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["BloodLogging"] = !bools["BloodLogging"];
            }
            ctx.Reply($"Sanguimancy progress logging is now {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "setBloodStat", shortHand: "sbs", adminOnly: true, usage: ".sbs <Stat>", description: "Choose a bloodline stat to enhance based on your mastery.")]
        public static void SetBloodlineStatCommand(ChatCommandContext ctx, string stat)
        {
            ulong steamId = ctx.Event.User.PlatformId;

            if (!DataStructures.PlayerBloodStats.TryGetValue(steamId, out var stats))
            {
                ctx.Reply("No blood mastery found for this SteamID.");
                return;
            }

            if (stats.StatsChosen >= 2)
            {
                ctx.Reply("You have already chosen two blood mastery stats. Please reset to choose new ones.");
                return;
            }

            if (Enum.TryParse(stat, out BloodMasteryStatManager.BloodFocusSystem.BloodStatType statType))
            {
                stats.ChooseStat(statType);
                ctx.Reply($"Blood stat added: {stat}");
                DataStructures.SavePlayerBloodStats();
            }
            else
            {
                ctx.Reply("Invalid stat name. Please check and try again.");
            }
        }

        [Command(name: "resetBloodStats", shortHand: "rbs", adminOnly: true, usage: ".rbs", description: "Reset the stat choices for a player's bloodline stats.")]
        public static void ResetBloodlineStatsCommand(ChatCommandContext ctx)
        {
            ulong steamId = ctx.Event.User.PlatformId;

            if (!DataStructures.PlayerBloodStats.TryGetValue(steamId, out var stats))
            {
                ctx.Reply("No blood mastery found for this SteamID.");
                return;
            }
            stats.ResetChosenStats();
            DataStructures.SavePlayerBloodStats();
            ctx.Reply($"Blood stat choices reset.");
        }
    }
}