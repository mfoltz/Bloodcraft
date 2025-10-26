using System.Reflection;
using System.Reflection.Emit;
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Bloodcraft.Systems.Leveling;
using Bloodcraft.Utilities;
using HarmonyLib;
using Stunlock.Core;

namespace Bloodcraft.Tests.Utilities;

public sealed class ClassSpellCooldownConfigurationTests : TestHost
{
    static readonly FieldInfo ClassSpellsField = typeof(AbilityRunScriptsSystemPatch)
        .GetField("_classSpells", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("Failed to locate AbilityRunScriptsSystemPatch._classSpells");

    static readonly Dictionary<ClassManager.PlayerClass, string> ClassSpellOverrides = new();

    static ClassSpellCooldownConfigurationTests()
    {
        Harmony harmony = new("Bloodcraft.Tests.ClassSpellCooldownConfigurationTests");
        MethodInfo target = AccessTools.Method(typeof(Configuration), nameof(Configuration.GetClassSpellCooldowns))
            ?? throw new InvalidOperationException("Unable to locate Configuration.GetClassSpellCooldowns");
        MethodInfo transpiler = typeof(ClassSpellCooldownConfigurationTests)
            .GetMethod(nameof(ReplaceClassSpellsMapAccess), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Failed to locate transpiler method.");

        harmony.Patch(target, transpiler: new HarmonyMethod(transpiler));
    }

    static Dictionary<ClassManager.PlayerClass, string> GetClassSpellOverrides() => ClassSpellOverrides;

    static IEnumerable<CodeInstruction> ReplaceClassSpellsMapAccess(IEnumerable<CodeInstruction> instructions)
    {
        FieldInfo classSpellsField = typeof(Classes).GetField(nameof(Classes.ClassSpellsMap), BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Unable to locate Classes.ClassSpellsMap");
        MethodInfo replacement = typeof(ClassSpellCooldownConfigurationTests)
            .GetMethod(nameof(GetClassSpellOverrides), BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Unable to locate override accessor.");

        foreach (CodeInstruction instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldsfld && Equals(instruction.operand, classSpellsField))
            {
                yield return new CodeInstruction(OpCodes.Call, replacement);
            }
            else
            {
                yield return instruction;
            }
        }
    }

    static void RefreshClassSpellsMapFromConfig()
    {
        ClassSpellOverrides.Clear();
        ClassSpellOverrides[ClassManager.PlayerClass.BloodKnight] = ConfigService.BloodKnightSpells;
        ClassSpellOverrides[ClassManager.PlayerClass.DemonHunter] = ConfigService.DemonHunterSpells;
        ClassSpellOverrides[ClassManager.PlayerClass.VampireLord] = ConfigService.VampireLordSpells;
        ClassSpellOverrides[ClassManager.PlayerClass.ShadowBlade] = ConfigService.ShadowBladeSpells;
        ClassSpellOverrides[ClassManager.PlayerClass.ArcaneSorcerer] = ConfigService.ArcaneSorcererSpells;
        ClassSpellOverrides[ClassManager.PlayerClass.DeathMage] = ConfigService.DeathMageSpells;
    }

    static void ClearClassSpellCache()
    {
        if (ClassSpellsField.GetValue(null) is IDictionary<PrefabGUID, int> cache)
        {
            cache.Clear();
            return;
        }

        throw new InvalidOperationException("The class spell cache is not an IDictionary instance.");
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

        RefreshClassSpellsMapFromConfig();
        ClearClassSpellCache();

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

        RefreshClassSpellsMapFromConfig();
        ClearClassSpellCache();
        Configuration.GetClassSpellCooldowns();

        Assert.Equal(3, AbilityRunScriptsSystemPatch.ClassSpells.Count);
        Assert.Contains(new PrefabGUID(6001), AbilityRunScriptsSystemPatch.ClassSpells.Keys);
        Assert.Contains(new PrefabGUID(6002), AbilityRunScriptsSystemPatch.ClassSpells.Keys);
        Assert.Contains(new PrefabGUID(7001), AbilityRunScriptsSystemPatch.ClassSpells.Keys);

        initialScope.Dispose();

        using var refreshedScope = WithConfigOverrides(
            (nameof(ConfigService.BloodKnightSpells), "8101, , 8102"),
            (nameof(ConfigService.ShadowBladeSpells), "9101, , 9101, 9102"),
            (nameof(ConfigService.DemonHunterSpells), string.Empty),
            (nameof(ConfigService.VampireLordSpells), string.Empty),
            (nameof(ConfigService.ArcaneSorcererSpells), string.Empty),
            (nameof(ConfigService.DeathMageSpells), string.Empty));

        RefreshClassSpellsMapFromConfig();
        ClearClassSpellCache();
        Configuration.GetClassSpellCooldowns();

        IReadOnlyDictionary<PrefabGUID, int> spells = AbilityRunScriptsSystemPatch.ClassSpells;

        Assert.Equal(4, spells.Count);
        Assert.DoesNotContain(new PrefabGUID(6001), spells.Keys);
        Assert.DoesNotContain(new PrefabGUID(6002), spells.Keys);
        Assert.DoesNotContain(new PrefabGUID(7001), spells.Keys);
        Assert.Equal(0, spells[new PrefabGUID(8101)]);
        Assert.Equal(1, spells[new PrefabGUID(8102)]);
        Assert.Equal(0, spells[new PrefabGUID(9101)]);
        Assert.Equal(2, spells[new PrefabGUID(9102)]);
    }
}
