using Bloodstone.API;
using ProjectM.Network;
using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VampireCommandFramework;
using VCreate.Core.Toolbox;
using Unity.Entities;

namespace VCreate.Core.Commands
{
    public class WorldBuildToggle
    {
        private static bool wbFlag = false;

        public static bool WbFlag
        {
            get { return wbFlag; }
        }

        private static SetDebugSettingEvent BuildingCostsDebugSetting = new()
        {
            SettingType = (DebugSettingType)5,
            Value = false
        };

        private static SetDebugSettingEvent BuildingPlacementRestrictionsDisabledSetting = new()
        {
            SettingType = (DebugSettingType)16,
            Value = false
        };

        

        [Command(name: "toggleWorldBuild", shortHand: "twb", adminOnly: true, usage: ".twb", description: "Toggles worldbuilding debug settings for no-cost building anywhere by anyone.")]
        public static void ToggleBuildDebugCommand(ChatCommandContext ctx)
        {
            User user = ctx.Event.User;

            DebugEventsSystem existingSystem = VWorld.Server.GetExistingSystem<DebugEventsSystem>();
            if (!wbFlag)
            {
                // want to destroy resource nodes in active player territories here to remove overgrowth

                //ResourceFunctions.SearchAndDestroy();
                wbFlag = true;
                BuildingCostsDebugSetting.Value = wbFlag;
                existingSystem.SetDebugSetting(user.Index, ref BuildingCostsDebugSetting);

                BuildingPlacementRestrictionsDisabledSetting.Value = wbFlag;
                existingSystem.SetDebugSetting(user.Index, ref BuildingPlacementRestrictionsDisabledSetting);

                

                string enabledColor = FontColors.Green("enabled");
                ctx.Reply($"WorldBuild: {enabledColor}");
                ctx.Reply($"BuildingCostsDisabled: |{BuildingCostsDebugSetting.Value}| || BuildingPlacementRestrictionsDisabled: |{BuildingPlacementRestrictionsDisabledSetting.Value}|");
            }
            else
            {
                wbFlag = false;
                BuildingCostsDebugSetting.Value = wbFlag;
                existingSystem.SetDebugSetting(user.Index, ref BuildingCostsDebugSetting);

                BuildingPlacementRestrictionsDisabledSetting.Value = wbFlag;
                existingSystem.SetDebugSetting(user.Index, ref BuildingPlacementRestrictionsDisabledSetting);

                

                string disabledColor = FontColors.Red("disabled");
                ctx.Reply($"WorldBuild: {disabledColor}");
                ctx.Reply($"BuildingCostsDisabled: |{BuildingCostsDebugSetting.Value}| || BuildingPlacementRestrictionsDisabled: |{BuildingPlacementRestrictionsDisabledSetting.Value}|");
            }
        }
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

            DebugEventsSystem existingSystem = VWorld.Server.GetExistingSystem<DebugEventsSystem>();
            buildingCostsFlag = !buildingCostsFlag; // Toggle the flag

            BuildingCostsDebugSetting.Value = buildingCostsFlag;
            existingSystem.SetDebugSetting(user.Index, ref BuildingCostsDebugSetting);

            string toggleColor = buildingCostsFlag ? FontColors.Green("enabled") : FontColors.Red("disabled");
            ctx.Reply($"Building costs {toggleColor}");
            ctx.Reply($"BuildingCostsDisabled: {BuildingCostsDebugSetting.Value}");
        }
    }

    public class CastleHeartConnectionToggle
    {
        private static bool castleHeartConnectionRequirementFlag = false;

        private static SetDebugSettingEvent CastleHeartConnectionDebugSetting = new()
        {
            SettingType = (DebugSettingType)27,
            Value = false
        };

        [Command(name: "toggleCastleHeartConnectionRequirement", shortHand: "tchc", adminOnly: true, usage: ".tchc", description: "Toggles the Castle Heart connection requirement for structures. Handy for testing.")]
        public static void ToggleCastleHeartConnectionCommand(ChatCommandContext ctx)
        {
            User user = ctx.Event.User;

            DebugEventsSystem existingSystem = VWorld.Server.GetExistingSystem<DebugEventsSystem>();
            castleHeartConnectionRequirementFlag = !castleHeartConnectionRequirementFlag; // Toggle the flag

            CastleHeartConnectionDebugSetting.Value = castleHeartConnectionRequirementFlag;
            existingSystem.SetDebugSetting(user.Index, ref CastleHeartConnectionDebugSetting);

            string toggleColor = castleHeartConnectionRequirementFlag ? FontColors.Green("enabled") : FontColors.Red("disabled");
            ctx.Reply($"Castle Heart connection requirement {toggleColor}");
            ctx.Reply($"CastleHeartConnectionRequirementDisabled: {CastleHeartConnectionDebugSetting.Value}");
        }
        public static void ToggleCastleHeartConnectionCommandOnConnected(Entity userEntity)
        {
            User user = userEntity.Read<User>();

            DebugEventsSystem existingSystem = VWorld.Server.GetExistingSystem<DebugEventsSystem>();
            castleHeartConnectionRequirementFlag = !castleHeartConnectionRequirementFlag; // Toggle the flag

            CastleHeartConnectionDebugSetting.Value = castleHeartConnectionRequirementFlag;
            existingSystem.SetDebugSetting(user.Index, ref CastleHeartConnectionDebugSetting);

        }
    }
}
