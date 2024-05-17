using Cobalt.Systems.Experience;
using Cobalt.Systems.Expertise;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Cobalt.Commands
{
    public static class LevelingCommands
    {
        [Command(name: "logLevelingProgress", shortHand: "llp", adminOnly: false, usage: ".llp", description: "Toggles leveling progress logging.")]
        public static void LogExperienceCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ExperienceLogging"] = !bools["ExperienceLogging"];
            }
            ctx.Reply($"Leveling progress logging is now {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "getLevelingProgress", shortHand: "glp", adminOnly: false, usage: ".glp", description: "Display current leveling progress.")]
        public static void GetLevelCommand(ChatCommandContext ctx)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var Leveling))
            {
                int level = Leveling.Key;
                int progress = (int)Leveling.Value;
                int percent = LevelingSystem.GetLevelProgress(steamId);
                ctx.Reply($"You're level <color=white>{level}</color> and have <color=yellow>{progress}</color> experience (<color=white>{percent}%</color>)");
            }
            else
            {
                ctx.Reply("No leveling progress yet.");
            }
        }

        [Command(name: "setLevel", shortHand: "sl", adminOnly: true, usage: ".sl [Level]", description: "Sets your level.")]
        public static void SetLevelCommand(ChatCommandContext ctx, int level)
        {
            if (level < 0 || level > LevelingSystem.MaxLevel)
            {
                ctx.Reply($"Level must be between 0 and {LevelingSystem.MaxLevel}.");
                return;
            }
            ulong steamId = ctx.Event.User.PlatformId;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var _))
            {
                var xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
                Core.DataStructures.PlayerExperience[steamId] = xpData;
                Core.DataStructures.SavePlayerExperience();
                ctx.Reply($"Level set to {level}.");
            }
            else
            {
                ctx.Reply("No experience data found.");
            }
        }
    }
}