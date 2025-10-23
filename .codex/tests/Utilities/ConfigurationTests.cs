using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx.Logging;
using Bloodcraft;
using Bloodcraft.Commands;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Quests;
using Bloodcraft.Utilities;
using Stunlock.Core;
using Xunit;

namespace Bloodcraft.Tests.Utilities;

public sealed class ConfigurationTests : TestHost
{
    [Fact]
    public void ParseIntegersFromString_UsesOverriddenConfigValues()
    {
        using var scope = WithConfigOverrides(("QuestRewards", "101, 202, not-a-number, 303, 202"));

        List<int> values = Configuration.ParseIntegersFromString(ConfigService.QuestRewards);

        Assert.Equal(new[] { 101, 202, 303, 202 }, values);
    }

    [Fact]
    public void ParseEnumsFromString_ResolvesEnumValuesFromConfig()
    {
        using var scope = WithConfigOverrides(("BleedingEdge", "Sword, Whip, invalid, Daggers"));

        List<WeaponType> weapons = Configuration.ParseEnumsFromString<WeaponType>(ConfigService.BleedingEdge);

        Assert.Equal(new[] { WeaponType.Sword, WeaponType.Whip, WeaponType.Daggers }, weapons);
    }

    [Fact]
    public void GetQuestRewardItems_PopulatesDictionaryAndSkipsDuplicates()
    {
        using var config = WithConfigOverrides(
            ("QuestRewards", "1001, 1002, 1002, 1003"),
            ("QuestRewardAmounts", "5, 10, 99, 20"));
        using var log = LogCaptureScope.Create();
#nullable enable

using Bloodcraft.Commands;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Familiars;
using Bloodcraft.Systems.Quests;
using ProjectM;
using Stunlock.Core;

namespace Bloodcraft.Utilities;
internal static class Configuration
{
    public static void GetExcludedFamiliars()
    {
        List<PrefabGUID> unitBans = [..ParseIntegersFromString(ConfigService.BannedUnits).Select(unit => new PrefabGUID(unit))];

        foreach (PrefabGUID unit in unitBans)
        {
            if (unit.HasValue()) FamiliarUnlockSystem.ConfiguredPrefabGuidBans.Add(unit);
        }

        List<string> categoryBans = ConfigService.BannedTypes.Split(',').Select(s => s.Trim()).ToList();

        foreach (string category in categoryBans)
        {
            if (Enum.TryParse(category, out UnitCategory unitCategory))
            {
                FamiliarUnlockSystem.ConfiguredCategoryBans.Add(unitCategory);
            }
        }
    }
    public static List<int> ParseIntegersFromString(string configString)
    {
        if (string.IsNullOrWhiteSpace(configString))
        {
            return [];
        }

        List<int> results = [];

        foreach (string segment in configString.Split(','))
        {
            string trimmedSegment = segment.Trim();

            if (string.IsNullOrEmpty(trimmedSegment))
            {
                continue;
            }

            if (int.TryParse(trimmedSegment, out int value))
            {
                results.Add(value);
            }
        }

        return results;
    }
    public static List<T> ParseEnumsFromString<T>(string configString) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(configString))
            return [];

        List<T> result = [];

        foreach (var part in configString.Split(','))
        {
            if (Enum.TryParse<T>(part.Trim(), ignoreCase: true, out var value))
            {
                result.Add(value);
            }
        }

        return result;
    }
    public static void GetQuestRewardItems()
    {
        GetQuestRewardItems(
            ConfigService.QuestRewards,
            ConfigService.QuestRewardAmounts,
            QuestSystem.QuestRewards,
            message => Core.Log.LogWarning(message));
    }

    public static void GetQuestRewardItems(
        string questRewardsConfig,
        string questRewardAmountsConfig,
        IDictionary<PrefabGUID, int> destination,
        Action<string>? logWarning = null)
    {
        ArgumentNullException.ThrowIfNull(destination);

        List<int> rewardAmounts = ParseIntegersFromString(questRewardAmountsConfig);
        List<PrefabGUID> questRewards = [..ParseIntegersFromString(questRewardsConfig).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (questRewards.Count != rewardAmounts.Count)
        {
            logWarning?.Invoke("QuestRewards and QuestRewardAmounts are not the same length, please correct this for predictable behavior when receiving quest rewards!");
        }

        int pairCount = questRewards.Count < rewardAmounts.Count ? questRewards.Count : rewardAmounts.Count;

        for (int i = 0; i < pairCount; i++)
        {
            PrefabGUID reward = questRewards[i];

            if (!destination.ContainsKey(reward))
            {
                destination[reward] = rewardAmounts[i];
            }
        }
    }

    public static void GetStarterKitItems()
    {
        GetStarterKitItems(
            ConfigService.KitPrefabs,
            ConfigService.KitQuantities,
            MiscCommands.StarterKitItemPrefabGUIDs,
            message => Core.Log.LogWarning(message));
    }

    public static void GetStarterKitItems(
        string kitPrefabsConfig,
        string kitQuantitiesConfig,
        IDictionary<PrefabGUID, int> destination,
        Action<string>? logWarning = null)
    {
        ArgumentNullException.ThrowIfNull(destination);

        List<int> kitAmounts = ParseIntegersFromString(kitQuantitiesConfig);
        List<PrefabGUID> kitPrefabs = [..ParseIntegersFromString(kitPrefabsConfig).Select(itemPrefab => new PrefabGUID(itemPrefab))];

        if (kitPrefabs.Count != kitAmounts.Count)
        {
            logWarning?.Invoke("KitPrefabs and KitQuantities are not the same length, please correct this for predictable behavior when using the kit command!");
        }

        int pairCount = kitPrefabs.Count < kitAmounts.Count ? kitPrefabs.Count : kitAmounts.Count;

        for (int i = 0; i < pairCount; i++)
        {
            PrefabGUID prefab = kitPrefabs[i];

            if (!destination.ContainsKey(prefab))
            {
                destination[prefab] = kitAmounts[i];
            }
        }
    }
        Configuration.GetQuestRewardItems();

        Assert.Equal(3, QuestSystem.QuestRewards.Count);
        Assert.Equal(5, QuestSystem.QuestRewards[new PrefabGUID(1001)]);
        Assert.Equal(10, QuestSystem.QuestRewards[new PrefabGUID(1002)]);
        Assert.Equal(20, QuestSystem.QuestRewards[new PrefabGUID(1003)]);
        Assert.DoesNotContain(log.Events, entry => entry.Level.HasFlag(LogLevel.Warning));
    }

    [Fact]
    public void GetQuestRewardItems_LogsWarningWhenCountsMismatch()
    {
        using var config = WithConfigOverrides(
            ("QuestRewards", "2001"),
            ("QuestRewardAmounts", "7, 11"));
        using var log = LogCaptureScope.Create();
        using var questScope = new DictionaryStateScope<PrefabGUID, int>(QuestSystem.QuestRewards);

        Configuration.GetQuestRewardItems();

        LogEventArgs warning = Assert.Single(log.Events.Where(entry => entry.Level.HasFlag(LogLevel.Warning)));
        Assert.Contains("QuestRewards and QuestRewardAmounts are not the same length", warning.Data.ToString());
        Assert.Equal(7, QuestSystem.QuestRewards[new PrefabGUID(2001)]);
    }

    [Fact]
    public void GetStarterKitItems_PopulatesDictionaryAndSkipsDuplicates()
    {
        using var config = WithConfigOverrides(
            ("KitPrefabs", "3001, 3002, 3002"),
            ("KitQuantities", "3, 6, 9"));
        using var log = LogCaptureScope.Create();
        using var kitScope = new DictionaryStateScope<PrefabGUID, int>(MiscCommands.StarterKitItemPrefabGUIDs);

        Configuration.GetStarterKitItems();

        Assert.Equal(2, MiscCommands.StarterKitItemPrefabGUIDs.Count);
        Assert.Equal(3, MiscCommands.StarterKitItemPrefabGUIDs[new PrefabGUID(3001)]);
        Assert.Equal(6, MiscCommands.StarterKitItemPrefabGUIDs[new PrefabGUID(3002)]);
        Assert.DoesNotContain(log.Events, entry => entry.Level.HasFlag(LogLevel.Warning));
    }

    [Fact]
    public void GetStarterKitItems_LogsWarningWhenCountsMismatch()
    {
        using var config = WithConfigOverrides(
            ("KitPrefabs", "4001"),
            ("KitQuantities", "4, 8"));
        using var log = LogCaptureScope.Create();
        using var kitScope = new DictionaryStateScope<PrefabGUID, int>(MiscCommands.StarterKitItemPrefabGUIDs);

        Configuration.GetStarterKitItems();

        LogEventArgs warning = Assert.Single(log.Events.Where(entry => entry.Level.HasFlag(LogLevel.Warning)));
        Assert.Contains("KitPrefabs and KitQuantities are not the same length", warning.Data.ToString());
        Assert.Equal(4, MiscCommands.StarterKitItemPrefabGUIDs[new PrefabGUID(4001)]);
    }

    sealed class DictionaryStateScope<TKey, TValue> : IDisposable where TKey : notnull
    {
        readonly Dictionary<TKey, TValue> dictionary;
        readonly Dictionary<TKey, TValue> snapshot;

        public DictionaryStateScope(Dictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
            snapshot = new Dictionary<TKey, TValue>(dictionary);
            dictionary.Clear();
        }

        public void Dispose()
        {
            dictionary.Clear();
            foreach (KeyValuePair<TKey, TValue> entry in snapshot)
            {
                dictionary[entry.Key] = entry.Value;
            }
        }
    }

    sealed class LogCaptureScope : IDisposable
    {
        readonly ManualLogSource logger;
        readonly Plugin? originalInstance;
        readonly ManualLogSource? originalLogger;
        readonly Plugin pluginInstance;
        readonly bool createdInstance;

        LogCaptureScope()
        {
            logger = new ManualLogSource("ConfigurationTests");
            logger.LogEvent += OnLogEvent;
            originalInstance = Plugin.Instance;
            originalLogger = ExtractLogger(originalInstance);
            if (originalInstance is null)
            {
                pluginInstance = (Plugin)FormatterServices.GetUninitializedObject(typeof(Plugin));
                createdInstance = true;
            }
            else
            {
                pluginInstance = originalInstance;
            }

            Plugin.Instance = pluginInstance;

            if (!TryAssignLogger(pluginInstance, logger))
            {
                throw new InvalidOperationException("Failed to install test logger on Plugin.Instance.");
            }
        }

        public static LogCaptureScope Create() => new();

        public List<LogEventArgs> Events { get; } = new();

        void OnLogEvent(object? sender, LogEventArgs e) => Events.Add(e);

        public void Dispose()
        {
            logger.LogEvent -= OnLogEvent;
            TryAssignLogger(pluginInstance, originalLogger);

            if (createdInstance)
            {
                Plugin.Instance = null;
            }
            else
            {
                Plugin.Instance = originalInstance;
            }
        }

        static ManualLogSource? ExtractLogger(Plugin? instance)
        {
            if (instance is null)
            {
                return null;
            }

            return GetLoggerFromType(instance, instance.GetType())
                ?? GetLoggerFromType(instance, instance.GetType().BaseType);
        }

        static ManualLogSource? GetLoggerFromType(Plugin instance, Type? type)
        {
            if (type is null)
            {
                return null;
            }

            PropertyInfo? property = type.GetProperty("Log", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.CanRead == true)
            {
                if (property.GetValue(instance) is ManualLogSource manual)
                {
                    return manual;
                }
            }

            FieldInfo? field = type.GetField("_log", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(ManualLogSource).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(instance) as ManualLogSource;
            }

            return null;
        }

        static bool TryAssignLogger(Plugin instance, ManualLogSource? value)
        {
            return TryAssignLogger(instance, instance.GetType(), value)
                || TryAssignLogger(instance, instance.GetType().BaseType, value);
        }

        static bool TryAssignLogger(Plugin instance, Type? type, ManualLogSource? value)
        {
            if (type is null)
            {
                return false;
            }

            PropertyInfo? property = type.GetProperty("Log", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.CanWrite == true)
            {
                property.SetValue(instance, value);
                return true;
            }

            FieldInfo? field = type.GetField("_log", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(ManualLogSource).IsAssignableFrom(field.FieldType))
            {
                field.SetValue(instance, value);
                return true;
            }

            return false;
        }
    }
}
