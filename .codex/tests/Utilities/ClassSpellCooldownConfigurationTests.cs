using System.Reflection;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using Stunlock.Core;

namespace Bloodcraft.Tests.Utilities;

public sealed class ClassSpellCooldownConfigurationTests : TestHost
{
    static readonly Func<IReadOnlyDictionary<ClassManager.PlayerClass, string>> DefaultClassSpellsAccessor = Configuration.ClassSpellsMapAccessor;
    static readonly FieldInfo PrefabGuidValueField = typeof(PrefabGUID)
        .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        .First(field => field.FieldType == typeof(int));

    protected override void ResetState()
    {
        base.ResetState();
        Configuration.ClassSpellsMapAccessor = GetConfiguredClassSpells;
        AbilityRunScriptsSystemPatch.ClearClassSpells();
    }

    public override void Dispose()
    {
        Configuration.ClassSpellsMapAccessor = DefaultClassSpellsAccessor;
        AbilityRunScriptsSystemPatch.ClearClassSpells();
        base.Dispose();
    }

    static IReadOnlyDictionary<ClassManager.PlayerClass, string> GetConfiguredClassSpells()
    {
        return new Dictionary<ClassManager.PlayerClass, string>
        {
            [ClassManager.PlayerClass.BloodKnight] = ResolveSpellList(nameof(ConfigService.BloodKnightSpells)),
            [ClassManager.PlayerClass.DemonHunter] = ResolveSpellList(nameof(ConfigService.DemonHunterSpells)),
            [ClassManager.PlayerClass.VampireLord] = ResolveSpellList(nameof(ConfigService.VampireLordSpells)),
            [ClassManager.PlayerClass.ShadowBlade] = ResolveSpellList(nameof(ConfigService.ShadowBladeSpells)),
            [ClassManager.PlayerClass.ArcaneSorcerer] = ResolveSpellList(nameof(ConfigService.ArcaneSorcererSpells)),
            [ClassManager.PlayerClass.DeathMage] = ResolveSpellList(nameof(ConfigService.DeathMageSpells)),
        };
    }

    [Fact]
    public void GetClassSpellCooldowns_AssignsZeroBasedIndicesForUniquePrefabs()
    {
        using var scope = WithConfigOverrides(
            (nameof(ConfigService.BloodKnightSpells), "1111, 1111, , 2222, 3333"),
            (nameof(ConfigService.ShadowBladeSpells), "4444, , 5555, 4444"),
            (nameof(ConfigService.DemonHunterSpells), string.Empty),
            (nameof(ConfigService.VampireLordSpells), string.Empty),
            (nameof(ConfigService.ArcaneSorcererSpells), string.Empty),
            (nameof(ConfigService.DeathMageSpells), string.Empty));

        AbilityRunScriptsSystemPatch.ClearClassSpells();
        Configuration.GetClassSpellCooldowns();

        IReadOnlyDictionary<PrefabGUID, int> spells = AbilityRunScriptsSystemPatch.ClassSpells;

        Assert.Equal(5, spells.Count);
        Assert.Equal(0, spells[new PrefabGUID(1111)]);
        Assert.Equal(2, spells[new PrefabGUID(2222)]);
        Assert.Equal(3, spells[new PrefabGUID(3333)]);
        Assert.Equal(0, spells[new PrefabGUID(4444)]);
        Assert.Equal(1, spells[new PrefabGUID(5555)]);
    }

    [Fact]
    public void GetClassSpellCooldowns_ReplacesCacheOnSubsequentRuns()
    {
        using var initialScope = WithConfigOverrides(
            (nameof(ConfigService.BloodKnightSpells), "6001, , 6001, 6002"),
            (nameof(ConfigService.ShadowBladeSpells), "7001, 7002"),
            (nameof(ConfigService.DemonHunterSpells), string.Empty),
            (nameof(ConfigService.VampireLordSpells), string.Empty),
            (nameof(ConfigService.ArcaneSorcererSpells), string.Empty),
            (nameof(ConfigService.DeathMageSpells), string.Empty));

        AbilityRunScriptsSystemPatch.ClearClassSpells();
        Configuration.GetClassSpellCooldowns();

        IReadOnlyDictionary<PrefabGUID, int> initialSpells = AbilityRunScriptsSystemPatch.ClassSpells;

        int[] initialExpected = { 6001, 6002, 7001, 7002 };
        Assert.Equal(initialExpected, initialSpells.Keys.Select(GetPrefabValue).OrderBy(value => value));

        initialScope.Dispose();

        Assert.False(ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue(nameof(ConfigService.BloodKnightSpells), out _));
        Assert.False(ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue(nameof(ConfigService.ShadowBladeSpells), out _));

        using var refreshedScope = WithConfigOverrides(
            (nameof(ConfigService.BloodKnightSpells), "8101, , 8102"),
            (nameof(ConfigService.ShadowBladeSpells), "9101, , 9101, 9102"),
            (nameof(ConfigService.DemonHunterSpells), string.Empty),
            (nameof(ConfigService.VampireLordSpells), string.Empty),
            (nameof(ConfigService.ArcaneSorcererSpells), string.Empty),
            (nameof(ConfigService.DeathMageSpells), string.Empty));

        AbilityRunScriptsSystemPatch.ClearClassSpells();
        Configuration.GetClassSpellCooldowns();

        IReadOnlyDictionary<PrefabGUID, int> spells = AbilityRunScriptsSystemPatch.ClassSpells;

        int[] expected = { 8101, 8102, 9101, 9102 };
        Assert.Equal(expected, spells.Keys.Select(GetPrefabValue).OrderBy(value => value));
        Assert.Equal(0, spells[new PrefabGUID(8101)]);
        Assert.Equal(1, spells[new PrefabGUID(8102)]);
        Assert.Equal(0, spells[new PrefabGUID(9101)]);
        Assert.Equal(2, spells[new PrefabGUID(9102)]);
    }

    static int GetPrefabValue(PrefabGUID prefab)
    {
        object? value = PrefabGuidValueField.GetValue(prefab);
        return value is int intValue
            ? intValue
            : throw new InvalidOperationException("Unable to extract prefab GUID value.");
    }

    static string ResolveSpellList(string key)
    {
        if (ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue(key, out var value) && value is not null)
        {
            return Convert.ToString(value) ?? string.Empty;
        }

        var definition = ConfigService.ConfigInitialization.ConfigEntries.First(entry => entry.Key == key);
        return Convert.ToString(definition.DefaultValue) ?? string.Empty;
    }
}
