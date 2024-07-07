using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{ [CommandGroup(name: "profession", "prof")] 
internal static class ProfessionCommands
{
    [Command(name: "logprof", shortHand: "log", adminOnly: false, usage: ".prof log", description: "Toggles profession progress logging.")]
    public static void LogProgessionCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ProfessionSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Professions are not enabled.");
            return;
        }

        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ProfessionLogging"] = !bools["ProfessionLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Profession logging is now {(bools["ProfessionLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "getprogress", shortHand: "get", adminOnly: false, usage: ".prof get [Profession]", description: "Display your current profession progress.")]
    public static void GetProfessionCommand(ChatCommandContext ctx, string profession)
    {
        if (!Plugin.ProfessionSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Professions are not enabled.");
            return;
        }
        ulong steamID = ctx.Event.User.PlatformId;
        PrefabGUID empty = new(0);
        IProfessionHandler professionHandler = ProfessionHandlerFactory.GetProfessionHandler(empty, profession.ToLower());
        if (professionHandler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid profession.");
            return;
        }
        var data = professionHandler.GetExperienceData(steamID);
        int progress = (int)(data.Value - ProfessionUtilities.ConvertLevelToXp(data.Key));
        if (data.Key > 0)
        {
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{data.Key}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>proficiency</color> (<color=white>{ProfessionUtilities.GetLevelProgress(steamID, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"No progress in {professionHandler.GetProfessionName()} yet.");
        }
    }

    [Command(name: "setlevel", shortHand: "set", adminOnly: true, usage: ".prof set [Name] [Profession] [Level]", description: "Sets player profession level.")]
    public static void SetProfessionCommand(ChatCommandContext ctx, string name, string profession, int level)
    {
        if (!Plugin.ProfessionSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Professions are not enabled.");
            return;
        }
        Entity foundUserEntity = PlayerService.GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found...");
            return;
        }
        User foundUser = foundUserEntity.Read<User>();
        if (level < 0 || level > Plugin.MaxProfessionLevel.Value)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {Plugin.MaxProfessionLevel.Value}.");
            return;
        }
        PrefabGUID empty = new(0);
        IProfessionHandler professionHandler = ProfessionHandlerFactory.GetProfessionHandler(empty, profession.ToLower());
        if (professionHandler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid profession.");
            return;
        }

        ulong steamId = foundUser.PlatformId;
        float xp = ProfessionUtilities.ConvertLevelToXp(level);
        professionHandler.UpdateExperienceData(steamId, new KeyValuePair<int, float>(level, xp));
        professionHandler.SaveChanges();

        LocalizationService.HandleReply(ctx, $"{professionHandler.GetProfessionName()} set to [<color=white>{level}</color>] for <color=green>{foundUser.CharacterName}</color>");
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".prof l", description: "Lists professions available.")]
    public static void ListProfessionsCommand(ChatCommandContext ctx)
    {
        if (!Plugin.ProfessionSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Professions are not enabled.");
            return;
        }
        string professions = ProfessionHandlerFactory.GetAllProfessions();
        LocalizationService.HandleReply(ctx, $"Available professions: {professions}");
    }
}