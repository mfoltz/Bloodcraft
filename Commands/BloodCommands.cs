using Bloodcraft.Patches;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Legacy;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Legacies.BloodStats;

namespace Bloodcraft.Commands
{
    public static class BloodCommands
    {
        [Command(name: "getBloodLegacyProgress", shortHand: "gbl", adminOnly: false, usage: ".gbl [BloodType]", description: "Display your current blood legacy progress.")]
        public static void GetLegacyCommand(ChatCommandContext ctx, string blood)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Blood Legacies are not enabled.");
                return;
            }

            if (!Enum.TryParse<BloodSystem.BloodType>(blood, true, out var bloodType))
            {
                ctx.Reply("Invalid blood type, use .lbl to see options.");
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
                ctx.Reply($"You're level [<color=white>{data.Key}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>essence</color> (<color=white>{BloodSystem.GetLevelProgress(steamID, bloodHandler)}%</color>) in <color=red>{bloodHandler.GetBloodType()}</color>");
                if (Core.DataStructures.PlayerBloodStats.TryGetValue(steamID, out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
                {
                    List<KeyValuePair<BloodStatManager.BloodStatType, float>> bonusBloodStats = [];
                    foreach (var stat in stats)
                    {
                        float bonus = ModifyUnitStatBuffUtils.CalculateScaledBloodBonus(bloodHandler, steamID, stat);
                        bonusBloodStats.Add(new KeyValuePair<BloodStatManager.BloodStatType, float>(stat, bonus));
                    }
                    string bonuses = string.Join(", ", bonusBloodStats.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
                    ctx.Reply($"Current blood stat bonuses: {bonuses}");
                }
                else
                {
                    ctx.Reply("No bonuses from current legacy.");
                }
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

        [Command(name: "chooseBloodStat", shortHand: "cbs", adminOnly: false, usage: ".cbs [Blood] [BloodStat]", description: "Choose a blood stat to enhance based on your legacy.")]
        public static void ChooseBloodStat(ChatCommandContext ctx, string bloodType, string statType)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Legacies are not enabled.");
                return;
            }

            if (!Enum.TryParse<BloodStats.BloodStatManager.BloodStatType>(statType, true, out var StatType))
            {
                ctx.Reply("Invalid blood stat choice, use .lbs to see options.");
                return;
            }

            if (!Enum.TryParse<BloodSystem.BloodType>(bloodType, true, out var BloodType))
            {
                ctx.Reply("Invalid blood choice.");
                return;
            }

            //Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            //BloodSystem.BloodType bloodType = ModifyUnitStatBuffUtils.GetCurrentBloodType(character);
            if (BloodType.Equals(BloodSystem.BloodType.GateBoss) || BloodType.Equals(BloodSystem.BloodType.None) || BloodType.Equals(BloodSystem.BloodType.VBlood))
            {
                ctx.Reply($"No legacy available for <color=white>{BloodType}</color>.");
                return;
            }

            // Ensure that there is a dictionary for the player's stats
            if (!Core.DataStructures.PlayerBloodStats.TryGetValue(steamID, out var bloodStats))
            {
                bloodStats = [];
                Core.DataStructures.PlayerBloodStats[steamID] = bloodStats;
            }

            // Choose a stat for the specific weapon stats instance
            if (PlayerBloodUtilities.ChooseStat(steamID, BloodType, StatType))
            {
                ctx.Reply($"<color=#00FFFF>{StatType}</color> has been chosen for <color=red>{bloodType}</color> and will apply after reacquiring.");
                Core.DataStructures.SavePlayerBloodStats();
            }
            else
            {
                ctx.Reply($"You have already chosen {Plugin.LegacyStatChoices.Value} stats for this legacy.");
            }
        }
        [Command(name: "resetBloodStats", shortHand: "rbs", adminOnly: false, usage: ".rbs", description: "Reset stats for current blood.")]
        public static void ResetWeaponStats(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Legacies are not enabled.");
                return;
            }

            Entity character = ctx.Event.SenderCharacterEntity;
            ulong steamID = ctx.Event.User.PlatformId;
            BloodSystem.BloodType bloodType = ModifyUnitStatBuffUtils.GetCurrentBloodType(character);

            if (bloodType.Equals(BloodSystem.BloodType.GateBoss) || bloodType.Equals(BloodSystem.BloodType.None) || bloodType.Equals(BloodSystem.BloodType.VBlood))
            {
                ctx.Reply($"No legacy available for <color=white>{bloodType}</color>.");
                return;
            }

            if (!Plugin.ResetLegacyItem.Value.Equals(0))
            {
                PrefabGUID item = new(Plugin.ResetLegacyItem.Value);
                int quantity = Plugin.ResetLegacyItemQuantity.Value;
                // Check if the player has the item to reset stats
                if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && Core.ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
                {
                    if (Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                    {
                        PlayerBloodUtilities.ResetStats(steamID, bloodType);
                        ctx.Reply($"Your blood stats have been reset for <color=red>{bloodType}</color>.");
                        return;
                    }
                }
                else
                {
                    ctx.Reply($"You do not have the required item to reset your blood stats ({item.LookupName()}x{quantity})");
                    return;
                }
            }

            PlayerBloodUtilities.ResetStats(steamID, bloodType);
            ctx.Reply($"Your blood stats have been reset for <color=red>{bloodType}</color>.");
        }
        [Command(name: "listBloodStats", shortHand: "lbs", adminOnly: false, usage: ".lbs", description: "Lists blood stats available.")]
        public static void ListBloodStatsAvailable(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Legacies are not enabled.");
                return;
            }

            var bloodStatsWithCaps = Enum.GetValues(typeof(BloodStatManager.BloodStatType))
                .Cast<BloodStatManager.BloodStatType>()
                .Select(stat =>
                    $"<color=#00FFFF>{stat}</color>: <color=white>{BloodStatManager.BaseCaps[stat]}</color>")
                .ToArray();

            int halfLength = bloodStatsWithCaps.Length / 2;

            string bloodStatsLine1 = string.Join(", ", bloodStatsWithCaps.Take(halfLength));
            string bloodStatsLine2 = string.Join(", ", bloodStatsWithCaps.Skip(halfLength));

            ctx.Reply($"Available blood stats (1/2): {bloodStatsLine1}");
            ctx.Reply($"Available blood stats (2/2): {bloodStatsLine1}");
        }
        [Command(name: "setBloodLegacy", shortHand: "sbl", adminOnly: true, usage: ".sbl [Player] [Blood] [Level]", description: "Sets player Blood Legacy level.")]
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
                ctx.Reply("Invalid blood legacy.");
            }
            var BloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
            if (BloodHandler == null)
            {
                ctx.Reply("Invalid blood legacy.");
                return;
            }
            ulong steamId = foundUser.PlatformId;
            var xpData = new KeyValuePair<int, float>(level, BloodSystem.ConvertLevelToXp(level));
            BloodHandler.UpdateLegacyData(steamId, xpData);
            BloodHandler.SaveChanges();

            ctx.Reply($"<color=red>{BloodHandler.GetBloodType()}</color> legacy set to <color=white>{level}</color> for {foundUser.CharacterName}");
        }

        [Command(name: "listBloodLegacies", shortHand: "lbl", adminOnly: false, usage: ".lbl", description: "Lists blood legacies available.")]
        public static void ListBloodTypesCommand(ChatCommandContext ctx)
        {
            if (!Plugin.BloodSystem.Value)
            {
                ctx.Reply("Blood Legacies are not enabled.");
                return;
            }
            string bloodTypes = string.Join(", ", Enum.GetNames(typeof(BloodSystem.BloodType)));
            ctx.Reply($"Available Blood Legacies: <color=red>{bloodTypes}</color>");
        }
    }
}