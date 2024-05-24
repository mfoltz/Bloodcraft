using Bloodcraft.Patches;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Professions;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
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
                ctx.Reply($"You are level [<color=white>{data.Key}</color>] and have <color=yellow>{data.Value - ProfessionSystem.ConvertLevelToXp(data.Key)}</color> experience (<color=white>{ProfessionSystem.GetLevelProgress(steamID, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
            }
            else
            {
                ctx.Reply($"No progress in {professionHandler.GetProfessionName()} yet. ");
            }
        }

        [Command(name: "setProfessionLevel", shortHand: "spl", adminOnly: true, usage: ".spl [Name] [Profession] [Level]", description: "Sets player profession level.")]
        public static void SetProfessionCommand(ChatCommandContext ctx, string name, string profession, int level)
        {
            if (!Plugin.ProfessionSystem.Value)
            {
                ctx.Reply("Professions are not enabled.");
                return;
            }
            User foundUser = ServerBootstrapPatch.users.FirstOrDefault(user => user.CharacterName.ToString().ToLower() == name.ToLower());
            if (foundUser.CharacterName.IsEmpty)
            {
                ctx.Reply("Player not found.");
                return;
            }
            if (level < 0 || level > Plugin.MaxProfessionLevel.Value)
            {
                ctx.Reply($"Level must be between 0 and {Plugin.MaxProfessionLevel.Value}.");
                return;
            }
            PrefabGUID empty = new(0);
            IProfessionHandler professionHandler = ProfessionHandlerFactory.GetProfessionHandler(empty, profession.ToLower());
            if (professionHandler == null)
            {
                ctx.Reply("Invalid profession.");
                return;
            }

            ulong steamId = foundUser.PlatformId;
            float xp = ProfessionSystem.ConvertLevelToXp(level);
            professionHandler.UpdateExperienceData(steamId, new KeyValuePair<int, float>(level, xp));
            professionHandler.SaveChanges();

            ctx.Reply($"{professionHandler.GetProfessionName()} set to [<color=white>{level}</color>] for {foundUser.CharacterName}.");
        }
        [Command(name: "listProfessions", shortHand: "lp", adminOnly: false, usage: ".lp", description: "Lists professions available.")]
        public static void ListProfessionsCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ProfessionSystem.Value)
            {
                ctx.Reply("Professions are not enabled.");
                return;
            }
            string professions = ProfessionHandlerFactory.GetAllProfessions();
            ctx.Reply($"Available professions: {professions}");
        }
    }
}