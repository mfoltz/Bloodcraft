using VampireCommandFramework;

namespace Cobalt.Core.Commands
{
    public static class ExperienceCommands
    {

        [Command(name: "getExperienceProgress", shortHand: "gep", adminOnly: false, usage: ".gep", description: "Display your current experience progress.")]
        public static void GetExperienceCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (DataStructures.PlayerExperience.TryGetValue(SteamID, out var xpData))
            {
                ctx.Reply($"You have <color=white>{xpData.Key}</color> experience points.");
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

        [Command(name: "setExperienceProgress", shortHand: "sep", adminOnly: true, usage: ".sep [Player] [ExperiencePoints]", description: "Sets player experience.")]
        public static void MasterySetCommand(ChatCommandContext ctx, string name, int value)
        {

        }
    }
}
