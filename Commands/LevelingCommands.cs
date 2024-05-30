using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Leveling;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    public static class LevelingCommands
    {
        [Command(name: "quickStart", shortHand: "start", adminOnly: false, usage: ".start", description: "Completes GettingReadyForTheHunt if not already completed.")]
        public static void QuickStartCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            EntityCommandBuffer entityCommandBuffer = Core.EntityCommandBufferSystem.CreateCommandBuffer();
            PrefabGUID achievementPrefabGuid = new(560247139); // Journal_GettingReadyForTheHunt
            Entity userEntity = ctx.Event.SenderUserEntity;
            Entity characterEntity = ctx.Event.SenderCharacterEntity;
            Entity achievementOwnerEntity = userEntity.Read<AchievementOwner>().Entity._Entity;
            Core.ClaimAchievementSystem.CompleteAchievement(entityCommandBuffer, achievementPrefabGuid, userEntity, characterEntity, achievementOwnerEntity, false, true);
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
            ctx.Reply($"Leveling progress logging {(bools["ExperienceLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
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
                ctx.Reply($"You're level [<color=white>{level}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>experience</color> (<color=white>{percent}%</color>)");
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

        [Command(name: "playerPrestige", shortHand: "prestige", adminOnly: false, usage: ".prestige [PrestigeType]", description: "Handles player prestiging.")]
        public unsafe static void PrestigeCommand(ChatCommandContext ctx, string prestigeType)
        {
            if (!Plugin.PrestigeSystem.Value)
            {
                ctx.Reply("Prestiging is not enabled.");
                return;
            }
            if (!Enum.TryParse<PrestigeSystem.PrestigeType>(prestigeType, true, out var parsedPrestigeType))
            {
                ctx.Reply("Invalid prestige, use .lpp to see options (WIP).");
                return;
            }

            var steamId = ctx.Event.User.PlatformId;
            var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

            if (handler == null)
            {
                ctx.Reply("Invalid prestige type.");
                return;
            }

            var xpData = handler.GetExperienceData(steamId);
            if (xpData.Key >= PrestigeSystem.PrestigeTypeToMaxLevel[parsedPrestigeType] && Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) && prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) && prestigeLevel < PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType])
            {
                handler.Prestige(steamId);
                
                if (parsedPrestigeType.Equals(PrestigeSystem.PrestigeType.Experience))
                {
                    GearOverride.SetLevel(ctx.Event.SenderCharacterEntity);
                    string experienceString = (prestigeLevel * Plugin.PrestigeRatesMultiplier.Value * 100).ToString("F0") + "%";
                    ctx.Reply($"You have prestiged in <color=#90EE90>{parsedPrestigeType}</color>! Growth rates for all expertise/legacies increased by <color=yellow>{experienceString}</color>.");
                    handler.SaveChanges();
                    return;
                }

                // Calculate the cumulative effect of reduction in growth rates
                float reductionFactor = 1.0f / (1 + prestigeLevel * Plugin.PrestigeRatesReducer.Value);
                float growthRateAfterReduction = reductionFactor * (1 + prestigeLevel * Plugin.PrestigeRatesMultiplier.Value);
                float totalEffectPercentage = (growthRateAfterReduction - 1) * 100;

                // Calculate the percentage reduction and the net effect
                float percentageReduction = (1 - reductionFactor) * 100;
                string percentageReductionString = percentageReduction.ToString("F0") + "%";

                // Calculate the stat gain increase
                float statGainIncrease = prestigeLevel * Plugin.PrestigeStatMultiplier.Value;
                string statGainString = (statGainIncrease * 100).ToString("F0") + "%";

                string totalEffectString = totalEffectPercentage >= 0 ? "+" + totalEffectPercentage.ToString("F0") + "%" : totalEffectPercentage.ToString("F0") + "%";

                ctx.Reply($"You have prestiged in <color=#90EE90>{parsedPrestigeType}</color>! Growth rate reduced by <color=yellow>{percentageReductionString}</color> and stat bonuses improved by <color=green>{statGainString}</color>. The total effect on growth rates is <color=yellow>{totalEffectString}</color>.");
                handler.SaveChanges();
            }
            else
            {
                ctx.Reply($"You have not reached the required level to prestige in <color=#90EE90>{parsedPrestigeType}</color>.");
            }
        }
        [Command(name: "getPrestige", shortHand: "gp", adminOnly: false, usage: ".gp [PrestigeType]", description: "Shows information about player's prestige status.")]
        public unsafe static void GetPrestigeCommand(ChatCommandContext ctx, string prestigeType)
        {
            if (!Plugin.PrestigeSystem.Value)
            {
                ctx.Reply("Prestiging is not enabled.");
                return;
            }

            if (!Enum.TryParse<PrestigeSystem.PrestigeType>(prestigeType, true, out var parsedPrestigeType))
            {
                ctx.Reply("Invalid prestige type, use .lpp to see options (WIP).");
                return;
            }

            var steamId = ctx.Event.User.PlatformId;
            var handler = PrestigeHandlerFactory.GetPrestigeHandler(parsedPrestigeType);

            if (handler == null)
            {
                ctx.Reply("Invalid prestige type.");
                return;
            }

            var maxPrestigeLevel = PrestigeSystem.PrestigeTypeToMaxPrestigeLevel[parsedPrestigeType];
            if (Core.DataStructures.PlayerPrestiges.TryGetValue(steamId, out var prestigeData) && prestigeData.TryGetValue(parsedPrestigeType, out var prestigeLevel) && prestigeLevel > 0)
            {

                if (parsedPrestigeType.Equals(PrestigeSystem.PrestigeType.Experience))
                {
                    string experienceString = (prestigeLevel * Plugin.PrestigeRatesMultiplier.Value * 100).ToString() + "%";
                    ctx.Reply($"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
                    ctx.Reply($"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
                    ctx.Reply($"Growth rate improvement for expertise/legacies: <color=yellow>{experienceString}</color>");
                    return;
                }



                float reductionFactor = 1.0f / (1 + prestigeLevel * Plugin.PrestigeRatesReducer.Value);
                float growthRateAfterReduction = reductionFactor * (1 + prestigeLevel * Plugin.PrestigeRatesMultiplier.Value);
                float totalEffectPercentage = (growthRateAfterReduction - 1) * 100;

                // Calculate the percentage reduction and the net effect
                float percentageReduction = (1 - reductionFactor) * 100;
                string percentageReductionString = percentageReduction.ToString("F0") + "%";

                // Calculate the stat gain increase
                float statGainIncrease = prestigeLevel * Plugin.PrestigeStatMultiplier.Value;
                string statGainString = (statGainIncrease * 100).ToString("F0") + "%";

                string totalEffectString = totalEffectPercentage >= 0 ? "+" + totalEffectPercentage.ToString("F0") + "%" : totalEffectPercentage.ToString("F0") + "%";


                ctx.Reply($"<color=#90EE90>{parsedPrestigeType}</color> Prestige Info:");
                ctx.Reply($"Current Prestige Level: <color=yellow>{prestigeLevel}</color>/{maxPrestigeLevel}");
                ctx.Reply($"Growth rate reduction from <color=#90EE90>{parsedPrestigeType}</color> prestige level: <color=yellow>{percentageReductionString}</color>");
                ctx.Reply($"Stat bonuses improvement: <color=green>{statGainString}</color>");
                ctx.Reply($"Total effect on growth rate including leveling prestige bonus: <color=yellow>{totalEffectString}</color>");
            }
            else
            {
                ctx.Reply($"You have not prestiged in <color=#90EE90>{parsedPrestigeType}</color>.");
            }
        }

        [Command(name: "listPlayerPrestiges", shortHand: "lpp", adminOnly: false, usage: ".lpp", description: "Lists prestiges available.")]
        public static void ListPlayerPrestigeTypes(ChatCommandContext ctx)
        {
            if (!Plugin.PrestigeSystem.Value)
            {
                ctx.Reply("Prestige is not enabled.");
                return;
            }
            string prestigeTypes = string.Join(", ", Enum.GetNames(typeof(PrestigeSystem.PrestigeType)));
            ctx.Reply($"Available Prestiges: <color=#90EE90>{prestigeTypes}</color>");
        }

        [Command(name: "toggleGrouping", shortHand: "grouping", adminOnly: false, usage: ".grouping", description: "Toggles being able to be invited to group with players not in clan for exp sharing.")]
        public static void ToggleGroupingCommand(ChatCommandContext ctx)
        {
            if (!Plugin.LevelingSystem.Value)
            {
                ctx.Reply("Leveling is not enabled.");
                return;
            }
            if (!Plugin.PlayerGrouping.Value)
            {
                ctx.Reply("Grouping is not enabled.");
                return;
            }

            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["Grouping"] = !bools["Grouping"];
            }
            Core.DataStructures.SavePlayerBools();
            ctx.Reply($"Group invites {(bools["Grouping"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
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
                ctx.Reply("You don't have a group. Create one by adding a player who has group invites enabled.");
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
