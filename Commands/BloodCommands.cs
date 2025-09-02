using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Resources.Localization;
using Bloodcraft.Utilities;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;
using VampireCommandFramework;
using static Bloodcraft.Services.PlayerService;
using static Bloodcraft.Systems.Legacies.BloodManager;
using static Bloodcraft.Systems.Legacies.BloodManager.BloodStats;
using static Bloodcraft.Utilities.Misc.PlayerBools;
using static Bloodcraft.Utilities.Progression;
using static Bloodcraft.Utilities.Progression.ModifyUnitStatBuffSettings;
using static VCF.Core.Basics.RoleCommands;
using User = ProjectM.Network.User;

namespace Bloodcraft.Commands;

[CommandGroup(name: "bloodlegacy", "bl")]
internal static class BloodCommands
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    [Command(name: "get", adminOnly: false, usage: ".bl get [BloodType]", description: "Display current blood legacy details.")]
    public static void GetLegacyCommand(ChatCommandContext ctx, string blood = null)
    {
        if (!ConfigService.LegacySystem)
        {
            LocalizationService.Reply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        Blood playerBlood = playerCharacter.Read<Blood>();

        if (!TryParseBloodType(ctx, playerBlood, blood, out var bloodType))
            return;

        ulong steamId = ctx.Event.User.PlatformId;
        IBloodLegacy handler = BloodLegacyFactory.GetBloodHandler(bloodType);
        if (handler == null)
        {
            LocalizationService.Reply(ctx, "Invalid blood legacy.");
            return;
        }

        var data = handler.GetLegacyData(steamId);
        if (data.Key <= 0)
        {
            LocalizationService.Reply(ctx, MessageKeys.BLOOD_NO_PROGRESS, handler.GetBloodType());
            return;
        }

        int progress = (int)(data.Value - ConvertLevelToXp(data.Key));
        int prestigeLevel = steamId.TryGetPlayerPrestiges(out var prestiges) ? prestiges[BloodSystem.BloodPrestigeTypes[bloodType]] : 0;

        List<KeyValuePair<BloodStatType, string>> stats = BuildLegacyStatList(steamId, bloodType, handler, playerCharacter);
        PrintLegacyProgress(ctx, bloodType, handler, data, progress, prestigeLevel, stats);
    }

    [Command(name: "log", adminOnly: false, usage: ".bl log", description: "Toggles Legacy progress logging.")]
    public static void LogLegacyCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LegacySystem)
        {
            LocalizationService.Reply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        var steamId = ctx.Event.User.PlatformId;

        TogglePlayerBool(steamId, BLOOD_LOG_KEY);
        LocalizationService.Reply(ctx, GetPlayerBool(steamId, BLOOD_LOG_KEY)
            ? MessageKeys.BLOOD_LOGGING_ENABLED
            : MessageKeys.BLOOD_LOGGING_DISABLED);
    }

    [Command(name: "choosestat", shortHand: "cst", adminOnly: false, usage: ".bl cst [BloodOrStat] [BloodStat]", description: "Choose a bonus stat to enhance for your blood legacy.")]
    public static void ChooseBloodStat(ChatCommandContext ctx, string bloodOrStat, int statType = default)
    {
        if (!ConfigService.LegacySystem)
        {
            LocalizationService.Reply(ctx, "Legacies are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        Blood blood = playerCharacter.Read<Blood>();
        ulong steamId = ctx.Event.User.PlatformId;

        BloodType finalBloodType;
        BloodStatType finalBloodStat;

        if (int.TryParse(bloodOrStat, out int numericStat))
        {
            numericStat--;

            if (!Enum.IsDefined(typeof(BloodStatType), numericStat))
            {
                LocalizationService.Reply(ctx,
                    "Invalid blood stat, use '<color=white>.bl lst</color>' to see valid options.");
                return;
            }

            finalBloodStat = (BloodStatType)numericStat;

            finalBloodType = GetCurrentBloodType(blood);

            if (ChooseStat(steamId, finalBloodType, finalBloodStat))
            {
                Buffs.RefreshStats(playerCharacter);
                LocalizationService.Reply(ctx, MessageKeys.BLOOD_STAT_SELECTED, finalBloodStat, finalBloodType);
            }
        }
        else
        {
            if (!Enum.TryParse(bloodOrStat, true, out finalBloodType))
            {
                LocalizationService.Reply(ctx,
                    "Invalid blood type, use '<color=white>.bl l</color>' to see valid options.");
                return;
            }

            if (finalBloodType == BloodType.GateBoss || finalBloodType == BloodType.None || finalBloodType == BloodType.VBlood)
            {
                LocalizationService.Reply(ctx,
                    "Invalid blood legacy, use '<color=white>.bl l</color>' to see valid options.");
                return;
            }

            if (statType <= 0)
            {
                LocalizationService.Reply(ctx,
                    "Invalid blood stat, use '<color=white>.bl lst</color>' to see valid options.");
                return;
            }

            int typedStat = --statType;

            if (!Enum.IsDefined(typeof(BloodStatType), typedStat))
            {
                LocalizationService.Reply(ctx,
                    "Invalid blood stat, use '<color=white>.bl lst</color>' to see valid options.");
                return;
            }

            finalBloodStat = (BloodStatType)typedStat;

            if (ChooseStat(steamId, finalBloodType, finalBloodStat))
            {
                Buffs.RefreshStats(playerCharacter);
                LocalizationService.Reply(ctx, MessageKeys.BLOOD_STAT_SELECTED, finalBloodStat, finalBloodType);
            }
        }
    }

    [Command(name: "resetstats", shortHand: "rst", adminOnly: false, usage: ".bl rst", description: "Reset stats for current blood.")]
    public static void ResetBloodStats(ChatCommandContext ctx)
    {
        if (!ConfigService.LegacySystem)
        {
            LocalizationService.Reply(ctx, "Legacies are not enabled.");
            return;
        }

        Entity playerCharacter = ctx.Event.SenderCharacterEntity;
        Blood blood = playerCharacter.Read<Blood>();
        ulong steamId = ctx.Event.User.PlatformId;
        BloodType bloodType = GetCurrentBloodType(blood);

        if (!ValidateBloodType(ctx, bloodType))
            return;

        string freeKey = bloodType.ToString();
        if (GetPlayerBool(steamId, freeKey))
        {
            ApplyBloodStatReset(ctx, playerCharacter, steamId, bloodType);
            SetPlayerBool(steamId, freeKey, false);
            return;
        }

        if (ConfigService.ResetLegacyItem.Equals(0))
        {
            ApplyBloodStatReset(ctx, playerCharacter, steamId, bloodType);
            return;
        }

        PrefabGUID item = new(ConfigService.ResetLegacyItem);
        int quantity = ConfigService.ResetLegacyItemQuantity;

        if (TryConsumeResetItem(ctx, item, quantity))
        {
            ApplyBloodStatReset(ctx, playerCharacter, steamId, bloodType);
        }
    }

    [Command(name: "liststats", shortHand: "lst", adminOnly: false, usage: ".bl lst", description: "Lists blood stats available.")]
    public static void ListBloodStatsAvailable(ChatCommandContext ctx)
    {
        if (!ConfigService.LegacySystem)
        {
            LocalizationService.Reply(ctx, "Legacies are not enabled.");
            return;
        }

        var bloodStatsWithCaps = Enum.GetValues(typeof(BloodStatType))
            .Cast<BloodStatType>()
            .Select((stat, index) =>
                $"<color=yellow>{index + 1}</color>| <color=#00FFFF>{stat}</color>: <color=white>{Misc.FormatPercentStatValue(BloodStatBaseCaps[stat])}</color>")
            .ToList();

        if (bloodStatsWithCaps.Count == 0)
        {
            LocalizationService.Reply(ctx, "No blood stats available at this time.");
        }
        else
        {
            for (int i = 0; i < bloodStatsWithCaps.Count; i += 4)
            {
                var batch = bloodStatsWithCaps.Skip(i).Take(4);
                string replyMessage = string.Join(", ", batch);
                LocalizationService.Reply(ctx, replyMessage);
            }
        }
    }

    [Command(name: "set", adminOnly: true, usage: ".bl set [Player] [Blood] [Level]", description: "Sets player blood legacy level.")]
    public static void SetBloodLegacyCommand(ChatCommandContext ctx, string name, string blood, int level)
    {
        if (!ConfigService.LegacySystem)
        {
            LocalizationService.Reply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        PlayerInfo playerInfo = GetPlayerInfo(name);
        if (!playerInfo.UserEntity.Exists())
        {
            LocalizationService.Reply(ctx, MessageKeys.PLAYER_NOT_FOUND);
            return;
        }

        User foundUser = playerInfo.User;
        if (level < 0 || level > ConfigService.MaxBloodLevel)
        {
            LocalizationService.Reply(ctx, MessageKeys.LEVEL_MUST_BE_BETWEEN, ConfigService.MaxBloodLevel);
            return;
        }

        if (!Enum.TryParse<BloodType>(blood, true, out var bloodType))
        {
            LocalizationService.Reply(ctx, "Invalid blood legacy.");
            return;
        }

        var BloodHandler = BloodLegacyFactory.GetBloodHandler(bloodType);
        if (BloodHandler == null)
        {
            LocalizationService.Reply(ctx, "Invalid blood legacy.");
            return;
        }

        ulong steamId = foundUser.PlatformId;
        var xpData = new KeyValuePair<int, float>(level, ConvertLevelToXp(level));
        BloodHandler.SetLegacyData(steamId, xpData);

        Buffs.RefreshStats(playerInfo.CharEntity);
        LocalizationService.Reply(ctx, MessageKeys.BLOOD_LEGACY_SET, BloodHandler.GetBloodType(), level, foundUser.CharacterName);
    }

    [Command(name: "list", shortHand: "l", adminOnly: false, usage: ".bl l", description: "Lists blood legacies available.")]
    public static void ListBloodTypesCommand(ChatCommandContext ctx)
    {
        if (!ConfigService.LegacySystem)
        {
            LocalizationService.Reply(ctx, "Blood Legacies are not enabled.");
            return;
        }

        var excludedBloodTypes = new List<BloodType> { BloodType.None, BloodType.VBlood, BloodType.GateBoss };
        var bloodTypes = Enum.GetValues(typeof(BloodType))
                              .Cast<BloodType>()
                              .Where(b => !excludedBloodTypes.Contains(b))
                              .Select(b => b.ToString());

        string bloodTypesList = string.Join(", ", bloodTypes);
        LocalizationService.Reply(ctx, MessageKeys.BLOOD_AVAILABLE_LEGACIES, bloodTypesList);
    }

    static bool TryParseBloodType(ChatCommandContext ctx, Blood playerBlood, string bloodArg, out BloodType bloodType)
    {
        bloodType = GetCurrentBloodType(playerBlood);
        if (string.IsNullOrEmpty(bloodArg))
        {
            bloodType = BloodSystem.GetBloodTypeFromPrefab(playerBlood.BloodType);
            return true;
        }

        if (Enum.TryParse(bloodArg, true, out bloodType))
        {
            return true;
        }

        LocalizationService.Reply(ctx, "Invalid blood, use '.bl l' to see options.");
        return false;
    }

    static List<KeyValuePair<BloodStatType, string>> BuildLegacyStatList(ulong steamId, BloodType bloodType, IBloodLegacy handler, Entity playerCharacter)
    {
        List<KeyValuePair<BloodStatType, string>> stats = [];

        if (!steamId.TryGetPlayerBloodStats(out var bloodTypeStats) || !bloodTypeStats.TryGetValue(bloodType, out var bloodStatTypes))
            return stats;

        foreach (BloodStatType bloodStatType in bloodStatTypes)
        {
            if (!TryGetScaledModifyUnitLegacyStat(handler, playerCharacter, steamId, bloodType, bloodStatType, out float statValue, out ModifyUnitStatBuff _))
                continue;

            string bloodStatString = (statValue * 100).ToString("F1") + "%";
            stats.Add(new KeyValuePair<BloodStatType, string>(bloodStatType, bloodStatString));
        }

        return stats;
    }

    static void PrintLegacyProgress(ChatCommandContext ctx, BloodType bloodType, IBloodLegacy handler, KeyValuePair<int, float> data, int progress, int prestigeLevel, List<KeyValuePair<BloodStatType, string>> stats)
    {
        LocalizationService.Reply(ctx, MessageKeys.BLOOD_PROGRESS, data.Key, prestigeLevel, progress, BloodSystem.GetLevelProgress(ctx.Event.User.PlatformId, handler), handler.GetBloodType());

        if (stats.Count == 0)
        {
            LocalizationService.Reply(ctx, MessageKeys.BLOOD_NO_STATS_SELECTED, bloodType);
            return;
        }

        for (int i = 0; i < stats.Count; i += 6)
        {
            var batch = stats.Skip(i).Take(6);
            string bonuses = string.Join(", ", batch.Select(stat => $"<color=#00FFFF>{stat.Key}</color>: <color=white>{stat.Value}</color>"));
            LocalizationService.Reply(ctx, MessageKeys.BLOOD_STATS, bloodType, bonuses);
        }
    }

    static bool ValidateBloodType(ChatCommandContext ctx, BloodType bloodType)
    {
        if (bloodType == BloodType.GateBoss || bloodType == BloodType.None || bloodType == BloodType.VBlood)
        {
            LocalizationService.Reply(ctx, MessageKeys.BLOOD_NO_LEGACY_AVAILABLE, bloodType);
            return false;
        }

        return true;
    }

    static bool TryConsumeResetItem(ChatCommandContext ctx, PrefabGUID item, int quantity)
    {
        if (InventoryUtilities.TryGetInventoryEntity(EntityManager, ctx.User.LocalCharacter._Entity, out var inventoryEntity) && ServerGameManager.GetInventoryItemCount(inventoryEntity, item) >= quantity)
        {
            return ServerGameManager.TryRemoveInventoryItem(inventoryEntity, item, quantity);
        }

        LocalizationService.Reply(ctx, MessageKeys.BLOOD_RESET_ITEM_REQUIRED, item.GetLocalizedName(), quantity);
        return false;
    }

    static void ApplyBloodStatReset(ChatCommandContext ctx, Entity playerCharacter, ulong steamId, BloodType bloodType)
    {
        ResetStats(steamId, bloodType);
        Buffs.RefreshStats(playerCharacter);
        LocalizationService.Reply(ctx, MessageKeys.BLOOD_STATS_RESET, bloodType);
    }
}