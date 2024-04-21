using VampireCommandFramework;

namespace Cobalt.Core.Commands
{
    public static class ExperienceCommands
    {

        [Command(name: "getExperienceProgress", shortHand: "gep", adminOnly: false, usage: ".gep", description: "Display your current experience progress.")]
        public static void GetExperienceCommand(ChatCommandContext ctx)
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

        [Command(name: "logExperienceProgress", shortHand: "lep", adminOnly: false, usage: ".lep", description: "Toggles experience progress logging.")]
        public static void LogExperienceCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ExperienceLogging"] = !bools["ExperienceLogging"];
            }
            ctx.Reply($"Experience progress logging is now {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "set", shortHand: "s", adminOnly: true, usage: ".set [Player] [Level]", description: "Sets player level.")]
        public static void MasterySetCommand(ChatCommandContext ctx, string name, int value)
        {

        }
    }
}
