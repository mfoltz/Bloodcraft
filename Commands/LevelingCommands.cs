using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Experience;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Core.DataStructures;
namespace Bloodcraft.Commands;

[CommandGroup(name:"level", "lvl")]
internal static class LevelingCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PlayerService PlayerService => Core.PlayerService;

    [Command(name: "log", adminOnly: false, usage: ".lvl log", description: "Toggles leveling progress logging.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LevelingSystem)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ExperienceLogging"] = !bools["ExperienceLogging"];
        }
        SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Leveling experience logging {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
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

        if (PlayerExperience.TryGetValue(steamId, out var levelKvp))
        {
            int prestigeLevel = PlayerPrestiges.TryGetValue(steamId, out var prestiges) ? prestiges[SystemUtilities.Leveling.PrestigeSystem.PrestigeType.Experience] : 0;
            int level = levelKvp.Key;
            int progress = (int)(levelKvp.Value - LevelingSystem.ConvertLevelToXp(level));
            int percent = LevelingSystem.GetLevelProgress(steamId);
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{level}</color>][<color=#90EE90>{prestigeLevel}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
            if (ConfigService.RestedXP && PlayerRestedXP.TryGetValue(steamId, out var restedData) && restedData.Value > 0)
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

        Entity foundUserEntity = PlayerService.UserCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();

        if (level < 0 || level > ConfigService.MaxPlayerLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {ConfigService.MaxPlayerLevel}");
            return;
        }
        ulong steamId = foundUser.PlatformId;
        if (PlayerExperience.TryGetValue(steamId, out var _))
        {
            var xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
            PlayerExperience[steamId] = xpData;
            SavePlayerExperience();
            LevelingSystem.SetLevel(foundUser.LocalCharacter._Entity);
            LocalizationService.HandleReply(ctx, $"Level set to <color=white>{level}</color> for <color=green>{foundUser.CharacterName}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience found.");
        }
    }
}