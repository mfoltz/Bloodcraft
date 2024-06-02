using Bloodcraft.Patches;
using Bloodcraft.Systems.Expertise;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Expertise.WeaponStats;

namespace Bloodcraft.Commands
{
    public static class WeaponCommands
    {
        [Command(name: "getExpertiseProgress", shortHand: "get e", adminOnly: false, usage: ".get e", description: "Displays your current expertise.")] 
        public static void GetExpertiseCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }
            Entity character = ctx.Event.SenderCharacterEntity;


            ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);
            if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed) && !Plugin.Sanguimancy.Value)
            {
                ctx.Reply("Sanguimancy is not enabled.");
                return;
            }
            IExpertiseHandler handler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);
            if (handler == null)
            {
                ctx.Reply($"No expertise handler found for {weaponType}.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            var ExpertiseData = handler.GetExpertiseData(steamID);
            int progress = (int)(ExpertiseData.Value - ExpertiseSystem.ConvertLevelToXp(ExpertiseData.Key));
            // ExpertiseData.Key represents the level, and ExpertiseData.Value represents the experience.
            if (ExpertiseData.Key > 0 || ExpertiseData.Value > 0)
            {
                ctx.Reply($"Your weapon expertise is [<color=white>{ExpertiseData.Key}</color>] and you have <color=yellow>{progress}</color> experience (<color=white>{ExpertiseSystem.GetLevelProgress(steamID, handler)}%</color>) with <color=#c0c0c0>{weaponType}</color>");
                if (Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponStats) && weaponStats.TryGetValue(weaponType, out var stats))
                {
                    List<KeyValuePair<WeaponStatManager.WeaponStatType, string>> bonusWeaponStats = [];
                    foreach(var stat in stats)
                    {
                        float bonus = ModifyUnitStatBuffUtils.CalculateScaledWeaponBonus(handler, steamID, weaponType, stat);
                        if (bonus > 1)
                        {
                            int intBonus = (int)bonus;
                            string bonusString = intBonus.ToString();
                            bonusWeaponStats.Add(new KeyValuePair<WeaponStatManager.WeaponStatType, string>(stat, bonusString));
                        }
                        else
                        {
                            string bonusString = (bonus * 100).ToString("F0") + "%";
                            bonusWeaponStats.Add(new KeyValuePair<WeaponStatManager.WeaponStatType, string>(stat, bonusString));
                        }
                    }
                    //string bonuses = string.Join(", ", bonusWeaponStats.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
                    //ctx.Reply($"Current weapon stat bonuses: {bonuses}");
                    for (int i = 0; i < bonusWeaponStats.Count; i += 6)
                    {
                        var batch = bonusWeaponStats.Skip(i).Take(6);
                        string bonuses = string.Join(", ", batch.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
                        ctx.Reply($"Current weapon stat bonuses: {bonuses}");
                    }
                }
                else
                {
                    ctx.Reply("No bonuses from currently equipped weapon.");
                }
            }
            else
            {
                ctx.Reply($"You haven't gained any expertise for <color=#c0c0c0>{weaponType}</color> yet.");
            }
        }

        [Command(name: "logExpertiseProgress", shortHand: "log e", adminOnly: false, usage: ".log e", description: "Toggles expertise logging.")]
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
            Core.DataStructures.SavePlayerBools();
            ctx.Reply($"Expertise logging is now {(bools["ExpertiseLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
        }

        [Command(name: "chooseWeaponStat", shortHand: "cws", adminOnly: false, usage: ".cws [Weapon] [WeaponStat]", description: "Choose a weapon stat to enhance based on your expertise.")]
        public static void ChooseWeaponStat(ChatCommandContext ctx, string weaponType, string statType)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }

            if (!Enum.TryParse<WeaponStatManager.WeaponStatType>(statType, true, out var StatType))
            {
                ctx.Reply("Invalid stat choice, use .lws to see options.");
                return;
            }

            if (!Enum.TryParse<ExpertiseSystem.WeaponType>(weaponType, true, out var WeaponType))
            {
                ctx.Reply("Invalid weapon choice.");
                return;
            }

            ulong steamID = ctx.Event.User.PlatformId;
            //ExpertiseSystem.WeaponType weaponType = ModifyUnitStatBuffUtils.GetCurrentWeaponType(character);
            if (WeaponType.Equals(ExpertiseSystem.WeaponType.Unarmed) && !Plugin.Sanguimancy.Value)
            {
                ctx.Reply("Sanguimancy is not enabled.");
                return;
            }
            if (WeaponType.Equals(ExpertiseSystem.WeaponType.FishingPole))
            {
               ctx.Reply("You cannot choose weapon stats for the fishing pole.");
                return;
            }

            // Ensure that there is a dictionary for the player's stats
            if (!Core.DataStructures.PlayerWeaponStats.TryGetValue(steamID, out var weaponsStats))
            {
                weaponsStats = [];
                Core.DataStructures.PlayerWeaponStats[steamID] = weaponsStats;
            }

            // Choose a stat for the specific weapon stats instance
            if (PlayerWeaponUtilities.ChooseStat(steamID, WeaponType, StatType))
            {
                ctx.Reply($"<color=#00FFFF>{StatType}</color> has been chosen for <color=#c0c0c0>{WeaponType}</color> and will apply after reequiping.");
                Core.DataStructures.SavePlayerWeaponStats();
            }
            else
            {
                ctx.Reply($"You have already chosen {Plugin.ExpertiseStatChoices.Value} stats for this expertise, the stat has already been chosen for this expertise, or the stat is not allowed for your class.");
            }
        }

        [Command(name: "resetWeaponStats", shortHand: "rws", adminOnly: false, usage: ".rws", description: "Reset the stats for current weapon.")]
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

            if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed) && !Plugin.Sanguimancy.Value)
            {
                ctx.Reply("Sanguimancy is not enabled.");
                return;
            }
            if (weaponType.Equals(ExpertiseSystem.WeaponType.FishingPole))
            {
                ctx.Reply("You cannot reset weapon stats for fishing pole as none can be chosen.");
                return;
            }

            if (!Plugin.ResetExpertiseItem.Value.Equals(0))
            {
                PrefabGUID item = new(Plugin.ResetExpertiseItem.Value);
                int quantity = Plugin.ResetExpertiseItemQuantity.Value;
                // Check if the player has the item to reset stats
                if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && Core.ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
                {
                    if (Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                    {
                        PlayerWeaponUtilities.ResetStats(steamID, weaponType);
                        ctx.Reply($"Your weapon stats have been reset for <color=#00FFFF>{weaponType}</color>");
                        return;
                    }
                }
                else
                {
                    ctx.Reply($"You do not have the required item to reset your weapon stats ({item.LookupName()}x{quantity})");
                    return;
                }
            }

            PlayerWeaponUtilities.ResetStats(steamID, weaponType);
            ctx.Reply($"Your weapon stats have been reset for <color=#c0c0c0>{weaponType}</color>");
        }

        [Command(name: "setWeaponExpertise", shortHand: "swe", adminOnly: true, usage: ".swe [Name] [Weapon] [Level]", description: "Sets player weapon expertise level.")]
        public static void SetExpertiseCommand(ChatCommandContext ctx, string name, string weapon, int level)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }

            Entity foundUserEntity = Core.FindUserOnline(name);
            if (foundUserEntity.Equals(Entity.Null))
            {
                ctx.Reply("Player not found.");
                return;
            }
            User foundUser = foundUserEntity.Read<User>();

            if (level < 0 || level > Plugin.MaxExpertiseLevel.Value)
            {
                ctx.Reply($"Level must be between 0 and {Plugin.MaxExpertiseLevel.Value}.");
                return;
            }

            if (!Enum.TryParse<ExpertiseSystem.WeaponType>(weapon, true, out var weaponType))
            {
                ctx.Reply("Invalid weapon type.");
                return;
            }
            if (weaponType.Equals(ExpertiseSystem.WeaponType.Unarmed) && !Plugin.Sanguimancy.Value)
            {
                ctx.Reply("Sanguimancy is not enabled.");
                return;
            }
            var expertiseHandler = ExpertiseHandlerFactory.GetExpertiseHandler(weaponType);

            if (expertiseHandler == null)
            {
                ctx.Reply("Invalid weapon type.");
                return;
            }

            ulong steamId = foundUser.PlatformId;
            //Entity character = Core.ServerBootstrapSystem._ApprovedUsersLookup[foundUser.Index].UserEntity.Read<User>().LocalCharacter._Entity;
            Equipment equipment = foundUser.LocalCharacter._Entity.Read<Equipment>();

            var xpData = new KeyValuePair<int, float>(level, ExpertiseSystem.ConvertLevelToXp(level));
            expertiseHandler.UpdateExpertiseData(steamId, xpData);
            expertiseHandler.SaveChanges();
            if (Plugin.LevelingSystem.Value) GearOverride.SetWeaponItemLevel(equipment, level, Core.EntityManager);

            ctx.Reply($"<color=#c0c0c0>{expertiseHandler.GetWeaponType()}</color> expertise set to [<color=white>{level}</color>] for <color=green>{foundUser.CharacterName}</color>");
        }

        [Command(name: "listWeaponStats", shortHand: "lws", adminOnly: false, usage: ".lws", description: "Lists weapon stats available.")]
        public static void ListWeaponStatsCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Expertise is not enabled.");
                return;
            }

            var weaponStatsWithCaps = Enum.GetValues(typeof(WeaponStatManager.WeaponStatType))
                .Cast<WeaponStatManager.WeaponStatType>()
                .Select(stat =>
                    $"<color=#00FFFF>{stat}</color>: <color=white>{WeaponStatManager.BaseCaps[stat]}</color>")
                .ToArray();

            int halfLength = weaponStatsWithCaps.Length / 2;

            string weaponStatsLine1 = string.Join(", ", weaponStatsWithCaps.Take(halfLength));
            string weaponStatsLine2 = string.Join(", ", weaponStatsWithCaps.Skip(halfLength));

            ctx.Reply($"Available weapon stats (1/2): {weaponStatsLine1}");
            ctx.Reply($"Available weapon stats (2/2): {weaponStatsLine2}");
        }

        [Command(name: "listWeaponExpertises", shortHand: "lwe", adminOnly: false, usage: ".lwe", description: "Lists weapon expertises available.")]
        public static void ListWeaponsCommand(ChatCommandContext ctx)
        {
            if (!Plugin.ExpertiseSystem.Value)
            {
                ctx.Reply("Weapon Expertise is not enabled.");
                return;
            }
            string weaponTypes = string.Join(", ", Enum.GetNames(typeof(ExpertiseSystem.WeaponType)));
            ctx.Reply($"Available Weapon Expertises: <color=#c0c0c0>{weaponTypes}</color>");
        }

        [Command(name: "lockSpells", shortHand: "lock", adminOnly: false, usage: ".lock", description: "Locks in the next spells equipped to use in your unarmed slots.")]
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

            if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools) && Core.DataStructures.PlayerSanguimancy.TryGetValue(SteamID, out var data) && data.Key >= Plugin.FirstSlot.Value)
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
                Core.DataStructures.SavePlayerBools();
            }
            else
            {
                ctx.Reply($"You must be at least level {Plugin.FirstSlot.Value} in Sanguimancy to use this. Both slots are unlocked at {Plugin.SecondSlot.Value}");
            }
        }
    }
}