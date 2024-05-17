using Cobalt.Hooks;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Text;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Sanguimancy.BloodStats;

namespace Cobalt.Commands
{
    public static class SanguimancyCommands
    {
        [Command(name: "getSanguimancyProgress", shortHand: "gsp", adminOnly: false, usage: ".gsp", description: "Display your current sanguimancy progress.")]
        public static void GetSanguimancyCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var Sanguimancy))
            {
                ctx.Reply($"You are level <color=white>{Sanguimancy.Key}</color> in <color=red>sanguimancy</color>.");
            }
            else
            {
                ctx.Reply("No progress in sanguimancy yet.");
            }
        }

        [Command(name: "logSanguimancyProgress", shortHand: "lsp", adminOnly: false, usage: ".lsp", description: "Toggles sanguimancy progress logging.")]
        public static void LogSanguimancyProgress(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["BloodLogging"] = !bools["BloodLogging"];
            }
            ctx.Reply($"Sanguimancy progress logging is now {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "chooseBloodStat", shortHand: "cbs", adminOnly: false, usage: ".cbs <Stat>", description: "Choose a blood stat to enhance based on your sanguimancy.")]
        public static void ChooseBloodStatCommand(ChatCommandContext ctx, string statChoice)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            string statType = statChoice.ToLower();
            // try to parse statType from choice string
            if (!Enum.TryParse<BloodStatManager.BloodStatType>(statType, true, out _))
            {
                ctx.Reply("Invalid blood stat choice, use .lbs to see options.");
                return;
            }
            if (!Core.DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var _))
            {
                List<string> bloodStats = [];
                Core.DataStructures.PlayerBloodChoices[steamId] = bloodStats;
            }

            if (PlayerBloodUtilities.ChooseStat(steamId, statType))
            {
                ctx.Reply($"{statType} has been chosen for Sanguimancy.");
                Core.DataStructures.SavePlayerBloodChoices();
            }
            else
            {
                ctx.Reply("You have already chosen two stats for this weapon.");
            }
        }

        [Command(name: "resetBloodStats", shortHand: "rbs", adminOnly: false, usage: ".rbs", description: "Reset the stat choices for a player's blood stats.")]
        public static void ResetBloodlineStatsCommand(ChatCommandContext ctx)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            Entity character = ctx.Event.SenderCharacterEntity;
            if (!Core.DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var stats))
            {
                ctx.Reply("No blood stat choices found to reset.");
                return;
            }
            UnitStatsOverride.RemoveBloodBonuses(character);
            stats.Clear();
            Core.DataStructures.SavePlayerBloodChoices();
            ctx.Reply($"Blood stat choices reset.");
        }

        [Command(name: "listBloodStats", shortHand: "lbs", adminOnly: false, usage: ".lbs", description: "Lists blood stat choices.")]
        public static void ListBloodStatsCommand(ChatCommandContext ctx)
        {
            string bloodStats = string.Join(", ", Enum.GetNames(typeof(BloodStatManager.BloodStatType)));
            ctx.Reply($"Available blood stats: {bloodStats}");
        }
    }
}