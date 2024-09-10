using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using ProjectM.Network;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;

namespace Bloodcraft.Commands;

[CommandGroup(name: "level", "lvl")]
internal static class LevelingCommands
{
    [Command(name: "log", adminOnly: false, usage: ".lvl log", description: "Toggles leveling progress logging.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;
        PlayerUtilities.
                TogglePlayerBool(SteamID, "ExperienceLogging");

        LocalizationService.HandleReply(ctx, $"Leveling experience logging {(PlayerUtilities.GetPlayerBool(SteamID, "ExperienceLogging") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "get", adminOnly: false, usage: ".lvl get", description: "Display current leveling progress.")]
    public static void GetLevelCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            int prestigeLevel = steamId.TryGetPlayerPrestiges(out var prestiges) ? prestiges[PrestigeType.Experience] : 0;
            int level = xpData.Key;
            int progress = (int)(xpData.Value - LevelingSystem.ConvertLevelToXp(level));
            int percent = LevelingSystem.GetLevelProgress(steamId);
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{level}</color>][<color=#90EE90>{prestigeLevel}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
            if (ConfigService.RestedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
            {
                int roundedXP = (int)(Math.Round(restedData.Value / 100.0) * 100);
                LocalizationService.HandleReply(ctx, $"<color=#FFD700>{roundedXP}</color> bonus <color=#FFC0CB>experience</color> remaining from <color=green>resting</color>~");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience yet.");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".lvl set [Player] [Level]", description: "Sets player level.")]
    public static void SetLevelCommand(ChatCommandContext ctx, string name, int level)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }

        PlayerInfo playerInfo = PlayerCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = playerInfo.User;

        if (level < 0 || level > ConfigService.MaxLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {ConfigService.MaxLevel}");
            return;
        }

        ulong steamId = foundUser.PlatformId;

        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
            steamId.SetPlayerExperience(xpData);

            LevelingSystem.SetLevel(playerInfo.CharEntity);
            LocalizationService.HandleReply(ctx, $"Level set to <color=white>{level}</color> for <color=green>{foundUser.CharacterName.Value}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience found.");
        }
    }
}