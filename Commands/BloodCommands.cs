using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Legacies.BloodManager;
using static Bloodcraft.Utilities.Misc.PlayerBoolsManager;
using static Bloodcraft.Utilities.Progression;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;

namespace Bloodcraft.Commands;

[CommandGroup(name: "bloodlegacy", "bl")]
internal static class BloodCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    [Command(name: "getlegacy", adminOnly: false, usage: ".bl get [BloodType]", description: "Display current blood legacy details.")]
    public static void GetLegacyCommand(ChatCommandContext ctx, string blood = "")
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        Entity character = ctx.Event.SenderCharacterEntity;
        Blood playerBlood = character.Read<Blood>();
        BloodType bloodType = GetCurrentBloodType(character);

        if (string.IsNullOrEmpty(blood))
        {
            bloodType = BloodSystem.GetBloodTypeFromPrefab(playerBlood.BloodType);
        }
        else if (!Enum.TryParse<BloodType>(blood, true, out bloodType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood type, use '.bl l' to see options.");
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
        int progress = (int)(data.Value - ConvertLevelToXp(data.Key));

        int prestigeLevel = steamID.TryGetPlayerPrestiges(out var prestiges) ? prestiges[BloodSystem.BloodTypeToPrestigeMap[bloodType]] : 0;

        if (data.Key > 0)
        {
            LocalizationService.HandleReply(ctx, $"You're level [<color=white>{data.Key}</color>][<color=#90EE90>{prestigeLevel}</color>] and have <color=yellow>{progress}</color> <color=#FFC0CB>essence</color> (<color=white>{BloodSystem.GetLevelProgress(steamID, bloodHandler)}%</color>) in <color=red>{bloodHandler.GetBloodType()}</color>");

            if (steamID.TryGetPlayerBloodStats(out var bloodStats) && bloodStats.TryGetValue(bloodType, out var stats))
            {
                List<KeyValuePair<BloodStats.BloodStatType, string>> bonusBloodStats = [];

                foreach (var stat in stats)
                {
                    float bonus = CalculateScaledBloodBonus(bloodHandler, steamID, bloodType, stat);
                    string bonusString = (bonus * 100).ToString("F1") + "%";
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

    [Command(name: "loglegacies", adminOnly: false, usage: ".bl log", description: "Toggles Legacy progress logging.")]
    public static void LogLegacyCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, "BloodLogging");
        LocalizationService.HandleReply(ctx, $"Blood Legacy logging {(GetPlayerBool(steamId, "BloodLogging") ? "<color=green>enabled</color>" : "<color=red>disabled</color>")}.");
    }

    [Command(name: "choosestat", shortHand: "cst", adminOnly: false, usage: ".bl cst [Blood] [BloodStat]", description: "Choose a bonus stat to enhance for your blood legacy.")]
    public static void ChooseBloodStat(ChatCommandContext ctx, string blood, string statType)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Legacies are not enabled.");
            return;
        }

        if (int.TryParse(statType, out int value))
        {
            int length = Enum.GetValues(typeof(BloodStats.BloodStatType)).Length;

            if (value < 1 || value > length)
            {
                LocalizationService.HandleReply(ctx, $"Invalid integer, please use the corresponding stat number shown when using '<color=white>.bl lst</color>'. (<color=white>1</color>-<color=white>{length}</color>)");
                return;
            }

            --value;
            statType = value.ToString();
        }

        if (!Enum.TryParse<BloodStats.BloodStatType>(statType, true, out var StatType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood stat choice, use '<color=white>.bl lst</color>' to see options.");
            return;
        }

        if (!Enum.TryParse<BloodType>(blood, true, out var BloodType))
        {
            LocalizationService.HandleReply(ctx, "Invalid blood type, use '<color=white>.bl l</color>' to see options.");
            return;
        }

        ulong steamId = ctx.Event.User.PlatformId;

        if (BloodType.Equals(BloodType.GateBoss) || BloodType.Equals(BloodType.None) || BloodType.Equals(BloodType.VBlood))
        {
            LocalizationService.HandleReply(ctx, $"No legacy available for <color=white>{BloodType}</color>.");
            return;
        }

        if (ChooseStat(steamId, BloodType, StatType))
        {
            LocalizationService.HandleReply(ctx, $"<color=#00FFFF>{StatType}</color> has been chosen for <color=red>{BloodType}</color> and will apply after refreshing blood.");
        }
        else
        {
            LocalizationService.HandleReply(ctx, $"You have already chosen {ConfigService.LegacyStatChoices} stats for this legacy, the stat has already been chosen for this legacy, or the stat is not allowed for your class.");
            // UpdateBloodStats(character, bloodType);
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
        User user = ctx.Event.User;
        ulong steamId = user.PlatformId;
        BloodType bloodType = GetCurrentBloodType(character);

        if (bloodType.Equals(BloodType.GateBoss) || bloodType.Equals(BloodType.None) || bloodType.Equals(BloodType.VBlood))
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
                    ResetStats(steamId, bloodType);
                    // UpdateBloodStats(character, bloodType);

                    LocalizationService.HandleReply(ctx, $"Your blood stats have been reset for <color=red>{bloodType}</color>!");
                }
            }
            else
            {
                LocalizationService.HandleReply(ctx, $"You do not have the required item to reset your blood stats (<color=#ffd9eb>{item.GetLocalizedName()}</color>x<color=white>{quantity}</color>)");
            }
        }
        else
        {
            ResetStats(steamId, bloodType);
            // UpdateBloodStats(character, bloodType);

            LocalizationService.HandleReply(ctx, $"Your blood stats have been reset for <color=red>{bloodType}</color>.");
        }
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
            .Select((stat, index) =>
                $"<color=yellow>{index + 1}</color>| <color=#00FFFF>{stat}</color>: <color=white>{Utilities.Misc.FormatBloodStatValue(BloodStats.BloodStatValues[stat])}</color>")
            .ToList();

        if (bloodStatsWithCaps.Count == 0)
        {
            LocalizationService.HandleReply(ctx, "No blood stats available at this time.");
        }
        else
        {
            for (int i = 0; i < bloodStatsWithCaps.Count; i += 4)
            {
                var batch = bloodStatsWithCaps.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.HandleReply(ctx, replyMessage);
            }
        }
    }

    [Command(name: "setlegacy", adminOnly: true, usage: ".bl set [Player] [Blood] [Level]", description: "Sets player blood legacy level.")]
    public static void SetBloodLegacyCommand(ChatCommandContext ctx, string name, string blood, int level)
    {
        if (!ConfigService.BloodSystem)
        {
            LocalizationService.HandleReply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            ctx.Reply($"Couldn't find player.");
            return;
        }

        User foundUser = playerInfo.User;
        if (level < 0 || level > ConfigService.MaxBloodLevel)
        {
            LocalizationService.HandleReply(ctx, $"Level must be between 0 and {ConfigService.MaxBloodLevel}.");
            return;
        }

        if (!Enum.TryParse<BloodType>(blood, true, out var bloodType))
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
        var xpData = new KeyValuePair<int, float>(level, ConvertLevelToXp(level));
        BloodHandler.SetLegacyData(steamId, xpData);

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

        var excludedBloodTypes = new List<BloodType> { BloodType.None, BloodType.VBlood, BloodType.GateBoss };
        var bloodTypes = Enum.GetValues(typeof(BloodType))
                              .Cast<BloodType>()
                              .Where(b => !excludedBloodTypes.Contains(b))
                              .Select(b => b.ToString());

        string bloodTypesList = string.Join(", ", bloodTypes);
        LocalizationService.HandleReply(ctx, $"Available Blood Legacies: <color=red>{bloodTypesList}</color>");
    }
}