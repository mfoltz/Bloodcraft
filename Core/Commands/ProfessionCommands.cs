
using Cobalt.Systems;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using VampireCommandFramework;

namespace Cobalt.Core.Commands
{
    public static class ProfessionCommands
    {
        [Command(name: "logProfessionProgress", shortHand: "lpp", adminOnly: false, usage: ".lpp", description: "Toggles profession progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ProfessionLogging"] = !bools["ProfessionLogging"];
            }
            ctx.Reply($"Profession progress logging is now {(bools["ProfessionLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "getProfessionProgress", shortHand: "gpp", adminOnly: false, usage: ".gpp [Profession]", description: "Display your current mastery progress.")]
        public static void GetProfessionCommand(ChatCommandContext ctx, string profession)
        {
            ulong steamID = ctx.Event.User.PlatformId;
            PrefabGUID empty = new(0);
            IProfessionHandler professionHandler = ProfessionHandlerFactory.GetProfessionHandler(empty, profession.ToLower());
            if (professionHandler == null)
            {
                ctx.Reply("Invalid profession.");
                return;
            }
            if (DataStructures.professionMap.TryGetValue(profession, out var professionDictionary) && professionDictionary.TryGetValue(steamID, out var prof))
            {
                ctx.Reply($"You are level [<color=yellow>{prof.Key}</color>] (<color=white>{ProfessionSystem.GetLevelProgress(steamID, professionHandler)}%</color>) in {professionHandler.GetProfessionName()}");
            }
            else
            {
                ctx.Reply($"You haven't gained any levels in {professionHandler.GetProfessionName()} yet. ");
            }
        }
        [Command(name: "teststrip", shortHand: "teststrip", adminOnly: false, usage: ".teststrip", description: "tes")]
        public static void TestCommand(ChatCommandContext ctx)
        {
            Plugin.StripLevelSources();
        }

        public class BuildingCostsToggle
        {
            private static bool buildingCostsFlag = false;

            private static SetDebugSettingEvent BuildingCostsDebugSetting = new()
            {
                SettingType = (DebugSettingType)5, // Assuming this is the correct DebugSettingType for building costs
                Value = false
            };

            [Command(name: "toggleBuildingCosts", shortHand: "tbc", adminOnly: true, usage: ".tbc", description: "Toggles building costs, useful for setting up a castle linked to your heart easily.")]
            public static void ToggleBuildingCostsCommand(ChatCommandContext ctx)
            {
                User user = ctx.Event.User;

                DebugEventsSystem existingSystem = VWorld.Server.GetExistingSystemManaged<DebugEventsSystem>();
                buildingCostsFlag = !buildingCostsFlag; // Toggle the flag

                BuildingCostsDebugSetting.Value = buildingCostsFlag;
                existingSystem.SetDebugSetting(user.Index, ref BuildingCostsDebugSetting);

                ctx.Reply($"BuildingCostsDisabled: {BuildingCostsDebugSetting.Value}");
            }
        }
    }
}
