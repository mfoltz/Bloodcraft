using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
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
        [Command(name: "logProfessionProgress", shortHand: "log p", adminOnly: false, usage: ".log p", description: "Toggles profession progress logging.")]
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
            Core.DataStructures.SavePlayerBools();
            ctx.Reply($"Profession progress logging is now {(bools["ProfessionLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "getProfessionProgress", shortHand: "gp", adminOnly: false, usage: ".gp [Profession]", description: "Display your current profession progress.")]
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
            int progress = (int)(data.Value - ProfessionSystem.ConvertLevelToXp(data.Key));
            if (data.Key > 0)
            {
                ctx.Reply($"You're level [<color=white>{data.Key}</color>] and have <color=yellow>{progress}</color> experience (<color=white>{ProfessionSystem.GetLevelProgress(steamID, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
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
            Entity foundUserEntity = Core.FindUserOnline(name);
            if (foundUserEntity.Equals(Entity.Null))
            {
                ctx.Reply("Player not found.");
                return;
            }
            User foundUser = foundUserEntity.Read<User>();
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