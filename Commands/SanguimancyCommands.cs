using Cobalt.Hooks;
using Cobalt.Systems.Sanguimancy;
using ProjectM.Network;
using Steamworks;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Core;
using static Cobalt.Systems.Sanguimancy.BloodStats;

namespace Cobalt.Commands
{
    public static class SanguimancyCommands
    {
        [Command(name: "getSanguimancyProgress", shortHand: "gsp", adminOnly: false, usage: ".gsp", description: "Display your current sanguimancy progress.")]
        public static void GetSanguimancyCommand(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;
            if (Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var Sanguimancy))
            {
                ctx.Reply($"You are level <color=white>{Sanguimancy.Key}</color> in <color=red>sanguimancy</color>.");
            }
            else
            {
                ctx.Reply("No progress in sanguimancy yet.");
            }
        }

        [Command(name: "logSanguimancyProgress", shortHand: "lsp", adminOnly: false, usage: ".lsp", description: "Toggles sanguimancy progress logging.")]
        public static void LogSanguimancyProgress(ChatCommandContext ctx)
        {
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["BloodLogging"] = !bools["BloodLogging"];
            }
            ctx.Reply($"Sanguimancy progress logging is now {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "setSanguimancyProgress", shortHand: "ssp", adminOnly: true, usage: ".ssp [Player] [Level]", description: "Sets sanguimancy progress.")]
        public static void SetSanguimancyProgress(ChatCommandContext ctx, string name, int level)
        {
            User foundUser = ServerBootstrapPatches.users.FirstOrDefault(user => user.CharacterName.ToString().ToLower() == name.ToLower());
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

            if (Core.DataStructures.PlayerSanguimancy.TryGetValue(foundUser.PlatformId, out var Sanguimancy))
            {
                Sanguimancy = new KeyValuePair<int, float>(level, BloodSystem.ConvertLevelToXp(level));
                Core.DataStructures.PlayerSanguimancy[foundUser.PlatformId] = Sanguimancy;
                Core.DataStructures.SavePlayerSanguimancy();
                ctx.Reply($"Sanguimancy level set to {level} for {foundUser.CharacterName}.");
            }
        }

        [Command(name: "lockSpell", shortHand: "lock", adminOnly: false, usage: ".lock", description: "Locks in the next spells equipped to use in your unarmed slots.")]
        public static void LockPlayerSpells(ChatCommandContext ctx)
        {
            var user = ctx.Event.User;
            var SteamID = user.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var data) && data.Key >= 25)
            {
                bools["SpellLock"] = !bools["SpellLock"];
                if (bools["SpellLock"])
                {
                    ctx.Reply("Change spells to the ones you want in your unarmed slot(s). When done, toggle this again.");
                }
                else
                {
                    ctx.Reply("Spells set.");
                }
            }
            else
            {
                ctx.Reply("You must be level 25 in Sanguimancy to lock spells.");
            }
        }
    }
}