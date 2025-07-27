using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Professions;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Commands;

[CommandGroup(name: "profession", "prof")]
internal static class ProfessionCommands
{
    const int MAX_PROFESSION_LEVEL = 100;

    [Command(name: "log", adminOnly: false, usage: ".prof log", description: "Toggles profession progress logging.")]
    public static void LogProgessionCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.Reply(ctx, "Professions are not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, PROFESSION_LOG_KEY);
        LocalizationService.Reply(ctx, $"Profession logging is now {(GetPlayerBool(steamId, PROFESSION_LOG_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "get", adminOnly: false, usage: ".prof get [Profession]", description: "Display your current profession progress.")]
    public static void GetProfessionCommand(ChatCommandContext ctx, string profession)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.Reply(ctx, "Professions are not enabled.");
            return;
        }

        if (!Enum.TryParse(profession, true, out ProfessionType professionType))
        {
            LocalizationService.Reply(ctx, $"Valid professions: {ProfessionFactory.GetProfessionNames()}");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        IProfession professionHandler = ProfessionFactory.GetProfession(professionType);
        if (professionHandler == null)
        {
            LocalizationService.Reply(ctx, "Invalid profession.");
            return;
        }

        KeyValuePair<int, float> data = professionHandler.GetProfessionData(steamId);
        if (data.Key > 0)
        {
            int progress = (int)(data.Value - ConvertLevelToXp(data.Key));
            LocalizationService.Reply(ctx, $"You're level [<color=white>{data.Key}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>proficiency</color> (<color=white>{ProfessionSystem.GetLevelProgress(steamId, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
        }
        else
        {
            LocalizationService.Reply(ctx, $"No progress in {professionHandler.GetProfessionName()} yet!");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".prof set [Name] [Profession] [Level]", description: "Sets player profession level.")]
    public static void SetProfessionCommand(ChatCommandContext ctx, string name, string profession, int level)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.Reply(ctx, "Professions are not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            LocalizationService.Reply(ctx, $"Couldn't find player.");
            return;
        }

        if (level < 0 || level > MAX_PROFESSION_LEVEL)
        {
            LocalizationService.Reply(ctx, $"Level must be between 0 and {MAX_PROFESSION_LEVEL}.");
            return;
        }

        if (!Enum.TryParse(profession, true, out ProfessionType professionType))
        {
            LocalizationService.Reply(ctx, $"Valid professions: {ProfessionFactory.GetProfessionNames()}");
            return;
        }

        IProfession professionHandler = ProfessionFactory.GetProfession(professionType);
        if (professionHandler == null)
        {
            LocalizationService.Reply(ctx, "Invalid profession.");
            return;
        }

        ulong steamId = playerInfo.User.PlatformId;

        float xp = ConvertLevelToXp(level);
        professionHandler.SetProfessionData(steamId, new KeyValuePair<int, float>(level, xp));

        LocalizationService.Reply(ctx, $"{professionHandler.GetProfessionName()} set to [<color=white>{level}</color>] for <color=green>{playerInfo.User.CharacterName.Value}</color>");
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".prof l", description: "Lists professions available.")]
    public static void ListProfessionsCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.ProfessionSystem)
        {
            LocalizationService.Reply(ctx, "Professions are not enabled.");
            return;
        }

        LocalizationService.Reply(ctx, $"Available professions: {ProfessionFactory.GetProfessionNames()}");
    }
}