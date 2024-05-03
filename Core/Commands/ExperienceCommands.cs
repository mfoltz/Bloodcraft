using Bloodstone.API;
using Cobalt.Systems.Experience;
using Cobalt.Systems.Weapon;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using UnityEngine.Rendering.HighDefinition;
using VampireCommandFramework;
using VRising.GameData.Utils;

namespace Cobalt.Core.Commands
{
    public static class ExperienceCommands
    {
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

        [Command(name: "setLevel", shortHand: "sl", adminOnly: true, usage: ".sl [Level]", description: "Sets your level.")]
        public static void SetLevelCommand(ChatCommandContext ctx, int level)
        {
            if (level < 0 || level > ExperienceSystem.MaxLevel)
            {
                ctx.Reply($"Level must be between 0 and {ExperienceSystem.MaxLevel}.");
                return;
            }
            ulong steamId = ctx.Event.User.PlatformId;
            if (DataStructures.PlayerExperience.TryGetValue(steamId, out var _))
            {
                var xpData = new KeyValuePair<int, float>(level, ExperienceSystem.ConvertLevelToXp(level));
                DataStructures.PlayerExperience[steamId] = xpData;
                DataStructures.SavePlayerExperience();
                ctx.Reply($"Level set to {level}.");
            }
            else
            {
                ctx.Reply("No experience data found.");
            }
        }
    }
}