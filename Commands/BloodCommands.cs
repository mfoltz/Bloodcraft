using Bloodcraft.Patches;
using Bloodcraft.Systems.Experience;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacy;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Expertise.ExpertiseSystem;

namespace Bloodcraft.Commands
{
    public static class BloodCommands
    {
        [Command(name: "getBloodLegacyProgress", shortHand: "get bl", adminOnly: false, usage: ".get bl", description: "Display your current Blood Legacy progress.")]
        public static void GetBloodLegacyCommand(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Blood Legacies are not enabled.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            Blood blood = character.Read<Blood>();
            BloodSystem.BloodType bloodType = BloodSystem.GetBloodTypeFromPrefab(blood.BloodType);

            IBloodHandler handler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (handler == null)
            {
                ctx.Reply($"No Blood Legacy handler found for {bloodType}.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            var LegacyData = handler.GetLegacyData(steamID);
            int progress = (int)(LegacyData.Value - BloodSystem.ConvertLevelToXp(LegacyData.Key));
            // LegacyData.Key represents the level, and LegacyData.Value represents the experience.
            if (LegacyData.Key > 0 || LegacyData.Value > 0)
            {
                ctx.Reply($"Your blood legacy is level [<color=white>{LegacyData.Key}</color>] and you have <color=yellow>{progress}</color> <color=#FFC0CB>essence</color> (<color=white>{BloodSystem.GetLevelProgress(steamID, handler)}%</color>) in <color=red>{bloodType}</color>");
            }
            else
            {
                ctx.Reply($"You haven't gained any <color=#FFC0CB>essence</color> for {bloodType} yet.");
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

            ctx.Reply($"<color=red>{BloodHandler.GetBloodType()}</color> lineage set to <color=white>{level}</color> for {foundUser.CharacterName}.");
        }

        [Command(name: "listBloodTypes", shortHand: ".lbt", adminOnly: false, usage: ".lbt", description: "Lists blood types.")]
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