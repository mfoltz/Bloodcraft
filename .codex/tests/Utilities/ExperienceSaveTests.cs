using System;
using System.Collections.Generic;
using System.Reflection;
using Bloodcraft.Interfaces;
using Bloodcraft.Services;
using Bloodcraft.Systems.Expertise;
using Bloodcraft.Systems.Legacies;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using Xunit;

namespace Bloodcraft.Tests.Utilities;

public sealed class ExperienceSaveTests
{
    const ulong SteamId = 76561198000012345UL;
    static readonly Harmony Harmony = new("Bloodcraft.Tests.Utilities.ExperienceSaveTests");

    static ExperienceSaveTests()
    {
        PatchWeaponSystem();
        PatchBloodSystem();
    }

    static void PatchWeaponSystem()
    {
        MethodBase? cctor = typeof(WeaponSystem).TypeInitializer;
        if (cctor != null)
        {
            MethodInfo prefix = typeof(ExperienceSaveTests).GetMethod(nameof(WeaponSystemCctorPrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
            var processor = Harmony.CreateProcessor(cctor);
            processor.AddPrefix(new HarmonyMethod(prefix));
            processor.Patch();
        }

        MethodBase? progressionCctor = typeof(Progression).TypeInitializer;
        if (progressionCctor != null)
        {
            MethodInfo skip = typeof(ExperienceSaveTests).GetMethod(nameof(SkipOriginal), BindingFlags.NonPublic | BindingFlags.Static)!;
            var processor = Harmony.CreateProcessor(progressionCctor);
            processor.AddPrefix(new HarmonyMethod(skip));
            processor.Patch();
        }
    }

    static void PatchBloodSystem()
    {
        MethodBase? cctor = typeof(BloodSystem).TypeInitializer;
        if (cctor == null)
        {
            return;
        }

        MethodInfo prefix = typeof(ExperienceSaveTests).GetMethod(nameof(BloodSystemCctorPrefix), BindingFlags.NonPublic | BindingFlags.Static)!;
        var processor = Harmony.CreateProcessor(cctor);
        processor.AddPrefix(new HarmonyMethod(prefix));
        processor.Patch();
    }

    public static IEnumerable<object[]> LevelingScenarios()
    {
        int levelZero = 0;
        float zeroXp = Progression.ConvertLevelToXp(levelZero);
        float toLevelOne = Progression.ConvertLevelToXp(levelZero + 1) - zeroXp;
        yield return new object[]
        {
            levelZero,
            zeroXp,
            toLevelOne,
            0,
            true,
            levelZero + 1,
            Progression.ConvertLevelToXp(levelZero + 1)
        };

        int prestigeLevel = 4;
        int midLevel = 5;
        float midXp = Progression.ConvertLevelToXp(midLevel);
        float toNext = Progression.ConvertLevelToXp(midLevel + 1) - midXp;
        float reductionFactor = 1f - ConfigService.LevelingPrestigeReducer * prestigeLevel;
        float expectedMidXp = midXp + toNext * reductionFactor;
        yield return new object[]
        {
            midLevel,
            midXp,
            toNext,
            prestigeLevel,
            false,
            midLevel,
            expectedMidXp
        };

        int maxLevel = ConfigService.MaxLevel;
        int penultimate = Math.Max(0, maxLevel - 1);
        float penultimateXp = Progression.ConvertLevelToXp(penultimate);
        float oversizedGain = Progression.ConvertLevelToXp(maxLevel + 5) - penultimateXp;
        yield return new object[]
        {
            penultimate,
            penultimateXp,
            oversizedGain,
            0,
            true,
            maxLevel,
            Progression.ConvertLevelToXp(maxLevel)
        };
    }

    [Theory]
    [MemberData(nameof(LevelingScenarios))]
    public void SaveLevelingExperience_HandlesPrestigeAndCaps(
        int startingLevel,
        float startingXp,
        float baseGain,
        int prestigeLevel,
        bool expectedLeveledUp,
        int expectedLevel,
        float expectedXp)
    {
        using var reset = PlayerDataReset.ForLeveling();

        DataService.PlayerDictionaries._playerExperience[SteamId] = new KeyValuePair<int, float>(startingLevel, startingXp);

        if (prestigeLevel > 0)
        {
            DataService.PlayerDictionaries._playerPrestiges[SteamId] = new Dictionary<PrestigeType, int>
            {
                [PrestigeType.Experience] = prestigeLevel
            };
        }

        float reductionFactor = prestigeLevel > 0
            ? Math.Max(0f, 1f - ConfigService.LevelingPrestigeReducer * prestigeLevel)
            : 1f;
        float gainedXp = baseGain * reductionFactor;

        LevelingSystem.SaveLevelingExperience(SteamId, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> stored = DataService.PlayerDictionaries._playerExperience[SteamId];

        Assert.Equal(expectedLeveledUp, leveledUp);
        Assert.Equal(expectedLevel, newLevel);
        Assert.Equal(expectedLevel, stored.Key);
        Assert.Equal(expectedXp, stored.Value);
    }

    public static IEnumerable<object[]> ExpertiseScenarios()
    {
        int levelZero = 0;
        float zeroXp = Progression.ConvertLevelToXp(levelZero);
        float toLevelOne = Progression.ConvertLevelToXp(levelZero + 1) - zeroXp;
        yield return new object[]
        {
            levelZero,
            zeroXp,
            toLevelOne,
            0,
            0,
            true,
            levelZero + 1,
            Progression.ConvertLevelToXp(levelZero + 1)
        };

        int expertisePrestige = 3;
        int xpPrestige = 0;
        int midLevel = 7;
        float midXp = Progression.ConvertLevelToXp(midLevel);
        float toNext = Progression.ConvertLevelToXp(midLevel + 1) - midXp;
        float changeFactor = 1f - ConfigService.PrestigeRatesReducer * expertisePrestige;
        float expectedMidXp = midXp + toNext * changeFactor;
        yield return new object[]
        {
            midLevel,
            midXp,
            toNext,
            expertisePrestige,
            xpPrestige,
            false,
            midLevel,
            expectedMidXp
        };

        int maxLevel = ConfigService.MaxExpertiseLevel;
        int penultimate = Math.Max(0, maxLevel - 1);
        float penultimateXp = Progression.ConvertLevelToXp(penultimate);
        float oversizedGain = Progression.ConvertLevelToXp(maxLevel + 5) - penultimateXp;
        yield return new object[]
        {
            penultimate,
            penultimateXp,
            oversizedGain,
            0,
            0,
            true,
            maxLevel,
            Progression.ConvertLevelToXp(maxLevel)
        };
    }

    [Theory]
    [MemberData(nameof(ExpertiseScenarios))]
    public void SaveExpertiseExperience_HandlesPrestigeAndCaps(
        int startingLevel,
        float startingXp,
        float baseGain,
        int expertisePrestige,
        int xpPrestige,
        bool expectedLeveledUp,
        int expectedLevel,
        float expectedXp)
    {
        using var reset = PlayerDataReset.ForExpertise();

        IWeaponExpertise handler = new StubWeaponExpertise(WeaponType.Sword);

        handler.SetExpertiseData(SteamId, new KeyValuePair<int, float>(startingLevel, startingXp));

        if (expertisePrestige > 0 || xpPrestige > 0)
        {
            var prestige = new Dictionary<PrestigeType, int>();
            if (expertisePrestige > 0)
            {
                prestige[PrestigeType.SwordExpertise] = expertisePrestige;
            }

            if (xpPrestige > 0)
            {
                prestige[PrestigeType.Experience] = xpPrestige;
            }

            DataService.PlayerDictionaries._playerPrestiges[SteamId] = prestige;
        }

        float changeFactor = 1f
            - ConfigService.PrestigeRatesReducer * expertisePrestige
            + ConfigService.PrestigeRateMultiplier * xpPrestige;
        float gainedXp = baseGain * Math.Max(0f, changeFactor);

        WeaponSystem.SaveExpertiseExperience(SteamId, handler, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> stored = handler.GetExpertiseData(SteamId);

        Assert.Equal(expectedLeveledUp, leveledUp);
        Assert.Equal(expectedLevel, newLevel);
        Assert.Equal(expectedLevel, stored.Key);
        Assert.Equal(expectedXp, stored.Value);
    }

    public static IEnumerable<object[]> BloodScenarios()
    {
        BloodType bloodType = BloodType.Warrior;
        int levelZero = 0;
        float zeroXp = Progression.ConvertLevelToXp(levelZero);
        float toLevelOne = Progression.ConvertLevelToXp(levelZero + 1) - zeroXp;
        yield return new object[]
        {
            bloodType,
            levelZero,
            zeroXp,
            toLevelOne,
            0,
            0,
            true,
            levelZero + 1,
            Progression.ConvertLevelToXp(levelZero + 1)
        };

        int bloodPrestige = 2;
        int xpPrestige = 0;
        int midLevel = 6;
        float midXp = Progression.ConvertLevelToXp(midLevel);
        float toNext = Progression.ConvertLevelToXp(midLevel + 1) - midXp;
        float changeFactor = 1f - ConfigService.PrestigeRatesReducer * bloodPrestige;
        float expectedMidXp = midXp + toNext * changeFactor;
        yield return new object[]
        {
            bloodType,
            midLevel,
            midXp,
            toNext,
            bloodPrestige,
            xpPrestige,
            false,
            midLevel,
            expectedMidXp
        };

        int maxLevel = ConfigService.MaxBloodLevel;
        int penultimate = Math.Max(0, maxLevel - 1);
        float penultimateXp = Progression.ConvertLevelToXp(penultimate);
        float oversizedGain = Progression.ConvertLevelToXp(maxLevel + 5) - penultimateXp;
        yield return new object[]
        {
            bloodType,
            penultimate,
            penultimateXp,
            oversizedGain,
            0,
            0,
            true,
            maxLevel,
            Progression.ConvertLevelToXp(maxLevel)
        };
    }

    [Theory]
    [MemberData(nameof(BloodScenarios))]
    public void SaveBloodExperience_HandlesPrestigeAndCaps(
        BloodType bloodType,
        int startingLevel,
        float startingXp,
        float baseGain,
        int bloodPrestige,
        int xpPrestige,
        bool expectedLeveledUp,
        int expectedLevel,
        float expectedXp)
    {
        using var reset = PlayerDataReset.ForBlood();

        IBloodLegacy handler = new StubBloodLegacy(bloodType);

        handler.SetLegacyData(SteamId, new KeyValuePair<int, float>(startingLevel, startingXp));

        if (bloodPrestige > 0 || xpPrestige > 0)
        {
            var prestige = new Dictionary<PrestigeType, int>();
            if (bloodPrestige > 0)
            {
                prestige[GetPrestigeTypeFor(bloodType)] = bloodPrestige;
            }

            if (xpPrestige > 0)
            {
                prestige[PrestigeType.Experience] = xpPrestige;
            }

            DataService.PlayerDictionaries._playerPrestiges[SteamId] = prestige;
        }

        float changeFactor = 1f
            - ConfigService.PrestigeRatesReducer * bloodPrestige
            + ConfigService.PrestigeRateMultiplier * xpPrestige;
        float gainedXp = baseGain * Math.Max(0f, changeFactor);

        BloodSystem.SaveBloodExperience(SteamId, handler, gainedXp, out bool leveledUp, out int newLevel);

        KeyValuePair<int, float> stored = handler.GetLegacyData(SteamId);

        Assert.Equal(expectedLeveledUp, leveledUp);
        Assert.Equal(expectedLevel, newLevel);
        Assert.Equal(expectedLevel, stored.Key);
        Assert.Equal(expectedXp, stored.Value);
    }

    sealed class StubWeaponExpertise : IWeaponExpertise
    {
        readonly Dictionary<ulong, KeyValuePair<int, float>> storage = new();
        readonly WeaponType weaponType;

        public StubWeaponExpertise(WeaponType weaponType)
        {
            this.weaponType = weaponType;
        }

        public KeyValuePair<int, float> GetExpertiseData(ulong steamId)
        {
            return storage.TryGetValue(steamId, out var value) ? value : new KeyValuePair<int, float>(0, 0f);
        }

        public void SetExpertiseData(ulong steamId, KeyValuePair<int, float> xpData)
        {
            storage[steamId] = xpData;
        }

        public WeaponType GetWeaponType()
        {
            return weaponType;
        }
    }

    sealed class StubBloodLegacy : IBloodLegacy
    {
        readonly Dictionary<ulong, KeyValuePair<int, float>> storage = new();
        readonly BloodType bloodType;

        public StubBloodLegacy(BloodType bloodType)
        {
            this.bloodType = bloodType;
        }

        public KeyValuePair<int, float> GetLegacyData(ulong steamId)
        {
            return storage.TryGetValue(steamId, out var value) ? value : new KeyValuePair<int, float>(0, 0f);
        }

        public void SetLegacyData(ulong steamId, KeyValuePair<int, float> xpData)
        {
            storage[steamId] = xpData;
        }

        public BloodType GetBloodType()
        {
            return bloodType;
        }
    }

    static PrestigeType GetPrestigeTypeFor(BloodType bloodType)
    {
        return bloodType switch
        {
            BloodType.Worker => PrestigeType.WorkerLegacy,
            BloodType.Warrior => PrestigeType.WarriorLegacy,
            BloodType.Scholar => PrestigeType.ScholarLegacy,
            BloodType.Rogue => PrestigeType.RogueLegacy,
            BloodType.Mutant => PrestigeType.MutantLegacy,
            BloodType.Draculin => PrestigeType.DraculinLegacy,
            BloodType.Immortal => PrestigeType.ImmortalLegacy,
            BloodType.Creature => PrestigeType.CreatureLegacy,
            BloodType.Brute => PrestigeType.BruteLegacy,
            BloodType.Corruption => PrestigeType.CorruptionLegacy,
            _ => PrestigeType.WarriorLegacy
        };
    }

    static bool BloodSystemCctorPrefix()
    {
        SetStaticField("_maxBloodLevel", ConfigService.MaxBloodLevel);
        SetStaticField("_legacyStatChoices", ConfigService.LegacyStatChoices);
        SetStaticField("_vBloodLegacyMultiplier", ConfigService.VBloodLegacyMultiplier);
        SetStaticField("_unitLegacyMultiplier", ConfigService.UnitLegacyMultiplier);
        SetStaticField("_prestigeRatesReducer", ConfigService.PrestigeRatesReducer);
        SetStaticField("_prestigeRateMultiplier", ConfigService.PrestigeRateMultiplier);

        var prestigeMap = new Dictionary<BloodType, PrestigeType>
        {
            [BloodType.Worker] = PrestigeType.WorkerLegacy,
            [BloodType.Warrior] = PrestigeType.WarriorLegacy,
            [BloodType.Scholar] = PrestigeType.ScholarLegacy,
            [BloodType.Rogue] = PrestigeType.RogueLegacy,
            [BloodType.Mutant] = PrestigeType.MutantLegacy,
            [BloodType.Draculin] = PrestigeType.DraculinLegacy,
            [BloodType.Immortal] = PrestigeType.ImmortalLegacy,
            [BloodType.Creature] = PrestigeType.CreatureLegacy,
            [BloodType.Brute] = PrestigeType.BruteLegacy,
            [BloodType.Corruption] = PrestigeType.CorruptionLegacy
        };
        SetStaticField("_bloodPrestigeTypes", prestigeMap);
        SetStaticField("_tryGetExtensions", new Dictionary<BloodType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>>());
        SetStaticField("_setExtensions", new Dictionary<BloodType, Action<ulong, KeyValuePair<int, float>>>());

        return false;
    }

    static void SetStaticField(string fieldName, object value)
    {
        FieldInfo field = typeof(BloodSystem).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!;
        field.SetValue(null, value);
    }

    static bool WeaponSystemCctorPrefix()
    {
        Type weaponSystemType = typeof(WeaponSystem);

        SetReadonlyField(weaponSystemType, "_maxExpertiseLevel", ConfigService.MaxExpertiseLevel);
        SetReadonlyField(weaponSystemType, "_expertiseStatChoices", ConfigService.ExpertiseStatChoices);
        SetReadonlyField(weaponSystemType, "_unitExpertiseMultiplier", ConfigService.UnitExpertiseMultiplier);
        SetReadonlyField(weaponSystemType, "_vBloodExpertiseMultiplier", ConfigService.VBloodExpertiseMultiplier);
        SetReadonlyField(weaponSystemType, "_prestigeRatesReducer", ConfigService.PrestigeRatesReducer);
        SetReadonlyField(weaponSystemType, "_prestigeRateMultiplier", ConfigService.PrestigeRateMultiplier);
        SetReadonlyField(weaponSystemType, "_unitSpawnerExpertiseFactor", ConfigService.UnitSpawnerExpertiseFactor);
        SetReadonlyField(weaponSystemType, "TryGetExtensionMap", new Dictionary<WeaponType, Func<ulong, (bool Success, KeyValuePair<int, float> Data)>>());
        SetReadonlyField(weaponSystemType, "SetExtensionMap", new Dictionary<WeaponType, Action<ulong, KeyValuePair<int, float>>>());
        SetReadonlyField(weaponSystemType, "_delay", null);

        return false;
    }

    static void SetReadonlyField(Type type, string fieldName, object? value)
    {
        FieldInfo field = type.GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;
        if (field.IsInitOnly)
        {
            FieldInfo? attributesField = typeof(FieldInfo).GetField("m_fieldAttributes", BindingFlags.Instance | BindingFlags.NonPublic);
            attributesField?.SetValue(field, field.Attributes & ~FieldAttributes.InitOnly);
        }

        field.SetValue(null, value);
    }

    static bool SkipOriginal()
    {
        return false;
    }
}
