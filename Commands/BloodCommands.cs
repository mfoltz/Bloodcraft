using Bloodcraft.Patches;
using Bloodcraft.Systems.Legacy;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    public static class BloodCommands
    {
        [Command(name: "getBloodLegacyProgress", shortHand: "gbl", adminOnly: false, usage: ".gbl [BloodType]", description: "Display your current blood legacy progress.")]
        public static void GetProfessionCommand(ChatCommandContext ctx, string blood)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Blood Legacies are not enabled.");
                return;
            }

            if (!Enum.TryParse<BloodSystem.BloodType>(blood, true, out var bloodType))
            {
                ctx.Reply("Invalid blood type, use .lbt to see options.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (bloodHandler == null)
            {
                ctx.Reply("Invalid blood type.");
                return;
            }
            var data = bloodHandler.GetLegacyData(steamID);
            int progress = (int)(data.Value - BloodSystem.ConvertLevelToXp(data.Key));
            if (data.Key > 0)
            {
                ctx.Reply($"You're level [<color=white>{data.Key}</color>] and have <color=yellow>{progress}</color> experience (<color=white>{BloodSystem.GetLevelProgress(steamID, bloodHandler)}%</color>) in <color=red>{bloodHandler.GetBloodType()}</color>");
            }
            else
            {
                ctx.Reply($"No progress in <color=red>{bloodHandler.GetBloodType()}</color> yet. ");
            }
        }

        [Command(name: "logBloodLegacyProgress", shortHand: "log bl", adminOnly: false, usage: ".log bl", description: "Toggles Legacy progress logging.")]
        public static void LogLegacyCommand(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Blood Legacies are not enabled.");
                return;
            }
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["BloodLogging"] = !bools["BloodLogging"];
            }
            Core.DataStructures.SavePlayerBools();
            ctx.Reply($"Blood Legacy logging is now {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "setBloodLegacy", shortHand: "sbl", adminOnly: true, usage: ".sbl [Player] [BloodType] [Level]", description: "Sets player Blood Legacy level.")]
        public static void SetBloodLegacyCommand(ChatCommandContext ctx, string name, string blood, int level)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Blood Legacies are not enabled.");
                return;
            }
            Entity foundUserEntity = Core.FindUserOnline(name);
            if (foundUserEntity.Equals(Entity.Null))
            {
                ctx.Reply("Player not found.");
                return;
            }
            User foundUser = foundUserEntity.Read<User>();
            if (level < 0 || level > Plugin.MaxBloodLevel.Value)
            {
                ctx.Reply($"Level must be between 0 and {Plugin.MaxBloodLevel.Value}.");
                return;
            }
            if (!Enum.TryParse<BloodSystem.BloodType>(blood, true, out var bloodType))
            {
                ctx.Reply("Invalid blood type.");
            }
            var BloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (BloodHandler == null)
            {
                ctx.Reply("Invalid blood type.");
                return;
            }
            ulong steamId = foundUser.PlatformId;
            var xpData = new KeyValuePair<int, float>(level, BloodSystem.ConvertLevelToXp(level));
            BloodHandler.UpdateLegacyData(steamId, xpData);
            BloodHandler.SaveChanges();

            ctx.Reply($"<color=red>{BloodHandler.GetBloodType()}</color> lineage set to <color=white>{level}</color> for {foundUser.CharacterName}.");
        }

        [Command(name: "listBloodTypes", shortHand: "lbt", adminOnly: false, usage: ".lbt", description: "Lists blood types.")]
        public static void ListBloodTypesCommand(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Blood Legacies are not enabled.");
                return;
            }
            string bloodTypes = string.Join(", ", Enum.GetNames(typeof(BloodSystem.BloodType)));
            ctx.Reply($"Available Blood Legacies: {bloodTypes}");
        }
    }
}