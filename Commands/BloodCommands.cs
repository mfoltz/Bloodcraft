using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.SystemUtilities.Legacies;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Core.DataStructures;
using static Bloodcraft.SystemUtilities.Legacies.BloodHandler;

namespace Bloodcraft.Commands;

[CommandGroup("bloodlegacy", "bl")]
internal static class BloodCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static ConfigService ConfigService => Core.ConfigService;
    static LocalizationService LocalizationService => Core.LocalizationService;
    static PlayerService PlayerService => Core.PlayerService;

    [Command(name: "get", adminOnly: false, usage: ".bl get [BloodType]", description: "Display your current blood legacy progress.")]
    public static void GetLegacyCommand(ChatCommandContext ctx, string blood = "")
    {
        if (!ConfigService.BloodSystem)
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

        int prestigeLevel = PlayerPrestiges.TryGetValue(steamID, out var prestiges) ? prestiges[BloodSystem.BloodPrestigeMap[bloodType]] : 0;

        if (data.Key > 0)
        {
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{data.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>essence</color> (<color=white>{BloodSystem.GetLevelProgress(steamID, bloodHandler)}%</color>) in <color=red>{bloodHandler.GetBloodType()}</color>");

            if (PlayerBloodStats.TryGetValue(steamID, out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
            {
                List<KeyValuePair<BloodStats.BloodStatType, string>> bonusBloodStats = [];
                foreach (var stat in stats)
                {
                    float bonus = CalculateScaledBloodBonus(bloodHandler, steamID, bloodType, stat);
                    string bonusString = (bonus * 100).ToString("F0") + "%";
                    bonusBloodStats.Add(new KeyValuePair<BloodStats.BloodStatType, string>(stat, bonusString));
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

    [Command(name: "log", adminOnly: false, usage: ".bl log", description: "Toggles Legacy progress logging.")]
    public static void LogLegacyCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }
        var SteamID = ctx.Event.User.PlatformId;

        if (PlayerBools.TryGetValue(SteamID, out var bools))
        {
            bools["BloodLogging"] = !bools["BloodLogging"];
        }
        SavePlayerBools();
        LocalizationService.HandleReply(ctx, $"Blood Legacy logging {(bools["BloodLogging"] ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "choosestat", shortHand: "cst", adminOnly: false, usage: ".bl cst [Blood] [BloodStat]", description: "Choose a blood stat to enhance based on your legacy.")]
    public static void ChooseBloodStat(ChatCommandContext ctx, string bloodType, string statType)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Legacies are not enabled.");
            return;
        }

        if (!Enum.TryParse<BloodHandler.BloodStats.BloodStatType>(statType, true, out var StatType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood stat choice, use .bl lst to see options.");
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
        if (!PlayerBloodStats.TryGetValue(steamID, out var bloodStats))
        {
            bloodStats = [];
            PlayerBloodStats[steamID] = bloodStats;
        }

        // Choose a stat for the specific weapon stats instance
        if (ChooseStat(steamID, BloodType, StatType))
        {
            LocalizationService.HandleReply(ctx, $"<color=#00FFFF>{StatType}</color> has been chosen for <color=red>{BloodType}</color> and will apply after reacquiring.");
            SavePlayerBloodStats();
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have already chosen {ConfigService.LegacyStatChoices} stats for this legacy, the stat has already been chosen for this legacy, or the stat is not allowed for your class.");
        }
    }

    [Command(name: "resetstats", shortHand: "rst", adminOnly: false, usage: ".bl rst", description: "Reset stats for current blood.")]
    public static void ResetBloodStats(ChatCommandContext ctx)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Legacies are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        ulong steamID = ctx.Event.User.PlatformId;
        BloodSystem.BloodType bloodType = GetCurrentBloodType(character);

        if (bloodType.Equals(BloodSystem.BloodType.GateBoss) || bloodType.Equals(BloodSystem.BloodType.None) || bloodType.Equals(BloodSystem.BloodType.VBlood))
        {
            LocalizationService.HandleReply(ctx, $"No legacy available for <color=white>{bloodType}</color>.");
            return;
        }

        if (!ConfigService.ResetLegacyItem.Equals(0))
        {
            PrefabGUID item = new(ConfigService.ResetLegacyItem);
            int quantity = ConfigService.ResetLegacyItemQuantity;
            if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out Entity inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
            {
                if (ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity))
                {
                    ResetStats(steamID, bloodType);
                    LocalizationService.HandleReply(ctx, $"Your blood stats have been reset for <color=red>{bloodType}</color>.");
                    return;
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You do not have the required item to reset your blood stats (<color=#ffd9eb>{item.GetPrefabName()}</color> x<color=white>{quantity}</color>)");
                return;
            }
        }

        ResetStats(steamID, bloodType);
        LocalizationService.HandleReply(ctx, $"Your blood stats have been reset for <color=red>{bloodType}</color>.");
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".bl lst", description: "Lists blood stats available.")]
    public static void ListBloodStatsAvailable(ChatCommandContext ctx)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Legacies are not enabled.");
            return;
        }

        var bloodStatsWithCaps = Enum.GetValues(typeof(BloodStats.BloodStatType))
            .Cast<BloodStats.BloodStatType>()
            .Select(stat =>
                $"<color=#00FFFF>{stat}</color>: <color=white>{BloodStats.BloodStatValues[stat]}</color>")
            .ToArray();

        int halfLength = bloodStatsWithCaps.Length / 2;

        string bloodStatsLine1 = string.Join(", ", bloodStatsWithCaps.Take(halfLength));
        string bloodStatsLine2 = string.Join(", ", bloodStatsWithCaps.Skip(halfLength));

        LocalizationService.HandleReply(ctx, $"Available blood stats (1/2): {bloodStatsLine1}");
        LocalizationService.HandleReply(ctx, $"Available blood stats (2/2): {bloodStatsLine2}");
    }

    [Command(name: "set", adminOnly: true, usage: ".bl set [Player] [Blood] [Level]", description: "Sets player Blood Legacy level.")]
    public static void SetBloodLegacyCommand(ChatCommandContext ctx, string name, string blood, int level)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        Entity foundUserEntity = PlayerService.UserCache.FirstOrDefault(kvp => kvp.Key.ToLower() == name.ToLower()).Value;
        if (!EntityManager.Exists(foundUserEntity))
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = foundUserEntity.Read<User>();
        if (level < 0 || level > ConfigService.MaxBloodLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {ConfigService.MaxBloodLevel}.");
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

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".bl l", description: "Lists blood legacies available.")]
    public static void ListBloodTypesCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.BloodSystem)
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