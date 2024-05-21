using Bloodcraft.Systems.Professions;
using Stunlock.Core;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    public static class ProfessionCommands
    {
        [Command(name: "logProfessionProgress", shortHand: "log professions", adminOnly: false, usage: ".log professions", description: "Toggles profession progress logging.")]
        public static void LogProgessionCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ProfessionSystem.Value)
            {
                ctx.Reply("Professions are not enabled.");
                return;
            }

            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ProfessionLogging"] = !bools["ProfessionLogging"];
            }
            ctx.Reply($"Profession progress logging is now {(bools["ProfessionLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "getProfessionProgress", shortHand: "get [Profession]", adminOnly: false, usage: ".get [Profession]", description: "Display your current profession progress.")]
        public static void GetProfessionCommand(ChatCommandContext ctx, string profession)
        {
            if (!Plugin.ProfessionSystem.Value)
            {
                ctx.Reply("Professions are not enabled.");
                return;
            }
            ulong steamID = ctx.Event.User.PlatformId;
            PrefabGUID empty = new(0);
            IProfessionHandler professionHandler = ProfessionHandlerFactory.GetProfessionHandler(empty, profession.ToLower());
            if (professionHandler == null)
            {
                ctx.Reply("Invalid profession.");
                return;
            }
            var data = professionHandler.GetExperienceData(steamID);
            if (data.Key > 0)
            {
                ctx.Reply($"You are level [<color=yellow>{data.Key}</color>] (<color=white>{ProfessionSystem.GetLevelProgress(steamID, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
            }
            else
            {
                ctx.Reply($"No progress in {professionHandler.GetProfessionName()} yet. ");
            }
        }
    }
}