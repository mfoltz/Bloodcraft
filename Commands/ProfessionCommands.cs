using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Professions;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands;

[CommandGroup(name: "profession", "prof")] 
internal static class ProfessionCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PlayerService PlayerService => Core.PlayerService;

    [Command(name: "log", adminOnly: false, usage: ".prof log", description: "Toggles profession progress logging.")]
    public static void LogProgessionCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ProfessionSystem)
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

    [Command(name: "get", adminOnly: false, usage: ".prof get [Profession]", description: "Display your current profession progress.")]
    public static void GetProfessionCommand(ChatCommandContext ctx, string profession)
    {
        if (!ConfigService.ProfessionSystem)
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
        int progress = (int)(data.Value - ProfessionSystem.ConvertLevelToXp(data.Key));
        if (data.Key > 0)
        {
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{data.Key}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>proficiency</color> (<color=white>{ProfessionSystem.GetLevelProgress(steamID, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"No progress in {professionHandler.GetProfessionName()} yet.");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".prof set [Name] [Profession] [Level]", description: "Sets player profession level.")]
    public static void SetProfessionCommand(ChatCommandContext ctx, string name, string profession, int level)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.HandleReply(ctx, "Professions are not enabled.");
            return;
        }
        Entity foundUserEntity = PlayerService.UserCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }
        User foundUser = foundUserEntity.Read<User>();
        if (level < 0 || level > ConfigService.MaxProfessionLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {ConfigService.MaxProfessionLevel}.");
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
        float xp = ProfessionSystem.ConvertLevelToXp(level);
        professionHandler.UpdateExperienceData(steamId, new KeyValuePair<int, float>(level, xp));
        professionHandler.SaveChanges();

        LocalizationService.HandleReply(ctx, $"{professionHandler.GetProfessionName()} set to [<color=white>{level}</color>] for <color=green>{foundUser.CharacterName}</color>");
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".prof l", description: "Lists professions available.")]
    public static void ListProfessionsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.HandleReply(ctx, "Professions are not enabled.");
            return;
        }
        string professions = ProfessionHandlerFactory.GetAllProfessions();
        LocalizationService.HandleReply(ctx, $"Available professions: {professions}");
    }
}