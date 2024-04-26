using VampireCommandFramework;

namespace Cobalt.Core.Commands
{
    public static class ProfessionCommands
    {

        [Command(name: "logProfessionProgress", shortHand: "lpp", adminOnly: false, usage: ".lpp", description: "Toggles profession progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ProfessionLogging"] = !bools["ProfessionLogging"];
            }
            ctx.Reply($"Profession progress logging is now {(bools["ProfessionLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

       


    }
}