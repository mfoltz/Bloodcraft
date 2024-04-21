using VampireCommandFramework;

namespace Cobalt.Core.Commands
{
    public static class BloodlineCommands
    {
        [Command(name: "getBloodlineProgress", shortHand: "gbp", adminOnly: false, usage: ".gbp", description: "Display your current bloodline progress.")]
        public static void GetMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (DataStructures.PlayerMastery.TryGetValue(SteamID, out var mastery))
            {
                ctx.Reply($"You have <color=white>{mastery.Key}</color> mastery points. To spend them, use ");
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
                bools["BloodlineLogging"] = !bools["BloodlineLogging"];
            }
            ctx.Reply($"Bloodline progress logging is now {(bools["BloodlineLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "setBloodlineProgress", shortHand: "sbp", adminOnly: false, usage: ".sbp [Player] [BloodlinePoints]", description: "Sets player bloodline points.")]
        public static void SetMasteryCommand(ChatCommandContext ctx, string name, int value)
        {

        }


    }
}