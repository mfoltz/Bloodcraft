using Cobalt.Hooks;
using Cobalt.Systems.Experience;
using ProjectM.Network;
using VampireCommandFramework;

namespace Cobalt.Commands
{
    public static class LevelingCommands
    {
        [Command(name: "logLevelingProgress", shortHand: "log level", adminOnly: false, usage: ".log level", description: "Toggles leveling progress logging.")]
        public static void LogExperienceCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ExperienceLogging"] = !bools["ExperienceLogging"];
            }
            ctx.Reply($"Leveling progress logging is now {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "getLevelingProgress", shortHand: "get level", adminOnly: false, usage: ".get level", description: "Display current leveling progress.")]
        public static void GetLevelCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            ulong steamId = ctx.Event.User.PlatformId;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var Leveling))
            {
                int level = Leveling.Key;
                int progress = (int)(Leveling.Value - LevelingSystem.ConvertLevelToXp(level));
                int percent = LevelingSystem.GetLevelProgress(steamId);
                ctx.Reply($"You're level <color=white>{level}</color> and have <color=yellow>{progress}</color> experience (<color=white>{percent}%</color>)");
            }
            else
            {
                ctx.Reply("No leveling progress yet.");
            }
        }

        [Command(name: "setLevel", shortHand: "sl", adminOnly: true, usage: ".sl [Player] [Level]", description: "Sets your level.")]
        public static void SetLevelCommand(ChatCommandContext ctx, string name, int level)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            User foundUser = ServerBootstrapPatches.users.FirstOrDefault(user => user.CharacterName.ToString().ToLower() == name.ToLower());
            if (foundUser.CharacterName.IsEmpty)
            {
                ctx.Reply("Player not found.");
                return;
            }

            if (level < 0 || level > LevelingSystem.MaxLevel)
            {
                ctx.Reply($"Level must be between 0 and {LevelingSystem.MaxLevel}.");
                return;
            }
            ulong steamId = foundUser.PlatformId;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var _))
            {
                var xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
                Core.DataStructures.PlayerExperience[steamId] = xpData;
                Core.DataStructures.SavePlayerExperience();
                ctx.Reply($"Level set to {level} for {foundUser.CharacterName}.");
            }
            else
            {
                ctx.Reply("No experience data found.");
            }
        }
    }
}