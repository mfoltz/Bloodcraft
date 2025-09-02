using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Resources.Localization;
using Bloodcraft.Systems.Leveling;
using ProjectM.Network;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using static Bloodcraft.Utilities.Progression;

namespace Bloodcraft.Commands;

[CommandGroup(name: "level", "lvl")]
internal static class LevelingCommands
{
    [Command(name: "log", adminOnly: false, usage: ".lvl log", description: "Toggles leveling progress logging.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.Reply(ctx, "Leveling is not enabled.");
            return;
        }

        var SteamID = ctx.Event.User.PlatformId;

        TogglePlayerBool(SteamID, EXPERIENCE_LOG_KEY);
        LocalizationService.Reply(ctx, "Level logging {0}.", GetPlayerBool(SteamID, EXPERIENCE_LOG_KEY) ? "<color=green>enabled</color>" : "<color=red>disabled</color>");
    }

    [Command(name: "get", adminOnly: false, usage: ".lvl get", description: "Display current leveling progress.")]
    public static void GetLevelCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.Reply(ctx, "Leveling is not enabled.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            int prestigeLevel = steamId.TryGetPlayerPrestiges(out var prestiges) ? prestiges[PrestigeType.Experience] : 0;
            int level = xpData.Key;

            int progress = (int)(xpData.Value - ConvertLevelToXp(level));
            int percent = LevelingSystem.GetLevelProgress(steamId);

            LocalizationService.Reply(ctx, "You're level [<color=white>{0}</color>][<color=#90EE90>{1}</color>] with <color=yellow>{2}</color> <color=#FFC0CB>experience</color> (<color=white>{3}%</color>)!", level, prestigeLevel, progress, percent);

            if (ConfigService.RestedXPSystem && steamId.TryGetPlayerRestedXP(out var restedData) && restedData.Value > 0)
            {
                int roundedXP = (int)(Math.Round(restedData.Value / 100.0) * 100);

                LocalizationService.Reply(ctx, "<color=#FFD700>{0}</color> bonus <color=#FFC0CB>experience</color> remaining from <color=green>resting</color>~", roundedXP);
            }
        }
        else
        {
            LocalizationService.Reply(ctx, "You haven't earned any experience yet!");
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".lvl set [Player] [Level]", description: "Sets player level.")]
    public static void SetLevelCommand(ChatCommandContext ctx, string name, int level)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.Reply(ctx, "Leveling is not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            LocalizationService.Reply(ctx, "Couldn't find player.");
            return;
        }

        User foundUser = playerInfo.User;

        if (level < 0 || level > ConfigService.MaxLevel)
        {
            LocalizationService.Reply(ctx, "Level must be between <color=white>0</color> and <color=white>{0}</color>!", ConfigService.MaxLevel);
            return;
        }

        ulong steamId = foundUser.PlatformId;

        if (steamId.TryGetPlayerExperience(out var xpData))
        {
            xpData = new KeyValuePair<int, float>(level, ConvertLevelToXp(level));
            steamId.SetPlayerExperience(xpData);

            LevelingSystem.SetLevel(playerInfo.CharEntity);
            LocalizationService.Reply(ctx, "Level set to <color=white>{0}</color> for <color=green>{1}</color>!", level, foundUser.CharacterName.Value);
        }
        else
        {
            LocalizationService.Reply(ctx, "Couldn't find experience data for {0}", foundUser.CharacterName.Value);
        }
    }

    [Command(name: "ignoresharedexperience", shortHand: "ignore", adminOnly: true, usage: ".lvl ignore [Player]", description: "Adds (or removes) player to list of those who are not eligible to receive shared experience.")]
    public static void IgnoreSharedExperiencePlayerCommand(ChatCommandContext ctx, string name)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.Reply(ctx, "Leveling is not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            LocalizationService.Reply(ctx, MessageKeys.PLAYER_NOT_FOUND);
            return;
        }

        if (!DataService.PlayerDictionaries._ignoreSharedExperience.Contains(playerInfo.User.PlatformId))
        {
            DataService.PlayerDictionaries._ignoreSharedExperience.Add(playerInfo.User.PlatformId);
            DataService.PlayerPersistence.SaveIgnoredSharedExperience();

            LocalizationService.Reply(ctx, MessageKeys.LEVEL_SHARED_EXPERIENCE_ADDED, playerInfo.User.CharacterName.Value);
        }
        else if (DataService.PlayerDictionaries._ignoreSharedExperience.Contains(playerInfo.User.PlatformId))
        {
            DataService.PlayerDictionaries._ignoreSharedExperience.Remove(playerInfo.User.PlatformId);
            DataService.PlayerPersistence.SaveIgnoredSharedExperience();

            LocalizationService.Reply(ctx, MessageKeys.LEVEL_SHARED_EXPERIENCE_REMOVED, playerInfo.User.CharacterName.Value);
        }
    }
}