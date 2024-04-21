using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using Bloodstone.API;

namespace Cobalt.Core.Commands
{
    public static class MasteryCommands
    {
        [Command(name: "getMasteryProgress", shortHand: "gmp", adminOnly: false, usage: ".gmp", description: "Display your current mastery progress.")]
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

        [Command(name: "logMasteryProgress", shortHand: "lmp", adminOnly: false, usage: ".lmp", description: "Toggles mastery progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["MasteryLogging"] = !bools["MasteryLogging"];
            }
            ctx.Reply($"Mastery progress logging is now {(bools["MasteryLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "setMasteryPoints", shortHand: "smp", adminOnly: false, usage: ".smp [Player] [MasteryPoints]", description: "Sets player mastery points.")]
        public static void SetMasteryCommand(ChatCommandContext ctx, string name, int value)
        {

        }

        
    }
}