using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Legacy;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Systems.Legacies.BloodStats;

namespace Bloodcraft.Commands;
internal static class BloodCommands
{
    [Command(name: "getBloodLegacyProgress", shortHand: "gbl", adminOnly: false, usage: ".gbl [BloodType]", description: "Display your current blood legacy progress.")]
    public static void GetLegacyCommand(ChatCommandContext ctx, string blood = "")
    {
        if (!Plugin.BloodSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        Blood playerBlood = character.Read<Blood>();
        BloodSystem.BloodType bloodType;

        if (string.IsNullOrEmpty(blood))
        {
            bloodType = BloodSystem.GetBloodTypeFromPrefab(playerBlood.BloodType);
        }
        else if (!Enum.TryParse<BloodSystem.BloodType>(blood, true, out bloodType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood type, use .lbl to see options.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;

        IBloodHandler bloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);

        if (bloodHandler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid blood type.");
            return;
        }

        var data = bloodHandler.GetLegacyData(steamID);
        int progress = (int)(data.Value - BloodSystem.ConvertLevelToXp(data.Key));

        if (data.Key > 0)
        {
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{data.Key}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>essence</color> (<color=white>{BloodSystem.GetLevelProgress(steamID, bloodHandler)}%</color>) in <color=red>{bloodHandler.GetBloodType()}</color>");

            if (Core.DataStructures.PlayerBloodStats.TryGetValue(steamID, out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
            {
                List<KeyValuePair<BloodStatManager.BloodStatType, string>> bonusBloodStats = [];
                foreach (var stat in stats)
                {
                    float bonus = ModifyUnitStatBuffUtils.CalculateScaledBloodBonus(bloodHandler, steamID, bloodType, stat);
                    string bonusString = (bonus * 100).ToString("F0") + "%";
                    bonusBloodStats.Add(new KeyValuePair<BloodStatManager.BloodStatType, string>(stat, bonusString));
                }
                for (int i = 0; i < bonusBloodStats.Count; i += 6)
                {
                    var batch = bonusBloodStats.Skip(i).Take(6);
                    string bonuses = string.Join(", ", batch.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
                    LocalizationService.HandleReply(ctx, $"Current blood stat bonuses: {bonuses}");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, "No bonuses from legacy.");
            }
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"No progress in <color=red>{bloodHandler.GetBloodType()}</color> yet.");
        }
    }

    [Command(name: "logBloodLegacyProgress", shortHand: "log bl", adminOnly: false, usage: ".log bl", description: "Toggles Legacy progress logging.")]
    public static void LogLegacyCommand(ChatCommandContext ctx)
    {
        if (!Plugin.BloodSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (Core.DataStructures.PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["BloodLogging"] = !bools["BloodLogging"];
        }
        Core.DataStructures.SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Blood Legacy logging {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "chooseBloodStat", shortHand: "cbs", adminOnly: false, usage: ".cbs [Blood] [BloodStat]", description: "Choose a blood stat to enhance based on your legacy.")]
    public static void ChooseBloodStat(ChatCommandContext ctx, string bloodType, string statType)
    {
        if (!Plugin.BloodSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Legacies are not enabled.");
            return;
        }

        if (!Enum.TryParse<BloodStats.BloodStatManager.BloodStatType>(statType, true, out var StatType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood stat choice, use .lbs to see options.");
            return;
        }

        if (!Enum.TryParse<BloodSystem.BloodType>(bloodType, true, out var BloodType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood type.");
            return;
        }

        ulong steamID = ctx.Event.User.PlatformId;
        if (BloodType.Equals(BloodSystem.BloodType.GateBoss) || BloodType.Equals(BloodSystem.BloodType.None) || BloodType.Equals(BloodSystem.BloodType.VBlood))
        {
            LocalizationService.HandleReply(ctx, $"No legacy available for <color=white>{BloodType}</color>.");
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
            LocalizationService.HandleReply(ctx, $"<color=#00FFFF>{StatType}</color> has been chosen for <color=red>{BloodType}</color> and will apply after reacquiring.");
            Core.DataStructures.SavePlayerBloodStats();
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have already chosen {Plugin.LegacyStatChoices.Value} stats for this legacy, the stat has already been chosen for this legacy, or the stat is not allowed for your class.");
        }
    }

    [Command(name: "resetBloodStats", shortHand: "rbs", adminOnly: false, usage: ".rbs", description: "Reset stats for current blood.")]
    public static void ResetBloodStats(ChatCommandContext ctx)
    {
        if (!Plugin.BloodSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Legacies are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamID = ctx.Event.User.PlatformId;
        BloodSystem.BloodType bloodType = ModifyUnitStatBuffUtils.GetCurrentBloodType(character);

        if (bloodType.Equals(BloodSystem.BloodType.GateBoss) || bloodType.Equals(BloodSystem.BloodType.None) || bloodType.Equals(BloodSystem.BloodType.VBlood))
        {
            LocalizationService.HandleReply(ctx, $"No legacy available for <color=white>{bloodType}</color>.");
            return;
        }

        if (!Plugin.ResetLegacyItem.Value.Equals(0))
        {
            PrefabGUID item = new(Plugin.ResetLegacyItem.Value);
            int quantity = Plugin.ResetLegacyItemQuantity.Value;
            if (InventoryUtilities.TryGetInventoryEntity(Core.EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && Core.ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
            {
                if (Core.ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                {
                    PlayerBloodUtilities.ResetStats(steamID, bloodType);
                    LocalizationService.HandleReply(ctx, $"Your blood stats have been reset for <color=red>{bloodType}</color>.");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You do not have the required item to reset your blood stats ({item.GetPrefabName()}x{quantity})");
                return;
            }
        }

        PlayerBloodUtilities.ResetStats(steamID, bloodType);
        LocalizationService.HandleReply(ctx, $"Your blood stats have been reset for <color=red>{bloodType}</color>.");
    }

    [Command(name: "listBloodStats", shortHand: "lbs", adminOnly: false, usage: ".lbs", description: "Lists blood stats available.")]
    public static void ListBloodStatsAvailable(ChatCommandContext ctx)
    {
        if (!Plugin.BloodSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Legacies are not enabled.");
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

        LocalizationService.HandleReply(ctx, $"Available blood stats (1/2): {bloodStatsLine1}");
        LocalizationService.HandleReply(ctx, $"Available blood stats (2/2): {bloodStatsLine2}");
    }

    [Command(name: "setBloodLegacy", shortHand: "sbl", adminOnly: true, usage: ".sbl [Player] [Blood] [Level]", description: "Sets player Blood Legacy level.")]
    public static void SetBloodLegacyCommand(ChatCommandContext ctx, string name, string blood, int level)
    {
        if (!Plugin.BloodSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }
        Entity foundUserEntity = PlayerService.GetUserByName(name, true);
        if (foundUserEntity.Equals(Entity.Null))
        {
            LocalizationService.HandleReply(ctx, "Player not found.");
            return;
        }
        User foundUser = foundUserEntity.Read<User>();
        if (level < 0 || level > Plugin.MaxBloodLevel.Value)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {Plugin.MaxBloodLevel.Value}.");
            return;
        }
        if (!Enum.TryParse<BloodSystem.BloodType>(blood, true, out var bloodType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood legacy.");
            return;
        }
        var BloodHandler = BloodHandlerFactory.GetBloodHandler(bloodType);
        if (BloodHandler == null)
        {
            LocalizationService.HandleReply(ctx, "Invalid blood legacy.");
            return;
        }
        ulong steamId = foundUser.PlatformId;
        var xpData = new KeyValuePair<int, float>(level, BloodSystem.ConvertLevelToXp(level));
        BloodHandler.UpdateLegacyData(steamId, xpData);
        BloodHandler.SaveChanges();
        LocalizationService.HandleReply(ctx, $"<color=red>{BloodHandler.GetBloodType()}</color> legacy set to <color=white>{level}</color> for <color=green>{foundUser.CharacterName}</color>");
    }

    [Command(name: "listBloodLegacies", shortHand: "lbl", adminOnly: false, usage: ".lbl", description: "Lists blood legacies available.")]
    public static void ListBloodTypesCommand(ChatCommandContext ctx)
    {
        if (!Plugin.BloodSystem.Value)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }
        var excludedBloodTypes = new HashSet<BloodSystem.BloodType> { BloodSystem.BloodType.None, BloodSystem.BloodType.VBlood, BloodSystem.BloodType.GateBoss };
        var bloodTypes = Enum.GetValues(typeof(BloodSystem.BloodType))
                              .Cast<BloodSystem.BloodType>()
                              .Where(b => !excludedBloodTypes.Contains(b))
                              .Select(b => b.ToString());

        string bloodTypesList = string.Join(", ", bloodTypes);
        LocalizationService.HandleReply(ctx, $"Available Blood Legacies: <color=red>{bloodTypesList}</color>");
    }
}