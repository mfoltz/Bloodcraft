using Cobalt.Systems.Professions;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using VampireCommandFramework;

namespace Cobalt.Commands
{
    public static class ProfessionCommands
    {
        [Command(name: "logProfessionProgress", shortHand: "lpp", adminOnly: false, usage: ".lpp", description: "Toggles profession progress logging.")]
        public static void LogProgessionCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ProfessionLogging"] = !bools["ProfessionLogging"];
            }
            ctx.Reply($"Profession progress logging is now {(bools["ProfessionLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "getProfessionProgress", shortHand: "gpp", adminOnly: false, usage: ".gpp [Profession]", description: "Display your current profession progress.")]
        public static void GetProfessionCommand(ChatCommandContext ctx, string profession)
        {
            ulong steamID = ctx.Event.User.PlatformId;
            PrefabGUID empty = new(0);
            IProfessionHandler professionHandler = ProfessionHandlerFactory.GetProfessionHandler(empty, profession.ToLower());
            if (professionHandler == null)
            {
                ctx.Reply("Invalid profession.");
                return;
            }
            if (Core.DataStructures.professionMap.TryGetValue(profession, out var professionDictionary) && professionDictionary.TryGetValue(steamID, out var prof))
            {
                ctx.Reply($"You are level [<color=yellow>{prof.Key}</color>] (<color=white>{ProfessionSystem.GetLevelProgress(steamID, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
            }
            else
            {
                ctx.Reply($"No progress in {professionHandler.GetProfessionName()} yet. ");
            }
        }
    }
}