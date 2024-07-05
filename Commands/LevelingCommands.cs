using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Experience;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    [CommandGroup(name:"level", ".lvl")]
internal static class LevelingCommands
{
    static readonly bool Leveling = Plugin.LevelingSystem.Value;
    static readonly int MaxPlayerLevel = Plugin.MaxPlayerLevel.Value;

 
    [Command(name: "log", shortHand: "log", adminOnly: false, usage: ".lvl log", description: "Toggles leveling progress logging.")]
    public static void LogExperienceCommand(ChatCommandContext ctx)
    {
        if (!Leveling)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["ExperienceLogging"] = !bools["ExperienceLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Leveling experience logging {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "get", shortHand: "get", adminOnly: false, usage: ".lvl get", description: "Display current leveling progress.")]
    public static void GetLevelCommand(ChatCommandContext ctx)
    {
        if (!Leveling)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }
        ulong steamId = ctx.Event.User.PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var levelKvp))
        {
            int level = levelKvp.Key;
            int progress = (int)(levelKvp.Value - LevelingSystem.ConvertLevelToXp(level));
            int percent = LevelingSystem.GetLevelProgress(steamId);
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{level}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience yet.");
        }
    }

    [Command(name: "set", shortHand: "set", adminOnly: true, usage: ".lvl set [Player] [Level]", description: "Sets player level.")]
    public static void SetLevelCommand(ChatCommandContext ctx, string name, int level)
    {
        if (!Leveling)
        {
            LocalizationService.HandleReply(ctx, "Leveling is not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found...");
            return;
        }
        User foundUser = foundUserEntity.Read<User>();

        if (level < 0 || level > MaxPlayerLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {MaxPlayerLevel}");
            return;
        }
        ulong steamId = foundUser.PlatformId;
        if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var _))
        {
            var xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
            Core.DataStructures.PlayerExperience[steamId] = xpData;
            Core.DataStructures.SavePlayerExperience();
            GearOverride.SetLevel(foundUser.LocalCharacter._Entity);
            LocalizationService.HandleReply(ctx, $"Level set to <color=white>{level}</color> for <color=green>{foundUser.CharacterName}</color>");
        }
        else
        {
            LocalizationService.HandleReply(ctx, "No experience found.");
        }
    }

   


}
}