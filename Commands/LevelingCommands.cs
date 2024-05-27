using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    public static class LevelingCommands
    {
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

        [Command(name: "setLevel", shortHand: "sl", adminOnly: true, usage: ".sl [Player] [Level]", description: "Sets player level.")]
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
                ctx.Reply($"Level set to <color=white>{level}</color> for <color=green>{foundUser.CharacterName}</color>");
            }
            else
            {
                ctx.Reply("No experience data found.");
            }
        }

        [Command(name: "toggleGrouping", shortHand: "grouping", adminOnly: false, usage: ".grouping", description: "Toggles being able to be invited to group with players not in clan for exp sharing.")]
        public static void ToggleGroupingCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["Grouping"] = !bools["Grouping"];
            }
            Core.DataStructures.SavePlayerBools();
            ctx.Reply($"Group invites are now {(bools["Grouping"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "groupAdd", shortHand: "ga", adminOnly: false, usage: ".ga [Player]", description: "Adds player to group for exp sharing if they permit it in settings.")]
        public static void GroupAddCommand(ChatCommandContext ctx, string name)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            ulong ownerId = ctx.Event.User.PlatformId;
            Entity foundUserEntity = Core.FindUserOnline(name);
            if (foundUserEntity.Equals(Entity.Null))
            {
                ctx.Reply("Player not found...");
                return;
            }
            User foundUser = foundUserEntity.Read<User>();
            if (foundUser.PlatformId == ownerId)
            {
                ctx.Reply("You can't add yourself to your own group.");
                return;
            }
            Entity characterToAdd = foundUser.LocalCharacter._Entity;
            if (Core.DataStructures.PlayerBools.TryGetValue(foundUser.PlatformId, out var bools) && bools["Grouping"] && !Core.DataStructures.PlayerGroups.ContainsKey(foundUser.PlatformId)) // get consent, make sure they don't have their own group made first
            {
                if (!Core.DataStructures.PlayerGroups.ContainsKey(ownerId)) // check if player has a group, make one if not
                {
                    Core.DataStructures.PlayerGroups[ownerId] = [];
                }
                List<Entity> group = Core.DataStructures.PlayerGroups[ownerId]; // check size and if player is already present in group before adding
                if (group.Count < Plugin.MaxGroupSize.Value && !group.Contains(characterToAdd))
                {
                    group.Add(characterToAdd);
                    ctx.Reply($"<color=green>{foundUser.CharacterName}</color> added to your group.");
                }
                else
                {
                    ctx.Reply($"Group is full or <color=green>{foundUser.CharacterName}</color> is already in group.");
                }
            }
            else
            {
                ctx.Reply($"<color=green>{foundUser.CharacterName}</color> does not have grouping enabled or they already have a group created.");
            }
        }

        [Command(name: "groupRemove", shortHand: "gr", adminOnly: false, usage: ".gr [Player]", description: "Removes player from group for exp sharing.")]
        public static void GroupRemoveCommand(ChatCommandContext ctx, string name)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            ulong ownerId = ctx.Event.User.PlatformId;
            Entity foundUserEntity = Core.FindUserOnline(name);
            if (foundUserEntity.Equals(Entity.Null))
            {
                ctx.Reply("Player not found...");
                return;
            }
            User foundUser = foundUserEntity.Read<User>();
            Entity characterToRemove = foundUser.LocalCharacter._Entity;
            
            if (!Core.DataStructures.PlayerGroups.ContainsKey(ownerId)) // check if player has a group, make one if not
            {
                ctx.Reply("You don't have a group. Create one and add people before trying to remove them.");
                return;
            }
            List<Entity> group = Core.DataStructures.PlayerGroups[ownerId]; // check size and if player is already present in group before adding
            if (group.Contains(characterToRemove))
            {
                group.Remove(characterToRemove);
                ctx.Reply($"<color=green>{foundUser.CharacterName}</color> removed from your group.");
            }
            else
            {
                ctx.Reply($"<color=green>{foundUser.CharacterName}</color> is not in the group and therefore cannot be removed from it.");
            }
           
        }
        [Command(name: "groupDisband", shortHand: "disband", adminOnly: false, usage: ".disband", description: "Disbands exp sharing group.")]
        public static void GroupDisbandCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }

            ulong ownerId = ctx.Event.User.PlatformId;

            if (!Core.DataStructures.PlayerGroups.ContainsKey(ownerId)) // check if player has a group, make one if not
            {
                ctx.Reply("You don't have a group. Create one by adding a player before trying to disband it.");
                return;
            }
            else
            {
                Core.DataStructures.PlayerGroups.Remove(ownerId);
                ctx.Reply("Group disbanded.");
            }
        }
    }
}
