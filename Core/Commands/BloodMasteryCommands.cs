using Cobalt.Hooks;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Bloodline.BloodStatsSystem;

namespace Cobalt.Core.Commands
{
    public static class BloodMasteryCommands
    {
        private static PrefabGUID holder;

        [Command(name: "getSanguimancyProgress", shortHand: "gsp", adminOnly: false, usage: ".gsp", description: "Display your current sanguimancy progress.")]
        public static void GetMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var mastery))
            {
                ctx.Reply($"You are level <color=white>{mastery.Key}</color> in <color=red>sanguimancy</color>.");
            }
            else
            {
                ctx.Reply("You haven't gained any sanguimancy  ");
            }
        }

        [Command(name: "logBloodlineProgress", shortHand: "lbp", adminOnly: false, usage: ".lbp", description: "Toggles bloodline progress logging.")]
        public static void LogMasteryCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["BloodLogging"] = !bools["BloodLogging"];
            }
            ctx.Reply($"Sanguimancy progress logging is now {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "chooseBloodStat", shortHand: "cbs", adminOnly: false, usage: ".cbs <Stat>", description: "Choose a bloodline stat to enhance based on your mastery.")]
        public static void SetBloodlineStatCommand(ChatCommandContext ctx, string statChoice)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            string statType = statChoice.ToLower();
            // try to parse statType from choice string
            if (!Enum.TryParse<BloodStatManager.BloodStatType>(statType, true, out _))
            {
                ctx.Reply("Invalid bloodStat type.");
                return;
            }
            if (!DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var _))
            {
                List<string> bloodStats = [];
                DataStructures.PlayerBloodChoices[steamId] = bloodStats;
            }

            if (PlayerBloodUtilities.ChooseStat(steamId, statType))
            {
                ctx.Reply($"Stat {statType} has been chosen for Sanguimancy.");
                DataStructures.SavePlayerBloodChoices();
            }
            else
            {
                ctx.Reply("You have already chosen two stats for this weapon.");
            }
        }

        [Command(name: "resetBloodStats", shortHand: "rbs", adminOnly: false, usage: ".rbs", description: "Reset the stat choices for a player's bloodline stats.")]
        public static void ResetBloodlineStatsCommand(ChatCommandContext ctx)
        {
            ulong steamId = ctx.Event.User.PlatformId;
            Entity character = ctx.Event.SenderCharacterEntity;
            if (!DataStructures.PlayerBloodChoices.TryGetValue(steamId, out var stats))
            {
                ctx.Reply("No blood choices found for this SteamID.");
                return;
            }
            UnitStatsOverride.RemoveBloodBonuses(character);
            //PlayerBloodUtilities.ResetChosenStats(steamId);
            stats.Clear();
            DataStructures.SavePlayerBloodChoices();
            ctx.Reply($"Blood stat choices reset.");
        }

        [Command(name: "addAbility", shortHand: "ability", adminOnly: false, usage: ".ability [AbilityGroupName] [Slot]", description: "Puts prefab ability group on shift.")]
        public static void AbilityTestCommand(ChatCommandContext ctx, string abilityGroup, int slot)
        {
            ServerGameManager serverGameManager = VWorld.Server.GetExistingSystemManaged<ServerScriptMapper>()._ServerGameManager;
            PrefabCollectionSystem prefabCollectionSystem = VWorld.Server.GetExistingSystemManaged<PrefabCollectionSystem>();
            Entity character = ctx.Event.SenderCharacterEntity;
            
            if (slot == 1 || slot == 4)
            {
                ctx.Reply("Invalid slot number.");
                return;
            }
            if (serverGameManager.TryGetBuff(character, UnitStatsOverride.unarmed.ToIdentifier(), out Entity buff))
            {
                if (prefabCollectionSystem.NameToPrefabGuidDictionary.TryGetValue(abilityGroup, out PrefabGUID prefabGUID))
                {
                    serverGameManager.ModifyAbilityGroupOnSlot(buff, character, slot, prefabGUID);
                }
                else
                {
                    ctx.Reply("Invalid ability group.");
                }
            }
            else
            {
                ctx.Reply("You must have the unarmed buff to use this command.");
            }
        }
    }
}