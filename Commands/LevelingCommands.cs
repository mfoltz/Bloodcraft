using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    public static class LevelingCommands //560247139 Journal_GettingReadyForTheHunt
    {
        [Command(name: "quickStart", shortHand: "start", adminOnly: false, usage: ".start", description: "Completes GettingReadyForTheHunt.")]
        public static void QuickStartCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            User user = ctx.Event.User;
            ulong steamId = user.PlatformId;
            if (Core.DataStructures.PlayerBools.TryGetValue(ctx.Event.User.PlatformId, out var bools) && bools["QuickStart"])
            {
                ctx.Reply("You are already prepared for the hunt.");
                return;
            }

            ProgressionMapper progressionMapper = ctx.Event.SenderUserEntity.Read<ProgressionMapper>();
            Entity unlockedProgressionEntity = progressionMapper.ProgressionEntity._Entity;
            var buffer = unlockedProgressionEntity.ReadBuffer<UnlockedProgressionElement>();
            bool hasQuest = false;
            foreach (var element in buffer)
            {
                if (element.UnlockedPrefab.GuidHash == 560247139)
                {
                    hasQuest = true;
                    break;
                }
            }
            if (!hasQuest) buffer.Add(new UnlockedProgressionElement { UnlockedPrefab = new(560247139) });

            Core.DataStructures.PlayerBools[steamId]["QuickStart"] = true;
            Core.DataStructures.SavePlayerBools();
            ctx.Reply("You are now prepared for the hunt.");
        }

        [Command(name: "logLevelingProgress", shortHand: "log l", adminOnly: false, usage: ".log l", description: "Toggles leveling progress logging.")]
        public static void LogExperienceCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ExperienceLogging"] = !bools["ExperienceLogging"];
            }
            Core.DataStructures.SavePlayerBools();
            ctx.Reply($"Leveling progress logging is now {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "getLevelingProgress", shortHand: "get l", adminOnly: false, usage: ".get l", description: "Display current leveling progress.")]
        public static void GetLevelCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            ulong steamId = ctx.Event.User.PlatformId;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var Leveling))
            {
                int level = Leveling.Key;
                int progress = (int)(Leveling.Value - LevelingSystem.ConvertLevelToXp(level));
                int percent = LevelingSystem.GetLevelProgress(steamId);
                ctx.Reply($"You're level [<color=white>{level}</color>] and have <color=yellow>{progress}</color> experience (<color=white>{percent}%</color>)");
            }
            else
            {
                ctx.Reply("No leveling progress yet.");
            }
        }

        [Command(name: "setLevel", shortHand: "sl", adminOnly: true, usage: ".sl [Player] [Level]", description: "Sets your level.")]
        public static void SetLevelCommand(ChatCommandContext ctx, string name, int level)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }

            Entity foundUserEntity = Core.FindUserOnline(name);
            if (foundUserEntity.Equals(Entity.Null))
            {
                ctx.Reply("Player not found...");
                return;
            }
            User foundUser = foundUserEntity.Read<User>();

            if (level < 0 || level > Plugin.MaxPlayerLevel.Value)
            {
                ctx.Reply($"Level must be between 0 and {Plugin.MaxPlayerLevel.Value}");
                return;
            }
            ulong steamId = foundUser.PlatformId;
            //Entity character = Core.ServerBootstrapSystem._ApprovedUsersLookup[foundUser.Index].UserEntity.Read<User>().LocalCharacter._Entity;
            if (Core.DataStructures.PlayerExperience.TryGetValue(steamId, out var _))
            {
                var xpData = new KeyValuePair<int, float>(level, LevelingSystem.ConvertLevelToXp(level));
                Core.DataStructures.PlayerExperience[steamId] = xpData;
                Core.DataStructures.SavePlayerExperience();
                GearOverride.SetLevel(foundUser.LocalCharacter._Entity);
                ctx.Reply($"Level set to <color=white>{level}</color> for {foundUser.CharacterName}");
            }
            else
            {
                ctx.Reply("No experience data found.");
            }
        }

    }
}