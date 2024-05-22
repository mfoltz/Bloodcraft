using Bloodcraft.Patches;
using Bloodcraft.Systems.Legacy;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;

namespace Bloodcraft.Commands
{
    public static class BloodCommands
    {
        [Command(name: "getLegacyProgress", shortHand: "get legacy", adminOnly: false, usage: ".get legacy", description: "Display your current Legacy progress.")]
        public static void GetLegacyCommand(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Legacies are not enabled.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            Blood blood = character.Read<Blood>();
            BloodSystem.BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(blood.BloodType);

            IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (handler == null)
            {
                ctx.Reply($"No Legacy handler found for {bloodType}.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            var LegacyData = handler.GetLegacyData(steamID);

            // LegacyData.Key represents the level, and LegacyData.Value represents the experience.
            if (LegacyData.Key > 0 || LegacyData.Value > 0)
            {
                ctx.Reply($"Your Legacy is <color=yellow>{LegacyData.Key}</color> (<color=white>{BloodSystem.GetLevelProgress(steamID, handler)}%</color>) with {bloodType}.");
            }
            else
            {
                ctx.Reply($"You haven't gained any Legacy for {bloodType} yet.");
            }
        }

        [Command(name: "logLegacyProgress", shortHand: "log legacy", adminOnly: false, usage: ".log legacy", description: "Toggles Legacy progress logging.")]
        public static void LogLegacyCommand(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Legacies are not enabled.");
                return;
            }
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["BloodLogging"] = !bools["BloodLogging"];
            }
            ctx.Reply($"Legacy logging is now {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "setBloodLegacy", shortHand: "sbl", adminOnly: true, usage: ".sbl [Player] [BloodType] [Level]", description: "Sets your blood Legacy level.")]
        public static void SetLegacyCommand(ChatCommandContext ctx, string name, string blood, int level)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Legacies are not enabled.");
                return;
            }

            User foundUser = ServerBootstrapPatch.users.FirstOrDefault(user => user.CharacterName.ToString().ToLower() == name.ToLower());
            if (foundUser.CharacterName.IsEmpty)
            {
                ctx.Reply("Player not found.");
                return;
            }
            if (level < 0 || level > BloodSystem.MaxBloodLevel)
            {
                ctx.Reply($"Level must be between 0 and {BloodSystem.MaxBloodLevel}.");
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

            ulong steamId = ctx.Event.User.PlatformId;
            var xpData = new KeyValuePair<int, float>(level, BloodSystem.ConvertLevelToXp(level));
            BloodHandler.UpdateLegacyData(steamId, xpData);
            BloodHandler.SaveChanges();

            ctx.Reply($"Legacy for <color=red>{BloodHandler.GetBloodType()}</color> set to <color=white>{level}</color> for {foundUser.CharacterName}.");
        }

        [Command(name: "listBloodTypes", shortHand: ".lbt", adminOnly: true, usage: ".lbt", description: "Lists blood types.")]
        public static void ListBloodTypesCommand(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Legacies are not enabled.");
                return;
            }
            string bloodTypes = string.Join(", ", Enum.GetNames(typeof(BloodSystem.BloodType)));
            ctx.Reply($"Available blood legacies: {bloodTypes}");
        }
    }
}