using Cobalt.Hooks;
using Cobalt.Systems.Expertise;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Cobalt.Systems.Expertise.WeaponStats;

namespace Cobalt.Commands
{
    public static class ExpertiseCommands
    {
        [Command(name: "getExpertiseProgress", shortHand: "get expertise", adminOnly: false, usage: ".get expertise", description: "Display your current Expertise progress.")]
        public static void GetExpertiseCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            Equipment equipment = character.Read<Equipment>();
            PrefabGUID weaponGuid = equipment.WeaponSlot.SlotEntity._Entity.Read<PrefabGUID>();
            ExpertiseSystem.WeaponType weaponType = ExpertiseSystem.GetWeaponTypeFromPrefab(weaponGuid);

            IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
            if (handler == null)
            {
                ctx.Reply($"No expertise handler found for {weaponType}.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            var ExpertiseData = handler.GetExpertiseData(steamID);

            // ExpertiseData.Key represents the level, and ExpertiseData.Value represents the experience.
            if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
            {
                ctx.Reply($"Your expertise is <color=yellow>{ExpertiseData.Key}</color> (<color=white>{ExpertiseSystem.GetLevelProgress(steamID, handler)}%</color>) with {weaponType}.");
            }
            else
            {
                ctx.Reply($"You haven't gained any expertise for {weaponType} yet.");
            }
        }

        [Command(name: "logExpertiseProgress", shortHand: "log expertise", adminOnly: false, usage: ".log expertise", description: "Toggles Expertise progress logging.")]
        public static void LogExpertiseCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            var SteamID = ctx.Event.User.PlatformId;

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
            {
                bools["ExpertiseLogging"] = !bools["ExpertiseLogging"];
            }
            ctx.Reply($"Weapon expertise logging is now {(bools["ExpertiseLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "chooseWeaponStat", shortHand: "cws", adminOnly: false, usage: ".cws [Stat]", description: "Choose a weapon stat to enhance based on your weapon Expertise.")]
        public static void ChooseWeaponStat(ChatCommandContext ctx, string statType)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            if (!Enum.TryParse<WeaponStatManager.WeaponStatType>(statType, true, out var weaponStatType))
            {
                ctx.Reply("Invalid weapon stat choice, use .lws to see options.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);

            if (weaponType == ExpertiseSystem.WeaponType.Unarmed)
            {
                ctx.Reply("You cannot choose weapon stats for unarmed (sanguimancy). It bestows other powers...");
                return;
            }

            // Ensure that there is a dictionary for the player's stats
            if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponsStats))
            {
                weaponsStats = [];
                Core.DataStructures.PlayerWeaponStats[steamID] = weaponsStats;
            }

            // Choose a stat for the specific weapon stats instance
            if (PlayerWeaponUtilities.ChooseStat(steamID, weaponType, weaponStatType))
            {
                ctx.Reply($"{statType} has been chosen for {weaponType} and will apply after reequiping.");
                //ModifyUnitStatBuffUtils.ApplyWeaponBonuses(character, weaponType, weaponEntity);
                Core.DataStructures.SavePlayerWeaponStats();
            }
            else
            {
                ctx.Reply("You have already chosen two stats for this weapon.");
            }
        }

        [Command(name: "resetWeaponStats", shortHand: "rws", adminOnly: false, usage: ".rws", description: "Reset the stat Stats for a player's currently equipped weapon stats.")]
        public static void ResetWeaponStats(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);

            if (weaponType == ExpertiseSystem.WeaponType.Unarmed)
            {
                ctx.Reply("You cannot reset weapon stats for unarmed (sanguimancy) as none can be chosen.");
                return;
            }

            //ModifyUnitStatBuffUtils.ResetWeaponModifications(weapon);
            PlayerWeaponUtilities.ResetStats(steamID, weaponType);
            //Core.DataStructures.SavePlayerWeaponStats();
            ctx.Reply("Your weapon stats have been reset for the currently equipped weapon.");
        }

        [Command(name: "setWeaponExpertise", shortHand: "swe", adminOnly: true, usage: ".swe [Weapon] [Level]", description: "Sets your weapon expertise level.")]
        public static void SetExpertiseCommand(ChatCommandContext ctx, string weapon, int level)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            if (level < 0 || level > ExpertiseSystem.MaxExpertiseLevel)
            {
                ctx.Reply($"Level must be between 0 and {ExpertiseSystem.MaxExpertiseLevel}.");
                return;
            }
            if (!Enum.TryParse<ExpertiseSystem.WeaponType>(weapon, true, out var weaponType))
            {
                ctx.Reply("Invalid weapon type.");
            }
            var expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
            if (expertiseHandler == null)
            {
                ctx.Reply("Invalid weapon type.");
                return;
            }

            ulong steamId = ctx.Event.User.PlatformId;
            //var xpData = ExpertiseHandler.GetExperienceData(steamId);
            Entity character = ctx.Event.SenderCharacterEntity;
            Equipment equipment = character.Read<Equipment>();
            // Update Expertise level and XP
            var xpData = new KeyValuePair<int, float>(level, ExpertiseSystem.ConvertLevelToXp(level));
            expertiseHandler.UpdateExpertiseData(steamId, xpData);
            expertiseHandler.SaveChanges();
            GearOverride.SetWeaponItemLevel(equipment, level, Core.Server.EntityManager);

            ctx.Reply($"Expertise for {expertiseHandler.GetWeaponType()} set to {level}.");
        }

        [Command(name: "listWeaponStats", shortHand: "lws", adminOnly: false, usage: ".lws", description: "Lists weapon stat Stats.")]
        public static void ListWeaponStatsCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            string weaponStats = string.Join(", ", Enum.GetNames(typeof(WeaponStatManager.WeaponStatType)));
            ctx.Reply($"Available weapon stats: {weaponStats}");
        }

        [Command(name: "lockSpell", shortHand: "lock", adminOnly: false, usage: ".lock", description: "Locks in the next spells equipped to use in your unarmed slots.")]
        public static void LockPlayerSpells(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            if (!Plugin.Sanguimancy.Value)
            {
                ctx.Reply("Sanguimancy is not enabled.");
                return;
            }
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
                ctx.Reply("You must be at least level 25 in Sanguimancy to lock spells.");
            }
        }
    }
}