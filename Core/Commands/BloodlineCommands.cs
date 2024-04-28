using VampireCommandFramework;

namespace Cobalt.Core.Commands
{
    public static class BloodlineCommands
    {
        [Command(name: "getBloodlineProgress", shortHand: "gbp", adminOnly: false, usage: ".gbp", description: "Display your current bloodline progress.")]
        public static void GetMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (DataStructures.PlayerCombatMastery.TryGetValue(SteamID, out var mastery))
            {
                ctx.Reply($"You have <color=white>{mastery.Key}</color> blood points. To spend them, use ");
            }
            else
            {
                ctx.Reply("You haven't earned any mastery points yet.");
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

        [Command(name: "setBloodlineProgress", shortHand: "sbp", adminOnly: false, usage: ".sbp [Player] [BloodlinePoints]", description: "Sets player blood points.")]
        public static void SetMasteryCommand(ChatCommandContext ctx, string name, int value)
        {

        }


    }
}